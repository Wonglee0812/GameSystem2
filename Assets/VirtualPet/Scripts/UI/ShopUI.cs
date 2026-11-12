using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace WindPet.UI
{
    /// <summary>
    /// The shop on the pachinko machine's round screen. Click the screen to open it.
    /// It sells the next turbine level and the potions from WorkConfig. A bought potion
    /// is a copy of its shelf bottle and goes straight into the item slot.
    /// The footer shows running potion buffs and the open loan.
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        [System.Serializable]
        public class PotionRow
        {
            public Image icon;
            public TMP_Text title;
            public TMP_Text effect;
            public Button buy;
            public TMP_Text price;
        }

        public WorkConfig config;
        public Wallet wallet;
        public PetStats stats;
        public GameObject panel;
        public Button blocker;
        public Button closeButton;
        public TMP_Text turbineInfo;
        public Button upgradeButton;
        public TMP_Text upgradeLabel;
        public PotionRow[] rows;
        public TMP_Text footer;
        [Tooltip("The pachinko machine and the centre and radius of its round screen.")]
        public Transform pachinko;
        public Vector3 screenCentre;
        public float screenRadius = 0.9f;
        [Tooltip("Where bought potions appear if they are put down.")]
        public Transform tray;

        public bool IsOpen => panel != null && panel.activeSelf;

        void Start()
        {
            panel.SetActive(false);
            blocker.onClick.AddListener(Close);
            closeButton.onClick.AddListener(Close);
            upgradeButton.onClick.AddListener(BuyUpgrade);
            for (int i = 0; i < rows.Length; i++)
            {
                int index = i;
                rows[i].buy.onClick.AddListener(() => BuyPotion(index));
            }
        }

        void Update()
        {
            if (IsOpen)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
                Refresh();
                return;
            }
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame || pachinko == null) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (ItemGrabber.Instance != null && ItemGrabber.Instance.Hovered != null) return;
            Ray ray = Camera.main.ScreenPointToRay(pointer.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore) || !hit.collider.transform.IsChildOf(pachinko)) return;
            Vector3 d = hit.point - screenCentre;
            d.z = 0f;   // the screen faces the camera, so only x and y matter
            if (d.magnitude <= screenRadius) Open();
        }

        public void Open()
        {
            panel.SetActive(true);
            Refresh();
        }

        public void Close() => panel.SetActive(false);

        void Refresh()
        {
            var t = Turbine.Instance;
            if (t != null)
            {
                turbineInfo.text = $"Turbine  <b>Lv {t.Level}/{config.maxTurbineLevel}</b>\n<size=75%>output x{t.OutputMultiplier:0.##}  ·  bills up to x{t.LevelMultiplier:0.##}</size>";
                upgradeLabel.text = t.MaxLevel ? "Max level" : $"Upgrade  €{t.UpgradeCost:0}";
                upgradeButton.interactable = !t.MaxLevel && wallet.Money >= t.UpgradeCost;
            }
            for (int i = 0; i < rows.Length && i < config.potions.Length; i++)
            {
                var def = config.potions[i];
                rows[i].title.text = def.name;
                rows[i].effect.text = def.Effect;
                float price = config.PotionPrice(def);
                rows[i].price.text = "€" + price.ToString("0");
                rows[i].buy.interactable = wallet.Money >= price;
            }
            string text = "";
            foreach (PetBar bar in System.Enum.GetValues(typeof(PetBar)))
            {
                float left = stats.BuffLeft(bar);
                if (left > 0f) text += $"{bar} x{stats.BuffMultiplier(bar):0.#} ({left:0} s)   ";
            }
            var office = ContractOffice.Instance;
            if (office != null && office.Strikes > 0) text += $"Fines: {office.Strikes}/{config.strikesBeforeBigFine}   ";
            var bank = LoanOffice.Instance;
            if (bank != null && bank.Debt > 0f) text += $"<color=#B03A2E>Loan to repay: €{bank.Debt:0.00}</color>";
            footer.text = text == "" ? "No potions active" : text;
        }

        void BuyUpgrade()
        {
            var t = Turbine.Instance;
            if (t == null || !t.TryUpgrade()) ToolBelt.Instance?.Shake();
        }

        void BuyPotion(int index)
        {
            var def = config.potions[index];
            var src = GameObject.Find(def.model);
            if (src == null || !wallet.TrySpend(config.PotionPrice(def))) { ToolBelt.Instance?.Shake(); return; }

            // a fresh bottle that looks like the shelf one
            Vector3 at = tray != null ? tray.position + Vector3.up * 0.6f : src.transform.position;
            var copy = Instantiate(src, at, src.transform.rotation);
            copy.name = "Potion " + def.name;
            var old = copy.GetComponent<HoldableItem>();
            Sprite icon = old != null ? old.icon : null;
            if (old != null) DestroyImmediate(old);
            var potion = copy.AddComponent<PotionItem>();
            potion.def = def;
            potion.displayName = def.name;
            potion.icon = icon;
            var rb = copy.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                PropViewportKeeper.Instance?.Register(rb);
            }
            ToolBelt.Instance?.Equip(potion);
            Close();
        }
    }
}
