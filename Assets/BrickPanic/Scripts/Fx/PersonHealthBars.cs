using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>A small health bar over every person (Wong, 2026-11-25). It shows during a disaster and whenever a person
    /// is hurt; the fill goes from green to red and shakes a little when the person takes damage. Drawn with two quads per
    /// person (the emote material, which is unlit and see-through), facing the camera.</summary>
    public class PersonHealthBars : MonoBehaviour
    {
        [Tooltip("See-through unlit material (M_BP_Emote).")]
        public Material barMaterial;
        [Tooltip("Bar width and height in metres. Default 1 x 0.14.")]
        public Vector2 size = new Vector2(1f, 0.14f);
        [Tooltip("Height of the bar above the feet in metres (times the person's scale). Default 2.15.")]
        public float height = 2.15f;
        [Tooltip("Show bars in the build phase only for hurt people (off = always show). Default on.")]
        public bool hideWhenFull = true;
        public Color backColor = new Color(0.12f, 0.08f, 0.16f, 0.85f);
        public Color fullColor = new Color(0.3f, 0.85f, 0.5f);
        public Color lowColor = new Color(1f, 0.3f, 0.3f);

        class Bar { public Transform root, fill; public MeshRenderer back, front; public float shake; }

        readonly Dictionary<Person, Bar> bars = new Dictionary<Person, Bar>();
        readonly List<Person> gone = new List<Person>();
        MaterialPropertyBlock mpb;
        Mesh quad;
        RoundManager round;
        Transform cam;
        static readonly int ColorId = Shader.PropertyToID("_BaseColor"), MapId = Shader.PropertyToID("_BaseMap");

        void Start()
        {
            mpb = new MaterialPropertyBlock();
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad = q.GetComponent<MeshFilter>().sharedMesh;
            Destroy(q);
            round = FindFirstObjectByType<RoundManager>();
            if (Camera.main != null) cam = Camera.main.transform;
            Person.Hurt += OnHurt;
        }

        void OnDestroy() { Person.Hurt -= OnHurt; }

        void OnHurt(Person p, float amount) { if (bars.TryGetValue(p, out var b)) b.shake = 0.3f; }

        void LateUpdate()
        {
            if (barMaterial == null || cam == null) return;
            bool disaster = round != null && (round.Current == RoundManager.State.Disaster || round.Current == RoundManager.State.Settle);
            gone.Clear();
            foreach (var kv in bars) if (kv.Key == null || !kv.Key.Alive) gone.Add(kv.Key);
            foreach (var p in gone) { if (bars[p].root != null) Destroy(bars[p].root.gameObject); bars.Remove(p); }

            foreach (var p in Person.All)
            {
                if (p == null || !p.Alive) continue;
                float k = p.MaxHealth > 0f ? p.Health / p.MaxHealth : 0f;
                bool show = BPJuice.On && (disaster || !hideWhenFull || k < 0.999f);
                if (!bars.TryGetValue(p, out var b)) { if (!show) continue; bars[p] = b = Make(); }
                if (b.root.gameObject.activeSelf != show) b.root.gameObject.SetActive(show);
                if (!show) continue;
                Vector3 shake = Vector3.zero;
                if (b.shake > 0f) { b.shake -= Time.deltaTime; shake = Random.insideUnitSphere * 0.06f; }
                b.root.position = p.transform.position + Vector3.up * (height * p.transform.localScale.y) + shake;
                b.root.rotation = cam.rotation;
                b.fill.localScale = new Vector3(Mathf.Max(0.001f, k) * (size.x - 0.06f), size.y - 0.06f, 1f);
                b.fill.localPosition = new Vector3(-(size.x - 0.06f) * (1f - k) * 0.5f, 0f, -0.01f);
                mpb.SetTexture(MapId, Texture2D.whiteTexture);
                mpb.SetColor(ColorId, Color.Lerp(lowColor, fullColor, k));
                b.front.SetPropertyBlock(mpb);
            }
        }

        Bar Make()
        {
            var root = new GameObject("HealthBar").transform;
            root.SetParent(transform, false);
            var back = Quad("Back", root);
            back.transform.localScale = new Vector3(size.x, size.y, 1f);
            mpb.SetTexture(MapId, Texture2D.whiteTexture);
            mpb.SetColor(ColorId, backColor);
            back.SetPropertyBlock(mpb);
            var front = Quad("Fill", root);
            return new Bar { root = root, back = back, front = front, fill = front.transform };
        }

        MeshRenderer Quad(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = barMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }
    }
}
