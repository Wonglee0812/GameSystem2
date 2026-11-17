# Phase A: ColonyManager edits (food, foragers, winter, death). Safe to run twice.
import sys, os
p = os.path.join(sys.argv[1] if len(sys.argv) > 1 else ".", "Assets/Sandbox/Scripts/Colony/ColonyManager.cs")
s = open(p, encoding="utf-8").read()
if "LodgeFrozenIn" in s:
    print("ColonyManager.cs: already applied"); sys.exit(0)
E = [
("""        public float foodPerMeal = 70f;
        public float eatTime = 3f;""","""        public float foodPerMeal = 70f;
        public float eatTime = 3f;
        [Tooltip("Game days a beaver lives with an empty stomach before it dies.")]
        public float starveDays = 1.5f;"""),
("""        [Tooltip("Wood (food) a litter costs, taken from the storages.")]
        public int litterWood = 5;""","""        [Tooltip("Food a litter costs, taken from the food storages.")]
        public int litterFood = 5;"""),
("""        public BuildOrders orders;
        public DayNight dayNight;""","""        public BuildOrders orders;
        public DayNight dayNight;
        public Seasons seasons;"""),
("""        public float cutTime = 4f;
        public float digTime = 3f;""","""        public float cutTime = 4f;
        public float digTime = 3f;
        [Tooltip("Seconds to pick water plants or cut branches for food.")]
        public float forageTime = 4f;"""),
("""        public int buildWorkers = 1;""","""        public int buildWorkers = 1;
        public int forageWorkers = 2;"""),
("""        System.Collections.IEnumerator Start()
        {""","""        void Awake()
        {
            if (seasons == null) seasons = GetComponent<Seasons>();
            if (seasons == null) seasons = gameObject.AddComponent<Seasons>();
            seasons.colony = this;
        }

        public bool IsWinter => seasons != null && seasons.IsWinter;
        public bool GameOver { get; private set; }

        System.Collections.IEnumerator Start()
        {"""),
("""            woodWorkers = 3; earthWorkers = 3; buildWorkers = 2;""","""            forageWorkers = 2; woodWorkers = 3; earthWorkers = 2; buildWorkers = 2;"""),
("""            if (groupsChanged) ApplyGroups();
            UpdateNurseries(dt);""","""            if (groupsChanged) ApplyGroups();
            UpdateNurseries(dt);
            lodgeCheck -= dt;
            if (lodgeCheck <= 0f) { lodgeCheck = 0.5f; MeasureLodgeWater(); }"""),
("""                if (Stock(Item.Wood) < family.litterWood) { n.litterTimer = family.litterDays * DaySeconds; n.waitReason = $"needs {family.litterWood} wood"; continue; } // wait for food
""","""                if (IsWinter) { n.waitReason = "no kits in winter"; n.litterTimer = Mathf.Min(n.litterTimer, family.litterDays * DaySeconds); continue; }
                if (Stock(Item.Food) < family.litterFood) { n.litterTimer = family.litterDays * DaySeconds; n.waitReason = $"needs {family.litterFood} food"; continue; } // wait for food
"""),
("""                for (int i = 0; i < family.litterWood; i++) TakeAny(Item.Wood);
                Stat("wood used for kits", family.litterWood);""","""                for (int i = 0; i < family.litterFood; i++) TakeAny(Item.Food);
                Stat("food used for kits", family.litterFood);"""),
("""                Log($"{born} kit(s) born in the nursery: {string.Join(", ", names)} (-{family.litterWood} wood)", true);""","""                Log($"{born} kit(s) born in the nursery: {string.Join(", ", names)} (-{family.litterFood} food)", true);"""),
("""        // ------------------------------------------------------------------ work targets""","""        // ------------------------------------------------------------------ winter and death

        float lodgeCheck;
        /// <summary>Deepest water next to the main lodge (the lodge entrance), measured twice a second.</summary>
        public float LodgeWaterDepth { get; private set; }

        void MeasureLodgeWater()
        {
            float best = 0f;
            foreach (var l in lodgeCells)
                foreach (var n in grid.Neighbours(l))
                {
                    if (n.blocked) continue;
                    best = Mathf.Max(best, water.DepthAt(n.center.x, n.center.z));
                }
            LodgeWaterDepth = best;
        }

        float IceLimit => seasons != null ? seasons.Ice + seasons.iceMargin : 0f;

        /// <summary>In winter the lodge entrance must be deeper than the ice, or nobody can get out.</summary>
        public bool LodgeFrozenIn => IsWinter && seasons.Ice > 0.01f && LodgeWaterDepth < IceLimit;

        /// <summary>A food storage can be reached in winter when the water next to it is deeper than the ice.</summary>
        public bool ReachableNow(BuildingSite s)
        {
            if (!IsWinter || seasons.Ice <= 0.01f) return true;
            return orders.DeepestNeighbour(s.hex) >= IceLimit;
        }

        public int ReachableFood() { int n = 0; foreach (var s in Storages) if (s.storeType == Item.Food && ReachableNow(s)) n += s.stock; return n; }

        /// <summary>Nearest food storage with food that can be reached now (under the ice in winter).</summary>
        public BuildingSite NearestFood(Vector3 from) => NearestStorage(from, s => s.storeType == Item.Food && s.stock > 0 && ReachableNow(s));

        /// <summary>A beaver died of hunger. When the last one dies, the game is over.</summary>
        public void Die(Beaver b, string cause)
        {
            Release(b);
            beavers.Remove(b);
            Stat("beavers died");
            Log($"{b.beaverName} died ({cause})", true);
            Destroy(b.gameObject);
            ApplyGroups();
            if (beavers.Count == 0 && !GameOver)
            {
                GameOver = true;
                Log($"The whole family is gone. You lasted until {(seasons != null ? $"{seasons.Current} of year {seasons.Year}" : "now")}.", true);
            }
        }

        // ------------------------------------------------------------------ work targets"""),
("""        public void Release(Beaver b)
        {""","""        /// <summary>
        /// Where a forager finds food now. Spring and summer: water plants in shallow, slow water
        /// near the lodge. Autumn: branches from trees. Winter: nothing (the water is frozen).
        /// </summary>
        public bool PickForageSpot(Beaver b, out Vector3 pos, out HexCell hex, out bool tree)
        {
            pos = Vector3.zero; hex = null; tree = false;
            if (IsWinter) return false;
            if (seasons != null && seasons.Current == Season.Autumn)
            {
                tree = true;
                return PickTree(b, out pos, out hex);
            }
            Release(b);
            var options = new List<HexCell>();
            foreach (var c in grid.Cells)
            {
                if (c.blocked || reserved.ContainsValue(c)) continue;
                float d = water.DepthAt(c.center.x, c.center.z);
                if (d < 0.12f || d > 1.2f || water.VelocityAt(c.center.x, c.center.z).magnitude > 0.8f) continue;
                options.Add(c);
            }
            options.Sort((x, y) => Flat(x.center - LodgeCenter).CompareTo(Flat(y.center - LodgeCenter)));
            if (options.Count > 10) options.RemoveRange(10, options.Count - 10);
            if (options.Count == 0) return false;
            hex = options[Random.Range(0, options.Count)];
            reserved[b] = hex;
            pos = hex.center + new Vector3(Random.Range(-0.6f, 0.6f), 0f, Random.Range(-0.6f, 0.6f));
            return true;
        }

        public void Release(Beaver b)
        {"""),
("""            int total = Workers;
            woodWorkers = Mathf.Clamp(woodWorkers, 0, total);
            earthWorkers = Mathf.Clamp(earthWorkers, 0, total - woodWorkers);
            buildWorkers = Mathf.Clamp(buildWorkers, 0, total - woodWorkers - earthWorkers);
            int w = woodWorkers, e = earthWorkers, bl = buildWorkers;
            foreach (var b in beavers)
            {
                if (!b.CanWork) { b.SetGroup(WorkGroup.Idle); continue; }
                if (w > 0) { b.SetGroup(WorkGroup.Wood); w--; }""","""            int total = Workers;
            forageWorkers = Mathf.Clamp(forageWorkers, 0, total);
            woodWorkers = Mathf.Clamp(woodWorkers, 0, total - forageWorkers);
            earthWorkers = Mathf.Clamp(earthWorkers, 0, total - forageWorkers - woodWorkers);
            buildWorkers = Mathf.Clamp(buildWorkers, 0, total - forageWorkers - woodWorkers - earthWorkers);
            int f = forageWorkers, w = woodWorkers, e = earthWorkers, bl = buildWorkers;
            foreach (var b in beavers)
            {
                if (!b.CanWork) { b.SetGroup(WorkGroup.Idle); continue; }
                if (f > 0) { b.SetGroup(WorkGroup.Forage); f--; }
                else if (w > 0) { b.SetGroup(WorkGroup.Wood); w--; }"""),
("""        public int IdleWorkers => Workers - woodWorkers - earthWorkers - buildWorkers;""","""        public int IdleWorkers => Workers - forageWorkers - woodWorkers - earthWorkers - buildWorkers;"""),
]
for a, b in E:
    if a not in s:
        print("ANCHOR MISSING, not written:", a[:70]); sys.exit(1)
    s = s.replace(a, b, 1)
open(p, "w", encoding="utf-8").write(s)
print("ColonyManager.cs: ok")
