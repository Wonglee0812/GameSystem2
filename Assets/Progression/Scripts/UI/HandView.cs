using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PokerTD.UI
{
    /// <summary>
    /// The hand of cards at the bottom of the screen in the battle turn, fanned out like
    /// in Balatro. It mirrors TDGame.Hand: select cards and press Discard to swap them,
    /// or drag a card onto a tower to give it that card.
    /// </summary>
    public class HandView : MonoBehaviour
    {
        public RectTransform area;
        public CardView template;
        public Button discardButton;
        public TMP_Text discardLabel;
        public float spacing = 125f;
        public float fanAngle = 4f;

        readonly List<CardView> views = new List<CardView>();
        Button sortRank, sortSuit;
        Tower dropTarget;

        CardHand Hand => TDGame.Instance.Hand;

        void Start()
        {
            template.gameObject.SetActive(false);
            Hand.Changed += Rebuild;
            discardButton.onClick.AddListener(() => Hand.Discard());
            sortRank = SortButton("Sort: Rank", -40f, () => Hand.Sort(false));
            sortSuit = SortButton("Sort: Suit", -200f, () => Hand.Sort(true));
            Rebuild();
        }

        void OnDestroy()
        {
            if (TDGame.Instance != null) Hand.Changed -= Rebuild;
        }

        void Update()
        {
            var game = TDGame.Instance;
            bool show = game.Current == TDGame.Phase.Deploy && (BoardTransition.Instance == null || !BoardTransition.Instance.Busy);
            area.gameObject.SetActive(show);
            discardButton.gameObject.SetActive(show);
            sortRank.gameObject.SetActive(show);
            sortSuit.gameObject.SetActive(show);
            discardLabel.text = $"Discard  ({Hand.DiscardsLeft})";
            discardButton.interactable = Hand.DiscardsLeft > 0 && Hand.Selected.Count > 0;
        }

        /// <summary>A small copy of the discard button, in a row above it.</summary>
        Button SortButton(string label, float x, UnityEngine.Events.UnityAction action)
        {
            var b = Instantiate(discardButton, discardButton.transform.parent);
            b.name = label;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(action);
            b.interactable = true;
            var rt = (RectTransform)b.transform;
            var drt = (RectTransform)discardButton.transform;
            rt.sizeDelta = new Vector2(150f, 60f);
            rt.anchoredPosition = new Vector2(x, drt.anchoredPosition.y + drt.sizeDelta.y + 12f);
            var t = b.GetComponentInChildren<TMP_Text>();
            t.text = label; t.fontSize = 22f;
            ((RectTransform)t.transform).sizeDelta = rt.sizeDelta;
            return b;
        }

        void Rebuild()
        {
            var cards = Hand.Cards;
            while (views.Count < cards.Count)
            {
                var v = Instantiate(template, area);
                v.hand = this;
                views.Add(v);
            }
            for (int i = 0; i < views.Count; i++)
            {
                var v = views[i];
                bool used = i < cards.Count;
                v.gameObject.SetActive(used);
                if (!used) continue;
                float mid = i - (cards.Count - 1) / 2f;
                bool selected = Hand.Selected.Contains(i);
                v.index = i;
                v.restPosition = new Vector2(mid * spacing, -Mathf.Abs(mid) * Mathf.Abs(mid) * 4f + (selected ? 35f : 0f));
                v.restAngle = -mid * fanAngle;
                var rt = (RectTransform)v.transform;
                rt.anchoredPosition = v.restPosition;
                rt.localRotation = Quaternion.Euler(0f, 0f, v.restAngle);
                v.transform.SetSiblingIndex(i);
                v.Show(cards[i], selected);
            }
        }

        public void Click(int index) => Hand.Toggle(index);

        public void BeginDrag(int index) { }

        /// <summary>While dragging: light up the tower under the pointer.</summary>
        public void Dragging(Vector2 screen)
        {
            var t = TowerAt(screen);
            if (t == dropTarget) return;
            if (dropTarget != null) dropTarget.Highlight(false);
            dropTarget = t;
            if (dropTarget != null) dropTarget.Highlight(true);
        }

        public void EndDrag(int index, Vector2 screen)
        {
            if (dropTarget != null) dropTarget.Highlight(false);
            dropTarget = null;
            var tower = TowerAt(screen);
            if (tower != null) Hand.Assign(index, tower);
            else Rebuild();   // snap back
        }

        static Tower TowerAt(Vector2 screen)
        {
            var cam = Camera.main;
            if (cam == null) return null;
            Ray ray = cam.ScreenPointToRay(screen);
            if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) return null;
            var node = hit.collider.GetComponent<BuildNode>();
            return node != null ? node.tower : null;
        }
    }
}
