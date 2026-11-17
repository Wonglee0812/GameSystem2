using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>How fast the needs of a beaver change. All values per real second, on a 0-100 scale.</summary>
    [System.Serializable]
    public class NeedsSettings
    {
        public float awakeEnergyPerSecond = 0.3f;
        public float workEnergyPerSecond = 0.8f;
        [Tooltip("Energy use is multiplied by this in daylight (beavers are night animals).")]
        public float dayTiredness = 2f;
        public float sleepEnergyPerSecond = 6f;
        public float tiredBelow = 20f;
        public float wakeAt = 95f;
        public float hungerPerSecond = 0.3f;
        public float hungryBelow = 30f;
        public float foodPerMeal = 70f;
        public float eatTime = 3f;
        [Tooltip("Game days a beaver lives with an empty stomach before it dies.")]
        public float starveDays = 1.5f;
    }

    /// <summary>
    /// The beaver family and its main lodge. Holds the group sizes the player sets, hands out
    /// work targets so two beavers do not pick the same tree or dig spot, and keeps track of
    /// the materials in the storages. The main lodge is only for rest and hiding.
    /// A family is a breeding pair, yearlings and kits (PBS Nature, n.d.). The game starts with a
    /// bigger family (4 adults, 5 yearlings, 3 kits) so the leader has enough workers.
    /// </summary>
    [System.Serializable]
    public class FamilySettings
    {
        [Tooltip("Game days before a kit becomes a yearling (a worker).")]
        public float kitDays = 2f;
        [Tooltip("Game days before a yearling becomes an adult.")]
        public float yearlingDays = 3f;
        [Tooltip("Kits born per litter. Real litters are 2 to 4 kits (Montana Trappers Association, n.d.).")]
        public int litterSize = 2;
        [Tooltip("Game days between two litters of one nursery. Real beavers have one litter a year (Animal Diversity Web, n.d.).")]
        public float litterDays = 1f;
        [Tooltip("Food a litter costs, taken from the food storages.")]
        public int litterFood = 5;
        [Tooltip("Each nursery can raise this many kits at the same time.")]
        public int kitsPerNursery = 4;
        [Tooltip("Largest family. Nurseries stop at this size: more beavers cost a lot of CPU (path finding) and are hard to follow.")]
        public int maxFamily = 30;
    }

    public partial class ColonyManager : MonoBehaviour
    {
        [Header("World")]
        public HexGrid grid;
        public WaterGrid water;
        public GroundHeights ground;
        public Transform forestRoot;
        public BuildOrders orders;
        public DayNight dayNight;
        public Seasons seasons;
        public Industry industry;
        public FloodSystem floods;
        /// <summary>The leader rang the alarm bell: the family stays in the lodge until the all clear.</summary>
        public bool bellRinging;

        [Header("Needs and alarm")]
        public NeedsSettings needs = new NeedsSettings();
        public float alarmSeconds = 15f;
        public Vector2 preferredLodgeSpot = new Vector2(46f, 35f);

        [Header("Look")]
        public Material fur, tail, teeth, dark, lodgeMat, woodMat, mudMat, stoneMat;

        [Header("Work")]
        public float cutTime = 4f;
        public float digTime = 3f;
        [Tooltip("Seconds to pick water plants or cut branches for food.")]
        public float forageTime = 4f;
        [Tooltip("Every Nth earth load is a stone, the others are mud.")]
        public int stoneEvery = 3;

        [Header("Groups (set by the player)")]
        public int woodWorkers = 2;
        public int earthWorkers = 1;
        public int buildWorkers = 1;
        public int forageWorkers = 2;
        [Tooltip("Beavers that run the machines (Phase C).")]
        public int operateWorkers = 0;

        [Header("Family")]
        public FamilySettings family = new FamilySettings();

        [Header("Starting storage")]
        public int startWood = 12, startMud = 8, startStone = 6, startFood = 30;

        public HexPathfinder Pathfinder { get; private set; }
        public HexGrid Grid => grid;
        public readonly List<Beaver> beavers = new List<Beaver>();
        public readonly List<HexCell> lodgeCells = new List<HexCell>();
        public Vector3 LodgeCenter { get; private set; }

        readonly List<Transform> trees = new List<Transform>();
        readonly Dictionary<Beaver, object> reserved = new Dictionary<Beaver, object>();
        int earthCount;
        float alarmUntil = -1f;
        public readonly List<string> log = new List<string>();
        /// <summary>Important messages for the pop-up toasts on the HUD (text, time shown).</summary>
        public readonly List<(string text, float time)> toasts = new List<(string, float)>();
        public bool AlarmActive => Time.time < alarmUntil || bellRinging;

        /// <summary>
        /// Economy counters for balancing, e.g. "wood cut", "wood eaten", "wood built".
        /// Shown per game day in the Events tab (debug), so the leader and the designer can see
        /// where the materials go.
        /// </summary>
        public readonly SortedDictionary<string, int> stats = new SortedDictionary<string, int>();
        public float StatsSince { get; private set; }
        public void Stat(string key, int n = 1) { stats.TryGetValue(key, out int v); stats[key] = v + n; }
        public void ResetStats() { stats.Clear(); StatsSince = Time.time; }
        public float AlarmLeft => Mathf.Max(0f, alarmUntil - Time.time);

        /// <summary>Adds a line to the event log (newest first). Important lines also pop up on screen.</summary>
        public void Log(string message, bool important = false)
        {
            string clock = dayNight != null ? dayNight.Clock + "  " : "";
            log.Insert(0, clock + message);
            if (log.Count > 40) log.RemoveAt(log.Count - 1);
            if (important)
            {
                toasts.Insert(0, (message, Time.time));
                if (toasts.Count > 3) toasts.RemoveAt(toasts.Count - 1);
            }
        }

        /// <summary>
        /// Tail slap: an adult that is outside slaps its tail on the water and the whole family
        /// swims to the lodge (Smithsonian's National Zoo, n.d.). Used when a flood comes or a dam breaks.
        /// </summary>
        public void Alarm(string reason)
        {
            Beaver warner = null;
            foreach (var b in beavers) if (b.state != BeaverState.InLodge && b.age == BeaverAge.Adult) { warner = b; break; }
            if (warner == null) foreach (var b in beavers) if (b.state != BeaverState.InLodge) { warner = b; break; }
            Log(warner != null ? $"TAIL SLAP! {warner.beaverName} warns the family: {reason}" : $"Alarm: {reason}", true);
            alarmUntil = Time.time + alarmSeconds;
            foreach (var b in beavers) b.Interrupt();
        }

        public int Workers { get { int n = 0; foreach (var b in beavers) if (b.CanWork) n++; return n; } }

        public int Count(BeaverAge a) { int n = 0; foreach (var b in beavers) if (b.age == a) n++; return n; }

        static readonly string[] NamePool =
        {
            "Bram", "Elsa", "Joep", "Fien", "Pim", "Lotte", "Sem", "Noor", "Daan", "Saar", "Teun", "Mila",
            "Gijs", "Roos", "Luuk", "Isa", "Stijn", "Femke", "Tijn", "Anouk", "Koen", "Lieke", "Ruben", "Tess",
        };
        int nextName;

        void Awake()
        {
            if (seasons == null) seasons = GetComponent<Seasons>();
            if (seasons == null) seasons = gameObject.AddComponent<Seasons>();
            seasons.colony = this;
            if (industry == null) industry = GetComponent<Industry>();
            if (industry == null) industry = gameObject.AddComponent<Industry>();
            industry.colony = this;
            if (floods == null) floods = GetComponent<FloodSystem>();
            if (floods == null) floods = gameObject.AddComponent<FloodSystem>();
            floods.colony = this;
        }

        /// <summary>The alarm bell: ring = everybody swims home and stays; all clear = back to work.</summary>
        public void RingBell(bool on)
        {
            if (bellRinging == on) return;
            bellRinging = on;
            if (on)
            {
                Log("ALARM BELL! The family swims to the lodge", true);
                foreach (var b in beavers) b.Interrupt();
            }
            else Log("All clear: back to work", true);
        }

        public bool IsWinter => seasons != null && seasons.IsWinter;
        public bool GameOver { get; private set; }

        System.Collections.IEnumerator Start()
        {
            Pathfinder = new HexPathfinder(grid, water);
            PlaceLodge();
            if (forestRoot != null)
                foreach (Transform t in forestRoot)
                {
                    trees.Add(t);
                    // the ground is made again at play time and may differ a little from when the trees were planted
                    var tp = t.position; t.position = new Vector3(tp.x, ground.GroundAt(tp.x, tp.z), tp.z);
                }
            trees.Sort((a, b) => Flat(a.position - LodgeCenter).CompareTo(Flat(b.position - LodgeCenter)));

            // 4 adults, 5 yearlings, 3 kits. Ages are spread, so they do not all grow up at once.
            var ages = new[] { BeaverAge.Adult, BeaverAge.Adult, BeaverAge.Adult, BeaverAge.Adult,
                BeaverAge.Yearling, BeaverAge.Yearling, BeaverAge.Yearling, BeaverAge.Yearling, BeaverAge.Yearling,
                BeaverAge.Kit, BeaverAge.Kit, BeaverAge.Kit };
            foreach (var a in ages)
            {
                var b = Spawn(NextName(), a, null);
                b.ageSeconds = Random.Range(0f, 0.8f) * StageSeconds(a);
            }
            forageWorkers = 2; woodWorkers = 3; earthWorkers = 2; buildWorkers = 2;
            ApplyGroups();

            yield return null; // the water grid and the orders set themselves up in their own Start
            // one storage per material at the start, each on its own base near the lodge
            var types = new[] { (Item.Food, startFood), (Item.Wood, startWood), (Item.Mud, startMud), (Item.Stone, startStone) };
            var used = new List<HexCell>();
            foreach (var (type, amount) in types)
            {
                var spot = StarterStorageSpot(used);
                if (spot == null) { Debug.LogError("No dry spot for a starter storage."); break; }
                used.Add(spot);
                orders.CreateStarterStorage(spot, type, amount);
            }
        }

        string NextName() => NamePool[nextName++ % NamePool.Length] + (nextName > NamePool.Length ? " " + (nextName / NamePool.Length + 1) : "");

        /// <summary>Flat, dry hex 2-3 steps from the lodge for a starter storage, not next to another one.</summary>
        HexCell StarterStorageSpot(List<HexCell> used)
        {
            HexCell best = null; float bestScore = float.MaxValue;
            foreach (var c in grid.Cells)
            {
                if (c.blocked || orders.DepthOver(c) > 0.01f || c.maxGround - c.minGround > 0.8f) continue;
                bool near = false;
                foreach (var u in used) if (HexPathfinder.Distance(c.axial, u.axial) < 2) near = true;
                if (near) continue;
                int d = int.MaxValue;
                foreach (var l in lodgeCells) d = Mathf.Min(d, HexPathfinder.Distance(c.axial, l.axial));
                if (d < 2 || d > 4) continue;
                float score = d + (c.maxGround - c.minGround);
                if (score < bestScore) { bestScore = score; best = c; }
            }
            return best;
        }

        // ------------------------------------------------------------------ growing up and new kits

        float DaySeconds => dayNight != null ? dayNight.dayLength : 240f;
        public float StageSeconds(BeaverAge a) => a == BeaverAge.Kit ? family.kitDays * DaySeconds : a == BeaverAge.Yearling ? family.yearlingDays * DaySeconds : float.MaxValue;

        void Update()
        {
            float dt = Time.deltaTime;
            bool groupsChanged = false;
            foreach (var b in beavers)
            {
                if (b.age == BeaverAge.Adult) continue;
                b.ageSeconds += dt;
                if (b.ageSeconds < StageSeconds(b.age)) continue;
                b.ageSeconds = 0f;
                b.GrowUp();
                groupsChanged = true;
                Log(b.age == BeaverAge.Yearling ? $"{b.beaverName} is a yearling now and can work" : $"{b.beaverName} is an adult now", b.age == BeaverAge.Yearling);
            }
            if (groupsChanged) ApplyGroups();
            UpdateNurseries(dt);
            lodgeCheck -= dt;
            if (lodgeCheck <= 0f) { lodgeCheck = 0.5f; MeasureLodgeWater(); }
        }

        public int KitCapacity { get { int n = 0; foreach (var s in orders.Buildings) if (s.Kind == BuildingKind.Nursery && s.Finished) n += family.kitsPerNursery; return n; } }

        /// <summary>
        /// Each finished nursery with its entrance under water makes a litter every few game days,
        /// if the breeding pair is there (2 adults), there is room for more kits and there is
        /// wood to eat. The kits use up the wood (food) from the storages.
        /// </summary>
        void UpdateNurseries(float dt)
        {
            foreach (var n in orders.Buildings)
            {
                if (n.Kind != BuildingKind.Nursery || !n.Finished) continue;
                if (n.flooded) { n.waitReason = "flooded"; continue; }
                if (!n.entranceUnderWater) { n.waitReason = "entrance dry"; continue; }
                if (Count(BeaverAge.Adult) < 2) { n.waitReason = "needs 2 adults"; continue; }
                n.litterTimer += dt;
                n.waitReason = "";
                if (n.litterTimer < family.litterDays * DaySeconds) continue;
                if (Count(BeaverAge.Kit) >= KitCapacity) { n.litterTimer = family.litterDays * DaySeconds; n.waitReason = "no room for kits"; continue; } // wait for room
                if (beavers.Count >= family.maxFamily) { n.litterTimer = family.litterDays * DaySeconds; n.waitReason = $"family is full ({family.maxFamily})"; continue; }
                if (IsWinter) { n.waitReason = "no kits in winter"; n.litterTimer = Mathf.Min(n.litterTimer, family.litterDays * DaySeconds); continue; }
                if (Stock(Item.Food) < family.litterFood) { n.litterTimer = family.litterDays * DaySeconds; n.waitReason = $"needs {family.litterFood} food"; continue; } // wait for food
                n.litterTimer = 0f;
                for (int i = 0; i < family.litterFood; i++) TakeAny(Item.Food);
                Stat("food used for kits", family.litterFood);
                int born = Mathf.Min(Mathf.Min(family.litterSize, KitCapacity - Count(BeaverAge.Kit)), family.maxFamily - beavers.Count);
                var names = new List<string>();
                for (int i = 0; i < born; i++)
                {
                    var kit = Spawn(NextName(), BeaverAge.Kit, n.hex);
                    names.Add(kit.beaverName);
                }
                Log($"{born} kit(s) born in the nursery: {string.Join(", ", names)} (-{family.litterFood} food)", true);
            }
        }

        static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        // ------------------------------------------------------------------ lodge

        /// <summary>
        /// The main lodge covers 4 hexes in a diamond. It goes on the bank: some of its hexes in
        /// the stream and some dry, as close as possible to the preferred spot. Real lodges stand
        /// in or next to the water with the entrance under water (Wild Sweden, n.d.).
        /// </summary>
        void PlaceLodge()
        {
            HexCell[] best = null;
            float bestScore = float.MaxValue;
            var offs = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
            foreach (var c in grid.Cells)
            {
                var set = new HexCell[4];
                bool ok = true; int wet = 0;
                for (int k = 0; k < 4; k++)
                {
                    if (!grid.TryGet(c.axial + offs[k], out set[k])) { ok = false; break; }
                    if (set[k].startsWet) wet++;
                }
                if (!ok || wet == 0 || wet == 4) continue;
                Vector3 mid = (set[0].center + set[1].center + set[2].center + set[3].center) / 4f;
                float score = Vector2.Distance(new Vector2(mid.x, mid.z), preferredLodgeSpot);
                if (score < bestScore) { bestScore = score; best = set; }
            }
            if (best == null) { Debug.LogError("No place for the lodge."); return; }

            Vector3 sum = Vector3.zero;
            foreach (var c in best) { c.blocked = true; c.building = "Main lodge"; lodgeCells.Add(c); sum += c.center; }
            Vector3 centre = sum / 4f;
            float baseY = Mathf.Max(centre.y, LowestWaterOrGround(best));
            LodgeCenter = new Vector3(centre.x, baseY, centre.z);

            // look: a big dome of sticks and mud, with some sticks poking out
            var root = new GameObject("Main Lodge").transform;
            root.SetParent(transform);
            root.position = LodgeCenter;
            Piece(root, PrimitiveType.Sphere, lodgeMat, new Vector3(0f, 0.2f, 0f), new Vector3(6.2f, 3.2f, 6.2f), Vector3.zero);
            var rng = new System.Random(7);
            for (int i = 0; i < 26; i++)
            {
                float a = (float)rng.NextDouble() * 360f, up = 10f + (float)rng.NextDouble() * 50f;
                var dir = Quaternion.Euler(-up, a, 0f) * Vector3.forward;
                Vector3 p = new Vector3(dir.x * 2.9f, 0.2f + dir.y * 1.5f, dir.z * 2.9f);
                Piece(root, PrimitiveType.Cylinder, woodMat, p, new Vector3(0.12f, 0.9f, 0.12f),
                    (Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f)).eulerAngles);
            }
        }

        float LowestWaterOrGround(HexCell[] cells)
        {
            float y = float.MaxValue;
            foreach (var c in cells) y = Mathf.Min(y, c.meanGround);
            return y;
        }

        public HexCell NearestLodgeCell(HexCell from)
        {
            HexCell best = lodgeCells[0];
            if (from == null) return best;
            int bd = int.MaxValue;
            foreach (var c in lodgeCells)
            {
                int d = HexPathfinder.Distance(from.axial, c.axial);
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        /// <summary>Drop-off point on the edge of the lodge, on the side the beaver comes from.</summary>
        public Vector3 LodgeDoor(Vector3 from)
        {
            Vector3 d = from - LodgeCenter; d.y = 0f;
            if (d.sqrMagnitude < 0.01f) d = Vector3.forward;
            return LodgeCenter + d.normalized * 3.4f;
        }

        public HexCell RandomSpotNearLodge()
        {
            var options = new List<HexCell>();
            foreach (var c in grid.Cells)
            {
                if (c.blocked) continue;
                int d = int.MaxValue;
                foreach (var l in lodgeCells) d = Mathf.Min(d, HexPathfinder.Distance(c.axial, l.axial));
                if (d >= 1 && d <= 3) options.Add(c);
            }
            return options.Count > 0 ? options[Random.Range(0, options.Count)] : null;
        }

        // ------------------------------------------------------------------ winter and death

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

        // ------------------------------------------------------------------ work targets

        /// <summary>Nearest free trees first; a little randomness so the cutters spread out.</summary>
        public bool PickTree(Beaver b, out Vector3 pos, out HexCell hex)
        {
            Release(b);
            var options = new List<Transform>();
            foreach (var t in trees)
            {
                if (t == null || reserved.ContainsValue(t)) continue;
                if (!grid.TryGet(t.position, out var h) || h.blocked) continue;
                options.Add(t);
                if (options.Count >= 8) break;
            }
            pos = Vector3.zero; hex = null;
            if (options.Count == 0) return false;
            var pick = options[Random.Range(0, options.Count)];
            reserved[b] = pick;
            pos = pick.position;
            grid.TryGet(pos, out hex);
            return true;
        }

        /// <summary>Wet hexes in the stream bed, nearest to the lodge first.</summary>
        public bool PickDigSite(Beaver b, out HexCell hex)
        {
            Release(b);
            if (PickOrderedDig(b, out hex)) return true; // the leader's dig orders come first
            var options = new List<HexCell>();
            foreach (var c in grid.Cells)
            {
                if (c.blocked || reserved.ContainsValue(c) || !Pathfinder.IsWet(c)) continue;
                options.Add(c);
            }
            options.Sort((x, y) => Flat(x.center - LodgeCenter).CompareTo(Flat(y.center - LodgeCenter)));
            if (options.Count > 8) options.RemoveRange(8, options.Count - 8);
            hex = options.Count > 0 ? options[Random.Range(0, options.Count)] : null;
            if (hex != null) reserved[b] = hex;
            return hex != null;
        }

        /// <summary>
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
        {
            reserved.Remove(b);
            if (orders != null) orders.Release(b);
            if (industry != null) industry.Release(b);
        }

        /// <summary>Every Nth load is a stone, the rest mud; if one of them has no room, dig the other.</summary>
        public Item NextEarthItem()
        {
            bool stone = ++earthCount % Mathf.Max(1, stoneEvery) == 0;
            if (stone && FreeSpace(Item.Stone) <= 0) stone = false;
            if (!stone && FreeSpace(Item.Mud) <= 0 && FreeSpace(Item.Stone) > 0) stone = true;
            return stone ? Item.Stone : Item.Mud;
        }

        // ------------------------------------------------------------------ storages

        public IEnumerable<BuildingSite> Storages
        {
            get { foreach (var b in orders.Buildings) if (b.Kind == BuildingKind.Storage && b.Finished) yield return b; }
        }

        public int Stock(Item item) { int n = 0; foreach (var s in Storages) n += s.Stock(item); return n; }
        public int Capacity(Item item) { int n = 0; foreach (var s in Storages) if (s.storeType == item) n += orders.storageCapacity; return n; }
        public int Carried(Item item) { int n = 0; foreach (var b in beavers) if (b.carrying == item) n += Mathf.Max(1, b.carryCount); return n; }
        /// <summary>Room left for an item, counting what beavers are already carrying home.</summary>
        public int FreeSpace(Item item) => Capacity(item) - Stock(item) - Carried(item);

        public BuildingSite NearestStorageWith(Item item, Vector3 from) => NearestStorage(from, s => s.Stock(item) > 0);
        public BuildingSite NearestStorageWithSpace(Item item, Vector3 from) => NearestStorage(from, s => s.storeType == item && s.Stock(item) < orders.storageCapacity);

        BuildingSite NearestStorage(Vector3 from, System.Predicate<BuildingSite> ok)
        {
            BuildingSite best = null; float bd = float.MaxValue;
            foreach (var s in Storages)
            {
                if (!ok(s)) continue;
                float d = Flat(s.hex.center - from);
                if (d < bd) { bd = d; best = s; }
            }
            return best;
        }

        public bool TakeFrom(ISupply supply, Item item)
        {
            if (supply is MaterialPile p)
            {
                if (!piles.Contains(p) || p.item != item || p.count <= 0) return false;
                p.count--;
                if (p.count <= 0) RemovePile(p); else PileLook(p);
                Stat($"{item.ToString().ToLower()} picked up from piles");
                return true;
            }
            var s = supply as BuildingSite;
            if (s == null || !orders.sites.Contains(s) || s.Stock(item) <= 0) return false;
            s.Add(item, -1);
            return true;
        }

        // ------------------------------------------------------------------ piles on the ground

        public readonly List<MaterialPile> piles = new List<MaterialPile>();

        /// <summary>Materials in storages plus the piles on the ground (what builders can use).</summary>
        public int SupplyStock(Item item)
        {
            int n = Stock(item);
            foreach (var p in piles) if (p.item == item) n += p.count;
            return n;
        }

        public int PileCount(Item item) { int n = 0; foreach (var p in piles) if (p.item == item) n += p.count; return n; }

        /// <summary>Drops materials next to a hex (half of a washed-away dam stage, or a removed order).</summary>
        public void DropPile(HexCell hex, Item item, int count)
        {
            if (count <= 0 || item == Item.None || hex == null) return;
            Stat($"{item.ToString().ToLower()} dropped as piles", count);
            foreach (var p in piles)
                if (p.hex == hex && p.item == item) { p.count += count; PileLook(p); return; }
            // each material has its own corner of the hex, so piles do not overlap
            float a = (int)item * 120f + 30f;
            Vector3 off = Quaternion.Euler(0f, a, 0f) * Vector3.forward * 0.9f;
            var pile = new MaterialPile(hex, item, count, hex.center + off);
            piles.Add(pile);
            PileLook(pile);
        }

        void RemovePile(MaterialPile p)
        {
            if (p.visual != null) Destroy(p.visual);
            piles.Remove(p);
        }

        /// <summary>A small heap: logs, a mud lump or stones, bigger with more items (max 6 shown).</summary>
        void PileLook(MaterialPile p)
        {
            if (p.visual != null) Destroy(p.visual);
            var root = new GameObject($"Pile {p.item} {p.count}").transform;
            root.SetParent(transform);
            root.position = new Vector3(p.pos.x, ground.GroundAt(p.pos.x, p.pos.z), p.pos.z);
            p.visual = root.gameObject;
            int n = Mathf.Min(6, p.count);
            for (int i = 0; i < n; i++)
            {
                float ang = i * 67f;
                Vector3 q = Quaternion.Euler(0f, ang, 0f) * Vector3.forward * (0.12f + 0.1f * (i % 3));
                if (p.item == Item.Wood)
                    Piece(root, PrimitiveType.Cylinder, woodMat, new Vector3(q.x, 0.1f + (i / 3) * 0.15f, q.z), new Vector3(0.13f, 0.38f, 0.13f), new Vector3(90f, ang, 0f));
                else if (p.item == Item.Stone)
                    Piece(root, PrimitiveType.Cube, stoneMat, new Vector3(q.x, 0.1f + (i / 3) * 0.16f, q.z), Vector3.one * 0.22f, new Vector3(i * 23f, ang, i * 11f));
                else
                    Piece(root, PrimitiveType.Sphere, p.item == Item.Food && orders != null ? orders.FoodMat : mudMat, new Vector3(q.x, 0.05f, q.z), new Vector3(0.4f, 0.22f, 0.4f), Vector3.zero);
            }
        }

        /// <summary>
        /// The supply with this item that makes the shortest trip from the beaver to the site
        /// (beaver -> supply -> site), so builders use a pile next to a dam before a far storage.
        /// </summary>
        public ISupply NearestSupplyWith(Item item, Vector3 from, Vector3 to, int pending = 0)
        {
            ISupply best = null; float bd = float.MaxValue;
            foreach (var s in Storages)
            {
                if (s.Stock(item) <= 0) continue;
                float d = Flat(s.hex.center - from) + Flat(to - s.hex.center);
                if (d < bd) { bd = d; best = s; }
            }
            foreach (var p in piles)
            {
                if (p.item != item || p.count <= 0) continue;
                float d = Flat(p.pos - from) + Flat(to - p.pos);
                if (d < bd) { bd = d; best = p; }
            }
            return best;
        }

        /// <summary>Nearest free pile with one of the items this beaver collects.</summary>
        public bool PickPile(Beaver b, System.Predicate<Item> wants, out MaterialPile pile)
        {
            Release(b);
            pile = null; float bd = float.MaxValue;
            foreach (var p in piles)
            {
                if (p.count <= 0 || !wants(p.item) || reserved.ContainsValue(p) || FreeSpace(p.item) <= 0) continue;
                float d = Flat(p.pos - b.transform.position);
                if (d < bd) { bd = d; pile = p; }
            }
            if (pile != null) reserved[b] = pile;
            return pile != null;
        }

        public bool TakeAny(Item item) => TakeFrom(NearestStorageWith(item, LodgeCenter), item);

        /// <summary>Puts an item in a storage if it has room. False = full, the beaver keeps it.</summary>
        public bool StoreIn(BuildingSite s, Item item)
        {
            if (s == null || !orders.sites.Contains(s) || s.storeType != item || s.Stock(item) >= orders.storageCapacity) return false;
            s.Add(item, 1);
            return true;
        }

        // ------------------------------------------------------------------ groups

        /// <summary>Fills the groups in order wood, earth, build; the rest of the workers stay idle.</summary>
        public void ApplyGroups()
        {
            int total = Workers;
            forageWorkers = Mathf.Clamp(forageWorkers, 0, total);
            operateWorkers = Mathf.Clamp(operateWorkers, 0, total - forageWorkers);
            int used = forageWorkers + operateWorkers;
            woodWorkers = Mathf.Clamp(woodWorkers, 0, total - used);
            earthWorkers = Mathf.Clamp(earthWorkers, 0, total - used - woodWorkers);
            buildWorkers = Mathf.Clamp(buildWorkers, 0, total - used - woodWorkers - earthWorkers);
            int f = forageWorkers, op = operateWorkers, w = woodWorkers, e = earthWorkers, bl = buildWorkers;
            foreach (var b in beavers)
            {
                if (!b.CanWork) { b.SetGroup(WorkGroup.Idle); continue; }
                if (f > 0) { b.SetGroup(WorkGroup.Forage); f--; }
                else if (op > 0) { b.SetGroup(WorkGroup.Operate); op--; }
                else if (w > 0) { b.SetGroup(WorkGroup.Wood); w--; }
                else if (e > 0) { b.SetGroup(WorkGroup.Earth); e--; }
                else if (bl > 0) { b.SetGroup(WorkGroup.Build); bl--; }
                else b.SetGroup(WorkGroup.Idle);
            }
        }

        public int IdleWorkers => Workers - forageWorkers - operateWorkers - woodWorkers - earthWorkers - buildWorkers;

        // ------------------------------------------------------------------ water queries

        public bool IsSwimming(Vector3 p) => water.DepthAt(p.x, p.z) > 0.3f;

        /// <summary>Ground height, or just under the water surface where the beaver swims.</summary>
        public float SurfaceHeight(float x, float z)
        {
            float g = ground.GroundAt(x, z);
            float s = water.SurfaceAt(x, z, out bool wet);
            return wet && s - g > 0.3f ? s - 0.18f : g;
        }

        // ------------------------------------------------------------------ beavers

        Beaver Spawn(string name, BeaverAge age, HexCell at)
        {
            var spot = at ?? RandomSpotNearLodge();
            Vector3 p = spot != null ? spot.center : LodgeCenter + Vector3.forward * 4f;
            var b = BeaverLook.Create(this, name, age, p);
            beavers.Add(b);
            return b;
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
    }
}
