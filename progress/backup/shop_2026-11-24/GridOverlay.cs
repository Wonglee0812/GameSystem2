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

        Material mat;
        float alpha;

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
            var mesh = new Mesh { name = "GridLines", hideFlags = HideFlags.DontSave };
            int lines = cols + 1 + rows + 1;
            var v = new Vector3[lines * 4];
            var t = new int[lines * 6];
            float w = cols * tile, d = rows * tile, h = lineWidth / 2f;
            int i = 0;
            void Quad(Vector3 a, Vector3 b, Vector3 side)
            {
                int k = i * 4;
                v[k] = a - side; v[k + 1] = a + side; v[k + 2] = b + side; v[k + 3] = b - side;
                int j = i * 6;
                t[j] = k; t[j + 1] = k + 1; t[j + 2] = k + 2; t[j + 3] = k; t[j + 4] = k + 2; t[j + 5] = k + 3;
                i++;
            }
            for (int c = 0; c <= cols; c++)
            {
                float x = -w / 2f + c * tile;
                Quad(new Vector3(x, height, -d / 2f - h), new Vector3(x, height, d / 2f + h), new Vector3(h, 0f, 0f));
            }
            for (int r = 0; r <= rows; r++)
            {
                float z = -d / 2f + r * tile;
                Quad(new Vector3(-w / 2f - h, height, z), new Vector3(w / 2f + h, height, z), new Vector3(0f, 0f, -h));
            }
            mesh.vertices = v;
            mesh.triangles = t;
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;

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

        void Update()
        {
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
