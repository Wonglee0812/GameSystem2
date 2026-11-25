using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PokerTD.UI
{
    /// <summary>
    /// Button animation from Wong's own UIButton package (Uicontrol.cs): on hover the visual
    /// gets a small punch rotation and grows (OutBack), on press it shrinks back, on release
    /// it grows again and plays a click sound. Same timings as the original.
    /// It needs a "Visual" child for the look, so the hit area does not move: if the button has
    /// none, the button's image and children are moved into a new Visual child.
    /// (The original imported Microsoft.Unity.VisualStudio.Editor, which breaks a WebGL build,
    /// so the behaviour is rewritten here.)
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PolishButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public float hoverScale = 1.2f;
        public float punchAngle = 5f;
        public AudioClip clickClip;

        RectTransform visual;
        Image visualImage;
        Button button;
        bool hovering;

        public Image VisualImage { get { Setup(); return visualImage; } }

        void Awake() => Setup();

        void Setup()
        {
            if (visual != null) return;
            button = GetComponent<Button>();
            var existing = transform.Find("Visual") as RectTransform;
            if (existing != null)
            {
                visual = existing;
                visualImage = existing.GetComponent<Image>();
                return;
            }
            visual = new GameObject("Visual", typeof(RectTransform)).GetComponent<RectTransform>();
            visual.SetParent(transform, false);
            visual.anchorMin = Vector2.zero; visual.anchorMax = Vector2.one;
            visual.offsetMin = visual.offsetMax = Vector2.zero;
            // move the old children (label) into the visual
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (c != visual) c.SetParent(visual, true);
            }
            visualImage = visual.gameObject.AddComponent<Image>();
            visualImage.raycastTarget = false;
            var root = GetComponent<Image>();
            if (root != null)
            {
                visualImage.sprite = root.sprite;
                visualImage.type = root.type;
                visualImage.color = root.color;
                root.sprite = null;
                root.color = new Color(1f, 1f, 1f, 0f);   // the root stays as the hit box
            }
            visualImage.transform.SetAsFirstSibling();
            button.targetGraphic = visualImage;
        }

        bool Active => button != null && button.interactable;

        public void OnPointerEnter(PointerEventData e)
        {
            if (hovering || !Active) return;
            hovering = true;
            DOTween.Kill(visual, true);
            visual.localRotation = Quaternion.identity;
            visual.DOPunchRotation(Vector3.forward * punchAngle, 0.15f, 20, 1f).SetUpdate(true).SetLink(gameObject)
                .OnComplete(() => visual.localRotation = Quaternion.identity);
            visual.DOScale(hoverScale, 0.2f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (!hovering) return;
            hovering = false;
            visual.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!hovering || !Active) return;
            visual.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!hovering || !Active) return;
            if (clickClip != null && Camera.main != null) AudioSource.PlayClipAtPoint(clickClip, Camera.main.transform.position, 0.6f);
            visual.DOScale(hoverScale, 0.2f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        void OnDisable()
        {
            hovering = false;
            if (visual != null) { visual.DOKill(); visual.localScale = Vector3.one; visual.localRotation = Quaternion.identity; }
        }
    }
}
