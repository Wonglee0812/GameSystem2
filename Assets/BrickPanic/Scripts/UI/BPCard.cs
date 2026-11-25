using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrickPanic
{
    /// <summary>One card of the hotbar (a material, a shape or the person card).
    /// Hover: the card lifts, grows a little with a small wobble and shows its tooltip.
    /// Selected: it stays lifted with a pulsing yellow glow. Click: a punch, then the hotbar selects it.
    /// Too expensive: the face turns grey and the price red. Only the "Visual" child moves, so the hit area stays still.</summary>
    public class BPCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Parts (made by the HUD builder)")]
        [Tooltip("The child that moves (lift, scale, wobble).")]
        public RectTransform visual;
        [Tooltip("Card face (tinted grey when the player cannot pay).")]
        public Image face;
        [Tooltip("Yellow halo behind the card, visible when selected.")]
        public Image glow;
        [Tooltip("Picture of the brick, shape or person.")]
        public Image icon;
        [Tooltip("Name under the picture.")]
        public TextMeshProUGUI nameText;
        [Tooltip("Price line (turns red when the player cannot pay).")]
        public TextMeshProUGUI priceText;
        [Tooltip("Key badge text (1-4, Z/X, TAB).")]
        public TextMeshProUGUI keyText;

        [Header("Motion")]
        [Tooltip("Lift in canvas pixels when hovered. Default 14.")]
        public float hoverLift = 14f;
        [Tooltip("Lift in canvas pixels when selected. Default 22.")]
        public float selectedLift = 22f;
        [Tooltip("Size when hovered. Default 1.08.")]
        public float hoverScale = 1.08f;

        /// <summary>Called on a left click.</summary>
        public Action Clicked;
        /// <summary>Tooltip title and body, asked every frame while hovered (so prices stay current).</summary>
        public Func<string> TipTitle, TipBody;

        public bool Hovered { get; private set; }
        bool selected, affordable = true, started;
        Color faceColor = Color.white;
        Tween glowPulse;

        void Start()
        {
            started = true;
            if (face != null) faceColor = face.color;
            if (glow != null) { var c = glow.color; c.a = 0f; glow.color = c; }
            Apply(true);
        }

        void OnDisable() { Hovered = false; glowPulse?.Kill(); if (visual != null) visual.DOKill(); }

        public bool Selected
        {
            get => selected;
            set
            {
                if (selected == value) return;
                selected = value;
                if (started) Apply(false);
                if (selected && visual != null && started) visual.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.6f).SetUpdate(true);
            }
        }

        public bool Affordable
        {
            get => affordable;
            set
            {
                if (affordable == value) return;
                affordable = value;
                if (face != null) face.color = affordable ? faceColor : Color.Lerp(faceColor, new Color(0.55f, 0.55f, 0.6f), 0.6f);
                if (icon != null) icon.color = affordable ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                if (priceText != null) priceText.color = affordable ? new Color(0.86f, 0.36f, 0.05f) : new Color(0.9f, 0.2f, 0.2f);
            }
        }

        void Apply(bool instant)
        {
            if (visual == null) return;
            float lift = selected ? selectedLift : Hovered ? hoverLift : 0f;
            float scale = Hovered ? hoverScale : selected ? 1.04f : 1f;
            visual.DOKill();
            if (instant) { visual.anchoredPosition = new Vector2(0f, lift); visual.localScale = Vector3.one * scale; }
            else
            {
                visual.DOAnchorPosY(lift, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
                visual.DOScale(scale, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
            }
            glowPulse?.Kill();
            if (glow != null)
            {
                glow.DOKill();
                if (selected)
                {
                    glow.DOFade(1f, 0.15f).SetUpdate(true);
                    glowPulse = glow.rectTransform.DOScale(1.06f, 0.6f).From(Vector3.one).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(gameObject);
                }
                else glow.DOFade(0f, 0.15f).SetUpdate(true);
            }
        }

        public void OnPointerEnter(PointerEventData e)
        {
            Hovered = true;
            Apply(false);
            if (visual != null) visual.DOPunchRotation(new Vector3(0f, 0f, 4f), 0.3f, 8, 0.6f).SetUpdate(true);
        }

        public void OnPointerExit(PointerEventData e)
        {
            Hovered = false;
            Apply(false);
            if (visual != null) visual.DOLocalRotate(Vector3.zero, 0.12f).SetUpdate(true);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            Clicked?.Invoke();
            if (visual != null) { visual.DOComplete(); visual.DOPunchScale(Vector3.one * -0.1f, 0.2f, 8, 0.5f).SetUpdate(true); }
        }

        void Update()
        {
            if (Hovered && TipTitle != null)
            {
                var m = UnityEngine.InputSystem.Mouse.current;
                if (m != null) BPTooltip.Show(TipTitle(), TipBody != null ? TipBody() : "", m.position.ReadValue());
            }
        }

        /// <summary>A short wiggle to draw the eye (for example when the player cannot pay).</summary>
        public void Shake()
        {
            if (visual == null) return;
            visual.DOComplete();
            visual.DOShakeAnchorPos(0.3f, new Vector2(10f, 0f), 20, 0f).SetUpdate(true);
        }
    }
}
