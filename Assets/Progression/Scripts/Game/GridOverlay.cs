using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// Thin grid lines drawn just above the Kenney tiles, so the player can see each build tile
    /// (Wong, 2026-11-24: lines on top instead of splitting the board into separate blocks).
    /// One mesh made of thin quads, one quad per line. Brighter while deploying, faint in a wave.
    /// Runs in edit mode too, so the grid can be seen and tuned in the scene.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GridOverlay : MonoBehaviour
    {
        public int cols = 14, rows = 9;
        public float tile = 2f;
        [Tooltip("Height of the lines above the board root (tile surface is 0.2).")]
        public float height = 0.23f;
        public float lineWidth = 0.07f;
        public Color color = Color.white;
        [Range(0f, 1f)] public float alphaDeploy = 0.45f;
        [Range(0f, 1f)] public float alphaWave = 0.15f;
        [Tooltip("Seconds to fade between the two alphas.")]
        public float fadeTime = 0.3f;

        [Tooltip("Lines closer than this to the board centre are hidden (0 = full grid). BoardTransition grows it to erase the grid from the centre out, and shrinks it to draw the grid from the outside in.")]
        public float holeRadius = 0f;
        [Tooltip("Soft edge of the hole (metres).")]
        public float holeSoftness = 1.2f;

        Material mat;
        float alpha;
        Mesh mesh;
        Vector3[] mids;      // centre of each short segment (one per tile edge)
        Color[] colors;
        float lastHole = -1f;

        /// <summary>Radius that hides every line.</summary>
        public float FullRadius => Mathf.Sqrt(cols * cols + rows * rows) * tile * 0.5f + holeSoftness + 0.5f;

        void OnEnable()
        {
            BuildMesh();
            alpha = alphaDeploy;
            Apply();
        }

        void OnValidate() { if (isActiveAndEnabled) { BuildMesh(); alpha = alphaDeploy; Apply(); } }

        void OnDisable() { if (mat != null) DestroyImmediate(mat); mat = null; }

        void BuildMesh()
        {
            // one thin quad per tile edge, so parts of the grid can be hidden (eraser effect)
            mesh = new Mesh { name = "GridLines", hideFlags = HideFlags.DontSave };
            int segs = (cols + 1) * rows + (rows + 1) * cols;
            var v = new Vector3[segs * 4];
            var t = new int[segs * 6];
            mids = new Vector3[segs];
            colors = new Color[segs * 4];
            float w = cols * tile, d = rows * tile, h = lineWidth / 2f;
            int i = 0;
            void Quad(Vector3 a, Vector3 b, Vector3 side)
            {
                int k = i * 4;
                v[k] = a - side; v[k + 1] = a + side; v[k + 2] = b + side; v[k + 3] = b - side;
                int j = i * 6;
                t[j] = k; t[j + 1] = k + 1; t[j + 2] = k + 2; t[j + 3] = k; t[j + 4] = k + 2; t[j + 5] = k + 3;
                mids[i] = (a + b) * 0.5f;
                i++;
            }
            for (int c = 0; c <= cols; c++)
                for (int r = 0; r < rows; r++)
                {
                    float x = -w / 2f + c * tile, z0 = -d / 2f + r * tile;
                    Quad(new Vector3(x, height, z0 - (r == 0 ? h : 0f)), new Vector3(x, height, z0 + tile + (r == rows - 1 ? h : 0f)), new Vector3(h, 0f, 0f));
                }
            for (int r = 0; r <= rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    float z = -d / 2f + r * tile, x0 = -w / 2f + c * tile;
                    Quad(new Vector3(x0 - (c == 0 ? h : 0f), height, z), new Vector3(x0 + tile + (c == cols - 1 ? h : 0f), height, z), new Vector3(0f, 0f, -h));
                }
            mesh.vertices = v;
            mesh.triangles = t;
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            lastHole = -1f;
            ApplyHole();

            if (mat == null)
            {
                mat = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
                mat.renderQueue = 3000;
            }
            var mr = GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        /// <summary>Vertex alpha per segment: 0 inside the hole, 1 outside, soft in between.</summary>
        void ApplyHole()
        {
            if (mesh == null || Mathf.Approximately(lastHole, holeRadius)) return;
            lastHole = holeRadius;
            for (int s = 0; s < mids.Length; s++)
            {
                Vector3 m = mids[s]; m.y = 0f;
                float a = holeRadius <= 0f ? 1f : Mathf.Clamp01((m.magnitude - holeRadius) / Mathf.Max(0.01f, holeSoftness) + 0.5f);
                var c = new Color(1f, 1f, 1f, a);
                for (int k = 0; k < 4; k++) colors[s * 4 + k] = c;
            }
            mesh.colors = colors;
        }

        void Update()
        {
            ApplyHole();
            if (!Application.isPlaying || TDGame.Instance == null) return;
            float goal = TDGame.Instance.Current == TDGame.Phase.Wave ? alphaWave : alphaDeploy;
            alpha = Mathf.MoveTowards(alpha, goal, Time.deltaTime / Mathf.Max(0.01f, fadeTime));
            Apply();
        }

        void Apply()
        {
            if (mat != null) mat.color = new Color(color.r, color.g, color.b, alpha);
        }
    }
}
