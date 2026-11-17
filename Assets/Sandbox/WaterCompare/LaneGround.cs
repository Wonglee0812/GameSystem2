using UnityEngine;
using UnityEngine.Rendering;

namespace DamRush.WaterCompare
{
    /// <summary>Ground mesh of one lane (same formula as the simulations) and the dam wall pieces.</summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class LaneGround : MonoBehaviour
    {
        public float cell = 0.5f;
        public Material wallMaterial;
        GameObject breachPiece;

        void Awake()
        {
            int vx = Mathf.RoundToInt(CompareShape.LengthX / cell) + 1, vz = Mathf.RoundToInt(CompareShape.WidthZ / cell) + 1;
            var verts = new Vector3[vx * vz];
            var cols = new Color[vx * vz];
            var sand = new Color(0.82f, 0.74f, 0.55f); var grass = new Color(0.42f, 0.6f, 0.3f); var dark = new Color(0.3f, 0.45f, 0.24f);
            for (int j = 0; j < vz; j++)
                for (int i = 0; i < vx; i++)
                {
                    float x = i * cell, z = j * cell, h = CompareShape.Ground(x, z);
                    verts[j * vx + i] = new Vector3(x, h, z);
                    float d = Mathf.Abs(z - CompareShape.ChannelZ(x)) - (2.5f + 6f * CompareShape.Pond(x));
                    var c = Color.Lerp(sand, grass, Mathf.SmoothStep(0f, 1f, (d - 1f) / 3f));
                    c = Color.Lerp(c, dark, Mathf.PerlinNoise(x * 0.2f, z * 0.2f) * 0.35f);
                    cols[j * vx + i] = QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;
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
            var mesh = new Mesh { name = "Lane ground", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = verts; mesh.colors = cols; mesh.triangles = tris;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;

            // the dam wall: three blocks across the valley, the middle one is the part that breaks
            float zc = CompareShape.ChannelZ(CompareShape.WallX), top = CompareShape.WallTop, bw = CompareShape.BreachHalfWidth;
            WallBlock(4f, zc - bw, top);
            WallBlock(zc + bw, CompareShape.WidthZ - 4f, top);
            breachPiece = WallBlock(zc - bw, zc + bw, top);
        }

        GameObject WallBlock(float z0, float z1, float top)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Dam wall";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            float bottom = top - 4f;
            go.transform.localPosition = new Vector3(CompareShape.WallX, (top + bottom) * 0.5f, (z0 + z1) * 0.5f);
            go.transform.localScale = new Vector3(CompareShape.WallHalfThickness * 2f, top - bottom, z1 - z0);
            if (wallMaterial != null) go.GetComponent<Renderer>().sharedMaterial = wallMaterial;
            return go;
        }

        public void SetBreach(bool open) { if (breachPiece != null) breachPiece.SetActive(!open); }
    }
}
