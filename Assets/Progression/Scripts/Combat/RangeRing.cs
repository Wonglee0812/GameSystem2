using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// A dashed circle lying on the ground, to show a tower's range. Built as one
    /// mesh of short flat arc pieces, so it needs no texture.
    /// </summary>
    public class RangeRing : MonoBehaviour
    {
        static Material shared;

        MeshRenderer rend;
        Color color;

        public static RangeRing Create(Transform parent, float radius, Color color, int dashes = 40, float width = 0.08f)
        {
            var go = new GameObject("RangeRing");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.position = new Vector3(parent.position.x, 0.06f, parent.position.z);   // just above the tiles
            var ring = go.AddComponent<RangeRing>();
            go.AddComponent<MeshFilter>().sharedMesh = Build(radius, dashes, width);
            ring.rend = go.AddComponent<MeshRenderer>();
            if (shared == null) shared = new Material(Shader.Find("Sprites/Default"));
            ring.rend.sharedMaterial = shared;
            ring.rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.radiusNow = radius;
            ring.color = new Color(-1f, 0f, 0f, 0f);
            ring.SetColor(color);
            return ring;
        }

        public void SetColor(Color c)
        {
            if (c == color && rend != null) return;
            color = c;
            var mesh = GetComponent<MeshFilter>().sharedMesh;
            var cols = new Color[mesh.vertexCount];
            for (int i = 0; i < cols.Length; i++) cols[i] = c;
            mesh.colors = cols;
        }

        float radiusNow = -1f;

        public void SetRadius(float radius, int dashes = 40, float width = 0.08f)
        {
            if (Mathf.Approximately(radius, radiusNow)) return;
            radiusNow = radius;
            GetComponent<MeshFilter>().sharedMesh = Build(radius, dashes, width);
            var c = color;
            color = new Color(-1f, 0f, 0f, 0f);   // force the colours onto the new mesh
            SetColor(c);
        }

        static Mesh Build(float radius, int dashes, float width)
        {
            const int stepsPerDash = 3;
            var verts = new Vector3[dashes * (stepsPerDash + 1) * 2];
            var tris = new int[dashes * stepsPerDash * 6];
            int v = 0, t = 0;
            float slice = Mathf.PI * 2f / dashes;
            for (int d = 0; d < dashes; d++)
            {
                float a0 = d * slice, a1 = a0 + slice * 0.55f;   // 55 % dash, 45 % gap
                int start = v;
                for (int s = 0; s <= stepsPerDash; s++)
                {
                    float a = Mathf.Lerp(a0, a1, s / (float)stepsPerDash);
                    var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    verts[v++] = dir * (radius - width / 2f);
                    verts[v++] = dir * (radius + width / 2f);
                }
                for (int s = 0; s < stepsPerDash; s++)
                {
                    int i = start + s * 2;
                    tris[t++] = i; tris[t++] = i + 1; tris[t++] = i + 2;
                    tris[t++] = i + 1; tris[t++] = i + 3; tris[t++] = i + 2;
                }
            }
            var mesh = new Mesh { name = "DashedRing", vertices = verts, triangles = tris };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
