using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>One hex tile of the map. Later it holds what is built on it (dam, lodge...).</summary>
    public class HexCell
    {
        public Vector2Int axial;   // (q, r) axial coordinates
        public Vector2Int offset;  // (col, row) in the odd-r layout
        public Vector3 center;     // world centre on the ground
        public float minGround, maxGround, meanGround;
        public bool startsWet;     // water in this tile at the start
        public bool blocked;       // a building stands here (beavers only enter it as a goal)
        public string building;    // name of what stands here, or null
    }

    /// <summary>
    /// Hex grid laid over the whole map: pointy-top hexes, every second row shifted half a
    /// tile ("odd-r" offset layout). The coordinate maths (axial coordinates, cube rounding)
    /// follows Patel (2013). The grid is only data plus an outline; the water and the
    /// physics still run on their own grids.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HexGrid : MonoBehaviour
    {
        public GroundHeights ground;
        public WaterGrid water;
        [Tooltip("Distance from the centre to a corner (m).")]
        public float size = 2f;
        public float lineLift = 0.08f;

        public int Cols { get; private set; }
        public int Rows { get; private set; }
        readonly Dictionary<Vector2Int, HexCell> cells = new Dictionary<Vector2Int, HexCell>();
        public IEnumerable<HexCell> Cells => cells.Values;
        public int Count => cells.Count;

        float W => Mathf.Sqrt(3f) * size; // width of a pointy-top hex
        float H => 1.5f * size;           // distance between rows

        void Awake() { Build(); }

        public void Build()
        {
            cells.Clear();
            Cols = Mathf.FloorToInt((ground.LengthX - W * 0.5f) / W);
            Rows = Mathf.FloorToInt((ground.WidthZ - size * 0.5f) / H);
            for (int row = 0; row < Rows; row++)
                for (int col = 0; col < Cols; col++)
                {
                    var axial = OffsetToAxial(col, row);
                    Vector3 c = AxialToWorld(axial.x, axial.y);
                    var cell = new HexCell { axial = axial, offset = new Vector2Int(col, row) };
                    Measure(cell, c);
                    float level = ground.InitialWaterLevel(c.x, c.z);
                    cell.startsWet = !float.IsNegativeInfinity(level) && level > cell.center.y;
                    cells[axial] = cell;
                }
            BuildOutlineMesh();
        }

        /// <summary>Lowest, highest and mean ground of a hex (7 sample points) and its centre.</summary>
        void Measure(HexCell cell, Vector3 c)
        {
            float mn = float.MaxValue, mx = float.MinValue, sum = 0f;
            for (int k = 0; k < 7; k++)
            {
                Vector3 p = k < 6 ? Vector3.Lerp(c, Corner(c, k), 0.8f) : c;
                float h = ground.GroundAt(p.x, p.z);
                mn = Mathf.Min(mn, h); mx = Mathf.Max(mx, h); sum += h;
            }
            cell.minGround = mn; cell.maxGround = mx; cell.meanGround = sum / 7f;
            cell.center = new Vector3(c.x, ground.GroundAt(c.x, c.z), c.z);
        }

        /// <summary>Measures a hex again after the ground changed (digging).</summary>
        public void Remeasure(HexCell cell) => Measure(cell, AxialToWorld(cell.axial.x, cell.axial.y));

        // ------------------------------------------------------------------ coordinates

        /// <summary>odd-r offset (col, row) -> axial (q, r).</summary>
        public static Vector2Int OffsetToAxial(int col, int row) => new Vector2Int(col - (row - (row & 1)) / 2, row);

        /// <summary>Centre of an axial hex on the x/z plane (y = 0). The first hex sits half a tile inside the map.</summary>
        public Vector3 AxialToWorld(int q, int r)
        {
            float x = W * (q + r * 0.5f) + W * 0.5f;
            float z = H * r + size;
            return new Vector3(x, 0f, z);
        }

        /// <summary>World position -> axial hex (with cube rounding).</summary>
        public Vector2Int WorldToAxial(Vector3 p)
        {
            float px = p.x - W * 0.5f, pz = p.z - size;
            float q = (Mathf.Sqrt(3f) / 3f * px - 1f / 3f * pz) / size;
            float r = (2f / 3f * pz) / size;
            return CubeRound(q, r);
        }

        static Vector2Int CubeRound(float q, float r)
        {
            float s = -q - r;
            int rq = Mathf.RoundToInt(q), rr = Mathf.RoundToInt(r), rs = Mathf.RoundToInt(s);
            float dq = Mathf.Abs(rq - q), dr = Mathf.Abs(rr - r), ds = Mathf.Abs(rs - s);
            if (dq > dr && dq > ds) rq = -rr - rs;
            else if (dr > ds) rr = -rq - rs;
            return new Vector2Int(rq, rr);
        }

        /// <summary>Corner k (0-5) of a pointy-top hex.</summary>
        public Vector3 Corner(Vector3 centre, int k)
        {
            float a = Mathf.Deg2Rad * (60f * k - 30f);
            return centre + new Vector3(size * Mathf.Cos(a), 0f, size * Mathf.Sin(a));
        }

        public bool TryGet(Vector3 world, out HexCell cell) => cells.TryGetValue(WorldToAxial(world), out cell);
        public bool TryGet(Vector2Int axial, out HexCell cell) => cells.TryGetValue(axial, out cell);

        static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
            new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1),
        };

        public IEnumerable<HexCell> Neighbours(HexCell c)
        {
            foreach (var d in Dirs)
                if (cells.TryGetValue(c.axial + d, out var n)) yield return n;
        }

        // ------------------------------------------------------------------ drawing

        /// <summary>Outline of every hex, laid on the ground (each edge bent over the terrain).</summary>
        void BuildOutlineMesh()
        {
            var verts = new List<Vector3>();
            var idx = new List<int>();
            const int seg = 3;
            foreach (var c in cells.Values)
            {
                Vector3 centre = new Vector3(c.center.x, 0f, c.center.z);
                for (int k = 0; k < 6; k++)
                {
                    Vector3 a = Corner(centre, k), b = Corner(centre, (k + 1) % 6);
                    for (int s = 0; s < seg; s++)
                    {
                        Vector3 p0 = Vector3.Lerp(a, b, (float)s / seg), p1 = Vector3.Lerp(a, b, (float)(s + 1) / seg);
                        p0.y = ground.GroundAt(p0.x, p0.z) + lineLift;
                        p1.y = ground.GroundAt(p1.x, p1.z) + lineLift;
                        idx.Add(verts.Count); verts.Add(p0);
                        idx.Add(verts.Count); verts.Add(p1);
                    }
                }
            }
            var mesh = new Mesh { name = "Hex outline" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetIndices(idx, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        public void SetVisible(bool on) => GetComponent<MeshRenderer>().enabled = on;
        public bool Visible => GetComponent<MeshRenderer>().enabled;

        /// <summary>Outline of one hex for highlights (uses DebugLines).</summary>
        public void DrawHighlight(HexCell c, Color col)
        {
            Vector3 centre = new Vector3(c.center.x, 0f, c.center.z);
            for (int k = 0; k < 6; k++)
            {
                Vector3 a = Corner(centre, k), b = Corner(centre, (k + 1) % 6);
                for (int s = 0; s < 3; s++)
                {
                    Vector3 p0 = Vector3.Lerp(a, b, s / 3f), p1 = Vector3.Lerp(a, b, (s + 1) / 3f);
                    p0.y = Top(p0) + 0.12f; p1.y = Top(p1) + 0.12f;
                    DebugLines.Line(p0, p1, col);
                }
            }
        }

        float Top(Vector3 p)
        {
            float g = ground.GroundAt(p.x, p.z);
            if (water == null) return g;
            float s = water.SurfaceAt(p.x, p.z, out bool wet);
            return wet ? Mathf.Max(g, s) : g;
        }
    }
}
