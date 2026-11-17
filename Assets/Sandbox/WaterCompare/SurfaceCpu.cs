using UnityEngine;
using UnityEngine.Rendering;

namespace DamRush.WaterCompare
{
    /// <summary>
    /// Water surface for versions A and B: a mesh rebuilt on the CPU every frame, on its own
    /// grid (renderCell, finer than A's 1 m cells). Heights are sampled bilinearly from the
    /// simulation and blended between the last two steps, so the surface moves smoothly.
    /// Dry points sink just under the ground, so the shore follows the water line.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SurfaceCpu : MonoBehaviour
    {
        public CpuReadableSim sim;
        public float renderCell = 0.5f;

        Mesh mesh;
        int vx, vz;
        Vector3[] verts, normals;
        Vector2[] uvDepth, uvVel;
        float[] heights, groundAt; // ground under each point, computed once

        void Start()
        {
            vx = Mathf.RoundToInt(CompareShape.LengthX / renderCell) + 1;
            vz = Mathf.RoundToInt(CompareShape.WidthZ / renderCell) + 1;
            int n = vx * vz;
            verts = new Vector3[n]; normals = new Vector3[n]; uvDepth = new Vector2[n]; uvVel = new Vector2[n]; heights = new float[n]; groundAt = new float[n];
            var uv0 = new Vector2[n];
            for (int j = 0; j < vz; j++)
                for (int i = 0; i < vx; i++)
                {
                    int id = j * vx + i;
                    verts[id] = new Vector3(i * renderCell, 0f, j * renderCell);
                    groundAt[id] = CompareShape.Ground(i * renderCell, j * renderCell);
                    uv0[id] = new Vector2(i, j);
                }
            var tris = new int[(vx - 1) * (vz - 1) * 6];
            int k = 0;
            for (int j = 0; j < vz - 1; j++)
                for (int i = 0; i < vx - 1; i++)
                {
                    int a = j * vx + i, b = a + 1, c = a + vx, d = c + 1;
                    tris[k++] = a; tris[k++] = c; tris[k++] = b;
                    tris[k++] = b; tris[k++] = c; tris[k++] = d;
                }
            mesh = new Mesh { name = "Water surface (CPU)", indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic();
            mesh.vertices = verts; mesh.uv = uv0; mesh.triangles = tris;
            mesh.bounds = new Bounds(new Vector3(CompareShape.LengthX * 0.5f, 2f, CompareShape.WidthZ * 0.5f), new Vector3(CompareShape.LengthX, 12f, CompareShape.WidthZ));
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        // bilinear weights from each surface point to the 4 nearest simulation cells, computed once
        int[] idx; float[] wgt;
        /// <summary>Real milliseconds spent rebuilding the surface mesh in the last frame.</summary>
        public float BuildMs { get; private set; }
        readonly System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

        void BuildWeights()
        {
            int n = vx * vz;
            idx = new int[n * 4]; wgt = new float[n * 4];
            for (int j = 0; j < vz; j++)
                for (int i = 0; i < vx; i++)
                {
                    int id = j * vx + i;
                    float fx = i * renderCell / sim.cellSize - 0.5f, fz = j * renderCell / sim.cellSize - 0.5f;
                    int i0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, sim.CellsX - 1), j0 = Mathf.Clamp(Mathf.FloorToInt(fz), 0, sim.CellsZ - 1);
                    int i1 = Mathf.Min(i0 + 1, sim.CellsX - 1), j1 = Mathf.Min(j0 + 1, sim.CellsZ - 1);
                    float tx = Mathf.Clamp01(fx - i0), tz = Mathf.Clamp01(fz - j0);
                    int k = id * 4;
                    idx[k] = sim.Index(i0, j0); idx[k + 1] = sim.Index(i1, j0); idx[k + 2] = sim.Index(i0, j1); idx[k + 3] = sim.Index(i1, j1);
                    wgt[k] = (1 - tx) * (1 - tz); wgt[k + 1] = tx * (1 - tz); wgt[k + 2] = (1 - tx) * tz; wgt[k + 3] = tx * tz;
                }
        }

        void LateUpdate()
        {
            if (sim == null || sim.depth == null || mesh == null) return;
            watch.Restart();
            if (idx == null) BuildWeights();
            float al = sim.Alpha, bl = 1f - al;
            float[] dep = sim.depth, pre = sim.prevDepth, sol = sim.solid, vxs = sim.velX, vzs = sim.velZ;
            int n = vx * vz;
            for (int id = 0; id < n; id++)
            {
                int k = id * 4;
                int a = idx[k], b = idx[k + 1], c = idx[k + 2], e = idx[k + 3];
                float wa = wgt[k], wb = wgt[k + 1], wc = wgt[k + 2], we = wgt[k + 3];
                float d = (pre[a] * wa + pre[b] * wb + pre[c] * wc + pre[e] * we) * bl + (dep[a] * wa + dep[b] * wb + dep[c] * wc + dep[e] * we) * al;
                float surf = sol[a] * wa + sol[b] * wb + sol[c] * wc + sol[e] * we + d;
                float ground = groundAt[id];
                // wet only where the water surface is above the real ground: a clean shore line
                bool wet = d > 0.003f && surf > ground + 0.003f;
                float h = wet ? surf : ground - 0.05f;
                heights[id] = h;
                verts[id].y = h;
                uvDepth[id].x = wet ? surf - ground : 0f;
                uvVel[id].x = vxs[a] * wa + vxs[b] * wb + vxs[c] * wc + vxs[e] * we;
                uvVel[id].y = vzs[a] * wa + vzs[b] * wb + vzs[c] * wc + vzs[e] * we;
            }
            // normals from the heights (central differences)
            float two = 2f * renderCell;
            for (int j = 0; j < vz; j++)
            {
                int jb = (j > 0 ? j - 1 : 0) * vx, jt = (j < vz - 1 ? j + 1 : j) * vx, row = j * vx;
                for (int i = 0; i < vx; i++)
                {
                    float nxv = heights[row + (i > 0 ? i - 1 : 0)] - heights[row + (i < vx - 1 ? i + 1 : i)];
                    float nzv = heights[jb + i] - heights[jt + i];
                    float inv = 1f / Mathf.Sqrt(nxv * nxv + two * two + nzv * nzv);
                    normals[row + i] = new Vector3(nxv * inv, two * inv, nzv * inv);
                }
            }
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.SetUVs(1, uvDepth);
            mesh.SetUVs(2, uvVel);
            watch.Stop();
            BuildMs = (float)watch.Elapsed.TotalMilliseconds;
        }
    }
}
