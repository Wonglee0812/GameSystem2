using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DamRush
{
    public enum BuildTool { None, Dam, Base, Nursery, Storage, Priority, Repair, DamMk2, Spillway, PumpBase, FoodFarm, Dig, Fell, MaterialLab, BuildingLab, WaterWheel, Machine, Levee, Floodgate, Sandbag, AlarmBell }

    /// <summary>
    /// The leader's orders. Pick a tool (keys 1-4 or the Build tab), then click a hex; the dam
    /// tool can also be dragged to order a line of dams. Right-click a hex to mark its order for
    /// removal (it flashes red), right-click it again to remove it. The flag tool (5) puts a
    /// priority flag on an order. Builders take materials from the nearest storage, carry them
    /// to the flagged sites first, then the most urgent ones, and build stage by stage.
    /// Adults carry 2 items per trip, yearlings 1.
    /// Rules:
    /// - Dam: on any free hex.
    /// - Base: on a free hex where the water is not deeper than the base can lift the floor.
    /// - Nursery: on a finished base, with water of at least 0.7 m next to it, so the entrance
    ///   is under water. Real lodges sit in water about 1.16 m deep on average, and only 5% in
    ///   water under 0.7 m (Science Insights, n.d.).
    /// - Storage: on a finished base.
    /// Repair (key 6 or R): click a worn dam to fix it before the water washes a stage away. A
    /// repair costs a few items, far less than building the stage again. When a stage is washed
    /// away anyway, half of its materials fall next to the dam as a pile, and the dam gets a
    /// priority flag so the builders come to rebuild it (no alarm: the family only hides from floods).
    /// </summary>
    public partial class BuildOrders : MonoBehaviour
    {
        public ColonyManager colony;
        public HexGrid grid;
        public WaterGrid water;
        public Camera cam;
        [Tooltip("Seconds a builder needs to put one item in place.")]
        public float buildTime = 2f;
        [Tooltip("Seconds the player has to right-click again to confirm a removal.")]
        public float removeConfirmTime = 3f;
        [Tooltip("Condition lost per second per metre of water running over the dam.")]
        public float wearRate = 0.06f;
        [Tooltip("Leak noise above this makes idle workers come and repair (instinct).")]
        public float repairInstinctNoise = 0.25f;
        [Tooltip("Repair items = items of the top stage x this share x wear (at least 1).")]
        public float repairShare = 0.5f;
        [Tooltip("Share of the materials that falls on the ground when a stage washes away or an order is removed.")]
        public float dropShare = 0.5f;
        public Material stoneMat, woodMat, mudMat;

        [Tooltip("Base, body and seal, in the order beavers build them.")]
        public DamStage[] damStages =
        {
            new DamStage { name = "base (stones + mud)", stone = 2, mud = 2, height = 0.6f, leakiness = 0.6f },
            new DamStage { name = "body (sticks)", wood = 4, height = 1.4f, leakiness = 1f },
            new DamStage { name = "seal (mud)", mud = 3, height = 1.6f, leakiness = 0.05f },
        };

        [Header("Buildings")]
        [Tooltip("Height of a base platform above the lowest ground of its hex (m).")]
        public float baseHeight = 1.2f;
        [Tooltip("The floor must stay this far above the water when the base is ordered (m).")]
        public float baseDryMargin = 0.2f;
        [Tooltip("Water depth needed next to a nursery so its entrance is under water (m).")]
        public float nurseryMinDepth = 0.7f;
        [Tooltip("Each storage holds this much of its one material.")]
        public int storageCapacity = 40;
        /// <summary>Material of the next storage the leader orders.</summary>
        public Item storageType = Item.Wood;
        public BuildingDef[] buildingDefs =
        {
            new BuildingDef { kind = BuildingKind.Base, name = "Base",
                rule = "Platform for buildings. Water here max 1.0 m deep.",
                stages = new[] { new BuildStage { name = "stones", stone = 3, mud = 1 }, new BuildStage { name = "mud top", mud = 3 } } },
            new BuildingDef { kind = BuildingKind.Nursery, name = "Nursery",
                rule = "On a base. Needs water 0.7 m deep next to it (entrance under water). Makes kits from wood.",
                stages = new[] { new BuildStage { name = "frame", wood = 6 }, new BuildStage { name = "mud walls", mud = 4 } } },
            new BuildingDef { kind = BuildingKind.Storage, name = "Storage",
                rule = "On a base. Holds 40 of one material or food: pick the type before you place it.",
                stages = new[] { new BuildStage { name = "frame", wood = 4 }, new BuildStage { name = "floor", mud = 2 } } },
        };

        public BuildTool Tool { get; private set; }
        /// <summary>Order marked for removal: a second right-click (or x) within a few seconds removes it.</summary>
        public Site PendingRemove { get; private set; }
        float pendingUntil;
        public bool ShowGridAlways { get; set; }
        public void SetTool(BuildTool t) => Tool = Tool == t ? BuildTool.None : t;
        public void ToggleDamTool() => SetTool(BuildTool.Dam);
        public bool DamTool => Tool == BuildTool.Dam;

        public readonly List<Site> sites = new List<Site>();
        public IEnumerable<DamSite> Dams { get { foreach (var s in sites) if (s is DamSite d) yield return d; } }
        public IEnumerable<BuildingSite> Buildings { get { foreach (var s in sites) if (s is BuildingSite b) yield return b; } }

        // hover info for the HUD tooltip
        public HexCell HoverHex { get; private set; }
        public bool HoverOk { get; private set; }
        public string HoverText { get; private set; } = "";

        class Job { public Site site; public Item item; public int count = 1; public bool pickedUp; public ISupply storage; }
        readonly Dictionary<Beaver, Job> jobs = new Dictionary<Beaver, Job>();
        readonly Dictionary<Item, int> pendingPickups = NewPending();
        static Dictionary<Item, int> NewPending() { var d = new Dictionary<Item, int>(); foreach (var i in BuildStage.Materials) d[i] = 0; return d; }
        Dictionary<Vector2Int, List<int>> cellsByHex;
        Mesh prism;
        float nurseryCheck;

        public BuildingDef Def(BuildingKind k) => System.Array.Find(buildingDefs, d => d.kind == k) ?? PhaseBDef(k) ?? colony?.industry?.DefFor(k);

        void Awake() { prism = HexPrism(grid.size * 0.97f); }

        /// <summary>Which water cells belong to which hex (cell centre inside the hex). Built on first use,
        /// because the water grid sets itself up in its own Start.</summary>
        List<int> CellsOf(HexCell c)
        {
            if (cellsByHex == null || cellsByHex.Count == 0)
            {
                cellsByHex = new Dictionary<Vector2Int, List<int>>();
                for (int i = 0; i < water.CellsX; i++)
                    for (int j = 0; j < water.CellsZ; j++)
                    {
                        int id = water.CellIndex((i + 0.5f) * water.CellSize, (j + 0.5f) * water.CellSize);
                        var ax = grid.WorldToAxial(water.CellCenter(id));
                        if (!cellsByHex.TryGetValue(ax, out var list)) cellsByHex[ax] = list = new List<int>();
                        list.Add(id);
                    }
            }
            return cellsByHex.TryGetValue(c.axial, out var cells) ? cells : new List<int>();
        }

        // ------------------------------------------------------------------ player input

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null)
            {
                if (kb.bKey.wasPressedThisFrame || kb.digit1Key.wasPressedThisFrame) SetTool(BuildTool.Dam);
                if (kb.digit2Key.wasPressedThisFrame) SetTool(BuildTool.Base);
                if (kb.digit3Key.wasPressedThisFrame) SetTool(BuildTool.Nursery);
                if (kb.digit4Key.wasPressedThisFrame) SetTool(BuildTool.Storage);
                if (kb.digit5Key.wasPressedThisFrame || kb.pKey.wasPressedThisFrame) SetTool(BuildTool.Priority);
                if (kb.digit6Key.wasPressedThisFrame || kb.rKey.wasPressedThisFrame) SetTool(BuildTool.Repair);
                if (kb.digit7Key.wasPressedThisFrame) SetTool(BuildTool.DamMk2);
                if (kb.digit8Key.wasPressedThisFrame) SetTool(BuildTool.Spillway);
                if (kb.digit9Key.wasPressedThisFrame) SetTool(BuildTool.PumpBase);
                if (kb.digit0Key.wasPressedThisFrame) SetTool(BuildTool.FoodFarm);
                if (kb.mKey.wasPressedThisFrame) SetTool(BuildTool.Machine);
                if (kb.zKey.wasPressedThisFrame) SetTool(BuildTool.Dig);
                if (kb.xKey.wasPressedThisFrame) SetTool(BuildTool.Fell);
                if (kb.escapeKey.wasPressedThisFrame) { if (Tool == BuildTool.None) Selected = null; Tool = BuildTool.None; }
                if (kb.gKey.wasPressedThisFrame) ShowGridAlways = !ShowGridAlways;
            }
            // the grid only shows while the leader is building
            bool showGrid = (Tool != BuildTool.None && Tool != BuildTool.Priority && Tool != BuildTool.Repair) || ShowGridAlways;
            if (grid.Visible != showGrid) grid.SetVisible(showGrid);

            HexCell hovered = null;
            if (mouse != null && !ColonyHud.PointerOverHud(mouse.position.ReadValue()))
                PickHex(cam.ScreenPointToRay(mouse.position.ReadValue()), out hovered);
            HoverHex = hovered;
            UpdateSelection(hovered, mouse != null && mouse.leftButton.wasPressedThisFrame);

            if (Tool == BuildTool.Priority)
            {
                if (hovered != null)
                {
                    var target = TopSiteOn(hovered);
                    HoverOk = target != null && target.HasWork;
                    HoverText = target == null ? "No order here" : !target.HasWork ? $"{target.Title} is already finished"
                        : target.priority ? $"Click to take the flag off this {target.Title.ToLower()}" : $"Click to flag this {target.Title.ToLower()}: builders go here first";
                    grid.DrawHighlight(hovered, HoverOk ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.3f, 0.2f));
                    if (HoverOk && mouse.leftButton.wasPressedThisFrame) TogglePriority(target);
                }
            }
            else if (Tool == BuildTool.Repair)
            {
                ShowWornDams();
                if (hovered != null)
                {
                    HoverOk = CanRepair(TopSiteOn(hovered) as DamSite, out string why);
                    HoverText = why + (HoverOk ? "\nTip: hold the left button and drag over a dam line" : "");
                    grid.DrawHighlight(hovered, HoverOk ? new Color(0.3f, 0.85f, 1f) : new Color(1f, 0.3f, 0.2f));
                    if (HoverOk && mouse.leftButton.isPressed) OrderRepair((DamSite)TopSiteOn(hovered));
                }
            }
            else if (Tool != BuildTool.None)
            {
                ShowCandidates();
                if (hovered != null)
                {
                    HoverOk = CanOrder(Tool, hovered, out string why);
                    HoverText = why;
                    if (IsDamTool(Tool)) HoverText += "\nTip: hold the left button and drag to order a line of dams";
                    if (Tool == BuildTool.Fell) HoverText += "\nTip: hold the left button and drag over the forest";
                    grid.DrawHighlight(hovered, HoverOk ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.3f, 0.2f));
                    // dams can be painted: hold the button and drag along the line
                    bool click = mouse.leftButton.wasPressedThisFrame || ((IsDamTool(Tool) || Tool == BuildTool.Fell) && mouse.leftButton.isPressed);
                    if (HoverOk && click) Order(Tool, hovered);
                }
            }
            if (hovered != null && mouse.rightButton.wasPressedThisFrame) RightClick(hovered);
            if (PendingRemove != null && (Time.unscaledTime > pendingUntil || !sites.Contains(PendingRemove))) PendingRemove = null;

            // planned sites: orange outline; flagged: yellow; leaking dams: red; marked for removal: flashing red
            foreach (var s in sites)
            {
                if (s == PendingRemove) continue;
                if (s.priority && s.HasWork) grid.DrawHighlight(s.hex, new Color(1f, 0.88f, 0.2f));
                else if (s.stage < 0) grid.DrawHighlight(s.hex, new Color(1f, 0.6f, 0.1f));
                else if (s is DamSite d && d.leakNoise > repairInstinctNoise) grid.DrawHighlight(s.hex, new Color(1f, 0.15f, 0.1f));
            }
            if (PendingRemove != null && Mathf.Repeat(Time.unscaledTime * 4f, 1f) < 0.6f)
                grid.DrawHighlight(PendingRemove.hex, new Color(1f, 0.05f, 0.05f));
            UpdateFlags();
            UpdateDams(Time.deltaTime);
            UpdateNurseries(Time.deltaTime);
            UpdatePhaseB(Time.deltaTime);
            UpdateSandbags(Time.deltaTime);
            foreach (var b in Buildings)
                if (b.Kind == BuildingKind.Storage && b.stockChanged) { b.stockChanged = false; UpdatePiles(b); }
        }

        /// <summary>While a building tool is on, mark the bases where it can go (green) or not (red).</summary>
        void ShowCandidates()
        {
            if (Tool != BuildTool.Nursery && Tool != BuildTool.Storage) return;
            foreach (var b in sites)
            {
                if (!IsPlatformSite(b)) continue;
                if (BuildingOn(b) != null) continue;
                bool ok = CanOrder(Tool, b.hex, out _);
                grid.DrawHighlight(b.hex, ok ? new Color(0.3f, 1f, 0.4f, 0.8f) : new Color(1f, 0.5f, 0.3f, 0.6f));
            }
        }

        // ------------------------------------------------------------------ repair

        /// <summary>While the repair tool is on, worn dams light up (blue = can be repaired).</summary>
        void ShowWornDams()
        {
            foreach (var d in Dams)
                if (d.repair == null && d.RepairCost(repairShare) != null) grid.DrawHighlight(d.hex, new Color(0.3f, 0.75f, 1f, 0.8f));
        }

        public bool CanRepair(DamSite d, out string why)
        {
            if (d == null) { why = "No dam here"; return false; }
            if (d.stage < 0) { why = "Nothing built yet: the builders bring the base first"; return false; }
            if (d.repair != null) { why = $"Repair already ordered: {d.MissingText()} still needed"; return false; }
            var cost = d.RepairCost(repairShare);
            if (cost == null) { why = $"Condition {d.condition * 100f:0}%: no repair needed"; return false; }
            why = $"Condition {d.condition * 100f:0}% ({d.CurrentStage.name})\nRepair costs {cost.CostText()}" +
                  $"\n<size=13>Rebuilding the {d.CurrentStage.name} would cost {d.CurrentStage.CostText()}</size>";
            return true;
        }

        public void OrderRepair(DamSite d)
        {
            if (!CanRepair(d, out _)) return;
            var cost = d.RepairCost(repairShare);
            d.StartRepair(cost);
            colony.Log($"Repair ordered for the dam at {d.hex.axial} ({cost.CostText()})");
        }

        /// <summary>Orders a repair on every worn dam. Returns how many.</summary>
        public int RepairAll()
        {
            int n = 0;
            foreach (var d in new List<DamSite>(Dams))
                if (CanRepair(d, out _)) { OrderRepair(d); n++; }
            return n;
        }

        public int WornDams { get { int n = 0; foreach (var d in Dams) if (d.repair == null && d.RepairCost(repairShare) != null) n++; return n; } }

        /// <summary>Drops a share of the materials as piles next to the hex (rounded: 3 x 0.5 = 2).</summary>
        void Drop(HexCell hex, BuildStage st, float share)
        {
            if (st == null) return;
            foreach (Item i in BuildStage.Materials)
                colony.DropPile(hex, i, Mathf.FloorToInt(st.Need(i) * share + 0.5f));
        }

        // ------------------------------------------------------------------ water rules

        /// <summary>Water depth over the lowest ground of a hex (0 when dry).</summary>
        public float DepthOver(HexCell c)
        {
            float s = water.SurfaceAt(c.center.x, c.center.z, out bool wet);
            return wet ? Mathf.Max(0f, s - c.minGround) : 0f;
        }

        /// <summary>Deepest water in the hexes around a hex (where the nursery entrance can go).</summary>
        public float DeepestNeighbour(HexCell c)
        {
            float best = 0f;
            foreach (var n in grid.Neighbours(c))
            {
                if (n.blocked) continue;
                best = Mathf.Max(best, water.DepthAt(n.center.x, n.center.z));
            }
            return best;
        }

        public Site SiteOn(HexCell c)
        {
            Site found = null;
            foreach (var s in sites)
                if (s.hex == c) { if (!IsPlatformSite(s) || found == null) found = s; }
            return found;
        }

        public BuildingSite BaseOn(HexCell c)
        {
            foreach (var b in Buildings) if (b.hex == c && b.IsBase) return b;
            return null;
        }

        public BuildingSite BuildingOn(Site baseSite)
        {
            foreach (var b in Buildings) if (b.baseSite == baseSite) return b;
            return null;
        }

        /// <summary>Can the tool be used on this hex? The text says why not, or what will happen.</summary>
        public bool CanOrder(BuildTool tool, HexCell c, out string why)
        {
            float depth = DepthOver(c);
            string here = depth > 0.02f ? $"Water here: {depth:0.00} m deep" : "Dry ground";
            if (Locked(tool, out why)) return false;
            if (c.blocked && !IsPlatformBuildingTool(tool) && tool != BuildTool.Fell && tool != BuildTool.Sandbag) { why = $"{c.building ?? "Something"} stands here"; return false; }
            if (IsDamTool(tool) || tool == BuildTool.Base || tool == BuildTool.PumpBase)
                if (DigOrderOn(c) != null) { why = "Digging is planned here: wait until it is done or remove it"; return false; }
            switch (tool)
            {
                case BuildTool.Dam:
                case BuildTool.DamMk2:
                case BuildTool.Spillway:
                case BuildTool.Levee:
                case BuildTool.Floodgate:
                {
                    if (SiteOn(c) != null) { why = "Something is already planned here"; return false; }
                    float speed = water.VelocityAt(c.center.x, c.center.z).magnitude;
                    var st = DamStagesFor(tool);
                    float crest = st[st.Length - 1].height;
                    why = depth > 0.02f
                        ? $"{here}, flowing {speed:0.0} m/s\nA finished dam ({crest:0.0} m) can hold the water up to {crest:0.0} m deep here"
                        : $"{here}\nA dam on dry ground only helps when the water rises";
                    // water runs around a dam through lower neighbours: the line must reach high ground
                    int around = 0;
                    foreach (var n in grid.Neighbours(c))
                        if (!(SiteOn(n) is DamSite) && n.building != "Main lodge" && n.minGround < c.minGround + crest - 0.1f) around++;
                    if (around > 2) why += $"\n<color=#ffb070>Water can flow around it ({around} low sides): make the dam line reach high ground</color>";
                    if (tool == BuildTool.Spillway) why += "\nWater runs over the top without wear: the pond stays at this level";
                    if (tool == BuildTool.DamMk2) why += "\nWhen finished, a nursery or storage can stand on it";
                    return true;
                }
                case BuildTool.Base:
                case BuildTool.PumpBase:
                {
                    if (SiteOn(c) != null) { why = "Something is already planned here"; return false; }
                    float max = baseHeight - baseDryMargin;
                    if (depth > max) { why = $"{here}\nToo deep for a base (max {max:0.0} m)"; return false; }
                    why = $"{here}\nBase floor will be {baseHeight:0.0} m above the lowest ground";
                    return true;
                }
                case BuildTool.Nursery:
                case BuildTool.Storage:
                case BuildTool.MaterialLab:
                case BuildTool.BuildingLab:
                case BuildTool.Machine:
                {
                    var b = PlatformOn(c);
                    if (b == null) { why = "Build a base (or a Dam Mk2) here first"; return false; }
                    if (!b.Finished) { why = $"The {b.Title.ToLower()} is not finished yet"; return false; }
                    if (BuildingOn(b) != null) { why = $"This {b.Title.ToLower()} already has a building"; return false; }
                    if (tool == BuildTool.Storage) { why = $"{ItemInfo.Name(storageType)} storage for {storageCapacity} {ItemInfo.Lower(storageType)}"; return true; }
                    if (tool != BuildTool.Nursery) return CanOrderOnPlatform(tool, out why);
                    float entrance = DeepestNeighbour(c);
                    float lift = b is BuildingSite pb && pb.Kind == BuildingKind.PumpBase ? pumpLift : 0f;
                    if (entrance + lift < nurseryMinDepth)
                    {
                        why = $"Entrance needs water {nurseryMinDepth:0.0} m deep next to it\nDeepest now: {entrance:0.00} m" +
                              (lift > 0f ? $" (+{lift:0.0} m from the pump)" : ". Build a dam downstream, or use a pump base (9)");
                        return false;
                    }
                    why = lift > 0f && entrance < nurseryMinDepth ? $"Entrance: {entrance:0.00} m now, the pump adds up to {lift:0.0} m" : $"Entrance under water ({entrance:0.00} m deep)";
                    return true;
                }
            }
            if (tool == BuildTool.WaterWheel) return CanOrderC(tool, c, out why);
            if (tool == BuildTool.AlarmBell || tool == BuildTool.Sandbag) return CanOrderD(tool, c, out why);
            return CanOrderB(tool, c, out why);
        }

        public void Order(BuildTool tool, HexCell c)
        {
            if (!CanOrder(tool, c, out _)) return;
            switch (tool)
            {
                case BuildTool.Dam:
                    sites.Add(new DamSite(c, damStages, CellsOf(c), water));
                    break;
                case BuildTool.DamMk2:
                    sites.Add(new DamSite(c, damMk2, CellsOf(c), water));
                    break;
                case BuildTool.Spillway:
                    sites.Add(new DamSite(c, spillway, CellsOf(c), water));
                    break;
                case BuildTool.Levee:
                    sites.Add(new DamSite(c, levee, CellsOf(c), water));
                    break;
                case BuildTool.Floodgate:
                    sites.Add(new DamSite(c, floodgate, CellsOf(c), water));
                    break;
                case BuildTool.AlarmBell:
                case BuildTool.Sandbag:
                    OrderD(tool, c);
                    break;
                case BuildTool.Base:
                case BuildTool.PumpBase:
                    sites.Add(new BuildingSite(Def(tool == BuildTool.Base ? BuildingKind.Base : BuildingKind.PumpBase), c, null, CellsOf(c), water, baseHeight));
                    break;
                case BuildTool.Nursery:
                case BuildTool.Storage:
                case BuildTool.MaterialLab:
                case BuildTool.BuildingLab:
                case BuildTool.Machine:
                    var site = NewPlatformBuilding(tool, c);
                    if (site.Kind == BuildingKind.Storage) site.storeType = storageType;
                    sites.Add(site);
                    c.blocked = true; c.building = site.Title;
                    break;
                case BuildTool.WaterWheel:
                    OrderC(tool, c);
                    break;
                default:
                    OrderB(tool, c);
                    break;
            }
        }

        /// <summary>The order on top of a hex (a building before its base).</summary>
        public Site TopSiteOn(HexCell c)
        {
            Site top = null;
            foreach (var s in sites)
                if (s.hex == c && (top == null || !IsPlatformSite(s))) top = s;
            return top;
        }

        void RightClick(HexCell c)
        {
            if (Tool != BuildTool.None && TopSiteOn(c) == null) { Tool = BuildTool.None; return; } // right-click on empty ground drops the tool
            var top = TopSiteOn(c);
            if (top != null) RequestRemove(top);
            else if (DigOrderOn(c) != null) CancelDig(c);
            else if (FellMarkedOn(c)) UnmarkFell(c);
        }

        /// <summary>
        /// Two-step removal so a stray right-click does not destroy anything: the first call marks
        /// the order (it flashes red), a second call within a few seconds removes it.
        /// Returns true when the order was removed.
        /// </summary>
        public bool RequestRemove(Site site)
        {
            if (PendingRemove == site && Time.unscaledTime <= pendingUntil)
            {
                PendingRemove = null;
                Cancel(site);
                return true;
            }
            PendingRemove = site;
            pendingUntil = Time.unscaledTime + removeConfirmTime;
            return false;
        }

        public float PendingLeft => PendingRemove != null ? Mathf.Max(0f, pendingUntil - Time.unscaledTime) : 0f;

        public void TogglePriority(Site s)
        {
            if (s == null || !s.HasWork) return;
            s.priority = !s.priority;
            colony.Log(s.priority ? $"Priority flag on the {s.Title.ToLower()} at {s.hex.axial}" : $"Flag removed from the {s.Title.ToLower()} at {s.hex.axial}");
            // builders on other jobs finish their trip; idle builders pick the flagged site at once
        }

        /// <summary>A small red flag on a pole above every flagged site.</summary>
        void UpdateFlags()
        {
            foreach (var s in sites)
            {
                bool want = s.priority && s.HasWork;
                if (!want) { if (s.flag != null) { Destroy(s.flag); s.flag = null; } if (!s.HasWork) s.priority = false; continue; }
                if (s.flag != null) continue;
                float top = s is DamSite d ? Mathf.Max(d.Crest, s.hex.maxGround) : s is BuildingSite b && b.stage >= 0 ? b.Floor : s.hex.maxGround;
                var root = new GameObject("Priority flag").transform;
                root.SetParent(transform);
                root.position = new Vector3(s.hex.center.x + 0.9f, top, s.hex.center.z);
                Piece(root, PrimitiveType.Cylinder, woodMat, new Vector3(0f, 1.1f, 0f), new Vector3(0.07f, 1.1f, 0.07f), Vector3.zero);
                var cloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(cloth.GetComponent<Collider>());
                cloth.transform.SetParent(root, false);
                cloth.transform.localPosition = new Vector3(0.38f, 1.9f, 0f);
                cloth.transform.localScale = new Vector3(0.7f, 0.42f, 0.04f);
                var r = cloth.GetComponent<Renderer>();
                if (flagMat == null) { flagMat = new Material(r.sharedMaterial); flagMat.color = new Color(0.9f, 0.15f, 0.1f); }
                r.sharedMaterial = flagMat;
                s.flag = root.gameObject;
            }
        }
        Material flagMat;

        public void Cancel(Site site)
        {
            if (IsPlatformSite(site) && BuildingOn(site) != null) { colony.Log($"Remove the building on this {site.Title.ToLower()} first"); return; }
            if (site is BuildingSite b)
            {
                if (b.Kind == BuildingKind.Machine) colony.industry?.DropBuffer(b);
                if (!b.IsBase) { b.hex.blocked = false; b.hex.building = null; }
                if (b.Kind == BuildingKind.Storage && b.stock > 0)
                {
                    // the stock is not lost: it is put on the ground and the gatherers carry it to another storage
                    colony.DropPile(b.hex, b.storeType, b.stock);
                    colony.Log($"Storage removed: {b.stock} {ItemInfo.Lower(b.storeType)} put on the ground", true);
                }
            }
            // half of the finished stages and all loose materials stay on the ground as piles
            for (int st = 0; st <= site.stage && st < site.StageCount; st++) Drop(site.hex, site.StageAt(st), dropShare);
            foreach (Item i in BuildStage.Materials) colony.DropPile(site.hex, i, site.Loose(i));
            foreach (var pair in new List<KeyValuePair<Beaver, Job>>(jobs))
                if (pair.Value.site == site || ReferenceEquals(pair.Value.storage, site)) Release(pair.Key);
            site.Remove();
            sites.Remove(site);
        }

        /// <summary>The first storage at the start of the game: a finished base with a finished storage on it.</summary>
        public BuildingSite CreateStarterStorage(HexCell c, Item type, int amount)
        {
            var b = new BuildingSite(Def(BuildingKind.Base), c, null, CellsOf(c), water, baseHeight);
            b.stage = b.StageCount - 1;
            b.ApplyToWater();
            sites.Add(b);
            UpdateVisual(b);
            var s = new BuildingSite(Def(BuildingKind.Storage), c, b, null, water, baseHeight);
            s.stage = s.StageCount - 1;
            s.storeType = type; s.stock = amount;
            sites.Add(s);
            c.blocked = true; c.building = s.Title;
            UpdateVisual(s);
            return s;
        }

        // ------------------------------------------------------------------ hover info (HUD tooltip)

        static string Mat(Item i) => ItemInfo.Lower(i);

        /// <summary>
        /// Text for the tooltip when the mouse is over a hex and no tool is on.
        /// - Site under construction: name, progress, the stage it is on and its materials, water depth.
        /// - Finished building: name, what it does, water depth (storage: stock and room).
        /// - Free hex or empty base: water depth and a short list of what can be built there.
        /// </summary>
        public string HexInfo(HexCell c, out string head)
        {
            float depth = DepthOver(c);
            string water = depth > 0.02f ? $"Water: {depth:0.00} m deep" : "Water: dry ground";
            var lines = new List<string>();
            var top = TopSiteOn(c);

            if (top == null && c.building == "Main lodge")
            {
                head = "Main lodge";
                lines.Add("Home of the family: beavers sleep here and hide after a tail slap.");
                lines.Add(water);
                return string.Join("\n", lines);
            }
            if (top == null)
            {
                head = "Free hex";
                lines.Add(water);
                string extra = ExtraHexInfo(c);
                if (extra.Length > 0) lines.Add(extra);
                lines.Add("Can build: " + BuildList(c));
                return string.Join("\n", lines);
            }

            head = top.Title + (top.priority && top.HasWork ? "  <color=#ffd84a>[flag]</color>" : "");
            if (top.repair != null)
            {
                lines.Add($"<color=#80d0ff>Repairing: {Mathf.RoundToInt(top.Progress01() * 100f)}%</color>  - still needed: {top.MissingText()}");
                if (top is DamSite rd) lines.Add($"Condition {rd.condition * 100f:0}%");
            }
            else if (!top.Finished)
            {
                int pct = Mathf.RoundToInt(top.Progress01() * 100f);
                if (top is BuildingSite wb && wb.baseSite != null && !wb.baseSite.Finished)
                    lines.Add("Waiting for the base under it");
                lines.Add($"Building: {pct}%  (stage {top.stage + 2}/{top.StageCount})");
                var next = top.Next;
                var need = new List<string>();
                foreach (Item i in BuildStage.Materials)
                    if (next.Need(i) > 0) need.Add($"{Mat(i)} {top.delivered[i]}/{next.Need(i)}");
                lines.Add($"Now: {next.name} - {string.Join(", ", need)}");
                lines.Add($"Still needed: {top.MissingText()}");
                if (top is DamSite pd && pd.stage >= 0)
                {
                    lines.Add($"Dam top now {pd.Crest - c.minGround:0.0} m, condition {pd.condition * 100f:0}%");
                    if (pd.damaged) lines.Add("<color=#ffb070>Washed down by the water: builders rebuild it</color>");
                }
            }
            else lines.Add(Function(top));
            lines.Add(water);

            // an empty finished base: what can go on it
            if (IsPlatformSite(top) && top.Finished && BuildingOn(top) == null)
                lines.Add("Can build: " + BuildList(c));
            return string.Join("\n", lines);
        }

        /// <summary>What a finished site does, in one or two lines.</summary>
        string Function(Site s)
        {
            if (s is DamSite d)
            {
                string t = d.kind == DamKind.Spillway ? $"Keeps the pond at {d.Crest - d.hex.minGround:0.0} m: water runs over the top without wear"
                    : $"Holds the water back. Top {d.Crest - d.hex.minGround:0.0} m, condition {d.condition * 100f:0}%";
                if (d.platform) t += BuildingOn(d) != null ? $"\nCarries a {BuildingOn(d).Title.ToLower()}: lost if the dam loses a stage" : "\nA nursery or storage can stand on it";
                var cost = d.RepairCost(repairShare);
                if (cost != null) t += $"\n<color=#80d0ff>Repair now for {cost.CostText()} (tool 6 / R)</color>";
                if (d.leakNoise > repairInstinctNoise) t += "\n<color=#ff7060>Leaking: idle beavers come to plug it</color>";
                string defence = FunctionD(d);
                if (defence.Length > 0) t += "\n" + defence;
                return t;
            }
            var b = (BuildingSite)s;
            switch (b.Kind)
            {
                case BuildingKind.Base: return "Platform for one building (nursery or storage). Also holds back a little water.";
                case BuildingKind.PumpBase:
                case BuildingKind.FoodFarm:
                    return FunctionB(b);
                case BuildingKind.MaterialLab:
                case BuildingKind.BuildingLab:
                case BuildingKind.WaterWheel:
                case BuildingKind.Machine:
                    return (b.flooded ? "<color=#ff6050><b>FLOODED</b></color>\n" : "") + FunctionC(b);
                case BuildingKind.AlarmBell:
                    return FunctionD(b);
                case BuildingKind.Nursery:
                    return "Kits are born here from wood (food).\n" + (b.entranceUnderWater
                        ? $"<color=#8fe08f>Entrance under water ({b.entranceDepth:0.00} m): making kits</color>"
                        : $"<color=#ff8a70>Entrance dry ({b.entranceDepth:0.00} m, needs {nurseryMinDepth:0.0} m): no kits</color>");
                default:
                    return $"Keeps {ItemInfo.Lower(b.storeType)} for the family: {b.stock}/{storageCapacity}";
            }
        }

        /// <summary>Short list of the tools that work on this hex now.</summary>
        string BuildList(HexCell c)
        {
            var ok = new List<string>();
            if (CanOrder(BuildTool.Dam, c, out _)) ok.Add("Dam");
            if (CanOrder(BuildTool.Base, c, out _)) ok.Add("Base");
            if (CanOrder(BuildTool.Nursery, c, out _)) ok.Add("Nursery");
            if (CanOrder(BuildTool.Storage, c, out _)) ok.Add("Storage");
            if (CanOrder(BuildTool.FoodFarm, c, out _)) ok.Add("Food farm");
            if (CanOrder(BuildTool.Dig, c, out _)) ok.Add("Dig");
            if (ok.Count == 0) return "nothing";
            string t = string.Join(", ", ok);
            // a base that cannot hold a nursery yet: say why (the most common question)
            var b = BaseOn(c);
            if (b != null && b.Finished && !ok.Contains("Nursery") && BuildingOn(b) == null)
                t += $"\n<size=13>Nursery: needs water {nurseryMinDepth:0.0} m deep next to it (now {DeepestNeighbour(c):0.00} m)</size>";
            return t;
        }

        // ------------------------------------------------------------------ dams and nurseries over time

        /// <summary>
        /// Leak noise and wear. Noise = height difference of the water around the dam times how
        /// leaky the dam is, plus water running over the top. Overflowing water wears the dam down
        /// stage by stage; a dam that is gone raises the alarm.
        /// </summary>
        void UpdateDams(float dt)
        {
            foreach (var s in Dams)
            {
                if (s.stage < 0) { s.leakNoise = 0f; s.overtop = 0f; continue; }
                float over = 0f;
                foreach (int id in s.Cells)
                {
                    Vector3 c = water.CellCenter(id);
                    over = Mathf.Max(over, water.DepthAt(c.x, c.z));
                }
                s.overtop = over;
                float hi = float.MinValue, lo = float.MaxValue;
                foreach (var n in grid.Neighbours(s.hex))
                {
                    float surf = water.SurfaceAt(n.center.x, n.center.z, out bool wet);
                    if (!wet) continue;
                    hi = Mathf.Max(hi, surf); lo = Mathf.Min(lo, surf);
                }
                float head = hi > lo ? hi - lo : 0f;
                s.leakNoise = head * s.CurrentStage.leakiness + over * 3f;

                if (s.wearFactor <= 0f || s.GateOpen) s.leakNoise = head * s.CurrentStage.leakiness; // a spillway or open gate is meant to let water through
                var top = s.CurrentStage;
                // loose sticks wash away first, stones last
                float fragile = top.Woody >= top.Muddy && top.Woody >= top.Stony ? 2f : top.Stony > top.Muddy ? 0.5f : 1f;
                float wear = over > 0.05f && s.wearFactor > 0f && !s.GateOpen ? over * wearRate * s.wearFactor : 0f;
                wear += PressureWear(s, head); // a flood against a full pond: much more likely to break
                wear *= fragile * BagFactor(s);
                if (wear > 0f)
                {
                    s.condition -= wear * dt;
                    if (s.condition <= 0f)
                    {
                        var lost = s.Degrade();
                        LoseBuildingOn(s);
                        UpdateVisual(s);
                        Drop(s.hex, lost, dropShare);
                        // no alarm: the builders (and idle beavers) go and build it up again
                        s.priority = true;
                        if (s.stage < 0) colony.Log($"The {s.Title.ToLower()} at {s.hex.axial} broke! Half of its {lost.name} lies next to it, builders go to rebuild", true);
                        else colony.Log($"Water washed the {lost.name} off the {s.Title.ToLower()} at {s.hex.axial}: builders go to rebuild", true);
                    }
                }
            }
        }

        /// <summary>A nursery only works while its entrance is under water (checked twice a second).</summary>
        void UpdateNurseries(float dt)
        {
            nurseryCheck -= dt;
            if (nurseryCheck > 0f) return;
            nurseryCheck = 0.5f;
            foreach (var b in Buildings)
            {
                if (b.Kind != BuildingKind.Nursery || !b.Finished) continue;
                b.entranceDepth = DeepestNeighbour(b.hex);
                bool ok = b.entranceDepth >= nurseryMinDepth;
                if (b.entranceUnderWater && !ok) colony.Log($"Nursery entrance is dry ({b.entranceDepth:0.00} m): no new kits", true);
                b.entranceUnderWater = ok;
            }
        }

        // ------------------------------------------------------------------ builder jobs

        /// <summary>
        /// Flagged sites first, then the most urgent (loud leaks), then the oldest order.
        /// The builder takes as many items of one kind as it can carry (adult 2, yearling 1),
        /// but never more than the stage still needs.
        /// </summary>
        public bool PickJob(Beaver b, out Site site, out Item item, bool instinctOnly)
        {
            Release(b);
            // stable sort: equal keys keep the order in which the leader gave the orders
            var order = System.Linq.Enumerable.ToList(System.Linq.Enumerable.ThenByDescending(
                System.Linq.Enumerable.OrderByDescending(sites, x => x.priority ? 1 : 0), x => x.Urgency));
            foreach (var s in order)
            {
                if (instinctOnly && !(s is DamSite d && (d.leakNoise >= repairInstinctNoise || d.damaged))) continue;
                if (s is BuildingSite bs && bs.baseSite != null && !bs.baseSite.Finished) continue;
                if (!s.NeedsItem(out item)) continue;
                int free = colony.SupplyStock(item) - pendingPickups[item];
                if (free <= 0) continue;
                var storage = colony.NearestSupplyWith(item, b.transform.position, s.hex.center);
                if (storage == null) continue;
                int count = Mathf.Max(1, Mathf.Min(b.CarryCapacity, Mathf.Min(s.Missing(item), Mathf.Min(free, storage.Stock(item)))));
                s.incoming[item] += count;
                pendingPickups[item] += count;
                jobs[b] = new Job { site = s, item = item, count = count, storage = storage };
                site = s;
                return true;
            }
            site = null; item = Item.None;
            return false;
        }

        public ISupply JobStorage(Beaver b) => jobs.TryGetValue(b, out var job) ? job.storage : null;
        public int JobCount(Beaver b) => jobs.TryGetValue(b, out var job) ? job.count : 0;

        /// <summary>The builder is at the storage: take the items out. Returns how many it got (0 = none left).</summary>
        public int PickUp(Beaver b)
        {
            if (!jobs.TryGetValue(b, out var job)) return 0;
            pendingPickups[job.item] = Mathf.Max(0, pendingPickups[job.item] - job.count);
            int got = 0;
            while (got < job.count && colony.TakeFrom(job.storage, job.item)) got++;
            // someone else emptied the storage: bring what we have, free the rest
            job.site.incoming[job.item] = Mathf.Max(0, job.site.incoming[job.item] - (job.count - got));
            job.count = got;
            if (got == 0) { jobs.Remove(b); return 0; }
            job.pickedUp = true;
            return got;
        }

        /// <summary>The builder finished placing its item.</summary>
        public void Complete(Beaver b)
        {
            if (!jobs.TryGetValue(b, out var job)) return;
            jobs.Remove(b);
            if (!sites.Contains(job.site)) { b.carrying = job.item; b.carryCount = job.count; return; } // order was removed: keep the items
            for (int i = 0; i < job.count; i++)
            {
                if (!job.site.HasWork) { b.carrying = job.item; b.carryCount = job.count - i; return; } // leftovers go back to a storage
                bool repairing = job.site.repair != null;
                bool forRepair = repairing && job.site.repair.Need(job.item) > 0;
                colony.Stat(forRepair ? $"{Mat(job.item)} used for repairs" : $"{Mat(job.item)} built into {job.site.Title.ToLower()}s");
                if (job.site.Deliver(job.item))
                {
                    UpdateVisual(job.site);
                    if (repairing) colony.Log($"{job.site.Title} at {job.site.hex.axial} repaired");
                    else if (job.site.Finished) colony.Log($"{job.site.Title} finished at {job.site.hex.axial}", job.site is BuildingSite);
                }
            }
        }

        public bool StillValid(Beaver b) => jobs.TryGetValue(b, out var job) && sites.Contains(job.site);

        public void Release(Beaver b)
        {
            farmHeading.Remove(b);
            if (!jobs.TryGetValue(b, out var job)) return;
            jobs.Remove(b);
            job.site.incoming[job.item] = Mathf.Max(0, job.site.incoming[job.item] - job.count);
            if (!job.pickedUp) pendingPickups[job.item] = Mathf.Max(0, pendingPickups[job.item] - job.count);
        }

        // ------------------------------------------------------------------ looks

        public void UpdateVisual(Site s)
        {
            if (s.visual != null) Destroy(s.visual);
            s.visual = null;
            if (s.stage < 0) return;
            var root = new GameObject($"{s.Title} {s.hex.axial}");
            root.transform.SetParent(transform);
            s.visual = root;
            if (s is DamSite d) { DamLook(d, root.transform); DefenceLook(d, root.transform); }
            else if (s is BuildingSite b)
            {
                if (b.Kind == BuildingKind.Base) BaseLook(b, root.transform);
                else if (b.Kind == BuildingKind.PumpBase) { BaseLook(b, root.transform); PumpLook(b, root.transform); }
                else if (b.Kind == BuildingKind.FoodFarm) FarmLook(b, root.transform);
                else if (b.Kind == BuildingKind.MaterialLab || b.Kind == BuildingKind.BuildingLab) LabLook(b, root.transform);
                else if (b.Kind == BuildingKind.Machine) MachineLook(b, root.transform);
                else if (b.Kind == BuildingKind.WaterWheel) WheelLook(b, root.transform);
                else if (b.Kind == BuildingKind.AlarmBell) BellLook(b, root.transform);
                else if (b.Kind == BuildingKind.Nursery) NurseryLook(b, root.transform);
                else { StorageLook(b, root.transform); UpdatePiles(b); }
            }
            AddPick(root, s);
        }

        void DamLook(DamSite s, Transform root)
        {
            float bottom = s.hex.minGround - 0.4f;
            root.position = new Vector3(s.hex.center.x, bottom, s.hex.center.z);
            var top = s.CurrentStage;
            Column(root, 0f, s.Crest - bottom, top.Woody >= top.Muddy && top.Woody >= top.Stony ? woodMat : top.Stony > top.Muddy ? stoneMat : mudMat);
            bool sticks = false;
            for (int k = 0; k <= s.stage; k++) if (((DamStage)s.StageAt(k)).Woody > 0) sticks = true;
            if (sticks && s.kind != DamKind.Spillway)
            {
                // sticks lying on the dam, pointing upstream like real dams (Western Producer, n.d.)
                var rng = new System.Random(s.hex.axial.x * 31 + s.hex.axial.y);
                for (int i = 0; i < 9; i++)
                {
                    float a = (float)rng.NextDouble() * 360f;
                    Piece(root, PrimitiveType.Cylinder, woodMat,
                        new Vector3(((float)rng.NextDouble() - 0.5f) * 2.4f, s.Crest - bottom + 0.05f, ((float)rng.NextDouble() - 0.5f) * 2.4f),
                        new Vector3(0.12f, 0.8f, 0.12f), new Vector3(90f, a, 0f));
                }
            }
        }

        void BaseLook(BuildingSite b, Transform root)
        {
            float bottom = b.hex.minGround - 0.4f;
            root.position = new Vector3(b.hex.center.x, bottom, b.hex.center.z);
            float top = b.Floor - bottom;
            Column(root, 0f, b.stage >= 1 ? top - 0.15f : top, stoneMat);
            if (b.stage >= 1) Column(root, top - 0.15f, 0.15f, mudMat);
            // a few stones on the side, so it reads as a stone platform
            var rng = new System.Random(b.hex.axial.x * 17 + b.hex.axial.y * 5);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f + (float)rng.NextDouble() * 30f;
                Vector3 p = Quaternion.Euler(0f, a, 0f) * Vector3.forward * 1.75f;
                Piece(root, PrimitiveType.Cube, stoneMat, new Vector3(p.x, top * 0.5f, p.z), Vector3.one * (0.35f + (float)rng.NextDouble() * 0.2f),
                    new Vector3((float)rng.NextDouble() * 40f, a, (float)rng.NextDouble() * 40f));
            }
        }

        void NurseryLook(BuildingSite b, Transform root)
        {
            root.position = new Vector3(b.hex.center.x, b.Floor, b.hex.center.z);
            if (b.stage >= 1) Piece(root, PrimitiveType.Sphere, colony.lodgeMat, new Vector3(0f, 0.1f, 0f), new Vector3(2.8f, 1.6f, 2.8f), Vector3.zero);
            var rng = new System.Random(b.hex.axial.x * 13 + b.hex.axial.y);
            int sticks = b.stage >= 1 ? 14 : 22;
            for (int i = 0; i < sticks; i++)
            {
                float a = i * (360f / sticks) + (float)rng.NextDouble() * 10f;
                float up = b.stage >= 1 ? 15f + (float)rng.NextDouble() * 40f : 50f;
                var dir = Quaternion.Euler(-up, a, 0f) * Vector3.forward;
                Vector3 p = b.stage >= 1 ? new Vector3(dir.x * 1.3f, 0.1f + dir.y * 0.8f, dir.z * 1.3f) : new Vector3(dir.x * 0.7f, 0.5f, dir.z * 0.7f);
                Piece(root, PrimitiveType.Cylinder, woodMat, p, new Vector3(0.09f, 0.7f, 0.09f),
                    (Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f)).eulerAngles);
            }
            // a pale ring marks it as the nursery
            Piece(root, PrimitiveType.Cylinder, stoneMat, new Vector3(0f, 0.02f, 0f), new Vector3(3.2f, 0.02f, 3.2f), Vector3.zero);
        }

        void StorageLook(BuildingSite b, Transform root)
        {
            root.position = new Vector3(b.hex.center.x, b.Floor, b.hex.center.z);
            // four posts and two beams: an open shelter, so the piles stay visible
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -1.1f : 1.1f, z = i < 2 ? -1.1f : 1.1f;
                Piece(root, PrimitiveType.Cylinder, woodMat, new Vector3(x, 0.7f, z), new Vector3(0.14f, 0.7f, 0.14f), Vector3.zero);
            }
            Piece(root, PrimitiveType.Cube, woodMat, new Vector3(0f, 1.45f, -1.1f), new Vector3(2.5f, 0.1f, 0.12f), Vector3.zero);
            Piece(root, PrimitiveType.Cube, woodMat, new Vector3(0f, 1.45f, 1.1f), new Vector3(2.5f, 0.1f, 0.12f), Vector3.zero);
            if (b.stage >= 1)
            {
                // open roof of three planks, so the piles stay visible from the camera
                for (int i = -1; i <= 1; i++)
                    Piece(root, PrimitiveType.Cube, woodMat, new Vector3(i * 0.9f, 1.55f, 0f), new Vector3(0.25f, 0.08f, 2.6f), Vector3.zero);
                Piece(root, PrimitiveType.Cylinder, mudMat, new Vector3(0f, 0.02f, 0f), new Vector3(2.9f, 0.03f, 2.9f), Vector3.zero);
            }
        }

        /// <summary>Piles in the storage grow with the stock: logs, a mud heap and stones.</summary>
        void UpdatePiles(BuildingSite b)
        {
            if (b.visual == null || !b.Finished) return;
            var old = b.visual.transform.Find("Piles");
            if (old != null) Destroy(old.gameObject);
            var piles = new GameObject("Piles").transform;
            piles.SetParent(b.visual.transform, false);
            int n = Mathf.CeilToInt(Mathf.Min(b.stock, storageCapacity) / 3f);
            var t = b.storeType;
            for (int i = 0; i < n; i++)
            {
                int row = i / 4, col = i % 4;
                Vector3 p = new Vector3(-0.75f + col * 0.45f, 0.12f + row * 0.2f, -0.55f + (row % 2) * 0.2f + (i % 3) * 0.35f);
                if (t == Item.Wood || t == Item.Branches || t == Item.PressedWood)
                    Piece(piles, PrimitiveType.Cylinder, woodMat, p, t == Item.Branches ? new Vector3(0.08f, 0.35f, 0.08f) : new Vector3(0.16f, 0.4f, 0.16f), new Vector3(0f, 0f, 90f));
                else if (t == Item.Stone || t == Item.Gravel)
                    Piece(piles, PrimitiveType.Cube, stoneMat, p, Vector3.one * (t == Item.Gravel ? 0.14f : 0.24f), new Vector3(i * 23f, i * 47f, i * 11f));
                else if (t == Item.Food)
                    Piece(piles, PrimitiveType.Sphere, FoodMat, p, new Vector3(0.3f, 0.12f, 0.3f), Vector3.zero);
                else
                    Piece(piles, PrimitiveType.Sphere, mudMat, p, new Vector3(0.35f, 0.2f, 0.35f), Vector3.zero);
            }
        }

        Material foodMat;
        /// <summary>Green material for food (plants) piles, made once from the stone material.</summary>
        public Material FoodMat
        {
            get
            {
                if (foodMat == null) { foodMat = new Material(stoneMat); foodMat.color = ItemInfo.Colour(Item.Food); foodMat.SetColor("_BaseColor", ItemInfo.Colour(Item.Food)); }
                return foodMat;
            }
        }

        void Column(Transform root, float y, float height, Material m)
        {
            var go = new GameObject("Column", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);
            go.transform.localScale = new Vector3(1f, Mathf.Max(0.01f, height), 1f);
            go.GetComponent<MeshFilter>().sharedMesh = prism;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
        }

        static void Piece(Transform parent, PrimitiveType type, Material m, Vector3 pos, Vector3 scale, Vector3 euler)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
        }

        /// <summary>Six-sided column, 1 m high, matching the pointy-top hexes of the grid.</summary>
        static Mesh HexPrism(float r)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            Vector3 C(int k, float y) { float a = Mathf.Deg2Rad * (60f * k - 30f); return new Vector3(r * Mathf.Cos(a), y, r * Mathf.Sin(a)); }
            for (int k = 0; k < 6; k++)
            {
                int s = v.Count;
                v.Add(C(k, 0f)); v.Add(C(k + 1, 0f)); v.Add(C(k + 1, 1f)); v.Add(C(k, 1f));
                t.Add(s); t.Add(s + 2); t.Add(s + 1); t.Add(s); t.Add(s + 3); t.Add(s + 2);
            }
            int top = v.Count;
            v.Add(new Vector3(0f, 1f, 0f));
            for (int k = 0; k < 6; k++) v.Add(C(k, 1f));
            for (int k = 0; k < 6; k++) { t.Add(top); t.Add(top + 1 + (k + 1) % 6); t.Add(top + 1 + k); }
            var m = new Mesh { name = "Hex prism" };
            m.SetVertices(v); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }
}
