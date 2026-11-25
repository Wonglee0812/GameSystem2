using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PokerTD
{
    /// <summary>
    /// Placing towers and buff machines in the Deploy phase: pick a tower type (keys 1-3)
    /// or a machine bought in the shop (keys 4+), then click a free tile. Machines are
    /// placed for free (they were paid in the shop). Right-click a tower or a machine to sell it
    /// (config.sellBack of its cost); a sold tower gives its card back to the hand. Traps go on the path, the
    /// others next to it.
    /// Clicking a tower (with nothing selected) takes its card back into the hand.
    /// Rings: while a machine type is selected, all machine rings are shown and the new
    /// machine's rings follow the pointer. Hovering a machine shows its rings; hovering a
    /// tower shows the rings of the machines that reach it (the HUD shows the calculation).
    /// </summary>
    public class BuildManager : MonoBehaviour
    {
        public static BuildManager Instance { get; private set; }
        public TDConfig config;
        public Material material;
        public LayerMask nodeLayers = ~0;

        /// <summary>Index into towers (0..T-1), then the machine stock (T..), or -1 for none.</summary>
        public int Selected { get; private set; } = -1;
        public bool MachineSelected => Selected >= config.towers.Length;
        int StockIndex => Selected - config.towers.Length;

        /// <summary>Tower under the pointer (for the HUD calculation panel), or null.</summary>
        public Tower HoveredTower => hovered != null ? hovered.tower : null;

        BuildNode hovered;
        RangeRing preview;      // range of the selected tower type, under the pointer
        RingGuide ringPreview;  // rings of the selected machine type, under the pointer

        void Awake() => Instance = this;

        public void Select(int index)
        {
            Selected = Selected == index ? -1 : index;
            // one selection at a time: picking a tower/machine card drops the selected poker cards (Wong, 2026-11-24)
            if (Selected >= 0 && TDGame.Instance != null) TDGame.Instance.Hand.Selected.Clear();
        }

        /// <summary>No tower/machine card selected (a poker card was picked).</summary>
        public void Deselect() => Selected = -1;

        static readonly Key[] DigitKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };

        // left clicks on the board count on release without a drag (CameraDragPan), so dragging the camera never builds
        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                int options = Mathf.Min(DigitKeys.Length, config.towers.Length + TDGame.Instance.MachineStock.Count);
                for (int i = 0; i < options; i++)
                    if (kb[DigitKeys[i]].wasPressedThisFrame) Select(i);
                if (kb.escapeKey.wasPressedThisFrame) Selected = -1;
            }

            BuildNode node = null;
            var pointer = Pointer.current;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (pointer != null && !overUI && Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(pointer.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, 200f, nodeLayers)) node = hit.collider.GetComponent<BuildNode>();
            }
            if (node != hovered)
            {
                if (hovered != null) { hovered.Highlight(false, false); if (hovered.tower != null) hovered.tower.Highlight(false); }
                hovered = node;
                if (hovered != null && hovered.tower != null) hovered.tower.Highlight(true);
            }
            UpdatePreview();
            UpdateRings();
            MachineTip();
            var game = TDGame.Instance;
            if (MachineSelected && StockIndex >= game.MachineStock.Count) Selected = -1;
            if (hovered == null || game.Current != TDGame.Phase.Deploy) return;
            var mouse = Mouse.current;
            if (hovered.tower != null && mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                var t = hovered.tower;
                int back = Mathf.Max(1, Mathf.FloorToInt(t.Type.cost * config.sellBack));
                if (UI.HandView.Instance != null) UI.HandView.Instance.NoteReturnFrom(t);
                game.Hand.TakeBack(t);   // its card goes back to the hand
                game.AddGold(back);
                FloatText.Show($"+{back} gold", t.transform.position + Vector3.up * 2f, new Color(1f, 0.85f, 0.3f));
                hovered.tower = null;
                Tower.All.Remove(t);
                Destroy(t.gameObject);
                return;
            }
            if (hovered.machine != null && mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                ReturnMachine(hovered);
                return;
            }
            if (Selected < 0)
            {
                if (hovered.tower != null && CameraDragPan.Clicked)
                {
                    // selected card(s) in the hand: give the first one to this tower; else take its card back
                    var hand = UI.HandView.Instance;
                    if (game.Hand.Selected.Count > 0 && hand != null) hand.AssignSelected(hovered.tower);
                    else { if (hand != null) hand.NoteReturnFrom(hovered.tower); game.Hand.TakeBack(hovered.tower); }
                }
                return;
            }

            if (MachineSelected)
            {
                var buff = game.MachineStock[StockIndex];
                bool okM = hovered.IsFree && !hovered.isPath && game.CanPlaceMachine;
                hovered.Highlight(true, okM);
                if (okM && CameraDragPan.Clicked)
                {
                    hovered.machine = BuffMachine.Build(buff, hovered.Top, material);
                    if (BoardTransition.Instance != null) hovered.machine.transform.SetParent(BoardTransition.Instance.board, true);
                    game.MachineStock.RemoveAt(StockIndex);
                    Selected = -1;
                }
                return;
            }

            var type = config.towers[Selected];
            bool ok = hovered.IsFree && hovered.isPath == type.onPath && game.Gold >= type.cost && Tower.All.Count < game.TowerLimit;
            hovered.Highlight(true, ok);
            if (ok && CameraDragPan.Clicked && game.TrySpend(type.cost))
            {
                hovered.tower = Tower.Build(type, hovered.Top, material);
                if (BoardTransition.Instance != null) hovered.tower.transform.SetParent(BoardTransition.Instance.board, true);
            }
        }

        /// <summary>Shows the dashed range of the selected tower type on the free tile under the pointer.</summary>
        void UpdatePreview()
        {
            bool show = Selected >= 0 && !MachineSelected && hovered != null && hovered.IsFree;
            if (!show) { if (preview != null) preview.gameObject.SetActive(false); return; }
            var type = config.towers[Selected];
            float radius = type.kind == TDConfig.TowerKind.Trap ? type.areaRadius : type.range;
            if (preview == null) preview = RangeRing.Create(transform, radius, new Color(1f, 1f, 1f, 0.95f));
            preview.SetRadius(radius);
            preview.gameObject.SetActive(true);
            preview.transform.position = new Vector3(hovered.transform.position.x, 0.08f, hovered.transform.position.z);
        }

        /// <summary>A placed machine goes back to the hand (stock) so it can be moved; it is sold from the hand
        /// (Wong, 2026-11-24). It burns away on the board.</summary>
        public void ReturnMachine(BuildNode node)
        {
            var m = node != null ? node.machine : null;
            if (m == null) return;
            TDGame.Instance.MachineStock.Add(m.Type);
            FloatText.Show("Back to hand", m.transform.position + Vector3.up * 2f, new Color(0.6f, 0.85f, 1f));
            node.machine = null;
            BuffMachine.All.Remove(m);
            m.transform.SetParent(null, true);
            foreach (var t in m.GetComponentsInChildren<TMPro.TextMeshPro>()) t.gameObject.SetActive(false);
            m.enabled = false;
            if (ModelDissolve.Run(m.gameObject, 0f, 1f, 0.4f, true) == null) Destroy(m.gameObject);
        }

        bool machineTip;
        /// <summary>Tooltip over a placed machine: what it does and that right-click moves it back to the hand.</summary>
        void MachineTip()
        {
            var m = hovered != null ? hovered.machine : null;
            var p = Pointer.current;
            if (m == null || p == null || TDGame.Instance.Current != TDGame.Phase.Deploy && TDGame.Instance.Current != TDGame.Phase.Wave)
            {
                if (machineTip) { UI.UiKit.HideTip(); machineTip = false; }
                return;
            }
            UI.UiKit.ShowTip($"<b>{m.Type.name.ToUpper()} MACHINE</b>\n<size=80%>{m.Type.Short} for the towers in its rings (ring 1 first).\n" +
                             (TDGame.Instance.Current == TDGame.Phase.Deploy ? "Right-click: back to your hand, to move it." : "") + "</size>", p.position.ReadValue());
            machineTip = true;
        }

        // ---------- buff machine rings shown on the tiles (Wong, 2026-11-24) ----------
        BuildNode[] nodes;
        Vector3? ringCentre;
        float ringStart;
        static readonly Color[] RingColors =
        {
            new Color(1f, 0.85f, 0.25f, 0.55f), new Color(1f, 0.5f, 0.2f, 0.5f),  new Color(0.95f, 0.3f, 0.5f, 0.45f),
            new Color(0.65f, 0.35f, 0.95f, 0.42f), new Color(0.3f, 0.55f, 1f, 0.38f), new Color(0.3f, 0.85f, 0.9f, 0.35f),
        };
        [Tooltip("Seconds between one ring lighting up and the next (the scan from the machine outwards).")]
        public float ringScanStep = 0.06f;

        /// <summary>Instead of circles, the tiles of each ring get a colour and a number, lit ring by ring
        /// from the machine outwards (a scan over the floor). Shown for: the machine being placed (under the
        /// pointer), else the machine under the pointer, else a machine that reaches the hovered tower.</summary>
        void UpdateRings()
        {
            var game = TDGame.Instance;
            foreach (var m in BuffMachine.All) m.ShowRings(false, false);   // the old circles stay hidden
            if (ringPreview != null) ringPreview.Show(false);

            Vector3? centre = null;
            bool fight = game.Current == TDGame.Phase.Deploy || game.Current == TDGame.Phase.Wave;
            if (fight)
            {
                if (game.Current == TDGame.Phase.Deploy && MachineSelected && hovered != null && hovered.IsFree && !hovered.isPath)
                    centre = hovered.transform.position;
                else if (hovered != null && hovered.machine != null) centre = hovered.machine.transform.position;
                else if (HoveredTower != null)
                    foreach (var m in BuffMachine.All)
                        if (m.RingOf(HoveredTower.transform.position) > 0) { centre = m.transform.position; break; }
            }
            if (centre.HasValue != ringCentre.HasValue || (centre.HasValue && (centre.Value - ringCentre.Value).sqrMagnitude > 0.01f))
            {
                ringCentre = centre;
                ringStart = Time.time;
            }
            if (nodes == null || nodes.Length == 0) nodes = FindObjectsByType<BuildNode>(FindObjectsSortMode.None);
            float age = Time.time - ringStart;
            foreach (var n in nodes)
            {
                if (n == null) continue;
                int k = centre.HasValue ? BuffMachine.RingAt(centre.Value, n.transform.position) : 0;
                if (k > 0 && age < (k - 1) * ringScanStep) k = 0;   // not reached by the scan yet
                n.SetRing(k, k > 0 ? RingColors[Mathf.Min(k, RingColors.Length) - 1] : Color.clear);
            }
        }
    }
}
