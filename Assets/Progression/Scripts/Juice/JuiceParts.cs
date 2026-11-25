using DG.Tweening;
using TMPro;
using UnityEngine;
using PokerTD.UI;

namespace PokerTD
{
    /// <summary>Balatro-style score at a burst: "chips x mult" first, then the damage, bigger for better hands.</summary>
    public class BurstScore : MonoBehaviour
    {
        TextMeshPro text;

        public static void Show(Vector3 at, DamageCalc.Result r)
        {
            var go = new GameObject("BurstScore");
            go.transform.position = at;
            var t = go.AddComponent<TextMeshPro>();
            UiKit.SetFont(t, UiKit.Number);
            t.fontSize = 6f;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = new Vector2(10f, 3f);
            t.sortingOrder = 50;
            var b = go.AddComponent<BurstScore>();
            b.text = t;
            b.Play(r);
        }

        void Play(DamageCalc.Result r)
        {
            int rank = (int)r.hand;
            string name = $"<size=55%><color=#{ColorUtility.ToHtmlStringRGB(HandColor(r.hand))}>{HandEvaluator.Name(r.hand)}</color></size>";
            text.text = $"{name}\n<color=#5AA9FF>{r.chips:#,0}</color> <size=70%>x</size> <color=#FF5A4F>{r.mult:0.#}</color>";
            float big = 1f + 0.12f * rank;
            transform.localScale = Vector3.zero;
            var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            s.Append(transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack));
            s.AppendInterval(0.3f);
            s.AppendCallback(() => text.text = $"{name}\n<color=#FFD24A>{r.damage:#,0}</color>");
            s.Append(transform.DOScale(big * 1.35f, 0.08f));
            s.Append(transform.DOScale(big, 0.18f).SetEase(Ease.OutBack));
            s.Append(transform.DOMoveY(transform.position.y + 1f, 0.8f).SetEase(Ease.OutQuad));
            s.Join(DOTween.To(() => text.alpha, a => text.alpha = a, 0f, 0.35f).SetDelay(0.45f));
            s.OnComplete(() => Destroy(gameObject));
        }

        void LateUpdate()
        {
            if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
        }

