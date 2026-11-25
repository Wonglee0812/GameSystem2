using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>Speech bubbles over people, so the player can read the crowd at a glance: "!" when someone starts to panic,
    /// stars when knocked over, a skull when someone dies, a sweat drop when panic stays high, and a heart over every
    /// survivor when a round is survived. Bubbles pop in, bob, and shrink away. Sprites are drawn by
    /// progress/patches/make_bp_ui_art.py (the Kenney emotes are only 32 px).</summary>
    public class PersonEmotes : MonoBehaviour
    {
        [Tooltip("Bubble sprites (their textures are drawn on a quad).")]
        public Sprite panic, dazed, dead, scared, happy;
        [Tooltip("See-through unlit material for the bubbles (M_BP_Emote, made by \"Brick Panic/Apply Look\"). A quad is used because SpriteRenderers do not draw with this project's 3D renderer.")]
        public Material emoteMaterial;
        [Tooltip("Bubble size in metres. Default 1.3.")]
        public float size = 1.3f;
        [Tooltip("Height of the bubble above the feet, in metres (times the person's scale). Default 2.4.")]
        public float height = 2.4f;
        [Tooltip("Seconds a bubble stays. Default 1.3.")]
        public float showTime = 1.3f;
        [Tooltip("Seconds before the same person can show the same bubble again. Default 4.")]
        public float repeatDelay = 4f;
        [Tooltip("Chance per second of a sweat drop while panic is above 0.8. Default 0.08.")]
        public float scaredChance = 0.08f;

        class Bubble { public Transform t; public MeshRenderer r; public Person p; public float until; }
        MaterialPropertyBlock mpb;
        Mesh quad;
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        readonly List<Bubble> pool = new List<Bubble>();
        readonly Dictionary<Person, Person.State> last = new Dictionary<Person, Person.State>();
        readonly Dictionary<(Person, Sprite), float> lastShown = new Dictionary<(Person, Sprite), float>();
        RoundManager round;
        RoundManager.State lastState;
        Transform cam;

        void Start()
        {
            round = FindFirstObjectByType<RoundManager>();
            if (Camera.main != null) cam = Camera.main.transform;
            if (round != null) lastState = round.Current;
            mpb = new MaterialPropertyBlock();
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad = q.GetComponent<MeshFilter>().sharedMesh;
            Destroy(q);
        }

        void Update()
        {
            if (!BPJuice.On) return;
            foreach (var p in Person.All)
            {
                if (p == null) continue;
                var st = p.Current;
                if (last.TryGetValue(p, out var before) && before != st)
                {
                    if (st == Person.State.Panic && before == Person.State.Idle) Show(p, panic);
                    else if (st == Person.State.Tumble) Show(p, dazed);
                    else if (st == Person.State.Dead) Show(p, dead, 2f);
                }
                else if (st == Person.State.Panic && p.PanicLevel > 0.8f && Random.value < scaredChance * Time.deltaTime) Show(p, scared);
                last[p] = st;
            }
            if (round != null && round.Current != lastState)
            {
                if (round.Current == RoundManager.State.Result)
                {
                    int i = 0;
                    foreach (var p in Person.All) if (p != null && p.Alive) Show(p, happy, 2f, 0.05f * i++);
                }
                lastState = round.Current;
            }

            // follow the person, face the camera
            foreach (var b in pool)
            {
                if (!b.t.gameObject.activeSelf) continue;
                if (b.p == null || Time.time > b.until) { Hide(b); continue; }
                b.t.position = b.p.transform.position + Vector3.up * (height * b.p.transform.localScale.y + Mathf.Sin(Time.time * 6f) * 0.05f);
                if (cam != null) b.t.rotation = cam.rotation;
            }
        }

        void Show(Person p, Sprite s, float time = -1f, float delay = 0f)
        {
            if (s == null || p == null || emoteMaterial == null) return;
            if (lastShown.TryGetValue((p, s), out var t0) && Time.time - t0 < repeatDelay) return;
            lastShown[(p, s)] = Time.time;
            Bubble b = null;
            foreach (var x in pool) if (x.p == p && x.t.gameObject.activeSelf) { b = x; break; }   // one bubble per person
            if (b == null) foreach (var x in pool) if (!x.t.gameObject.activeSelf) { b = x; break; }
            if (b == null)
            {
                var go = new GameObject("Emote");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = quad;
                b = new Bubble { t = go.transform, r = go.AddComponent<MeshRenderer>() };
                b.r.sharedMaterial = emoteMaterial;
                b.r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                b.r.receiveShadows = false;
                pool.Add(b);
            }
            b.p = p;
            mpb.SetTexture(BaseMapId, s.texture);
            b.r.SetPropertyBlock(mpb);
            b.until = Time.time + delay + (time > 0f ? time : showTime);
            b.t.gameObject.SetActive(true);
            b.t.DOKill();
            b.t.localScale = Vector3.zero;
            float aspect = s.texture.width / (float)Mathf.Max(1, s.texture.height);
            b.t.DOScale(new Vector3(size * aspect, size, 1f), 0.3f).SetEase(Ease.OutBack).SetDelay(delay);
        }

        void Hide(Bubble b)
        {
            if (b.t.localScale.x <= 0.001f) { b.t.gameObject.SetActive(false); b.p = null; return; }
            if (DOTween.IsTweening(b.t)) return;
            b.t.DOScale(0f, 0.18f).SetEase(Ease.InBack).OnComplete(() => { b.t.gameObject.SetActive(false); b.p = null; });
        }
    }
}
