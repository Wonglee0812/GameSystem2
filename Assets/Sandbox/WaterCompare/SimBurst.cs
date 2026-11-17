using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DamRush.WaterCompare
{
    /// <summary>
    /// Version B: the same pipe model, compiled with Burst and split over all CPU cores with
    /// the job system, on 0.5 m cells (4x the cells of A). On a web build the jobs run on one
    /// thread and Burst is not used, so the gain there is much smaller.
    /// </summary>
    public class SimBurst : CpuReadableSim
    {
        NativeArray<float> nSolid, nDepth, nFL, nFR, nFB, nFT, nVX, nVZ, nSrc;
        int srcCount;

        protected override void Init()
        {
            int n = CellsX * CellsZ;
            solid = new float[n]; depth = new float[n]; prevDepth = new float[n]; velX = new float[n]; velZ = new float[n];
            var a = Allocator.Persistent;
            nSolid = new NativeArray<float>(n, a); nDepth = new NativeArray<float>(n, a);
            nFL = new NativeArray<float>(n, a); nFR = new NativeArray<float>(n, a); nFB = new NativeArray<float>(n, a); nFT = new NativeArray<float>(n, a);
            nVX = new NativeArray<float>(n, a); nVZ = new NativeArray<float>(n, a); nSrc = new NativeArray<float>(n, a);
            srcCount = 0;
            for (int j = 0; j < CellsZ; j++)
                for (int i = 0; i < CellsX; i++)
                {
                    var c = CellCentre(i, j);
                    if (CompareShape.IsSource(c.x, c.y)) { nSrc[Index(i, j)] = 1f; srcCount++; }
                }
            ApplyWall();
            ResetWater();
        }

        void OnDestroy()
        {
            if (!nDepth.IsCreated) return;
            nSolid.Dispose(); nDepth.Dispose(); nFL.Dispose(); nFR.Dispose(); nFB.Dispose(); nFT.Dispose();
            nVX.Dispose(); nVZ.Dispose(); nSrc.Dispose();
        }

        protected override void ApplyWall()
        {
            for (int j = 0; j < CellsZ; j++)
                for (int i = 0; i < CellsX; i++)
                    solid[Index(i, j)] = GroundAt(i, j) + WallAt(i, j);
            if (nSolid.IsCreated) nSolid.CopyFrom(solid);
        }

        public override void ResetWater()
        {
            for (int j = 0; j < CellsZ; j++)
                for (int i = 0; i < CellsX; i++)
                {
                    var c = CellCentre(i, j);
                    depth[Index(i, j)] = CompareShape.StartDepth(c.x, c.y);
                }
            nDepth.CopyFrom(depth);
            System.Array.Copy(depth, prevDepth, depth.Length);
            for (int id = 0; id < nFL.Length; id++) { nFL[id] = 0; nFR[id] = 0; nFB[id] = 0; nFT[id] = 0; nVX[id] = 0; nVZ[id] = 0; }
        }

        protected override void SolidChanged() { if (nSolid.IsCreated) nSolid.CopyFrom(solid); }

        public override void AddLevel(float dh)
        {
            for (int id = 0; id < nDepth.Length; id++) if (nDepth[id] > 0.01f) nDepth[id] = math.max(0f, nDepth[id] + dh);
            nDepth.CopyTo(depth); nDepth.CopyTo(prevDepth);
        }

        public override void Push(Vector2 centre, float radius, Vector2 vel) => SimCpu.PushArrays(this, centre, radius, vel, new NativeList(nDepth), new NativeList(nFL), new NativeList(nFR), new NativeList(nFB), new NativeList(nFT));
        public override void Splash(Vector2 centre, float radius, float height) => SimCpu.SplashArray(this, centre, radius, height, new NativeList(nDepth));

        /// <summary>Lets the shared brush code write into a NativeArray (main thread, jobs are complete).</summary>
        class NativeList : System.Collections.Generic.IList<float>
        {
            NativeArray<float> a;
            public NativeList(NativeArray<float> arr) { a = arr; }
            public float this[int i] { get => a[i]; set => a[i] = value; }
            public int Count => a.Length;
            public bool IsReadOnly => false;
            public int IndexOf(float item) => throw new System.NotSupportedException();
            public void Insert(int index, float item) => throw new System.NotSupportedException();
            public void RemoveAt(int index) => throw new System.NotSupportedException();
            public void Add(float item) => throw new System.NotSupportedException();
            public void Clear() => throw new System.NotSupportedException();
            public bool Contains(float item) => throw new System.NotSupportedException();
            public void CopyTo(float[] array, int arrayIndex) => a.CopyTo(array);
            public bool Remove(float item) => throw new System.NotSupportedException();
            public System.Collections.Generic.IEnumerator<float> GetEnumerator() => a.GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => a.GetEnumerator();
        }

        bool stepped;

        protected override void Step(float dt)
        {
            if (!stepped) { nDepth.CopyTo(prevDepth); stepped = true; } // depth before this frame's last steps
            float area = cellSize * cellSize;
            var flux = new FluxJob
            {
                nx = CellsX, nz = CellsZ, dt = dt, k = dt * Gravity * cellSize, damp = math.max(0f, 1f - friction * dt), area = area,
                add = srcCount > 0 ? sourceRate * FloodFactor * dt / (srcCount * area) : 0f,
                solid = nSolid, depth = nDepth, src = nSrc, fL = nFL, fR = nFR, fB = nFB, fT = nFT,
            };
            var h = flux.Schedule(CellsX * CellsZ, 256);
            var dep = new DepthJob
            {
                nx = CellsX, nz = CellsZ, dt = dt, dx = cellSize, area = area,
                depth = nDepth, fL = nFL, fR = nFR, fB = nFB, fT = nFT, velX = nVX, velZ = nVZ,
                add = flux.add, src = nSrc,
            };
            dep.Schedule(CellsX * CellsZ, 256, h).Complete();
        }

        protected override void AfterSteps()
        {
            if (!stepped) return;
            stepped = false;
            nDepth.CopyTo(depth); nVX.CopyTo(velX); nVZ.CopyTo(velZ);
        }

        [BurstCompile]
        struct FluxJob : IJobParallelFor
        {
            public int nx, nz;
            public float dt, k, damp, area, add;
            [ReadOnly] public NativeArray<float> solid, src;
            // the inflow is counted here and added for real in the depth job
            [ReadOnly] public NativeArray<float> depth;
            public NativeArray<float> fL, fR, fB, fT;

            public void Execute(int id)
            {
                int i = id % nx, j = id / nx;
                float d = depth[id] + src[id] * add;
                float h = solid[id] + d;
                // neighbour heights include their inflow too, so all cells see the same state
                float l = i > 0 ? math.max(0f, fL[id] * damp + k * (h - solid[id - 1] - depth[id - 1] - src[id - 1] * add)) : 0f;
                float r = i < nx - 1 ? math.max(0f, fR[id] * damp + k * (h - solid[id + 1] - depth[id + 1] - src[id + 1] * add)) : math.max(0f, fR[id] * damp + k * (h - (solid[id] - 0.5f)));
                float b = j > 0 ? math.max(0f, fB[id] * damp + k * (h - solid[id - nx] - depth[id - nx] - src[id - nx] * add)) : 0f;
                float t = j < nz - 1 ? math.max(0f, fT[id] * damp + k * (h - solid[id + nx] - depth[id + nx] - src[id + nx] * add)) : 0f;
                float sum = l + r + b + t;
                if (sum > 0f)
                {
                    float s = math.min(1f, d * area / (sum * dt));
                    l *= s; r *= s; b *= s; t *= s;
                }
                fL[id] = l; fR[id] = r; fB[id] = b; fT[id] = t;
            }
        }

        [BurstCompile]
        struct DepthJob : IJobParallelFor
        {
            public int nx, nz;
            public float dt, dx, area;
            public NativeArray<float> depth, velX, velZ;
            [ReadOnly] public NativeArray<float> fL, fR, fB, fT;
            public float add;
            [ReadOnly] public NativeArray<float> src;

            public void Execute(int id)
            {
                int i = id % nx, j = id / nx;
                float inL = i > 0 ? fR[id - 1] : 0f, inR = i < nx - 1 ? fL[id + 1] : 0f;
                float inB = j > 0 ? fT[id - nx] : 0f, inT = j < nz - 1 ? fB[id + nx] : 0f;
                float before = depth[id] + src[id] * add; // inflow of this step
                float now = math.max(0f, before + dt * (inL + inR + inB + inT - fL[id] - fR[id] - fB[id] - fT[id]) / area);
                depth[id] = now;
                float avg = math.max(0.02f, 0.5f * (before + now));
                velX[id] = 0.5f * (inL - fL[id] + fR[id] - inR) / (dx * avg);
                velZ[id] = 0.5f * (inB - fB[id] + fT[id] - inT) / (dx * avg);
            }
        }
    }
}
