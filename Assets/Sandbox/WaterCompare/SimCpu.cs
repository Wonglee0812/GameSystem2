using UnityEngine;

namespace DamRush.WaterCompare
{
    /// <summary>
    /// A water simulation whose results live in normal C# arrays, so the CPU surface renderer
    /// can read them: ground (+ wall), depth now and one step before, and the flow velocity.
    /// </summary>
    public abstract class CpuReadableSim : WaterSimBase
    {
        [System.NonSerialized] public float[] solid, depth, prevDepth, velX, velZ;
        public int Index(int i, int j) => j * CellsX + i;

        protected override void RaiseSolid(Rect area, float top) { ForCellsIn(area, (i, j) => { int id = Index(i, j); solid[id] = Mathf.Max(solid[id], top); }); SolidChanged(); }
        protected virtual void SolidChanged() { }

        public override bool Sample(Vector2 p, out float ground, out float dep, out Vector2 vel)
        {
            CellOf(p, out int i, out int j);
            int id = Index(i, j);
            ground = solid[id]; dep = depth[id]; vel = new Vector2(velX[id], velZ[id]);
            return true;
        }
    }

    /// <summary>
    /// Version A: the pipe model in plain C# (one thread), 1 m cells, like the game now.
    /// The look is improved by the new renderer: blending between steps, a finer surface mesh,
    /// soft shores, depth colour and foam.
    /// </summary>
    public class SimCpu : CpuReadableSim
    {
        float[] fL, fR, fB, fT, src;
        int srcCount;

        protected override void Init()
        {
            int n = CellsX * CellsZ;
            solid = new float[n]; depth = new float[n]; prevDepth = new float[n]; velX = new float[n]; velZ = new float[n];
            fL = new float[n]; fR = new float[n]; fB = new float[n]; fT = new float[n]; src = new float[n];
            srcCount = 0;
            for (int j = 0; j < CellsZ; j++)
                for (int i = 0; i < CellsX; i++)
                {
                    var c = CellCentre(i, j);
                    if (CompareShape.IsSource(c.x, c.y)) { src[Index(i, j)] = 1f; srcCount++; }
                }
            ApplyWall();
            ResetWater();
        }

        protected override void ApplyWall()
        {
            for (int j = 0; j < CellsZ; j++)
                for (int i = 0; i < CellsX; i++)
                    solid[Index(i, j)] = GroundAt(i, j) + WallAt(i, j);
        }

        public override void ResetWater()
        {
            for (int j = 0; j < CellsZ; j++)
                for (int i = 0; i < CellsX; i++)
                {
                    int id = Index(i, j);
                    var c = CellCentre(i, j);
                    depth[id] = prevDepth[id] = CompareShape.StartDepth(c.x, c.y);
                    fL[id] = fR[id] = fB[id] = fT[id] = velX[id] = velZ[id] = 0f;
                }
        }

        public override void AddLevel(float dh) { for (int id = 0; id < depth.Length; id++) if (depth[id] > 0.01f) { depth[id] = Mathf.Max(0f, depth[id] + dh); prevDepth[id] = depth[id]; } }

        public override void Push(Vector2 centre, float radius, Vector2 vel) => PushArrays(this, centre, radius, vel, depth, fL, fR, fB, fT);
        public override void Splash(Vector2 centre, float radius, float height) => SplashArray(this, centre, radius, height, depth);

        /// <summary>Shared by A and B: adds flow in the direction of vel to the pipes of the cells in the circle.</summary>
        public static void PushArrays(WaterSimBase s, Vector2 centre, float radius, Vector2 vel, System.Collections.Generic.IList<float> dep,
            System.Collections.Generic.IList<float> l, System.Collections.Generic.IList<float> r, System.Collections.Generic.IList<float> b, System.Collections.Generic.IList<float> t)
        {
            ForCells(s, centre, radius, (id, w) =>
            {
                // pull the pipe flows towards the flow the push asks for (flow = speed x depth x width);
                // pulling (not adding) keeps it bounded, so holding the push does not pump water
                float q = dep[id] * s.cellSize, k = w * 0.5f;
                float vx = vel.x * q, vz = vel.y * q;
                if (vx > 0) { r[id] += (vx - r[id]) * k; l[id] *= 1f - k; } else { l[id] += (-vx - l[id]) * k; r[id] *= 1f - k; }
                if (vz > 0) { t[id] += (vz - t[id]) * k; b[id] *= 1f - k; } else { b[id] += (-vz - b[id]) * k; t[id] *= 1f - k; }
            });
        }

