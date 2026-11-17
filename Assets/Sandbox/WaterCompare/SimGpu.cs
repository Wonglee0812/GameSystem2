using UnityEngine;

namespace DamRush.WaterCompare
{
    /// <summary>
    /// Version C: the pipe model in a compute shader on the GPU (PipeWater.compute), on 0.25 m
    /// cells (16x the cells of A). The water never comes back to the CPU for rendering: the
    /// surface shader reads the State texture directly. It needs compute shaders: a web build only
    /// gets them with WebGPU, not WebGL 2.
    /// </summary>
    public class SimGpu : GpuWaterSim
    {
        ComputeBuffer srcBuf, depthBuf, fluxBuf, velBuf;
        float[] depth;
        int kFlux, kDepth, kPack, kBrush, srcCount;

        protected override void Init()
        {
            int n = CellsX * CellsZ;
            solidCpu = new float[n]; depth = new float[n];
            if (!Supported) { Debug.LogWarning("Water compare C: no compute shader support"); return; }
            solidBuf = new ComputeBuffer(n, 4); srcBuf = new ComputeBuffer(n, 4); depthBuf = new ComputeBuffer(n, 4);
            fluxBuf = new ComputeBuffer(n, 16); velBuf = new ComputeBuffer(n, 8);
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
            kFlux = shader.FindKernel("KFlux"); kDepth = shader.FindKernel("KDepth"); kPack = shader.FindKernel("KPack"); kBrush = shader.FindKernel("KBrush");
            foreach (int k in new[] { kFlux, kDepth, kPack, kBrush })
            {
                shader.SetBuffer(k, "_Solid", solidBuf); shader.SetBuffer(k, "_Src", srcBuf); shader.SetBuffer(k, "_Depth", depthBuf);
                shader.SetBuffer(k, "_Flux", fluxBuf); shader.SetBuffer(k, "_Vel", velBuf);
            }
            shader.SetTexture(kPack, "_State", State);
            ApplyWall();
            ResetWater();
        }

        protected override void OnDestroy()
        {
            srcBuf?.Release(); depthBuf?.Release(); fluxBuf?.Release(); velBuf?.Release();
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
            if (depthBuf == null) return;
            for (int j = 0; j < CellsZ; j++)
                for (int i = 0; i < CellsX; i++)
                {
                    var c = CellCentre(i, j);
                    depth[j * CellsX + i] = CompareShape.StartDepth(c.x, c.y);
                }
            depthBuf.SetData(depth);
            fluxBuf.SetData(new Vector4[CellsX * CellsZ]);
            velBuf.SetData(new Vector2[CellsX * CellsZ]);
            Pack(); KeepPrevious();
        }

        protected override void Step(float dt)
        {
            if (depthBuf == null) return;
            float area = cellSize * cellSize;
            shader.SetInt("_NX", CellsX); shader.SetInt("_NZ", CellsZ);
            shader.SetFloat("_Dt", dt); shader.SetFloat("_K", dt * Gravity * cellSize);
            shader.SetFloat("_Damp", Mathf.Max(0f, 1f - friction * dt)); shader.SetFloat("_Area", area); shader.SetFloat("_Dx", cellSize);
            shader.SetFloat("_Add", srcCount > 0 ? sourceRate * FloodFactor * dt / (srcCount * area) : 0f);
            shader.Dispatch(kFlux, gx, gz, 1);
            shader.Dispatch(kDepth, gx, gz, 1);
            KeepPrevious(); // the state before this step, for smooth blending
            Pack();
        }

        public override void Push(Vector2 centre, float radius, Vector2 vel) => Brush(0, centre, radius, new Vector4(vel.x, vel.y, 0f, 0f));
        public override void Splash(Vector2 centre, float radius, float height) => Brush(1, centre, radius, new Vector4(0f, 0f, height, 0f));
        public override void AddLevel(float dh) { Brush(2, Vector2.zero, 1f, new Vector4(0f, 0f, dh, 0f)); KeepPrevious(); }

        void Brush(int mode, Vector2 centre, float radius, Vector4 v)
        {
            if (depthBuf == null) return;
            shader.SetInt("_NX", CellsX); shader.SetInt("_NZ", CellsZ); shader.SetFloat("_Dx", cellSize);
            shader.SetInt("_BrushMode", mode);
            shader.SetVector("_BrushPos", new Vector4(centre.x, centre.y, radius, 0f));
            shader.SetVector("_BrushVec", v);
            shader.Dispatch(kBrush, gx, gz, 1);
        }

        void Pack()
        {
            if (depthBuf == null) return;
            shader.SetInt("_NX", CellsX); shader.SetInt("_NZ", CellsZ);
            shader.Dispatch(kPack, gx, gz, 1);
        }
    }
}
