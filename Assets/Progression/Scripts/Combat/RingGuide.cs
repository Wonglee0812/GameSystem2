using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// The rings around a buff machine, drawn on the ground: each ring is a filled band
    /// (inner ring green, outer ring red), with a dashed edge and the ring number (the
    /// buff order) inside the band. Also used as the preview while placing a machine.
    /// </summary>
    public class RingGuide : MonoBehaviour
    {
        /// <summary>Just above the grass tiles (top at 0.2).</summary>
        public const float Height = 0.23f;
        readonly List<RangeRing> rings = new List<RangeRing>();
        readonly List<TextMeshPro> labels = new List<TextMeshPro>();
        readonly List<MeshRenderer> fills = new List<MeshRenderer>();
        static Material fillMaterial;
        float alpha = 1f;
        int count = -1;
        float width = -1f;
        bool innerFirst;
        Color color = Color.white;

        public static RingGuide Create(Transform parent, Color color)
        {
            var go = new GameObject("RingGuide");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(parent.position.x, Height, parent.position.z);
            var guide = go.AddComponent<RingGuide>();
            guide.color = color;
            return guide;
        }

        /// <summary>Rebuilds the circles only when the number or the size of the rings changed.</summary>
        public void Refresh(int ringCount, float ringWidth, bool ringOneIsInner)
        {
            if (ringCount == count && Mathf.Approximately(ringWidth, width) && ringOneIsInner == innerFirst) return;
            count = ringCount; width = ringWidth; innerFirst = ringOneIsInner;
            foreach (var r in rings) Destroy(r.gameObject);
            foreach (var l in labels) Destroy(l.gameObject);
            foreach (var f in fills) Destroy(f.gameObject);
            rings.Clear(); labels.Clear(); fills.Clear();
            for (int k = 1; k <= count; k++)
            {
                var edge = RangeRing.Create(transform, k * width, color, 40 + k * 8, 0.07f);
                edge.transform.localPosition = new Vector3(0f, 0.01f, 0f);
                rings.Add(edge);
                fills.Add(Band(k, (k - 1) * width, k * width));
                var go = new GameObject("RingNumber" + k);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 0.02f, -(k - 0.5f) * width);   // on the side that faces the camera
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var t = go.AddComponent<TextMeshPro>();
                t.text = (innerFirst ? k : count + 1 - k).ToString();
                t.fontSize = 6f;
                t.fontStyle = FontStyles.Bold;
                t.alignment = TextAlignmentOptions.Center;
                t.rectTransform.sizeDelta = new Vector2(1.5f, 1f);
                t.color = Color.white;
                t.outlineWidth = 0.25f;
                t.outlineColor = new Color32(0, 0, 0, 200);
                labels.Add(t);
            }
            ApplyAlpha();
        }

        /// <summary>Inner ring green, outer ring red (by distance, not by order).</summary>
        Color BandColor(int k) => Color.Lerp(new Color(0.2f, 0.85f, 0.3f), new Color(0.95f, 0.2f, 0.15f), count <= 1 ? 0f : (k - 1f) / (count - 1f));

        MeshRenderer Band(int k, float inner, float outer)
        {
            const int seg = 64;
            var verts = new Vector3[(seg + 1) * 2];
            var tris = new int[seg * 6];
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                verts[i * 2] = dir * inner;
                verts[i * 2 + 1] = dir * outer;
            }
            for (int i = 0; i < seg; i++)
            {
                int v = i * 2, t = i * 6;
                tris[t] = v; tris[t + 1] = v + 1; tris[t + 2] = v + 2;
                tris[t + 3] = v + 1; tris[t + 4] = v + 3; tris[t + 5] = v + 2;
            }
            var mesh = new Mesh { name = "RingBand", vertices = verts, triangles = tris };
            mesh.RecalculateBounds();
            var go = new GameObject("RingFill" + k);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, -0.005f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            if (fillMaterial == null) fillMaterial = new Material(Shader.Find("Sprites/Default"));
            r.sharedMaterial = fillMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return r;
        }

        /// <summary>Fill strength: faint when many machines are shown, strong for the one in focus.</summary>
        public void SetFillAlpha(float a)
        {
            if (Mathf.Approximately(a, alpha)) return;
            alpha = a;
            ApplyAlpha();
        }

        void ApplyAlpha()
        {
            for (int i = 0; i < fills.Count; i++)
            {
                var c = BandColor(i + 1); c.a = alpha * 0.35f;
                var mesh = fills[i].GetComponent<MeshFilter>().sharedMesh;
                var cols = new Color[mesh.vertexCount];
                for (int v = 0; v < cols.Length; v++) cols[v] = c;
                mesh.colors = cols;
            }
            foreach (var l in labels) { var c = l.color; c.a = Mathf.Max(0.5f, alpha); l.color = c; }
        }

        public void SetColor(Color c)
        {
            if (c == color) return;
            color = c;
            foreach (var r in rings) r.SetColor(c);
        }

        public void Show(bool on) { if (gameObject.activeSelf != on) gameObject.SetActive(on); }
    }
}
