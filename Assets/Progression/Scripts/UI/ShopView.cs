using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PokerTD.UI
{
    /// <summary>
    /// The shop turn: one row of random offers (buff machines, hand upgrades, extra slots,
    /// one-use items and rare hand unlocks), like the shop in Balatro. Reroll buys a new row;
    /// its price goes up each time and resets next shop turn. Continue goes back to the board.
    /// </summary>
    public class ShopView : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text title;
        public Button continueButton;

        readonly System.Collections.Generic.List<Button> offerButtons = new System.Collections.Generic.List<Button>();
        Button reroll;

        void Start()
        {
            var game = TDGame.Instance;
            int n = game.config.shopOffers;
            continueButton.onClick.AddListener(() => game.LeaveShop());
            var prt = (RectTransform)panel.transform;
            prt.sizeDelta = new Vector2(Mathf.Max(1100f, n * 280f + 60f), 440f);
            for (int i = 0; i < n; i++)
            {
                int index = i;
                var b = Instantiate(continueButton, panel.transform);
                b.name = "Offer" + i;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => game.BuyOffer(index));
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(260f, 150f);
                rt.anchoredPosition = new Vector2((i - (n - 1) / 2f) * 280f, -50f);
                var t = b.GetComponentInChildren<TMP_Text>();
                t.fontSize = 24f;
                var trt = (RectTransform)t.transform;
                trt.sizeDelta = rt.sizeDelta - new Vector2(16f, 10f);
                t.textWrappingMode = TextWrappingModes.Normal;
                offerButtons.Add(b);
            }
            reroll = Instantiate(continueButton, continueButton.transform.parent);
            reroll.name = "Reroll";
            reroll.onClick.RemoveAllListeners();
            reroll.onClick.AddListener(() => game.Reroll());
            var rrt = (RectTransform)reroll.transform;
            rrt.anchoredPosition = ((RectTransform)continueButton.transform).anchoredPosition + new Vector2(-340f, 0f);
        }

        void Update()
        {
            var game = TDGame.Instance;
            bool show = game.Current == TDGame.Phase.Shop && !BoardTransition.Instance.Busy;
            panel.SetActive(show);
            if (!show) return;
            title.text = $"SHOP\n<size=45%>Ante {game.Ante} · wave {game.WaveInAnte} next   ·   {game.Gold} gold   ·   towers {game.TowerLimit}   ·   machines placed {BuffMachine.All.Count}/{game.MachineLimit} + {game.MachineStock.Count} in stock   ·   items {game.Items.Count}/{game.config.itemSlots}</size>";
            for (int i = 0; i < offerButtons.Count; i++)
            {
                bool has = i < game.Offers.Count;
                offerButtons[i].gameObject.SetActive(has);
                if (!has) continue;
                var offer = game.Offers[i];
                int cost = offer.Cost(game);
                offerButtons[i].interactable = game.CanBuy(offer);
                offerButtons[i].image.color = Color.Lerp(offer.Color, Color.white, 0.6f);
                string extra = offer.kind == OfferKind.HandLevel ? $"  (now lvl {game.Level(offer.hand)})" : "";
                string full = offer.kind == OfferKind.Item && game.ItemSlotsFull ? "\n<color=#C03020>item slots full</color>" : "";
                offerButtons[i].GetComponentInChildren<TMP_Text>().text = $"<b>{offer.Title}</b>{extra}\n<size=70%>{offer.Label}\n{cost} gold</size>{full}";
            }
            reroll.interactable = game.Gold >= game.RerollCost;
            reroll.GetComponentInChildren<TMP_Text>().text = $"Reroll  ({game.RerollCost} gold)";
        }
    }
}
