using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PokerTD
{
    /// <summary>
    /// Placing towers in the Deploy phase: pick a tower type (HUD button or keys 1-3),
    /// then click a free tile. Traps go on the path, the others next to it.
    /// A tower built during the battle turn draws one more card into the hand.
    /// Clicking a tower (with no tower type selected) takes its card back into the hand.
    /// </summary>
    public class BuildManager : MonoBehaviour
    {
        public static BuildManager Instance { get; private set; }
        public TDConfig config;
        public Material material;
        public LayerMask nodeLayers = ~0;

        /// <summary>Index into config.towers, or -1 for none.</summary>
        public int Selected { get; private set; } = -1;

        BuildNode hovered;
        RangeRing preview;   // range of the selected tower type, under the pointer

        void Awake() => Instance = this;

        public void Select(int index) => Selected = Selected == index ? -1 : index;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) Select(0);
                if (kb.digit2Key.wasPressedThisFrame) Select(1);
                if (kb.digit3Key.wasPressedThisFrame) Select(2);
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
            var game = TDGame.Instance;
            if (hovered == null || game.Current != TDGame.Phase.Deploy) return;
            if (Selected < 0)
            {
                if (hovered.tower != null && pointer.press.wasPressedThisFrame) game.Hand.TakeBack(hovered.tower);
                return;
            }
            var type = config.towers[Selected];
            bool ok = hovered.tower == null && hovered.isPath == type.onPath && game.Gold >= type.cost && Tower.All.Count < game.TowerLimit;
            hovered.Highlight(true, ok);
            if (ok && pointer.press.wasPressedThisFrame && game.TrySpend(type.cost))
            {
                hovered.tower = Tower.Build(type, hovered.Top, material);
                if (BoardTransition.Instance != null) hovered.tower.transform.SetParent(BoardTransition.Instance.board, true);
                game.Hand.Draw(1);
            }
        }

        /// <summary>Shows the dashed range of the selected tower type on the free tile under the pointer.</summary>
        void UpdatePreview()
        {
            bool show = Selected >= 0 && hovered != null && hovered.tower == null;
            if (!show) { if (preview != null) preview.gameObject.SetActive(false); return; }
            var type = config.towers[Selected];
            float radius = type.kind == TDConfig.TowerKind.Trap ? type.areaRadius : type.range;
            if (preview == null) preview = RangeRing.Create(transform, radius, new Color(1f, 1f, 1f, 0.95f));
            preview.SetRadius(radius);
            preview.gameObject.SetActive(true);
            preview.transform.position = new Vector3(hovered.transform.position.x, 0.08f, hovered.transform.position.z);
        }


    }
}
