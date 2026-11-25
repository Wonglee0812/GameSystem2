using DG.Tweening;
using UnityEngine;

namespace PokerTD.UI
{
    /// <summary>
    /// Lives as a row of hearts on the table (Wong, 2026-11-24): 5 hearts per row, one heart = 2 lives,
    /// so losing 1 life empties half a heart. Lies flat like the other world texts and is visible in
    /// the scene (edit mode too), so it can be moved by hand. A heart that changes gets a punch.
    /// </summary>
    [ExecuteAlways]
    public class HeartBar : MonoBehaviour
    {
        public static HeartBar Instance { get; private set; }

        [Tooltip("Hearts in edit mode; in play it is the start lives / 2.")]
        public int hearts = 10;
        public int perRow = 5;
        public float spacing = 0.62f;
        public float rowSpacing = 0.62f;
        public float size = 0.42f;

        SpriteRenderer[] icons;
        int[] shown;
        Sprite full, half, empty;

        void OnEnable()
        {
            Instance = this;
            full = Resources.Load<Sprite>("UI/Icons/Heart_Full");
            half = Resources.Load<Sprite>("UI/Icons/Heart_Half");
            empty = Resources.Load<Sprite>("UI/Icons/Heart_Empty");
            Build(hearts);
            foreach (var sr in icons) if (sr != null && sr.sprite == null) sr.sprite = full;
        }

        void OnDisable() { if (Instance == this) Instance = null; }

        void Build(int count)
        {
            if (icons != null && icons.Length == count && transform.childCount == count) return;
            if (icons == null && transform.childCount == count)   // already in the scene: reuse
            {
                icons = GetComponentsInChildren<SpriteRenderer>(true);
                shown = new int[count];
                for (int i = 0; i < count; i++) shown[i] = 2;
                if (icons.Length == count) return;
            }
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
            }
            icons = new SpriteRenderer[count];
            shown = new int[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Heart" + i);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3((i % perRow) * spacing, -(i / perRow) * rowSpacing, 0f);
                go.transform.localScale = Vector3.one * size;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = full;
                icons[i] = sr;
                shown[i] = 2;
            }
        }

        void Update()
        {
            if (!Application.isPlaying || TDGame.Instance == null) return;
            var game = TDGame.Instance;
            int count = Mathf.CeilToInt(game.config.startLives / 2f);
            if (icons == null || icons.Length != count) Build(count);
            for (int i = 0; i < icons.Length; i++)
            {
                int v = Mathf.Clamp(game.Lives - i * 2, 0, 2);
                if (v == shown[i]) continue;
                shown[i] = v;
                icons[i].sprite = v == 2 ? full : v == 1 ? half : empty;
                var t = icons[i].transform;
                t.DOComplete();
                t.DOPunchScale(Vector3.one * size * 0.6f, 0.4f, 8, 0.7f).SetLink(t.gameObject);
            }
        }

        /// <summary>The castle was hit: all hearts shake a little.</summary>
        public static void Hit()
        {
            if (Instance == null) return;
            var t = Instance.transform;
            t.DOComplete();
            t.DOShakePosition(0.35f, new Vector3(0.12f, 0.12f, 0f), 30, 90f).SetLink(t.gameObject);
        }
    }
}
