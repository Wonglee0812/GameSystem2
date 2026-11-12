using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PokerTD.UI
{
    /// <summary>
    /// The shop turn: random buff machines for sale (like jokers in Balatro). A bought
    /// machine waits in the stock and is placed on the board in the Deploy phase.
    /// Hand levels, tower slots and items come later in step 5. Continue goes back to the board.
    /// </summary>
    public class ShopView : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text title;
        public Button continueButton;

        readonly System.Collections.Generic.List<Button> offerButtons = new System.Collections.Generic.List<Button>();

        void Start()
        {
            continueButton.onClick.AddListener(() => TDGame.Instance.LeaveShop());
            var prt = (RectTransform)panel.transform;
            prt.sizeDelta = new Vector2(1100f, 420f);
            for (int i = 0; i < TDGame.Instance.config.shopMachineOffers; i++)
            {
                int index = i;
                var b = Instantiate(continueButton, panel.transform);
                b.name = "Offer" + i;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => TDGame.Instance.BuyOffer(index));
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(300f, 120f);
                rt.anchoredPosition = new Vector2((i - (TDGame.Instance.config.shopMachineOffers - 1) / 2f) * 330f, -40f);
                offerButtons.Add(b);
            }
        }

        void Update()
        {
            var game = TDGame.Instance;
            bool show = game.Current == TDGame.Phase.Shop && !BoardTransition.Instance.Busy;
            panel.SetActive(show);
            if (!show) return;
            title.text = $"SHOP\n<size=50%>Ante {game.Ante} · wave {game.WaveInAnte} next   ·   {game.Gold} gold   ·   machines {game.MachinesOwned}/{game.MachineLimit}</size>";
            for (int i = 0; i < offerButtons.Count; i++)
            {
                bool has = i < game.Offers.Count;
                offerButtons[i].gameObject.SetActive(has);
                if (!has) continue;
                var buff = game.Offers[i];
                bool can = game.Gold >= buff.cost && game.MachinesOwned < game.MachineLimit;
                offerButtons[i].interactable = can;
                offerButtons[i].image.color = Color.Lerp(buff.color, Color.white, 0.6f);
                offerButtons[i].GetComponentInChildren<TMP_Text>().text = $"<b>{buff.Short}</b>\n<size=70%>buff machine · {buff.cost} gold</size>";
            }
        }
    }
}
