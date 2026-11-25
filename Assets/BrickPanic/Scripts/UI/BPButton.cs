using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrickPanic
{
    /// <summary>Button animation (same feel as Poker TD's PolishButton, from Wong's UIButton package):
    /// hover = the "Visual" child grows with a small punch rotation, press = it shrinks, release = it grows back.
    /// Only the Visual child moves, so the click area stays still. Optional idle "breathing" for main buttons.</summary>
    [RequireComponent(typeof(Button))]
    public class BPButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("The child that moves and scales (made by the HUD builder).")]
        public RectTransform visual;
        [Tooltip("Size on hover, 1 = no change. Default 1.12.")]
        public float hoverScale = 1.12f;
        [Tooltip("Size while pressed. Default 0.92.")]
        public float pressScale = 0.92f;
        [Tooltip("Punch rotation on hover in degrees. Default 5.")]
        public float punchAngle = 5f;
        [Tooltip("Slow grow-shrink loop while nothing happens (for START / CONTINUE). Default off.")]
        public bool breathe;
        [Tooltip("Size change of the breathing loop. Default 0.04.")]
        public float breatheAmount = 0.04f;
        [Tooltip("Click sound (optional).")]
        public AudioClip clickClip;

        bool hovering;
        Tween idle;

        void OnEnable()
        {
            if (visual == null) return;
            visual.localScale = Vector3.one;
            visual.localRotation = Quaternion.identity;
            StartBreathing();
        }

        void OnDisable() { idle?.Kill(); if (visual != null) visual.DOKill(); }

        void StartBreathing()
        {
            idle?.Kill();
            if (!breathe || visual == null) return;
            idle = visual.DOScale(1f + breatheAmount, 0.7f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(gameObject);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (visual == null || !GetComponent<Button>().interactable) return;
            hovering = true;
            idle?.Kill();
            visual.DOKill();
            visual.localRotation = Quaternion.identity;
            visual.DOScale(hoverScale, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
            visual.DOPunchRotation(new Vector3(0f, 0f, punchAngle), 0.3f, 8, 0.6f).SetUpdate(true);
        }

        public void OnPointerExit(PointerEventData e)
        {
            hovering = false;
            if (visual == null) return;
            visual.DOKill();
            visual.DOScale(1f, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true).OnComplete(StartBreathing);
            visual.DOLocalRotate(Vector3.zero, 0.15f).SetUpdate(true);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (visual == null || !GetComponent<Button>().interactable) return;
            visual.DOKill();
            visual.DOScale(pressScale, 0.08f).SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (visual == null) return;
            visual.DOKill();
            visual.DOScale(hovering ? hoverScale : 1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
            if (clickClip != null && Camera.main != null) AudioSource.PlayClipAtPoint(clickClip, Camera.main.transform.position, 0.6f);
        }
    }
}
