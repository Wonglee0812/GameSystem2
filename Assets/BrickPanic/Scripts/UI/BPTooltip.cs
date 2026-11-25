using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>One tooltip for the whole HUD: a dark card with a title line and a body, shown next to the
    /// mouse. Call Show every frame while something is hovered; it hides itself one frame after the last call.
    /// It pops in with a small scale animation.</summary>
    public class BPTooltip : MonoBehaviour
    {
        public static BPTooltip I { get; private set; }
        /// <summary>True = no tooltip (the HUD sets it outside the build phase, when the hotbar is away).</summary>
        public static bool Suppressed;

        [Tooltip("The tooltip card (its pivot is the bottom-left corner).")]
        public RectTransform card;
        [Tooltip("Title line (yellow, title font).")]
        public TextMeshProUGUI titleText;
        [Tooltip("Body text (number font, can have several lines).")]
        public TextMeshProUGUI bodyText;
        [Tooltip("Widest the tooltip can be, in canvas pixels. Default 380.")]
        public float maxWidth = 380f;

        int shownFrame = -10;
        string lastTitle;
        Canvas canvas;

        void Awake() { I = this; canvas = GetComponentInParent<Canvas>(); if (card != null) card.gameObject.SetActive(false); }
        void OnDestroy() { if (I == this) I = null; }

        /// <summary>Shows the tooltip above the screen point (mouse position).</summary>
        public static void Show(string title, string body, Vector2 screen)
        {
            if (I == null || I.card == null || Suppressed) return;
            I.ShowAt(title, body, screen);
        }

        void ShowAt(string title, string body, Vector2 screen)
        {
            shownFrame = Time.frameCount;
            bool fresh = !card.gameObject.activeSelf || title != lastTitle;
            lastTitle = title;
            titleText.text = title;
            bodyText.text = body;
            float w = Mathf.Min(maxWidth, Mathf.Max(titleText.GetPreferredValues(title).x, bodyText.GetPreferredValues(body).x)) + 36f;
            float bodyH = bodyText.GetPreferredValues(body, w - 36f, 0f).y;
            card.sizeDelta = new Vector2(w, bodyH + 82f);
            float s = canvas != null ? canvas.scaleFactor : 1f;
            float x = Mathf.Clamp(screen.x + 18f, 8f, Screen.width - card.sizeDelta.x * s - 8f);
            float y = Mathf.Clamp(screen.y + 24f, 8f, Screen.height - card.sizeDelta.y * s - 8f);
            card.position = new Vector2(x, y);
            if (fresh)
            {
                card.gameObject.SetActive(true);
                card.SetAsLastSibling();
                card.localScale = Vector3.one * 0.85f;
                card.DOKill();
                card.DOScale(1f, 0.15f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        void LateUpdate()
        {
            if (card != null && card.gameObject.activeSelf && Time.frameCount > shownFrame + 1) card.gameObject.SetActive(false);
        }
    }
}
