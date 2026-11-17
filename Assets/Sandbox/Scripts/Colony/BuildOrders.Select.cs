using UnityEngine;

namespace DamRush
{
    /// <summary>Marks the collider of a site's visual, so a mouse click finds the site it sees, not the ground behind it.</summary>
    public class SitePick : MonoBehaviour
    {
        public Site site;
    }

    /// <summary>
    /// Selecting things on the map: with no tool on, a left click selects the building or order
    /// on that hex. The HUD shows a pop-up for it (research for a lab, orders for a machine,
    /// type for a storage, remove and flag for everything). Escape or a click on empty ground
    /// closes it.
    /// </summary>
    public partial class BuildOrders
    {
        public Site Selected { get; private set; }

        public void Select(Site s) { Selected = s; }
        public void ClearSelection() => Selected = null;

        /// <summary>The hex under the mouse. Building visuals have a collider, so a click on a tall base or dam picks that hex.</summary>
        bool PickHex(Ray ray, out HexCell hex)
        {
            hex = null;
            if (!Physics.Raycast(ray, out RaycastHit h, 500f)) return false;
            var pick = h.collider.GetComponentInParent<SitePick>();
            if (pick != null && pick.site != null && sites.Contains(pick.site)) { hex = pick.site.hex; return true; }
            return grid.TryGet(h.point, out hex);
        }

        void UpdateSelection(HexCell hovered, bool leftClick)
        {
            if (Selected != null && !sites.Contains(Selected)) Selected = null;
            if (Tool == BuildTool.None && leftClick && hovered != null) Selected = TopSiteOn(hovered);
            if (Selected != null)
            {
                // a bright, pulsing outline so the selected building is easy to find
                float p = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 6f);
                grid.DrawHighlight(Selected.hex, new Color(0.3f * p, 0.95f * p + 0.05f, 1f));
            }
        }

        /// <summary>Adds a box collider around a site's visual for mouse picking.</summary>
        static void AddPick(GameObject root, Site s)
        {
            var rs = root.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var bb = rs[0].bounds;
            foreach (var r in rs) bb.Encapsulate(r.bounds);
            var box = root.AddComponent<BoxCollider>();
            box.center = root.transform.InverseTransformPoint(bb.center);
            box.size = bb.size;
            root.AddComponent<SitePick>().site = s;
        }

        /// <summary>A storage gets a new type. What it holds now is put on the ground for the gatherers.</summary>
        public void ChangeStorageType(BuildingSite b, Item type)
        {
            if (b == null || b.Kind != BuildingKind.Storage || b.storeType == type) return;
            if (b.stock > 0) colony.DropPile(b.hex, b.storeType, b.stock);
            colony.Log($"{b.Title} at {b.hex.axial} now keeps {ItemInfo.Lower(type)}" + (b.stock > 0 ? $" ({b.stock} {ItemInfo.Lower(b.storeType)} put on the ground)" : ""));
            b.stock = 0;
            b.storeType = type;
            b.hex.building = b.Title;
            b.stockChanged = true;
            UpdateVisual(b);
        }

        /// <summary>Text about the site for the pop-up (the same as the hover tooltip).</summary>
        public string SelectedInfo(out string head)
        {
            if (Selected == null) { head = ""; return ""; }
            if (TopSiteOn(Selected.hex) == Selected) return HexInfo(Selected.hex, out head);
            // a base or Dam Mk2 under a building
            head = Selected.Title;
            return Selected.Finished ? Function(Selected)
                : $"Building: {Mathf.RoundToInt(Selected.Progress01() * 100f)}%, still needed: {Selected.MissingText()}";
        }
    }
}