        public static void SplashArray(WaterSimBase s, Vector2 centre, float radius, float height, System.Collections.Generic.IList<float> dep)
        {
            ForCells(s, centre, radius, (id, w) => dep[id] += height * w * w);
        }

        static void ForCells(WaterSimBase s, Vector2 centre, float radius, System.Action<int, float> act)
        {
            int i0 = Mathf.Max(0, Mathf.FloorToInt((centre.x - radius) / s.cellSize)), i1 = Mathf.Min(s.CellsX - 1, Mathf.CeilToInt((centre.x + radius) / s.cellSize));
            int j0 = Mathf.Max(0, Mathf.FloorToInt((centre.y - radius) / s.cellSize)), j1 = Mathf.Min(s.CellsZ - 1, Mathf.CeilToInt((centre.y + radius) / s.cellSize));
            for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                {
                    float w = BrushWeightPublic(Vector2.Distance(s.CellCentre(i, j), centre), radius);
                    if (w > 0f) act(j * s.CellsX + i, w);
                }
        }

        static float BrushWeightPublic(float d, float r) { float t = d / r; return t >= 1f ? 0f : 1f - t * t; }

        protected override void Step(float dt)
        {
            int nx = CellsX, nz = CellsZ;
            float dx = cellSize, area = dx * dx;
            float k = dt * Gravity * dx;            // pipe: flow += dt * g * (cross-section / length) * height difference
            float damp = Mathf.Max(0f, 1f - friction * dt);
            System.Array.Copy(depth, prevDepth, depth.Length);

            // 1. inflow
            if (srcCount > 0)
            {
                float add = sourceRate * FloodFactor * dt / (srcCount * area);
                for (int id = 0; id < depth.Length; id++) if (src[id] > 0f) depth[id] += add;
            }

            // 2. flow in the four pipes of every cell
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    int id = j * nx + i;
                    float h = solid[id] + depth[id];
                    fL[id] = i > 0 ? Mathf.Max(0f, fL[id] * damp + k * (h - solid[id - 1] - depth[id - 1])) : 0f;
                    // the lower end is open: water leaves the map there
                    fR[id] = i < nx - 1 ? Mathf.Max(0f, fR[id] * damp + k * (h - solid[id + 1] - depth[id + 1])) : Mathf.Max(0f, fR[id] * damp + k * (h - (solid[id] - 0.5f)));
                    fB[id] = j > 0 ? Mathf.Max(0f, fB[id] * damp + k * (h - solid[id - nx] - depth[id - nx])) : 0f;
                    fT[id] = j < nz - 1 ? Mathf.Max(0f, fT[id] * damp + k * (h - solid[id + nx] - depth[id + nx])) : 0f;
                    float sum = fL[id] + fR[id] + fB[id] + fT[id];
                    if (sum > 0f)
                    {
                        float s = Mathf.Min(1f, depth[id] * area / (sum * dt)); // never more than the cell holds
                        fL[id] *= s; fR[id] *= s; fB[id] *= s; fT[id] *= s;
                    }
                }

            // 3. new depth and velocity
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    int id = j * nx + i;
                    float inL = i > 0 ? fR[id - 1] : 0f, inR = i < nx - 1 ? fL[id + 1] : 0f;
                    float inB = j > 0 ? fT[id - nx] : 0f, inT = j < nz - 1 ? fB[id + nx] : 0f;
                    float before = depth[id];
                    depth[id] = Mathf.Max(0f, depth[id] + dt * (inL + inR + inB + inT - fL[id] - fR[id] - fB[id] - fT[id]) / area);
                    float avg = Mathf.Max(0.02f, 0.5f * (before + depth[id]));
                    velX[id] = 0.5f * (inL - fL[id] + fR[id] - inR) / (dx * avg);
                    velZ[id] = 0.5f * (inB - fB[id] + fT[id] - inT) / (dx * avg);
                }
        }
    }
}
