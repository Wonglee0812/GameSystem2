using UnityEngine;
using UnityEngine.Rendering;

namespace DamRush.WaterCompare
{
    /// <summary>
    /// Water surface for the GPU versions (C, S): a fixed grid with one point per simulation cell. The
    /// vertex shader reads the GPU State texture and lifts each point to the water surface,
    /// so the CPU never touches the water data.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SurfaceGpu : MonoBehaviour
    {
        public GpuWaterSim sim;
        Material mat;

        void Start()
        {
            int nx = sim.CellsX, nz = sim.CellsZ;
            var verts = new Vector3[nx * nz];
            var uv = new Vector2[nx * nz];
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    verts[j * nx + i] = new Vector3((i + 0.5f) * sim.cellSize, 0f, (j + 0.5f) * sim.cellSize);
                    uv[j * nx + i] = new Vector2(i, j);
                }
            var tris = new int[(nx - 1) * (nz - 1) * 6];
            int k = 0;
            for (int j = 0; j < nz - 1; j++)
                for (int i = 0; i < nx - 1; i++)
                {
                    int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
                    tris[k++] = a; tris[k++] = c; tris[k++] = b;
                    tris[k++] = b; tris[k++] = c; tris[k++] = d;
                }
            var mesh = new Mesh { name = "Water surface (GPU)", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = verts; mesh.uv = uv; mesh.triangles = tris;
            mesh.normals = new Vector3[verts.Length];
            mesh.bounds = new Bounds(new Vector3(CompareShape.LengthX * 0.5f, 2f, CompareShape.WidthZ * 0.5f), new Vector3(CompareShape.LengthX, 12f, CompareShape.WidthZ));
            GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = GetComponent<MeshRenderer>();
            mat = new Material(r.sharedMaterial) { name = "Water C" };
            mat.EnableKeyword("_USE_STATE");
            r.sharedMaterial = mat;
        }

        void LateUpdate()
        {
            if (mat == null || sim.State == null) return;
            mat.SetTexture("_State", sim.State);
            mat.SetTexture("_PrevState", sim.PrevState);
            mat.SetFloat("_Alpha", sim.Alpha);
            mat.SetFloat("_StepDt", sim.StepDt);
            mat.SetVector("_GridSize", new Vector4(sim.CellsX, sim.CellsZ, 0, 0));
            mat.SetFloat("_CellSize", sim.cellSize);
        }
    }
}
