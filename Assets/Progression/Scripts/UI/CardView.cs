using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PokerTD.UI
{
    /// <summary>
    /// One card in the hand, with Balatro-style game feel. The method follows Mix and Jam
    /// (2024), "Recreating Balatro's Game Feel" (MIT): the card moves smoothly to its
    /// place, swings with its own movement, wobbles a little when idle, tilts towards the
    /// pointer on hover, and gets small punch animations from DOTween.
    /// Layers: root (hit box, position, swing) > Shake (scale + punch) > Shadow + Tilt (idle float) > Face (fake 3D tilt).
    /// Click: select for discard. Drag onto a tower: give it the card.
    /// </summary>
    public class CardView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        // old graybox parts, made by PokerTDBuilder; hidden at runtime
        public TMP_Text rank;
        public TMP_Text suit;
        public Image face;

        [Header("Follow")]
        public float followSpeed = 18f;
        [Tooltip("Degrees of swing per pixel the card lags behind its target.")]
        public float rotationPerPixel = 0.12f;
        public float maxSwing = 25f;

        [Header("Tilt")]
        public float idleTilt = 9f;
        public float idleSpeed = 1.1f;
        [Tooltip("Idle float: pixels up/down and degrees of sway.")]
        public float idleBob = 3f;
        public float idleSway = 1.5f;
        [Tooltip("Degrees of hover tilt per pixel between the pointer and the card centre.")]
        public float hoverTilt = 0.32f;
        public float maxHoverTilt = 24f;
        public float tiltSpeed = 14f;

        [Header("Shadow")]
        public Color shadowColor = new Color(0f, 0f, 0f, 0.42f);
        public Vector2 shadowRest = new Vector2(4f, -14f);
        public Vector2 shadowHover = new Vector2(6f, -24f);
        public Vector2 shadowDrag = new Vector2(10f, -42f);

        [Header("Scale and punch")]
        public float hoverScale = 1.12f;
        public float dragScale = 1.2f;
        public float scaleTime = 0.15f;
        public float hoverPunch = 5f;
        public float swapPunch = 25f;

        [HideInInspector] public HandView hand;
        [HideInInspector] public int index;
        [HideInInspector] public Card card;
        [HideInInspector] public Vector2 restPosition;
        [HideInInspector] public float restAngle;

        public bool Hovering { get; private set; }
        public bool Dragging { get; private set; }
        public bool Leaving { get; private set; }

        RectTransform rt, shake, tilt, shadow;
        Image faceImage, shadowImage;
        CardPerspective perspective;
        CanvasGroup group;
        Vector2 dragTarget, holdPosition, smoothLag;
        float swing, holdUntil;
        Vector2 currentTilt;
        bool built, faceUp = true, selected;


        public RectTransform Rect { get { Build(); return rt; } }

        void Awake() => Build();

        void Build()
        {
            if (built) return;
            built = true;
            rt = (RectTransform)transform;

            if (rank != null) rank.gameObject.SetActive(false);
            if (suit != null) suit.gameObject.SetActive(false);
            var outline = GetComponent<Outline>();
            if (outline != null) outline.enabled = false;

            // the root image is only the hit box
            var hit = GetComponent<Image>();
            if (hit == null) hit = gameObject.AddComponent<Image>();
            hit.sprite = null;
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;

            // a copy of the template already has the layers of the template: remove them,
            // or an empty white Face would show behind the real one
            var stale = transform.Find("Shake");
            if (stale != null) { stale.gameObject.SetActive(false); Destroy(stale.gameObject); }

            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();

            shake = NewRect("Shake", rt);
            shadow = NewRect("Shadow", shake);
            shadowImage = shadow.gameObject.AddComponent<Image>();
            shadowImage.color = shadowColor;
            shadowImage.raycastTarget = false;
            shadow.anchoredPosition = shadowRest;
            tilt = NewRect("Tilt", shake);
            faceImage = NewRect("Face", tilt).gameObject.AddComponent<Image>();
            faceImage.raycastTarget = false;
            perspective = faceImage.gameObject.AddComponent<CardPerspective>();
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        /// <summary>Shows the card. A change of the selected state gives a small punch.</summary>
        public void Show(Card c, bool isSelected)
        {
            Build();
            card = c;
            if (isSelected != selected && !Leaving) Punch(hoverPunch * 0.6f, 0.2f);
            selected = isSelected;
            RefreshSprite();
        }

        void RefreshSprite()
        {
            var s = faceUp ? CardArt.Face(card) : CardArt.Back;
            faceImage.sprite = s;
            shadowImage.sprite = s;
        }

        /// <summary>Starts at 'from' face down, waits 'delay' seconds, then flies to its place and flips.</summary>
        public void DealFrom(Vector2 from, float delay)
        {
            Build();
            rt.anchoredPosition = from;
            rt.localRotation = Quaternion.identity;
            holdPosition = from;
            holdUntil = Time.unscaledTime + delay;
            faceUp = false;
            RefreshSprite();
            DOVirtual.DelayedCall(delay + 0.12f, Flip, true).SetLink(gameObject);
        }

        /// <summary>Starts at 'from' face up (a card that comes back from a tower).</summary>
        public void ComeFrom(Vector2 from)
        {
            Build();
            rt.anchoredPosition = from;
            shake.localScale = Vector3.one * 0.4f;
            shake.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        void Flip()
        {
            tilt.DOKill();
            tilt.DOScaleX(0f, 0.07f).SetUpdate(true).SetLink(gameObject).OnComplete(() =>
            {
                faceUp = true;
                RefreshSprite();
                tilt.DOScaleX(1f, 0.09f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
                Punch(hoverPunch, 0.2f);
            });
        }

        /// <summary>The card moved to another slot (sort or discard): a short shake.</summary>
        public void Swap(float dir)
        {
            if (Leaving || Dragging) return;
            DOTween.Kill(this, true);
            shake.DOPunchRotation(Vector3.forward * swapPunch * dir, 0.18f, 5, 1f).SetId(this).SetUpdate(true).SetLink(gameObject);
        }

        /// <summary>Leaves the hand: flies to 'to' (local position in the hand area), then fades and is destroyed.
        /// shrink = goes onto a tower; else it is thrown away.</summary>
        public void Leave(Vector2 to, bool shrink, float delay)
        {
            Build();
            Leaving = true;
            Hovering = Dragging = false;
            group.blocksRaycasts = false;
            var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            s.AppendInterval(delay);
            s.AppendCallback(() =>
            {
                restPosition = to;
                restAngle = shrink ? 0f : -35f;
                followSpeed = shrink ? 14f : 9f;
            });
            s.AppendInterval(shrink ? 0.12f : 0.2f);
            s.Append(shake.DOScale(shrink ? 0.15f : 0.85f, 0.25f).SetEase(Ease.InBack));
            s.Join(DOTween.To(() => group.alpha, a => group.alpha = a, 0f, 0.25f));
            s.OnComplete(() => Destroy(gameObject));
        }

        void Punch(float angle, float time)
        {
            if (shake == null) return;
            DOTween.Kill(this, true);
            shake.DOPunchRotation(Vector3.forward * angle, time, 20, 1f).SetId(this).SetUpdate(true).SetLink(gameObject);
        }

        void Update()
        {
            if (!built) return;
            float dt = Time.unscaledDeltaTime;

            // 1. smooth follow towards the target
            bool held = Time.unscaledTime < holdUntil;
            Vector2 goal = held ? holdPosition : Dragging ? dragTarget : restPosition;
            Vector2 pos = Vector2.Lerp(rt.anchoredPosition, goal, 1f - Mathf.Exp(-followSpeed * dt));
            rt.anchoredPosition = pos;

            // 2. swing: the card leans with its movement (lag behind the goal)
            Vector2 lag = pos - goal;
            smoothLag = Vector2.Lerp(smoothLag, lag, 1f - Mathf.Exp(-25f * dt));
            float wantSwing = Mathf.Clamp((Dragging ? smoothLag.x : lag.x) * rotationPerPixel, -maxSwing, maxSwing);
            swing = Mathf.Lerp(swing, wantSwing, 1f - Mathf.Exp(-20f * dt));
            float baseAngle = held || Dragging ? 0f : restAngle;
            rt.localRotation = Quaternion.Euler(0f, 0f, baseAngle + swing);

            // 3. tilt (fake 3D, see CardPerspective): idle wobble, each card out of phase,
            //    plus a lean towards the pointer on hover - the point under the pointer is pressed in
            float t = Time.unscaledTime * idleSpeed + index * 1.7f;
            float idle = Hovering ? 0.2f : 1f;
            float tx = Mathf.Sin(t) * idleTilt * idle;
            float ty = Mathf.Cos(t * 0.9f) * idleTilt * idle;
            if (Hovering && !Dragging && Mouse.current != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, Mouse.current.position.ReadValue(), null, out Vector2 local))
            {
                tx += Mathf.Clamp(local.y * hoverTilt, -maxHoverTilt, maxHoverTilt);
                ty += Mathf.Clamp(-local.x * hoverTilt, -maxHoverTilt, maxHoverTilt);
            }
            if (Dragging)
            {
                // while dragged, the card leans back against its movement
                tx += Mathf.Clamp(smoothLag.y * 0.25f, -20f, 20f);
                ty += Mathf.Clamp(-smoothLag.x * 0.25f, -20f, 20f);
            }
            currentTilt = Vector2.Lerp(currentTilt, new Vector2(tx, ty), 1f - Mathf.Exp(-tiltSpeed * dt));
            perspective.SetTilt(currentTilt);

            // 4. idle float: a slow bob and sway, so the hand never stands fully still
            float bob = Hovering || Dragging ? 0f : Mathf.Sin(t * 1.3f) * idleBob;
            float sway = Hovering || Dragging ? 0f : Mathf.Sin(t * 0.8f + 1f) * idleSway;
            tilt.anchoredPosition = new Vector2(0f, Mathf.Lerp(tilt.anchoredPosition.y, bob, 1f - Mathf.Exp(-8f * dt)));
            tilt.localRotation = Quaternion.Euler(0f, 0f, sway);

            // 5. shadow drops further when the card is lifted
            Vector2 shadowGoal = Dragging ? shadowDrag : Hovering || selected ? shadowHover : shadowRest;
            shadow.anchoredPosition = Vector2.Lerp(shadow.anchoredPosition, shadowGoal, 1f - Mathf.Exp(-15f * dt));
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (Leaving || hand.AnyDragging) return;
            Hovering = true;
            shake.DOScale(hoverScale, scaleTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
            Punch(hoverPunch, 0.15f);
        }

        public void OnPointerExit(PointerEventData e)
        {
            Hovering = false;
            if (!Dragging && !Leaving)
                shake.DOScale(1f, scaleTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (Leaving || e.button != PointerEventData.InputButton.Left) return;
            hand.Click(index);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (Leaving) return;
            Dragging = true;
            transform.SetAsLastSibling();
            dragTarget = PointerLocal(e);
            shake.DOScale(dragScale, scaleTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
            hand.BeginDrag(index);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!Dragging) return;
            dragTarget = PointerLocal(e);
            hand.Dragging(e.position);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!Dragging) return;
            Dragging = false;
            shake.DOScale(Hovering ? hoverScale : 1f, scaleTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
            hand.EndDrag(index, e.position);
        }

        Vector2 PointerLocal(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out Vector2 local);
            return local;
        }
    }
}
