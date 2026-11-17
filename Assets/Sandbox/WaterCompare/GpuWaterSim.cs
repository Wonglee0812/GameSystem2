using UnityEngine;
using UnityEngine.Rendering;

namespace DamRush.WaterCompare
{
    /// <summary>
    /// Shared part of the GPU water versions (C and S). The simulation writes its result into the
    /// State texture after every step (x = ground, y = depth, z/w = velocity) and keeps the step
    /// before it in PrevState. The surface shader blends the two with Alpha, so the water moves
    /// smoothly even when a frame gets zero or two steps (without this, C stuttered).
    /// A copy of State comes back to the CPU a frame later (async readback), so floating objects
    /// like the boat can read the water without stalling the GPU.
    /// </summary>
    public abstract class GpuWaterSim : WaterSimBase
    {
        public ComputeShader shader;
        public RenderTexture State { get; private set; }
        public RenderTexture PrevState { get; private set; }
        public bool Supported => SystemInfo.supportsComputeShaders && shader != null;

        protected float[] solidCpu;
        protected ComputeBuffer solidBuf;
        protected int gx, gz;
        Vector4[] readback;
        bool readPending, hasReadback;

        protected void CreateStateTextures()
        {
            State = NewState("Water state");
            PrevState = NewState("Water state (step before)");
            gx = Mathf.CeilToInt(CellsX / 8f); gz = Mathf.CeilToInt(CellsZ / 8f);
            readback = new Vector4[CellsX * CellsZ];
        }

        RenderTexture NewState(string n)
        {
            var t = new RenderTexture(CellsX, CellsZ, 0, RenderTextureFormat.ARGBFloat) { enableRandomWrite = true, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = n };
            t.Create();
            return t;
        }

        /// <summary>Keeps the current state as "the step before" (call before writing a new state).</summary>
        protected void KeepPrevious() { if (State != null) Graphics.CopyTexture(State, PrevState); }

        protected override void AfterSteps()
        {
            if (State == null || readPending || !SystemInfo.supportsAsyncGPUReadback) return;
            readPending = true;
            AsyncGPUReadback.Request(State, 0, r =>
            {
                readPending = false;
                if (r.hasError || readback == null) return;
                var data = r.GetData<Vector4>();
                if (data.Length == readback.Length) { data.CopyTo(readback); hasReadback = true; }
            });
        }

        public override bool Sample(Vector2 p, out float ground, out float depth, out Vector2 vel)
        {
            ground = 0f; depth = 0f; vel = Vector2.zero;
            if (!hasReadback) return false;
            CellOf(p, out int i, out int j);
            var s = readback[j * CellsX + i];
            ground = s.x; depth = s.y; vel = new Vector2(s.z, s.w);
            return true;
        }

        protected override void RaiseSolid(Rect area, float top)
        {
            ForCellsIn(area, (i, j) => { int id = j * CellsX + i; solidCpu[id] = Mathf.Max(solidCpu[id], top); });
            solidBuf?.SetData(solidCpu);
            AfterSolidChange();
        }

        /// <summary>Called after the ground changed (blocks, dam wall), to refresh the State texture.</summary>
        protected virtual void AfterSolidChange() { }

        protected virtual void OnDestroy()
        {
            solidBuf?.Release();
            if (State != null) State.Release();
            if (PrevState != null) PrevState.Release();
        }
    }
}
