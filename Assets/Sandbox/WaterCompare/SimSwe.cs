using UnityEngine;

namespace DamRush.WaterCompare
{
    /// <summary>
    /// Version S ("Sandcastle-style"): shallow water equations with momentum on the GPU
    /// (SweWater.compute; Chentanez and Mueller, 2010). Sandcastle's own method is not public,
    /// so this is our best match: the velocity is carried with the flow, so pushed water keeps
    /// going, eddies form behind blocks and waves bounce off walls, which the pipe model (A, B, C)
    /// cannot do. Same 0.25 m cells as C, so the two compare fairly.
    /// </summary>
    public class SimSwe : GpuWaterSim
    {
        [Tooltip("Friction against the ground: bigger = shallow water slows down more.")]
        public float drag = 0.02f;
        ComputeBuffer srcBuf, hBuf, h2Buf, uBuf, wBuf, utBuf, wtBuf, sBuf;
        float[] depth;
        int kAdvU, kAdvW, kLimit, kScaleU, kScaleW, kHeight, kVelU, kVelW, kCopy, kPack, kBrush, srcCount;
        int gux, guz, gwx, gwz;

        protected override void Init()
        {
            int n = CellsX * CellsZ, nu = (CellsX + 1) * CellsZ, nw = CellsX * (CellsZ + 1);
            solidCpu = new float[n]; depth = new float[n];
            if (!Supported) { Debug.LogWarning("Water compare S: no compute shader support"); return; }
            solidBuf = new ComputeBuffer(n, 4); srcBuf = new ComputeBuffer(n, 4);
            hBuf = new ComputeBuffer(n, 4); h2Buf = new ComputeBuffer(n, 4); sBuf = new ComputeBuffer(n, 4);
            uBuf = new ComputeBuffer(nu, 4); utBuf = new ComputeBuffer(nu, 4);
            wBuf = new ComputeBuffer(nw, 4); wtBuf = new ComputeBuffer(nw, 4);
            var src = new float[n];
            srcCount = 0;
            for (int j = 0; j < CellsZ; j++)
                for (int i = 0; i < CellsX; i++)
                {
                    var c = CellCentre(i, j);
                    if (CompareShape.IsSource(c.x, c.y)) { src[j * CellsX + i] = 1f; srcCount++; }
                }
            srcBuf.SetData(src);
            CreateStateTextures();
            gux = Mathf.CeilToInt((CellsX + 1) / 8f); guz = Mathf.CeilToInt(CellsZ / 8f);
            gwx = Mathf.CeilToInt(CellsX / 8f); gwz = Mathf.CeilToInt((CellsZ + 1) / 8f);
            kAdvU = shader.FindKernel("KAdvU"); kAdvW = shader.FindKernel("KAdvW"); kLimit = shader.FindKernel("KLimit");
            kScaleU = shader.FindKernel("KScaleU"); kScaleW = shader.FindKernel("KScaleW"); kHeight = shader.FindKernel("KHeight");
            kVelU = shader.FindKernel("KVelU"); kVelW = shader.FindKernel("KVelW"); kCopy = shader.FindKernel("KCopy");
            kPack = shader.FindKernel("KPack"); kBrush = shader.FindKernel("KBrush");
            foreach (int k in new[] { kAdvU, kAdvW, kLimit, kScaleU, kScaleW, kHeight, kVelU, kVelW, kCopy, kPack, kBrush })
            {
                shader.SetBuffer(k, "_Solid", solidBuf); shader.SetBuffer(k, "_Src", srcBuf);
                shader.SetBuffer(k, "_H", hBuf); shader.SetBuffer(k, "_H2", h2Buf); shader.SetBuffer(k, "_S", sBuf);
                shader.SetBuffer(k, "_U", uBuf); shader.SetBuffer(k, "_W", wBuf); shader.SetBuffer(k, "_Ut", utBuf); shader.SetBuffer(k, "_Wt", wtBuf);
            }
            shader.SetTexture(kPack, "_State", State);
            ApplyWall();
            ResetWater();
        }

