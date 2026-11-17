using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Phase B of the build orders (plan v2): Dam Mk2, Spillway, Pump base (Base Mk2), Food farm,
    /// Dig and Fell tree. The shared rules (stages, builders, repair, 50% drops) stay in
    /// BuildOrders.cs; this part only adds the new types and their rules.
    /// - Dam Mk2: stronger (4 stages, wears less) and a platform: a nursery or storage can stand
    ///   on it. When the dam loses a stage, the building on it is lost (50% comes back as piles).
    /// - Spillway: a low stone dam that never wears; water runs over its top, so the pond stays at
    ///   a fixed level.
    /// - Pump base: a base that moves water from the lowest wet hex nearby (downstream) into the
    ///   hexes around it, up to pumpLift above the water it takes from. Water is moved, not made.
    /// - Food farm: water plants (water lilies, duckweed) on a calm, shallow hex. Grows food that
    ///   foragers pick up; nothing grows in winter or when the hex runs dry.
    /// - Dig: diggers dig at the ordered hex; every few loads the ground goes down one step.
    /// - Fell tree: wood cutters fell the marked trees; a felled tree gives a lot of wood at once
    ///   and is gone for good (unmarked trees are only gnawed and stay).
    /// </summary>
    public partial class BuildOrders
    {
        [Header("Phase B: dams")]
        public DamDef damMk2 = new DamDef
        {
            kind = DamKind.Mk2, name = "Dam Mk2", wearFactor = 0.4f, platform = true,
            rule = "Stronger dam (wears 60% slower). When finished it works as a base: a nursery or storage can stand on it. If the dam loses a stage, the building on it is lost (half comes back).",
            stages = new[]
            {
                new DamStage { name = "base (stones + mud)", stone = 3, mud = 2, height = 0.7f, leakiness = 0.5f },
                new DamStage { name = "body (pressed wood)", pressedWood = 3, height = 1.6f, leakiness = 0.9f },
                new DamStage { name = "seal (mud)", mud = 3, height = 1.8f, leakiness = 0.05f },
                new DamStage { name = "cement cap", cement = 2, stone = 1, height = 2.0f, leakiness = 0.03f },
            }
        };
        public DamDef spillway = new DamDef
        {
            kind = DamKind.Spillway, name = "Spillway", wearFactor = 0f,
            rule = "Low stone dam (1.0 m). Water runs over the top without wear, so the pond behind it stays at this level. Put it in a dam line where the extra water should go.",
            stages = new[]
            {
                new DamStage { name = "base (stones + mud)", stone = 3, mud = 2, height = 0.5f, leakiness = 0.4f },
                new DamStage { name = "gravel crest", gravel = 4, mud = 1, height = 1.0f, leakiness = 0.05f },
            }
        };

        [Header("Phase B: pump base")]
        public BuildingDef pumpBaseDef = new BuildingDef
        {
            kind = BuildingKind.PumpBase, name = "Base Mk2",
            rule = "Base with a pump: moves water from the lowest wet hex nearby (downstream) into the hexes around it, up to 0.5 m higher. A nursery on it needs 0.5 m less water.",
            stages = new[] { new BuildStage { name = "stones", stone = 4, mud = 1 }, new BuildStage { name = "pipe", pressedWood = 2, wood = 2 }, new BuildStage { name = "cement top", mud = 2, cement = 1 } }
        };
        [Tooltip("How much higher than the water it takes from the pump can lift the water (m).")]
        public float pumpLift = 0.5f;
        [Tooltip("Water the pump moves per second (m3/s).")]
        public float pumpRate = 0.6f;
        [Tooltip("The pump looks for its source this many hexes away (min and max).")]
        public int pumpReachMin = 2, pumpReachMax = 5;

        [Header("Phase B: food farm")]
        public BuildingDef farmDef = new BuildingDef
        {
            kind = BuildingKind.FoodFarm, name = "Food farm",
            rule = "Water lilies and duckweed on a calm hex with water 0.15-1.0 m deep. Grows food for the foragers; nothing grows in winter.",
            stages = new[] { new BuildStage { name = "mud and sand beds", mud = 2, sand = 2 }, new BuildStage { name = "planting", mud = 2, wood = 1 } }
        };
        [Tooltip("Food a farm grows per game day in spring and summer (half in autumn, none in winter).")]
        public float farmFoodPerDay = 8f;
        [Tooltip("Most food a farm holds before the foragers pick it up.")]
        public int farmMax = 12;
        public float farmMinDepth = 0.15f, farmMaxDepth = 1.0f, farmMaxSpeed = 0.6f;

        [Header("Phase B: dig and fell")]
        [Tooltip("Ground removed by one dig order (m).")]
        public float digStep = 0.35f;
        [Tooltip("Deepest a hex can be dug (m below the start).")]
        public float digMax = 1.5f;
        [Tooltip("Loads of mud or stone the diggers take out for one step.")]
        public int digLoadsPerStep = 3;

        public BuildingDef PhaseBDef(BuildingKind k) => k == BuildingKind.PumpBase ? pumpBaseDef : k == BuildingKind.FoodFarm ? farmDef : null;

        // ------------------------------------------------------------------ helpers

        public static bool IsDamTool(BuildTool t) => t == BuildTool.Dam || t == BuildTool.DamMk2 || t == BuildTool.Spillway || t == BuildTool.Levee || t == BuildTool.Floodgate;

        public DamStage[] DamStagesFor(BuildTool t) => t == BuildTool.DamMk2 ? damMk2.stages : t == BuildTool.Spillway ? spillway.stages : t == BuildTool.Levee ? levee.stages : t == BuildTool.Floodgate ? floodgate.stages : damStages;

        /// <summary>Sites that can carry a building: base, pump base and Dam Mk2.</summary>
        public static bool IsPlatformSite(Site s) => s is BuildingSite b ? b.IsBase : s is DamSite d && d.platform;

        /// <summary>The platform on a hex (base, pump base or Dam Mk2), or null.</summary>
        public Site PlatformOn(HexCell c)
        {
            foreach (var s in sites) if (s.hex == c && IsPlatformSite(s)) return s;
            return null;
        }

        IEnumerable<BuildingSite> OfKind(BuildingKind k) { foreach (var b in Buildings) if (b.Kind == k) yield return b; }

        // ------------------------------------------------------------------ order rules

        bool CanOrderB(BuildTool tool, HexCell c, out string why)
        {
            float depth = DepthOver(c);
            string here = depth > 0.02f ? $"Water here: {depth:0.00} m deep" : "Dry ground";
            switch (tool)
            {
                case BuildTool.FoodFarm:
                {
                    if (SiteOn(c) != null) { why = "Something is already planned here"; return false; }
                    if (DigOrderOn(c) != null) { why = "Digging is planned here"; return false; }
                    float speed = water.VelocityAt(c.center.x, c.center.z).magnitude;
                    if (depth < farmMinDepth) { why = $"{here}\nWater plants need water {farmMinDepth:0.00}-{farmMaxDepth:0.0} m deep"; return false; }
                    if (depth > farmMaxDepth) { why = $"{here}\nToo deep for water plants (max {farmMaxDepth:0.0} m)"; return false; }
                    if (speed > farmMaxSpeed) { why = $"{here}, flowing {speed:0.0} m/s\nToo fast: water plants need calm water"; return false; }
                    why = $"{here}, calm\nGrows about {farmFoodPerDay:0} food per day (not in winter)";
                    return true;
                }
                case BuildTool.Dig:
                {
                    if (SiteOn(c) != null) { why = "Something is built or planned here"; return false; }
                    int trees = colony.TreesOn(c, false);
                    if (trees > 0) { why = $"{trees} tree(s) stand here: fell them first (X)"; return false; }
                    float dug = Dug(c);
                    var order = DigOrderOn(c);
                    float planned = dug + (order != null ? order.target - order.dug : 0f);
                    if (planned >= digMax - 0.01f) { why = $"Already dug {dug:0.00} m (max {digMax:0.0} m)"; return false; }
                    float next = Mathf.Min(digStep, digMax - planned);
                    why = $"{here}\nDig {next:0.00} m deeper ({digLoadsPerStep} loads for the diggers). Dug so far: {dug:0.00} m" +
                          (order != null ? $", ordered: {order.target - order.dug:0.00} m more" : "");
                    return true;
                }
                case BuildTool.Fell:
                {
                    int trees = colony.TreesOn(c, false), marked = colony.TreesOn(c, true);
                    if (trees == 0) { why = "No trees here"; return false; }
                    if (marked >= trees) { why = $"All {trees} tree(s) here are marked. Right-click to take the marks off"; return false; }
                    why = $"Mark {trees - marked} tree(s) to fell: {colony.fellWood} wood each, the tree is gone for good";
                    return true;
                }
            }
            why = "";
            return false;
        }

        void OrderB(BuildTool tool, HexCell c)
        {
            switch (tool)
            {
                case BuildTool.FoodFarm:
                    sites.Add(new BuildingSite(farmDef, c, null, null, water, baseHeight));
                    break;
                case BuildTool.Dig:
                {
                    var o = DigOrderOn(c);
                    if (o == null) { o = new DigOrder { hex = c }; digOrders[c] = o; }
                    o.target += Mathf.Min(digStep, digMax - Dug(c) - (o.target - o.dug));
                    break;
                }
                case BuildTool.Fell:
                    colony.MarkTrees(c);
                    break;
            }
        }

        // ------------------------------------------------------------------ every frame

        void UpdatePhaseB(float dt)
        {
            foreach (var o in digOrders.Values) grid.DrawHighlight(o.hex, new Color(0.75f, 0.5f, 0.25f));
            foreach (var h in colony.MarkedHexes()) grid.DrawHighlight(h, new Color(0.95f, 0.45f, 0.2f));
            UpdatePumps(dt);
            UpdateFarms(dt);
        }

        // ------------------------------------------------------------------ Dam Mk2: building lost when the dam breaks

        /// <summary>A Dam Mk2 lost a stage: the building on it falls into the water. Half of it (and half of a storage's stock) comes back as piles.</summary>
        void LoseBuildingOn(DamSite d)
        {
            var b = BuildingOn(d);
            if (b == null) return;
            for (int st = 0; st <= b.stage && st < b.StageCount; st++) Drop(b.hex, b.StageAt(st), dropShare);
            foreach (Item i in BuildStage.Materials) colony.DropPile(b.hex, i, b.Loose(i));
            if (b.Kind == BuildingKind.Storage && b.stock > 0) colony.DropPile(b.hex, b.storeType, Mathf.FloorToInt(b.stock * dropShare + 0.5f));
            if (b.Kind == BuildingKind.Machine) colony.industry?.DropBuffer(b);
            foreach (var pair in new List<KeyValuePair<Beaver, Job>>(jobs))
                if (pair.Value.site == b || ReferenceEquals(pair.Value.storage, b)) Release(pair.Key);
            b.hex.blocked = false; b.hex.building = null;
            b.Remove();
            sites.Remove(b);
            colony.Log($"The {d.Title} at {d.hex.axial} lost a stage: the {b.Title.ToLower()} on it is lost (half comes back as piles)", true);
        }

        // ------------------------------------------------------------------ pump base

        readonly Dictionary<BuildingSite, HexCell> pumpSource = new Dictionary<BuildingSite, HexCell>();
        float pumpSearch;

        /// <summary>
        /// Each finished pump takes water from its source hex (the lowest water surface 2-5 hexes
        /// away, so downstream) and puts it into the free hexes around it, until their surface is
        /// pumpLift above the source. The same volume leaves the source: water is moved, not made.
        /// </summary>
        void UpdatePumps(float dt)
        {
            pumpSearch -= dt;
            bool search = pumpSearch <= 0f;
            if (search) pumpSearch = 1f;
            foreach (var p in OfKind(BuildingKind.PumpBase))
            {
                if (!p.Finished) { p.pumpStatus = ""; continue; }
                if (colony.IsWinter && colony.seasons.Ice > 0.05f) { p.pumpStatus = "Frozen: no pumping in winter"; continue; }
                if (search || !pumpSource.ContainsKey(p)) pumpSource[p] = FindPumpSource(p.hex);
                var src = pumpSource[p];
                if (src == null) { p.pumpStatus = "No water nearby to pump"; continue; }

                var srcCells = CellsOf(src);
                float srcSurface = 0f; int wetSrc = 0;
                foreach (int id in srcCells) if (water.CellDepth(id) > 0.02f) { srcSurface += water.CellSurface(id); wetSrc++; }
                if (wetSrc == 0) { p.pumpStatus = "The source ran dry"; continue; }
                srcSurface /= wetSrc;
                float target = srcSurface + pumpLift;

                var needy = new List<int>();
                foreach (var n in grid.Neighbours(p.hex))
                {
                    if (n == src || n.blocked || SiteOn(n) != null) continue;
                    foreach (int id in CellsOf(n)) if (water.CellSurface(id) < target - 0.01f) needy.Add(id);
                }
                if (needy.Count == 0) { p.pumpStatus = $"Water around is {pumpLift:0.0} m above the source: pump resting"; continue; }

                float want = pumpRate * dt, got = 0f;
                for (int pass = 0; pass < 3 && got < want - 1e-5f; pass++)
                {
                    float share = (want - got) / srcCells.Count;
                    foreach (int id in srcCells) got += water.TakeWater(id, Mathf.Min(share, Mathf.Max(0f, water.CellDepth(id) - 0.05f) * water.CellSize * water.CellSize));
                }
                foreach (int id in needy) water.AddWater(id, got / needy.Count);
                p.pumped += got;
                p.pumpStatus = $"Pumping {got / Mathf.Max(dt, 1e-4f):0.0} m3/s from {src.axial} (+{pumpLift:0.0} m)";
            }
        }

        HexCell FindPumpSource(HexCell at)
        {
            HexCell best = null; float lowest = float.MaxValue;
            foreach (var c in grid.Cells)
            {
                int d = HexPathfinder.Distance(c.axial, at.axial);
                if (d < pumpReachMin || d > pumpReachMax || c.blocked || SiteOn(c) != null) continue;
                if (water.DepthAt(c.center.x, c.center.z) < 0.15f) continue;
                float s = water.SurfaceAt(c.center.x, c.center.z, out bool wet);
                if (wet && s < lowest) { lowest = s; best = c; }
            }
            return best;
        }

        void PumpLook(BuildingSite b, Transform root)
        {
            if (b.stage < 1) return;
            // a hollow log pipe down to the water and a small mud tower
            float top = b.Floor - root.position.y;
            Piece(root, PrimitiveType.Cylinder, woodMat, new Vector3(1.3f, top - 0.4f, 0f), new Vector3(0.35f, 0.9f, 0.35f), new Vector3(0f, 0f, 35f));
            if (b.stage >= 2) Piece(root, PrimitiveType.Cylinder, mudMat, new Vector3(0f, top + 0.35f, 0f), new Vector3(0.8f, 0.35f, 0.8f), Vector3.zero);
        }

        // ------------------------------------------------------------------ food farm

        readonly Dictionary<Beaver, BuildingSite> farmHeading = new Dictionary<Beaver, BuildingSite>();

        void UpdateFarms(float dt)
        {
            float day = colony.dayNight != null ? colony.dayNight.dayLength : 240f;
            foreach (var f in OfKind(BuildingKind.FoodFarm))
            {
                if (!f.Finished) continue;
                f.farmDry = water.DepthAt(f.hex.center.x, f.hex.center.z) < 0.08f;
                var se = colony.seasons;
                float rate = se == null ? 1f : se.Current == Season.Winter ? 0f : se.Current == Season.Autumn ? 0.5f : 1f;
                if (f.farmDry || f.farmStock >= farmMax) rate = 0f;
                int before = f.farmStock;
                f.growth += farmFoodPerDay * rate * dt / day;
                while (f.growth >= 1f && f.farmStock < farmMax) { f.growth -= 1f; f.farmStock++; }
                if (f.farmStock >= farmMax) f.growth = 0f;
                if (f.farmStock != before) UpdateVisual(f);
            }
        }

        /// <summary>Nearest finished farm with food that no other forager is already going to collect.</summary>
        public BuildingSite PickFarm(Beaver b)
        {
            farmHeading.Remove(b);
            BuildingSite best = null; float bd = float.MaxValue;
            foreach (var f in OfKind(BuildingKind.FoodFarm))
            {
                if (!f.Finished) continue;
                int going = 0;
                foreach (var v in farmHeading.Values) if (v == f) going++;
                if (f.farmStock - going * 2 < 1) continue;
                float d = (f.hex.center - b.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = f; }
            }
            if (best != null) farmHeading[b] = best;
            return best;
        }

        /// <summary>The forager is at the farm: take up to n food. Returns how many.</summary>
        public int TakeFarmFood(Beaver b, BuildingSite f, int n)
        {
            farmHeading.Remove(b);
            if (!sites.Contains(f)) return 0;
            int got = Mathf.Min(n, f.farmStock);
            f.farmStock -= got;
            if (got > 0) UpdateVisual(f);
            return got;
        }

        void FarmLook(BuildingSite b, Transform root)
        {
            float surface = water.SurfaceAt(b.hex.center.x, b.hex.center.z, out bool wet);
            root.position = new Vector3(b.hex.center.x, wet ? surface + 0.02f : b.hex.minGround + 0.05f, b.hex.center.z);
            // mud beds: a low brown ring; planted: lily pads, more of them with more food
            Piece(root, PrimitiveType.Cylinder, mudMat, new Vector3(0f, -0.05f, 0f), new Vector3(3.0f, 0.02f, 3.0f), Vector3.zero);
            if (b.stage < 1) return;
            var rng = new System.Random(b.hex.axial.x * 7 + b.hex.axial.y * 3);
            int pads = 5 + b.farmStock;
            for (int i = 0; i < pads; i++)
            {
                float a = (float)rng.NextDouble() * 360f, r = 0.3f + (float)rng.NextDouble() * 1.2f;
                Vector3 p = Quaternion.Euler(0f, a, 0f) * Vector3.forward * r;
                Piece(root, PrimitiveType.Cylinder, FoodMat, new Vector3(p.x, 0.01f, p.z), new Vector3(0.45f, 0.01f, 0.45f), Vector3.zero);
            }
        }

        // ------------------------------------------------------------------ dig

        public class DigOrder { public HexCell hex; public float target, dug; public int loads; }
        readonly Dictionary<HexCell, DigOrder> digOrders = new Dictionary<HexCell, DigOrder>();
        readonly Dictionary<HexCell, float> dugTotal = new Dictionary<HexCell, float>();

        public DigOrder DigOrderOn(HexCell c) => c != null && digOrders.TryGetValue(c, out var o) ? o : null;
        public IEnumerable<HexCell> DigHexes => digOrders.Keys;
        public bool HasDigOrders => digOrders.Count > 0;
        public float Dug(HexCell c) => dugTotal.TryGetValue(c, out float d) ? d : 0f;

        void CancelDig(HexCell c)
        {
            digOrders.Remove(c);
            colony.Log($"Dig order at {c.axial} removed");
        }

        /// <summary>A digger took one load out of an ordered hex. Every few loads the ground goes down one step.</summary>
        public void DugLoad(HexCell c)
        {
            var o = DigOrderOn(c);
            if (o == null) return;
            o.loads++;
            if (o.loads < digLoadsPerStep) return;
            o.loads = 0;
            float step = Mathf.Min(digStep, o.target - o.dug);
            LowerHex(c, step);
            o.dug += step;
            colony.Stat("hex steps dug");
            if (o.dug >= o.target - 0.001f)
            {
                digOrders.Remove(c);
                colony.Log($"Digging done at {c.axial}: {Dug(c):0.00} m deeper", true);
            }
        }

        /// <summary>Lowers the ground of one hex: terrain mesh, water grid and hex heights.</summary>
        void LowerHex(HexCell c, float amount)
        {
            colony.ground.Lower((x, z) => grid.TryGet(new Vector3(x, 0f, z), out var h) && h == c, amount);
            dugTotal[c] = Dug(c) + amount;
            var cells = new List<int>(CellsOf(c));
            grid.Remeasure(c);
            foreach (var n in grid.Neighbours(c)) { cells.AddRange(CellsOf(n)); grid.Remeasure(n); }
            water.RefreshGround(cells);
        }

        /// <summary>Extra lines in the tooltip of a free hex: digging and trees.</summary>
        string ExtraHexInfo(HexCell c)
        {
            var lines = new List<string>();
            float dug = Dug(c);
            var o = DigOrderOn(c);
            if (o != null) lines.Add($"<color=#d9a066>Dig order: {o.dug:0.00}/{o.target:0.00} m, load {o.loads}/{digLoadsPerStep}</color>");
            else if (dug > 0.01f) lines.Add($"Dug {dug:0.00} m deep");
            int trees = colony.TreesOn(c, false);
            if (trees > 0)
            {
                int marked = colony.TreesOn(c, true);
                lines.Add(marked > 0 ? $"<color=#f0904a>{marked}/{trees} tree(s) marked to fell</color>" : $"{trees} tree(s)");
            }
            return string.Join("\n", lines);
        }

        bool FellMarkedOn(HexCell c) => colony.TreesOn(c, true) > 0;
        void UnmarkFell(HexCell c) { colony.UnmarkTrees(c); colony.Log($"Fell marks at {c.axial} taken off"); }

        // ------------------------------------------------------------------ texts

        string FunctionB(BuildingSite b)
        {
            if (b.Kind == BuildingKind.PumpBase)
            {
                string t = "Platform for one building. Pumps water into the hexes around it.";
                if (b.pumpStatus.Length > 0) t += $"\n{b.pumpStatus}";
                return t;
            }
            if (b.farmDry) return $"Water plants: <color=#ff8a70>dry, nothing grows</color>. Food {b.farmStock}/{farmMax}";
            if (colony.IsWinter) return $"Water plants: frozen, nothing grows in winter. Food {b.farmStock}/{farmMax}";
            return $"Water plants: food {b.farmStock}/{farmMax}, foragers pick it up";
        }
    }
}
