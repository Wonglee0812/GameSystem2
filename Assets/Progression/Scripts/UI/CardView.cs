using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PokerTD.UI
{
    /// <summary>
    /// One card in the hand. Click: select for discard. Drag onto a tower: give it the
    /// card. Let go anywhere else: the card snaps back. (Game feel such as tilt and
    /// bounce comes later, see Mix and Jam, "Recreating Balatro's Game Feel".)
    /// </summary>
    public class CardView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public TMP_Text rank;
        public TMP_Text suit;
        public Image face;

        [HideInInspector] public HandView hand;
        [HideInInspector] public int index;
        [HideInInspector] public Vector2 restPosition;
        [HideInInspector] public float restAngle;

        public void Show(Card card, bool selected)
        {
            var color = card.IsRed ? new Color(0.85f, 0.22f, 0.18f) : new Color(0.17f, 0.1f, 0.06f);
            rank.text = card.RankText;
            suit.text = card.SuitText;
            rank.color = suit.color = color;
            face.color = selected ? new Color(1f, 0.93f, 0.7f) : new Color(1f, 0.97f, 0.9f);
        }

        public void OnPointerClick(PointerEventData e) => hand.Click(index);

        public void OnBeginDrag(PointerEventData e)
        {
            transform.SetAsLastSibling();
            transform.localRotation = Quaternion.identity;
            hand.BeginDrag(index);
        }

        public void OnDrag(PointerEventData e)
        {
            var rt = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out Vector2 local);
            rt.anchoredPosition = local;
            hand.Dragging(e.position);
        }

        public void OnEndDrag(PointerEventData e) => hand.EndDrag(index, e.position);
    }
}