        public static Color HandColor(HandType hand) =>
            hand >= HandType.FourOfAKind ? new Color(1f, 0.3f, 0.8f) :
            hand >= HandType.Straight ? new Color(1f, 0.75f, 0.2f) :
            hand >= HandType.Pair ? new Color(0.5f, 0.85f, 1f) : Color.white;
    }

    /// <summary>Small damage number for one card hit: pops up, drifts and fades fast.</summary>
    public class HitNumber : MonoBehaviour
    {
        TextMeshPro text;
        float age;
        Vector3 drift;
        const float Life = 0.55f;

        public static void Show(Vector3 at, string value, Color color)
        {
            var go = new GameObject("HitNumber");
            go.transform.position = at;
            var t = go.AddComponent<TextMeshPro>();
            UiKit.SetFont(t, UiKit.Number);
            t.text = value;
            t.fontSize = 3.2f;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = new Vector2(3f, 1f);
            t.sortingOrder = 40;
            var h = go.AddComponent<HitNumber>();
            h.text = t;
            h.drift = new Vector3(Random.Range(-0.8f, 0.8f), 2.2f, 0f);
            go.transform.localScale = Vector3.one * 0.5f;
            go.transform.DOScale(1f, 0.12f).SetEase(Ease.OutBack).SetLink(go);
        }

        void Update()
        {
            age += Time.deltaTime;
            drift.y -= 6f * Time.deltaTime;
            transform.position += drift * Time.deltaTime;
            if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
            text.alpha = 1f - Mathf.Clamp01((age - Life * 0.5f) / (Life * 0.5f));
            if (age > Life) Destroy(gameObject);
        }
    }

    /// <summary>Gold coins that jump from a dead enemy to the GOLD text on the table.</summary>
    public static class Coin
    {
        static Material mat;

        public static void Fly(Vector3 from, Transform goldText, int count)
        {
            if (mat == null) mat = new Material(Shader.Find("Sprites/Default")) { color = new Color(1f, 0.8f, 0.25f) };
            count = Mathf.Clamp(count, 1, 5);
            for (int i = 0; i < count; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.Destroy(c.GetComponent<Collider>());
                c.name = "Coin";
                c.transform.position = from + Random.insideUnitSphere * 0.3f;
                c.transform.localScale = new Vector3(0.35f, 0.03f, 0.35f);
                c.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var r = c.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                float d = 0.55f + i * 0.07f;
                Vector3 to = goldText.position + Vector3.up * 0.3f;
                c.transform.DOJump(to, 3f, 1, d).SetEase(Ease.InQuad).SetLink(c).OnComplete(() =>
                {
                    Object.Destroy(c);
                    if (goldText == null) return;
                    goldText.DOComplete();
                    goldText.DOPunchScale(Vector3.one * 0.15f, 0.2f, 6, 0.8f).SetLink(goldText.gameObject);
                });
                c.transform.DORotate(new Vector3(90f, 720f, 0f), d, RotateMode.FastBeyond360).SetLink(c);
            }
        }
    }

    /// <summary>Hit flash: swaps all renderers under the object to a plain white material for a moment.</summary>
    public class Flasher : MonoBehaviour
    {
        Renderer[] rends;
        Material[][] normal, white;
        float until;
        bool on;

        public static Flasher For(GameObject go, Material flash)
        {
            var f = go.GetComponent<Flasher>();
            if (f != null) return f;
            f = go.AddComponent<Flasher>();
            var all = go.GetComponentsInChildren<MeshRenderer>(true);
            f.rends = all;
            f.normal = new Material[all.Length][];
            f.white = new Material[all.Length][];
            for (int i = 0; i < all.Length; i++)
            {
                f.normal[i] = all[i].sharedMaterials;
                f.white[i] = new Material[f.normal[i].Length];
                for (int k = 0; k < f.white[i].Length; k++) f.white[i][k] = flash;
            }
            return f;
        }

        public void Flash(float seconds)
        {
            until = Mathf.Max(until, Time.unscaledTime + seconds);
            if (!on) Set(true);
        }

        void Update()
        {
            if (on && Time.unscaledTime >= until) Set(false);
        }

        void Set(bool value)
        {
            on = value;
            for (int i = 0; i < rends.Length; i++)
                if (rends[i] != null) rends[i].sharedMaterials = value ? white[i] : normal[i];
        }

        /// <summary>Back to the normal materials now (before a copy of the model is made).</summary>
        public void Off() { until = 0f; if (on) Set(false); }

        void OnDisable() { if (on) Set(false); }
    }

    /// <summary>
    /// Burns 3D models in or out with PokerTD/ModelDissolve (same noise and orange edge as the card dissolve),
    /// so cards, towers, machines and enemies appear and vanish the same way. Only the Kenney/Lit renderers
    /// are swapped (texts, sprites and rings stay as they are); their material comes back when it is done.
    /// </summary>
    public class ModelDissolve : MonoBehaviour
    {
        static Material mat;
        static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        Renderer[] rends;
        Material[][] normal;
        MaterialPropertyBlock block;
        float from, to, time, delay, age;
        bool destroyAfter;
        System.Action done;

        /// <summary>Starts a dissolve on every Lit renderer under root. from/to: 0 = whole, 1 = gone.</summary>
        public static ModelDissolve Run(GameObject root, float from, float to, float time, bool destroyAfter, float delay = 0f, System.Action done = null)
        {
            if (mat == null) mat = Resources.Load<Material>("M_ModelDissolve");
            if (mat == null || root == null) { done?.Invoke(); if (destroyAfter && root != null) Object.Destroy(root); return null; }
            var old = root.GetComponent<ModelDissolve>();
            if (old != null) old.Finish(false);
            var d = root.AddComponent<ModelDissolve>();
            d.from = from; d.to = to; d.time = Mathf.Max(0.01f, time); d.delay = delay; d.destroyAfter = destroyAfter; d.done = done;
            d.block = new MaterialPropertyBlock();
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var m = r.sharedMaterial;
                if (m != null && m.shader != null && m.shader.name.Contains("Universal Render Pipeline/Lit")) list.Add(r);
            }
            d.rends = list.ToArray();
            d.normal = new Material[d.rends.Length][];
            for (int i = 0; i < d.rends.Length; i++)
            {
                var r = d.rends[i];
                d.normal[i] = r.sharedMaterials;
                var src = r.sharedMaterial;
                r.GetPropertyBlock(d.block);
                d.block.SetTexture(BaseMapId, src.mainTexture != null ? src.mainTexture : Texture2D.whiteTexture);
                if (!d.block.HasColor(BaseColorId)) d.block.SetColor(BaseColorId, src.HasProperty(BaseColorId) ? src.GetColor(BaseColorId) : Color.white);
                d.block.SetFloat(DissolveId, from);
                r.SetPropertyBlock(d.block);
                var mats = new Material[d.normal[i].Length];
                for (int k = 0; k < mats.Length; k++) mats[k] = mat;
                r.sharedMaterials = mats;
            }
            return d;
        }

        void Update()
        {
            age += Time.deltaTime;
            float k = Mathf.Clamp01((age - delay) / time);
            float v = Mathf.Lerp(from, to, to > from ? k * k : 1f - (1f - k) * (1f - k));
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null) continue;
                rends[i].GetPropertyBlock(block);
                block.SetFloat(DissolveId, v);
                rends[i].SetPropertyBlock(block);
            }
            if (k >= 1f) Finish(true);
        }

        void Finish(bool callDone)
        {
            if (destroyAfter) { Destroy(gameObject); }
            else
                for (int i = 0; i < rends.Length; i++)
                    if (rends[i] != null) rends[i].sharedMaterials = normal[i];
            if (callDone) done?.Invoke();
            Destroy(this);
        }
    }
}