        protected override void OnDestroy()
        {
            srcBuf?.Release(); hBuf?.Release(); h2Buf?.Release(); sBuf?.Release();
            uBuf?.Release(); utBuf?.Release(); wBuf?.Release(); wtBuf?.Release();
            base.OnDestroy();
        }

        protected override void ApplyWall()
        {
            for (int j = 0; j < CellsZ; j++)
                for (int i = 0; i < CellsX; i++)
                    solidCpu[j * CellsX + i] = GroundAt(i, j) + WallAt(i, j);
            if (solidBuf != null) { solidBuf.SetData(solidCpu); AfterSolidChange(); }
        }

        protected override void AfterSolidChange() { Pack(); KeepPrevious(); }

        public override void ResetWater()
        {
            if (hBuf == null) return;
            for (int j = 0; j < CellsZ; j++)
                for (int i = 0; i < CellsX; i++)
                {
                    var c = CellCentre(i, j);
                    depth[j * CellsX + i] = CompareShape.StartDepth(c.x, c.y);
                }
            hBuf.SetData(depth); h2Buf.SetData(depth);
            uBuf.SetData(new float[uBuf.count]); utBuf.SetData(new float[utBuf.count]);
            wBuf.SetData(new float[wBuf.count]); wtBuf.SetData(new float[wtBuf.count]);
            Pack(); KeepPrevious();
        }

        void SetCommon()
        {
            shader.SetInt("_NX", CellsX); shader.SetInt("_NZ", CellsZ); shader.SetFloat("_Dx", cellSize);
        }

        protected override void Step(float dt)
        {
            if (hBuf == null) return;
            float area = cellSize * cellSize;
            SetCommon();
            shader.SetFloat("_Dt", dt); shader.SetFloat("_G", Gravity);
            shader.SetFloat("_Damp", Mathf.Max(0f, 1f - friction * dt)); shader.SetFloat("_Drag", drag);
            shader.SetFloat("_MaxVel", 0.45f * cellSize / dt); // keeps each step stable (CFL limit)
            shader.SetFloat("_Add", srcCount > 0 ? sourceRate * FloodFactor * dt / (srcCount * area) : 0f);
            shader.Dispatch(kAdvU, gux, guz, 1);
            shader.Dispatch(kAdvW, gwx, gwz, 1);
            shader.Dispatch(kLimit, gx, gz, 1);
            shader.Dispatch(kScaleU, gux, guz, 1);
            shader.Dispatch(kScaleW, gwx, gwz, 1);
            shader.Dispatch(kHeight, gx, gz, 1);
            shader.Dispatch(kVelU, gux, guz, 1);
            shader.Dispatch(kVelW, gwx, gwz, 1);
            shader.Dispatch(kCopy, gx, gz, 1);
            KeepPrevious();
            Pack();
        }

        public override void Push(Vector2 centre, float radius, Vector2 vel) => Brush(0, centre, radius, new Vector4(vel.x, vel.y, 0f, 0f));
        public override void Splash(Vector2 centre, float radius, float height) => Brush(1, centre, radius, new Vector4(0f, 0f, height, 0f));
        public override void AddLevel(float dh) { Brush(2, Vector2.zero, 1f, new Vector4(0f, 0f, dh, 0f)); Pack(); KeepPrevious(); }

        void Brush(int mode, Vector2 centre, float radius, Vector4 v)
        {
            if (hBuf == null) return;
            SetCommon();
            shader.SetInt("_BrushMode", mode);
            shader.SetVector("_BrushPos", new Vector4(centre.x, centre.y, radius, 0f));
            shader.SetVector("_BrushVec", v);
            shader.Dispatch(kBrush, gx, gz, 1);
        }

        void Pack()
        {
            if (hBuf == null) return;
            SetCommon();
            shader.Dispatch(kPack, gx, gz, 1);
        }
    }
}
