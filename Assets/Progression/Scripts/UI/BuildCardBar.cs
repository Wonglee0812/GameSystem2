using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PokerTD.UI
{
    /// <summary>
    /// Bottom-left corner in the build turn: a card for every tower type and for every bought
    /// machine that is not placed yet, on a black corner fade. Click a card to pick it (same as
    /// the number keys), then click a tile. Right-click a machine card to sell it.
    /// A price tag under each card shows the cost (red when the player cannot build it).
    /// The fade, the area and the tower cards are built into the scene by "Poker TD/Build New HUD
    /// In Scene" (Build); machine cards appear at runtime. In Play the cards are placed from
    /// 'origin' and 'step', so move the BuildCards area or change those values to move them.
    /// </summary>
    public class BuildCardBar : MonoBehaviour
    {
        public Vector2 cardSize = new Vector2(112f, 170f);
        public float maxWidth = 380f;
        public float step = 100f;
        public Vector2 origin = new Vector2(95f, 128f);

        [Serializable]
        public class Entry
        {
            public JuicyCard card;
            public TextMeshProUGUI tag;
            public Image tagBg;
            [NonSerialized] public TDConfig.BuffType buff;
        }

        [Header("Zone (Wong's TowerArea): cards as tall as the hand cards, price tag on the lower edge")]
        public RectTransform zone;

        [Header("Built in the scene")]
        public RectTransform area;
        public Image fade;
        public List<Entry> towers = new List<Entry>();

        readonly List<Entry> machines = new List<Entry>();
        bool wasShown;

        void Start()
        {
            var config = TDGame.Instance.config;
            if (area == null) Build(config);
            FitToZone();
            for (int i = 0; i < towers.Count; i++)
            {
                int index = i;
                towers[i].card.onClick = () => BuildManager.Instance.Select(index);
            }
        }

        /// <summary>Moves the card area into the zone and sizes the cards to its height (room for the price tags).</summary>
        public void FitToZone()
        {
            if (zone == null) return;
            if (fade != null) fade.gameObject.SetActive(false);   // the zone has its own background
            area.SetParent(zone, false);
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
            area.pivot = Vector2.zero;
            area.offsetMin = area.offsetMax = Vector2.zero;
            float zh = zone.rect.height, zw = zone.rect.width;
            // same card height and centre line as the hand (HandView.cardHeightInZone), so both rows line up
            var hand = GetComponent<HandView>();
            float h = zh * (hand != null ? hand.cardHeightInZone : 0.9f);
            cardSize = new Vector2(h * 264f / 400f, h);
            origin = new Vector2(cardSize.x * 0.5f + 16f, zh * 0.5f);
            maxWidth = Mathf.Max(0f, zw - cardSize.x - 32f);
            step = cardSize.x * 0.92f;
            foreach (var e in towers) ResizeEntry(e);
        }

        void ResizeEntry(Entry e)
        {
            e.card.Rect.sizeDelta = cardSize;
            e.tagBg.rectTransform.anchoredPosition = new Vector2(0f, -cardSize.y * 0.5f + 8f);   // on the card's lower edge
            e.tagBg.rectTransform.sizeDelta = new Vector2(Mathf.Min(84f, cardSize.x * 0.85f), 30f);
        }

        /// <summary>Makes the fade, the area and one card per tower type. Works in Edit mode.</summary>
        public void Build(TDConfig config)
        {
            Kill(fade != null ? fade.gameObject : null);
            Kill(area != null ? area.gameObject : null);
            towers.Clear();

            fade = UiKit.NewImage(transform, "BuildFade", UiKit.Sprite("UI/FadeCorner"), Color.white);
            var f = fade.rectTransform;
            f.anchorMin = f.anchorMax = f.pivot = Vector2.zero;
            f.anchoredPosition = Vector2.zero;
            f.sizeDelta = new Vector2(820f, 410f);
            f.SetAsFirstSibling();

            area = new GameObject("BuildCards", typeof(RectTransform)).GetComponent<RectTransform>();
            area.SetParent(transform, false);
            area.anchorMin = area.anchorMax = area.pivot = Vector2.zero;
            area.anchoredPosition = Vector2.zero;
            area.sizeDelta = new Vector2(520f, 320f);

            for (int i = 0; i < config.towers.Length; i++)
                towers.Add(NewEntry("Tower_" + config.towers[i].name, UiKit.TowerFace(config.towers[i].name)));
            for (int i = 0; i < towers.Count; i++) towers[i].tag.text = $"${config.towers[i].cost}";
            Layout(false);
            foreach (var e in towers) e.card.Rect.anchoredPosition = e.card.restPosition;   // Edit mode: no animation
        }

        static void Kill(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        Entry NewEntry(string name, Sprite face)
        {
            var e = new Entry { card = JuicyCard.Create(area, name, cardSize, face) };
            e.card.Rect.anchorMin = e.card.Rect.anchorMax = Vector2.zero;   // positions are from the bottom-left corner
            e.tagBg = UiKit.NewImage(e.card.transform, "Tag", UiKit.Pill, UiKit.Gold, true);
            var r = e.tagBg.rectTransform;
            r.sizeDelta = new Vector2(84f, 34f);
            r.anchoredPosition = new Vector2(0f, -cardSize.y * 0.5f - 6f);
            e.tag = UiKit.NewText(r, "Text", UiKit.Number, 22f, UiKit.Ink);
            e.tag.rectTransform.anchorMin = Vector2.zero; e.tag.rectTransform.anchorMax = Vector2.one;
            e.tag.rectTransform.offsetMin = e.tag.rectTransform.offsetMax = Vector2.zero;
            return e;
        }

        void Update()
        {
            var game = TDGame.Instance;
            bool show = game.Current == TDGame.Phase.Deploy && (BoardTransition.Instance == null || !BoardTransition.Instance.Busy);
            if (area.gameObject.activeSelf != show) { area.gameObject.SetActive(show); fade.gameObject.SetActive(show); }
            if (!show) { wasShown = false; return; }

            SyncMachines(game);
            var config = game.config;
            int selected = BuildManager.Instance.Selected;
            bool limit = Tower.All.Count >= game.TowerLimit;
            for (int i = 0; i < towers.Count && i < config.towers.Length; i++)
            {
                var t = config.towers[i];
                bool ok = !limit && game.Gold >= t.cost;
                towers[i].tag.text = limit ? "FULL" : $"${t.cost}";
                towers[i].tagBg.color = ok ? UiKit.Gold : UiKit.Red;
                towers[i].card.SetTint(ok ? Color.white : new Color(0.75f, 0.75f, 0.75f));
                towers[i].card.lifted = selected == i;
                if (towers[i].card.Hovering && Mouse.current != null)
                    UiKit.ShowTip($"<b>{t.name.ToUpper()}</b>  <color=#FFC640>${t.cost}</color>\n<size=80%>Key {i + 1}. Click, then click a {(t.onPath ? "path" : "grass")} tile.{(limit ? "\n<color=#FF7060>Tower limit reached.</color>" : "")}</size>",
                                  Mouse.current.position.ReadValue());
            }
            for (int i = 0; i < machines.Count; i++)
            {
                var e = machines[i];
                bool ok = game.CanPlaceMachine;
                e.tag.text = ok ? "FREE" : "FULL";
                e.tagBg.color = ok ? UiKit.Green : UiKit.Red;
                e.card.lifted = selected == config.towers.Length + i;
                if (e.card.Hovering && Mouse.current != null)
                    UiKit.ShowTip($"<b>{e.buff.name.ToUpper()} MACHINE</b>\n<size=80%>{e.buff.Short}. Key {config.towers.Length + i + 1}. Click, then click a grass tile.\nRight-click: sell for ${game.SellValue(e.buff)}.</size>",
                                  Mouse.current.position.ReadValue());
            }
            Layout(!wasShown);
            wasShown = true;
        }

        void SyncMachines(TDGame game)
        {
            var stock = game.MachineStock;
            // remove cards whose machine was placed or sold
            for (int i = machines.Count - 1; i >= 0; i--)
                if (i >= stock.Count || machines[i].buff != stock[i]) { Destroy(machines[i].card.gameObject); machines.RemoveAt(i); }
            for (int i = machines.Count; i < stock.Count; i++)
            {
                int index = i;
                int buffIndex = Array.IndexOf(game.config.buffs, stock[i]);
                var e = NewEntry("Machine_" + i, UiKit.MachineFace(buffIndex));
                if (zone != null) ResizeEntry(e);
                e.buff = stock[i];
                e.card.onClick = () => BuildManager.Instance.Select(game.config.towers.Length + index);
                e.card.onRightClick = () => game.SellStock(index);
                machines.Add(e);
                e.card.phase = towers.Count + i;
                e.card.restPosition = origin + Vector2.right * step * (towers.Count + i);
                e.card.PopIn();
            }
        }

        void Layout(bool pop)
        {
            int n = towers.Count + machines.Count;
            float s = n > 1 ? Mathf.Min(step, maxWidth / (n - 1)) : 0f;
            for (int i = 0; i < n; i++)
            {
                var card = i < towers.Count ? towers[i].card : machines[i - towers.Count].card;
                float mid = i - (n - 1) / 2f;
                card.phase = i;
                float edge = (n - 1) / 2f;   // same centred curve as the hand
                card.restPosition = origin + new Vector2(i * s, -mid * mid * 2.5f + edge * edge * 2.5f * 0.5f);
                card.restAngle = -mid * 3f;
                card.transform.SetSiblingIndex(i);
                if (pop) card.PopIn(i * 0.06f);
            }
            // the hovered card is drawn on top of its neighbours
            foreach (var e in towers) if (e.card.Hovering) e.card.transform.SetAsLastSibling();
            foreach (var e in machines) if (e.card.Hovering) e.card.transform.SetAsLastSibling();
        }
    }
}
