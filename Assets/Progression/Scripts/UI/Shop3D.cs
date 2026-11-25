using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PokerTD.UI
{
    /// <summary>
    /// The shop turn on the 3D shop floor, in zones like the stations in Unrailed (Wong, 2026-11-24):
    /// MACHINES (square pedestals with the machine model on top), ITEMS and UPGRADES (cards lying on
    /// a mat, like the item mat), and 3D REROLL / CONTINUE buttons. Hover = the shared tooltip,
    /// click = buy. Everything appears and goes away with the dissolve, the signature effect.
    /// The zones, pedestals and buttons are built into the scene by "Poker TD/Build 3D Shop"; this
    /// script only fills the slots with the offers of TDGame.
    /// </summary>
    public class Shop3D : MonoBehaviour
    {
        public static Shop3D Instance { get; private set; }

        [Header("Slots (filled by the builder; move them freely)")]
        public Transform[] machineSlots;
        public Transform[] itemSlots;
        public Transform[] upgradeSlots;
        [Header("Buttons (a collider and a TMP label each)")]
        public Transform rerollButton;
        public Transform continueButton;
        public TextMeshPro rerollLabel;

        [Header("Look")]
        public float cardWidth = 2.1f;
        public float machineScale = 1.15f;
        public float dissolveTime = 0.5f;
        [Tooltip("Height of the top of the machine pedestals above the slot.")]
        public float pedestalTop = 0.8f;

        class View
        {
            public ShopOffer offer;
            public Transform root, lift;
            public BoxCollider box;
            public TextMeshPro price, name;
            public WorldCard card;
            public Transform model;
        }

        readonly Dictionary<Transform, View> views = new Dictionary<Transform, View>();
        View hovered;
        Transform hoveredButton;
        bool tipShown;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnEnable()
        {
            // a new shop turn: the old offers are gone (the shop was hidden), start clean
            foreach (var v in views.Values) if (v != null && v.root != null) Destroy(v.root.gameObject);
            views.Clear();
            hovered = null;
        }

        void OnDisable()
        {
            if (tipShown) { UiKit.HideTip(); tipShown = false; }
        }

        void Update()
        {
            var game = TDGame.Instance;
            if (game == null || game.Current != TDGame.Phase.Shop) return;
            bool moving = BoardTransition.Instance != null && BoardTransition.Instance.Busy;

            Fill(machineSlots, OfferKind.Machine, game, moving);
            Fill(itemSlots, OfferKind.Item, game, moving);
            Fill(upgradeSlots, null, game, moving);
            UpdatePrices(game);
            if (rerollLabel != null) rerollLabel.text = $"REROLL  <color=#{(game.Gold >= game.RerollCost ? "FFC640" : "FF6050")}>${game.RerollCost}</color>";
            if (moving) { Hover(null, null); return; }
            Pointer(game);
        }

        // ---------- slots ----------

        static bool InZone(ShopOffer o, OfferKind? kind) =>
            kind.HasValue ? o.kind == kind.Value : o.kind != OfferKind.Machine && o.kind != OfferKind.Item;

        /// <summary>Keeps each offer in its own slot (no shifting when one is bought); new offers take empty slots.</summary>
        void Fill(Transform[] slots, OfferKind? kind, TDGame game, bool moving)
        {
            if (slots == null) return;
            var shown = new HashSet<ShopOffer>();
            foreach (var slot in slots)
            {
                if (slot == null || !views.TryGetValue(slot, out View have) || have == null) continue;
                if (game.Offers.Contains(have.offer)) { shown.Add(have.offer); continue; }
                Leave(have, false);                       // rerolled away
                views[slot] = null;
            }
            foreach (var o in game.Offers)
            {
                if (!InZone(o, kind) || shown.Contains(o)) continue;
                foreach (var slot in slots)
                {
                    if (slot == null || (views.TryGetValue(slot, out View v) && v != null)) continue;
                    views[slot] = Create(slot, o, !moving);
                    break;
                }
            }
        }

        View Create(Transform slot, ShopOffer offer, bool animate)
        {
            var v = new View { offer = offer };
            v.root = new GameObject("Offer_" + offer.Title).transform;
            v.root.SetParent(slot, false);
            v.lift = new GameObject("Lift").transform;
            v.lift.SetParent(v.root, false);
            float h = cardWidth * 400f / 264f;

            if (offer.kind == OfferKind.Machine)
            {
                var art = PTDArt.I;
                if (art != null)
                {
                    var m = new GameObject("Model").transform;
                    m.SetParent(v.lift, false);
                    m.localPosition = Vector3.up * pedestalTop;
                    m.localScale = Vector3.one * machineScale;
                    float y = PTDArt.Stack(m, 0f, 1f, art.machineBase);
                    PTDArt.Stack(m, y, 0.9f, art.machineCrystals, out Transform crystals);
                    PTDArt.Tint(crystals, Color.Lerp(offer.machine.color, Color.white, 0.25f));
                    v.model = m;
                    if (animate) ModelDissolve.Run(m.gameObject, 1f, 0f, dissolveTime, false);
                }
                v.box = v.root.gameObject.AddComponent<BoxCollider>();
                v.box.center = new Vector3(0f, pedestalTop * 0.5f + 1.1f, 0f);
                v.box.size = new Vector3(1.8f, pedestalTop + 2.4f, 1.8f);
                v.name = Label(v.root, offer.machine.name.ToUpper(), new Vector3(0f, 0.02f, -1.35f), 3.2f, UiKit.Cream);
                v.price = Label(v.root, "", new Vector3(0f, 0.02f, -1.95f), 4.2f, UiKit.Gold);
            }
            else
            {
                v.card = WorldCard.Create(v.lift, new Vector3(0f, 0.05f, 0f), cardWidth, 6);
                v.card.Billboard = false;
                v.card.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                v.card.SetSprite(Face(offer));
                if (animate) v.card.DissolveIn(dissolveTime);
                string onCard = CardName(offer);
                if (onCard != null)
                {
                    var t = Label(v.card.transform, onCard, new Vector3(0f, -h * 0.26f, -0.02f), 3.0f, new Color(0.18f, 0.18f, 0.18f));
                    t.transform.localRotation = Quaternion.identity;
                    t.fontSharedMaterial = t.font.material;   // no outline on the white card
                    t.rectTransform.sizeDelta = new Vector2(cardWidth * 0.95f, 0.5f);
                    t.enableAutoSizing = true; t.fontSizeMin = 1.6f; t.fontSizeMax = 3.0f;
                    v.name = t;
                }
                v.box = v.root.gameObject.AddComponent<BoxCollider>();
                v.box.center = new Vector3(0f, 0.1f, 0f);
                v.box.size = new Vector3(cardWidth, 0.3f, h);
                v.price = Label(v.root, "", new Vector3(0f, 0.02f, -h * 0.5f - 0.45f), 4.2f, UiKit.Gold);
            }
            if (animate)
            {
                v.lift.localScale = Vector3.one * 0.85f;
                v.lift.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetLink(v.root.gameObject);
            }
            return v;
        }

        static TextMeshPro Label(Transform parent, string text, Vector3 pos, float size, Color color)
        {
            var t = new GameObject("Label").AddComponent<TextMeshPro>();
            t.transform.SetParent(parent, false);
            t.transform.localPosition = pos;
            t.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // lying on the floor
            UiKit.SetFont(t, UiKit.Number, 0.2f);
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = new Vector2(4f, 0.8f);
            t.text = text;
            t.sortingOrder = 8;
            return t;
        }

        static Sprite Face(ShopOffer o)
        {
            switch (o.kind)
            {
                case OfferKind.Item: return UiKit.ItemFace;
                case OfferKind.HandLevel: return UiKit.Sprite("BuildCards/Upgrade_Hand");
                case OfferKind.TowerSlot: return UiKit.Sprite("BuildCards/Upgrade_TowerSlot");
                case OfferKind.MachineSlot: return UiKit.Sprite("BuildCards/Upgrade_MachineSlot");
                default: return UiKit.Sprite("BuildCards/Upgrade_Unlock");
            }
        }

        /// <summary>Text printed on the card's name line (null when the face already has it).</summary>
        static string CardName(ShopOffer o)
        {
            switch (o.kind)
            {
                case OfferKind.Item: return o.item.name.ToUpper();
                case OfferKind.HandLevel: return HandEvaluator.Name(o.hand).ToUpper() + " +1";
                case OfferKind.Unlock: return HandEvaluator.Name(o.hand).ToUpper();
                default: return null;
            }
        }

        void UpdatePrices(TDGame game)
        {
            foreach (var v in views.Values)
            {
                if (v == null || v.price == null) continue;
                int cost = v.offer.Cost(game);
                bool ok = game.CanBuy(v.offer);
                v.price.text = $"${cost}";
                v.price.color = ok ? UiKit.Gold : new Color(1f, 0.38f, 0.3f);
            }
        }

        /// <summary>The offer goes away: bought (flies up a little) or replaced (just burns away).</summary>
        void Leave(View v, bool bought)
        {
            if (v == null || v.root == null) return;
            if (hovered == v) hovered = null;
            if (v.box != null) Destroy(v.box);
            var root = v.root;
            root.SetParent(transform, true);
            if (bought) root.DOMoveY(root.position.y + 1.2f, dissolveTime).SetEase(Ease.OutQuad).SetLink(root.gameObject);
            if (v.card != null) v.card.DissolveOut(false, dissolveTime);
            if (v.model != null) ModelDissolve.Run(v.model.gameObject, 0f, 1f, dissolveTime, false);
            foreach (var t in root.GetComponentsInChildren<TextMeshPro>())
                DOTween.To(() => t.alpha, a => t.alpha = a, 0f, dissolveTime * 0.6f).SetLink(t.gameObject);
            Destroy(root.gameObject, dissolveTime + 0.1f);
        }

        // ---------- pointer ----------

        void Pointer(TDGame game)
        {
            var cam = Camera.main;
            var mouse = Mouse.current;
            if (cam == null || mouse == null) return;
            bool overUi = UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            View hitView = null;
            Transform hitButton = null;
            if (!overUi && Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hit, 200f))
            {
                foreach (var v in views.Values)
                    if (v != null && v.box != null && hit.collider == v.box) { hitView = v; break; }
                if (hitView == null && rerollButton != null && hit.collider.transform.IsChildOf(rerollButton)) hitButton = rerollButton;
                if (hitView == null && continueButton != null && hit.collider.transform.IsChildOf(continueButton)) hitButton = continueButton;
            }
            Hover(hitView, hitButton);

            if (hitView != null)
            {
                UiKit.ShowTip(Tip(hitView.offer, game), mouse.position.ReadValue());
                tipShown = true;
            }
            else if (tipShown) { UiKit.HideTip(); tipShown = false; }

            if (!CameraDragPan.Clicked) return;
            if (hitView != null) Buy(hitView, game);
            else if (hitButton == rerollButton)
            {
                if (game.Reroll()) { Juice.Shake(0.08f); Press(rerollButton); }
                else Deny(rerollButton);
            }
            else if (hitButton == continueButton) { Press(continueButton); game.LeaveShop(); }
        }

        void Hover(View v, Transform button)
        {
            if (hovered != v)
            {
                if (hovered != null && hovered.lift != null) hovered.lift.DOLocalMoveY(0f, 0.15f).SetLink(hovered.root.gameObject);
                hovered = v;
                if (v != null && v.lift != null)
                {
                    v.lift.DOLocalMoveY(0.35f, 0.15f).SetEase(Ease.OutBack).SetLink(v.root.gameObject);
                    if (v.card != null) v.card.Punch(0.5f);
                }
            }
            if (hoveredButton != button)
            {
                if (hoveredButton != null) hoveredButton.DOScale(1f, 0.12f).SetLink(hoveredButton.gameObject);
                hoveredButton = button;
                if (button != null) button.DOScale(1.08f, 0.12f).SetEase(Ease.OutBack).SetLink(button.gameObject);
            }
        }

        void Buy(View v, TDGame game)
        {
            int index = game.Offers.IndexOf(v.offer);
            if (index < 0) return;
            if (!game.CanBuy(v.offer) || !game.BuyOffer(index)) { Deny(v.lift); return; }
            // find and clear the slot, then let the offer burn away
            Transform key = null;
            foreach (var kv in views) if (kv.Value == v) { key = kv.Key; break; }
            if (key != null) views[key] = null;
            Leave(v, true);
            Juice.Shake(0.06f);
            FloatText.Show($"-${v.offer.Cost(game)}", v.root.position + Vector3.up * 2f, UiKit.Gold);
            if (tipShown) { UiKit.HideTip(); tipShown = false; }
        }

        static void Deny(Transform t)
        {
            if (t == null) return;
            t.DOComplete();
            t.DOShakePosition(0.3f, new Vector3(0.2f, 0f, 0f), 30, 0f).SetLink(t.gameObject);
        }

        static void Press(Transform t)
        {
            if (t == null) return;
            t.DOComplete();
            t.DOPunchScale(Vector3.one * -0.12f, 0.2f, 6, 0.6f).SetLink(t.gameObject);
        }

        static string Tip(ShopOffer o, TDGame game)
        {
            int cost = o.Cost(game);
            string head = $"<b>{Title(o)}</b>  <color=#FFC640>${cost}</color>\n<size=80%>";
            string body;
            switch (o.kind)
            {
                case OfferKind.Machine: body = $"Buff machine: {o.machine.Short} for the towers in its rings. Goes to your stock; place it on the board."; break;
                case OfferKind.Item: body = o.item.description + ". One use."; break;
                case OfferKind.HandLevel:
                    int lvl = game.Level(o.hand);
                    body = $"{HandEvaluator.Name(o.hand)}: level {lvl} -> {lvl + 1}. More chips for this hand."; break;
                case OfferKind.TowerSlot: body = "One more tower on the map at the same time."; break;
                case OfferKind.MachineSlot: body = "One more buff machine on the map at the same time."; break;
                default: body = $"{HandEvaluator.Name(o.hand)} counts as itself, not as High Card."; break;
            }
            string why = "";
            if (game.Gold < cost) why = "\n<color=#FF7060>Not enough gold.</color>";
            else if (o.kind == OfferKind.Item && game.ItemSlotsFull) why = "\n<color=#FF7060>Item slots are full.</color>";
            else why = "\n<color=#BBBBBB>Click to buy.</color>";
            return head + body + why + "</size>";
        }

        static string Title(ShopOffer o) =>
            o.kind == OfferKind.Machine ? o.machine.name.ToUpper() + " MACHINE" : o.Title.ToUpper();
    }
}
