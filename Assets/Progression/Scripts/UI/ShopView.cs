using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PokerTD.UI
{
    /// <summary>The shop turn. Placeholder for now: the items come in step 5. Continue goes back to the board.</summary>
    public class ShopView : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text title;
        public Button continueButton;

        void Start() => continueButton.onClick.AddListener(() => TDGame.Instance.LeaveShop());

        void Update()
        {
            var game = TDGame.Instance;
            bool show = game.Current == TDGame.Phase.Shop && !BoardTransition.Instance.Busy;
            panel.SetActive(show);
            if (show) title.text = $"SHOP\n<size=50%>Ante {game.Ante} · wave {game.WaveInAnte} next   ·   {game.Gold} gold\n(items arrive in step 5)</size>";
        }
    }
}
