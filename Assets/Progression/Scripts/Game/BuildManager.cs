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

        public void Select(int index) => Selected = Selected == index ? -1 : index;

        static readonly Key[] DigitKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };

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
            var game = TDGame.Instance;
            if (MachineSelected && StockIndex >= game.MachineStock.Count) Selected = -1;
            if (hovered == null || game.Current != TDGame.Phase.Deploy) return;
            var mouse = Mouse.current;
            if (hovered.tower != null && mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                var t = hovered.tower;
                int back = Mathf.Max(1, Mathf.FloorToInt(t.Type.cost * config.sellBack));
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
                var m = hovered.machine;
                game.AddGold(game.SellValue(m.Type));
                FloatText.Show($"+{game.SellValue(m.Type)} gold", m.transform.position + Vector3.up * 2f, new Color(1f, 0.85f, 0.3f));
                hovered.machine = null;
                BuffMachine.All.Remove(m);
                Destroy(m.gameObject);
                return;
            }
            if (Selected < 0)
            {
                if (hovered.tower != null && pointer.press.wasPressedThisFrame) game.Hand.TakeBack(hovered.tower);
                return;
            }

            if (MachineSelected)
            {
                var buff = game.MachineStock[StockIndex];
                bool okM = hovered.IsFree && !hovered.isPath;
                hovered.Highlight(true, okM);
                if (okM && pointer.press.wasPressedThisFrame)
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
            if (ok && pointer.press.wasPressedThisFrame && game.TrySpend(type.cost))
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

        /// <summary>Which machine rings are visible this frame.</summary>
        void UpdateRings()
        {
            var game = TDGame.Instance;
            bool placing = game.Current == TDGame.Phase.Deploy && MachineSelected;
            var tower = HoveredTower;
            foreach (var m in BuffMachine.All)
            {
                bool underPointer = hovered != null && hovered.machine == m;
                bool reachesTower = tower != null && m.RingOf(tower.transform.position) > 0;
                m.ShowRings(placing || underPointer || reachesTower, underPointer || reachesTower);
            }

            bool showPreview = placing && hovered != null && hovered.IsFree && !hovered.isPath;
            if (!showPreview) { if (ringPreview != null) ringPreview.Show(false); return; }
            if (ringPreview == null) ringPreview = RingGuide.Create(transform, new Color(1f, 1f, 1f, 0.95f));
            ringPreview.Refresh(game.TowerLimit, config.ringWidth, config.ringOneIsInner);
            ringPreview.Show(true);
            ringPreview.transform.position = new Vector3(hovered.transform.position.x, RingGuide.Height, hovered.transform.position.z);
        }
    }
}
