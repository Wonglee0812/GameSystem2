using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PokerTD.UI
{
    /// <summary>
    /// Item slots in the top-left corner as small cards, like the jokers in Balatro.
    /// Empty slots are faint outlines (built into the scene by "Poker TD/Build New HUD In Scene");
    /// the item cards appear at runtime. Hover a card for its text, click it to use the item.
    /// A blocked item (TDGame.ItemBlock) is greyed out; the item waiting for a hand pick is lifted.
    /// </summary>
    public class ItemCardBar : MonoBehaviour
    {
        public Vector2 cardSize = new Vector2(92f, 140f);
        public float step = 108f;
        public Vector2 origin = new Vector2(70f, -95f);

        [Header("Built in the scene")]
        public RectTransform area;
        public List<Image> slots = new List<Image>();

        readonly List<JuicyCard> cards = new List<JuicyCard>();
        readonly List<TDConfig.ItemType> shown = new List<TDConfig.ItemType>();

        void Start()
        {
            if (area == null) Build(TDGame.Instance.config.itemSlots);
        }

        /// <summary>Makes the area and the empty slot outlines. Works in Edit mode.</summary>
        public void Build(int count)
        {
            if (area != null) { if (Application.isPlaying) Destroy(area.gameObject); else DestroyImmediate(area.gameObject); }
            slots.Clear();
            area = new GameObject("ItemCards", typeof(RectTransform)).GetComponent<RectTransform>();
            area.SetParent(transform, false);
            area.anchorMin = area.anchorMax = area.pivot = new Vector2(0f, 1f);
            area.anchoredPosition = Vector2.zero;
            area.sizeDelta = new Vector2(500f, 220f);
            for (int i = 0; i < count; i++) AddSlot();
        }

        void AddSlot()
        {
            int i = slots.Count;
            var slot = UiKit.NewImage(area, "Slot" + i, UiKit.Panel, new Color(1f, 1f, 1f, 0.16f), true);
            slot.rectTransform.anchorMin = slot.rectTransform.anchorMax = new Vector2(0f, 1f);
            slot.rectTransform.sizeDelta = cardSize;
            slot.rectTransform.anchoredPosition = origin + Vector2.right * step * i;
            slot.transform.SetSiblingIndex(i);
            slots.Add(slot);
        }

        void Update()
        {
            var game = TDGame.Instance;
            bool show = game.Current == TDGame.Phase.Deploy || game.Current == TDGame.Phase.Wave;
            if (area.gameObject.activeSelf != show) area.gameObject.SetActive(show);
            if (!show) return;

            int n = game.config.itemSlots;
            while (slots.Count < n) AddSlot();
            while (cards.Count < slots.Count) { cards.Add(null); shown.Add(null); }

            for (int i = 0; i < n; i++)
            {
                var item = i < game.Items.Count ? game.Items[i] : null;
                if (item != shown[i])
                {
                    if (cards[i] != null) Destroy(cards[i].gameObject);
                    cards[i] = null;
                    shown[i] = item;
                    if (item != null) cards[i] = NewCard(i, item);
                }
                var card = cards[i];
                if (card == null) continue;
                string block = game.ItemBlock(i);
                bool picking = game.PickingHandForItem == i;
                card.SetTint(block == null ? Color.white : new Color(0.6f, 0.6f, 0.6f));
                card.lifted = picking;
                if (card.Hovering && Mouse.current != null)
                {
                    string sub = picking ? "<color=#FFD860>Click a hand in COMBOS on the right board (Esc cancels).</color>"
                               : block != null ? $"<color=#FF7060>{block}</color>" : item.description;
                    UiKit.ShowTip($"<b>{item.name.ToUpper()}</b>\n<size=85%>{sub}</size>", Mouse.current.position.ReadValue());
                }
            }
            if (game.PickingHandForItem >= 0 && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) game.CancelHandPick();
        }

        JuicyCard NewCard(int i, TDConfig.ItemType item)
        {
            var card = JuicyCard.Create(area, "Item" + i, cardSize, UiKit.ItemFace);
            card.Rect.anchorMin = card.Rect.anchorMax = new Vector2(0f, 1f);
            card.phase = i + 3;
            card.idleTilt = 5f;
            // sit on the slot, wherever the slot was moved in the scene
            card.restPosition = i < slots.Count ? slots[i].rectTransform.anchoredPosition : origin + Vector2.right * step * i;
            int index = i;
            card.onClick = () =>
            {
                TDGame.Instance.UseItem(index);
                UiKit.HideTip();
            };
            // the item name on a small dark tag under the card
            var tag = UiKit.NewImage(card.transform, "Tag", UiKit.Pill, new Color(0.15f, 0.12f, 0.12f, 0.92f), true);
            tag.rectTransform.sizeDelta = new Vector2(cardSize.x + 14f, 28f);
            tag.rectTransform.anchoredPosition = new Vector2(0f, -cardSize.y * 0.5f - 4f);
            var t = UiKit.NewText(tag.rectTransform, "Text", UiKit.Number, 16f, UiKit.Cream);
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
            t.text = item.name.ToUpper();
            t.enableAutoSizing = true; t.fontSizeMin = 10f; t.fontSizeMax = 16f;
            card.PopIn();
            return card;
        }
    }
}
