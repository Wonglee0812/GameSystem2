using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Any ground made from a height grid (the valley, the water lab).
    /// The water grid reads the ground through this class, so it works on every map.
    /// Subclasses only describe the shape; the mesh, collider and queries are shared.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public abstract class GroundHeights : MonoBehaviour
    {
        public SandboxConfig config;

        [Tooltip("Mesh points per map cell along each side. 2 = a point every 0.5 m on the 1 m map: smoother banks.")]
        [Range(1, 4)] public int meshSubdivide = 2;

        protected float[,] vertexHeights; // (cellsX * meshSubdivide + 1) x (cellsZ * meshSubdivide + 1)
        int Sub => Mathf.Max(1, meshSubdivide);
        float Step => config.cellSize / Sub;

        public float LengthX => config.cellsX * config.cellSize;
        public float WidthZ => config.cellsZ * config.cellSize;

        /// <summary>Height of the ground at a grid corner (world x, z).</summary>
        protected abstract float HeightFormula(float x, float z);

        /// <summary>Vertex colour of the ground mesh.</summary>
        protected virtual Color ColorFormula(float x, float z, float h) => new Color(0.45f, 0.55f, 0.35f);

        /// <summary>Water surface height at the start, or negative infinity for a dry cell.</summary>
        public virtual float InitialWaterLevel(float x, float z) => float.NegativeInfinity;

        // a map randomizer may have made the ground already this frame
        protected virtual void Awake() { if (vertexHeights == null) Generate(); }

        public void Generate()
        {
            int nx = config.cellsX * Sub + 1, nz = config.cellsZ * Sub + 1;
            float s = Step;
            vertexHeights = new float[nx, nz];
            var verts = new Vector3[nx * nz];
            var colors = new Color[nx * nz];
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float x = i * s, z = j * s;
                    float h = HeightFormula(x, z);
                    vertexHeights[i, j] = h;
                    verts[i * nz + j] = new Vector3(x, h, z);
                    colors[i * nz + j] = QualitySettings.activeColorSpace == ColorSpace.Linear ? ColorFormula(x, z, h).linear : ColorFormula(x, z, h);
                }

            var tris = new int[(nx - 1) * (nz - 1) * 6];
            int k = 0;
            for (int i = 0; i < nx - 1; i++)
                for (int j = 0; j < nz - 1; j++)
                {
                    int a = i * nz + j, b = a + 1, c = a + nz, d = c + 1;
                    tris[k++] = a; tris[k++] = b; tris[k++] = c;
                    tris[k++] = b; tris[k++] = d; tris[k++] = c;
                }

            var mesh = new Mesh { name = name + " ground" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        /// <summary>
        /// Lowers every ground corner for which inside(x, z) is true (digging) and updates the
        /// mesh and the collider. The water grid must read its cells again (WaterGrid.RefreshGround).
        /// </summary>
        public void Lower(System.Func<float, float, bool> inside, float amount)
        {
            if (vertexHeights == null) Generate();
            int nx = config.cellsX * Sub + 1, nz = config.cellsZ * Sub + 1;
            float s = Step;
            var mesh = GetComponent<MeshFilter>().sharedMesh;
            var verts = mesh.vertices;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    if (!inside(i * s, j * s)) continue;
                    vertexHeights[i, j] -= amount;
                    verts[i * nz + j].y = vertexHeights[i, j];
                }
            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var col = GetComponent<MeshCollider>();
            col.sharedMesh = null;
            col.sharedMesh = mesh;
        }

        /// <summary>Ground height at a world position (bilinear between grid points).</summary>
        public float GroundAt(float x, float z)
        {
            if (vertexHeights == null) Generate();
            float s = Step;
            float fx = Mathf.Clamp(x / s, 0f, config.cellsX * Sub - 0.001f);
            float fz = Mathf.Clamp(z / s, 0f, config.cellsZ * Sub - 0.001f);
            int i = (int)fx, j = (int)fz;
            float tx = fx - i, tz = fz - j;
            float a = Mathf.Lerp(vertexHeights[i, j], vertexHeights[i + 1, j], tx);
            float b = Mathf.Lerp(vertexHeights[i, j + 1], vertexHeights[i + 1, j + 1], tx);
            return Mathf.Lerp(a, b, tz);
        }

        /// <summary>Ground height of water cell (i, j) = mean of its four corners.</summary>
        public float CellGround(int i, int j)
        {
            if (vertexHeights == null) Generate();
            int a = i * Sub, b = j * Sub, e = Sub;
            return 0.25f * (vertexHeights[a, b] + vertexHeights[a + e, b] + vertexHeights[a, b + e] + vertexHeights[a + e, b + e]);
        }
    }
}
