using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    [System.Serializable]
    public class ItemCount
    {
        public Item item;
        public int count = 1;
        public ItemCount() { }
        public ItemCount(Item item, int count) { this.item = item; this.count = count; }
    }

    /// <summary>What a machine turns into what, and how long one batch takes when the water wheel turns at the reference speed.</summary>
    [System.Serializable]
    public class RecipeDef
    {
        public ItemCount[] inputs;
        public ItemCount output;
        [Tooltip("Game seconds for one batch at the reference water speed (1 m/s). A game day is 240 s.")]
        public float seconds = 60f;

        public string Text()
        {
            var parts = new List<string>();
            foreach (var i in inputs) parts.Add($"{i.count} {ItemInfo.Lower(i.item)}");
            return $"{string.Join(" + ", parts)} -> {output.count} {ItemInfo.Lower(output.item)}";
        }
    }

    /// <summary>One machine type. A new machine is a new entry here, not new code.</summary>
    [System.Serializable]
    public class MachineDef
    {
        public string id;
        public string name;
        [Tooltip("Tech node that unlocks this machine.")]
        public string tech;
        public Color colour = Color.gray;
        public RecipeDef recipe;
        public BuildStage[] stages;
    }

    public enum LabKind { Material, Building }

    /// <summary>One node of the tech tree: paid with food at the start, done after a research time.</summary>
    [System.Serializable]
    public class TechNode
    {
        public string id;
        public string name;
        public LabKind lab;
        public int food;
        [Tooltip("Research time in game days.")]
        public float days = 1f;
        public string[] requires = new string[0];
        [TextArea] public string effect;
    }

    /// <summary>
    /// Phase C (plan v2/v3): labs, the tech tree, machines and water wheels.
    /// - Two labs (material, building). Each lab researches one node at a time. A node costs
    ///   food (paid at the start) and takes a research time; then its building or machine unlocks.
    ///   The step-by-step tree with costs that grow is modelled on the upgrade tree of Plague Inc.
    /// - Machines stand on a base and turn materials into other materials (recipes in data).
    ///   Each needs a water wheel next to it; the faster the water, the faster the machine
    ///   (0.25x-3x of the reference speed, stopped when slow or frozen). A machine with an order
    ///   calls one beaver from the family to run it (idle ones first), and lets it go when done.
    ///   One wheel drives at most two machines.
    /// - The leader orders a number of batches (+1, +5), or, after the Automation node, turns
    ///   a machine on "auto" so it runs until its inputs run out or the storage is full
    ///   (control in the style of Hay Day).
    /// </summary>
    public class Industry : MonoBehaviour
    {
        public ColonyManager colony;

        [Header("Water wheel")]
        public float wheelMinDepth = 0.3f;
        public float wheelMinSpeed = 0.2f;
        [Tooltip("Water speed (m/s) at which machines work at 1x.")]
        public float wheelRefSpeed = 0.5f;
        public float wheelMinFactor = 0.25f, wheelMaxFactor = 3f;
        [Tooltip("An operator loads inputs for up to this many batches per trip and runs them one after the other.")]
        public int batchesPerTrip = 3;
        public int machinesPerWheel = 2;
        [Tooltip("Storage size after the Big storage node.")]
        public int bigStorageCapacity = 60;

        [Header("Buildings")]
        public BuildingDef materialLabDef = new BuildingDef
        {
            kind = BuildingKind.MaterialLab, name = "Material lab",
            rule = "On a base. Researches the water wheel, the machines and automation, paid with food.",
            stages = new[] { new BuildStage { name = "frame", wood = 6 }, new BuildStage { name = "walls", mud = 4, stone = 2 } }
        };
        public BuildingDef buildingLabDef = new BuildingDef
        {
            kind = BuildingKind.BuildingLab, name = "Building lab",
            rule = "On a base. Researches new buildings (food farm, spillway, Base Mk2, Dam Mk2, big storage), paid with food.",
            stages = new[] { new BuildStage { name = "frame", wood = 6 }, new BuildStage { name = "walls", mud = 4, stone = 4 } }
        };
        public BuildingDef wheelDef = new BuildingDef
        {
            kind = BuildingKind.WaterWheel, name = "Water wheel",
            rule = "On flowing water (0.3 m deep, 0.2 m/s). Drives up to 2 machines next to it; faster water = faster machines. Stops when frozen.",
            stages = new[] { new BuildStage { name = "frame", wood = 4 }, new BuildStage { name = "paddles", wood = 2, stone = 2 } }
        };

        public MachineDef[] machines =
        {
            new MachineDef { id = "splitter", name = "Wood splitter", tech = "M2", colour = new Color(0.75f, 0.58f, 0.36f),
                recipe = new RecipeDef { inputs = new[] { new ItemCount(Item.Wood, 1) }, output = new ItemCount(Item.Food, 2), seconds = 40f },
                stages = new[] { new BuildStage { name = "frame", wood = 4 }, new BuildStage { name = "blade", stone = 2 } } },
            new MachineDef { id = "crusher", name = "Stone crusher", tech = "M3", colour = new Color(0.55f, 0.55f, 0.58f),
                recipe = new RecipeDef { inputs = new[] { new ItemCount(Item.Stone, 1) }, output = new ItemCount(Item.Gravel, 2), seconds = 50f },
                stages = new[] { new BuildStage { name = "frame", wood = 2 }, new BuildStage { name = "crushing stones", stone = 4 } } },
            new MachineDef { id = "sifter", name = "Sand sifter", tech = "M4", colour = new Color(0.88f, 0.8f, 0.58f),
                recipe = new RecipeDef { inputs = new[] { new ItemCount(Item.Mud, 2) }, output = new ItemCount(Item.Sand, 1), seconds = 50f },
                stages = new[] { new BuildStage { name = "frame", wood = 3 }, new BuildStage { name = "sieve", mud = 2 } } },
            new MachineDef { id = "press", name = "Wood press", tech = "M5", colour = new Color(0.82f, 0.62f, 0.38f),
                recipe = new RecipeDef { inputs = new[] { new ItemCount(Item.Wood, 3) }, output = new ItemCount(Item.PressedWood, 1), seconds = 80f },
                stages = new[] { new BuildStage { name = "frame", wood = 4 }, new BuildStage { name = "press stones", stone = 3 } } },
            new MachineDef { id = "mixer", name = "Cement mixer", tech = "M6", colour = new Color(0.78f, 0.78f, 0.75f),
                recipe = new RecipeDef { inputs = new[] { new ItemCount(Item.Gravel, 1), new ItemCount(Item.Sand, 1) }, output = new ItemCount(Item.Cement, 1), seconds = 100f },
                stages = new[] { new BuildStage { name = "drum", pressedWood = 2 }, new BuildStage { name = "base", stone = 2, gravel = 2 } } },
        };

        [Header("Tech tree")]
        public TechNode[] tech =
        {
            new TechNode { id = "M1", name = "Water wheel", lab = LabKind.Material, food = 20, days = 1f, effect = "Build water wheels that drive machines." },
            new TechNode { id = "M2", name = "Wood splitter", lab = LabKind.Material, food = 25, days = 1f, requires = new[] { "M1" }, effect = "1 wood -> 2 food (branches)." },
            new TechNode { id = "M3", name = "Stone crusher", lab = LabKind.Material, food = 35, days = 1.5f, requires = new[] { "M1" }, effect = "1 stone -> 2 gravel." },
            new TechNode { id = "M4", name = "Sand sifter", lab = LabKind.Material, food = 35, days = 1.5f, requires = new[] { "M1" }, effect = "2 mud -> 1 sand." },
            new TechNode { id = "M5", name = "Wood press", lab = LabKind.Material, food = 50, days = 2f, requires = new[] { "M2" }, effect = "3 wood -> 1 pressed wood." },
            new TechNode { id = "M6", name = "Cement mixer", lab = LabKind.Material, food = 60, days = 2.5f, requires = new[] { "M3", "M4" }, effect = "1 gravel + 1 sand -> 1 cement." },
            new TechNode { id = "M7", name = "Automation", lab = LabKind.Material, food = 50, days = 2f, requires = new[] { "M2", "M3" }, effect = "Machines can run on auto until their inputs run out." },
            new TechNode { id = "B1", name = "Food farm", lab = LabKind.Building, food = 30, days = 1f, requires = new[] { "M4" }, effect = "Water plants on calm water: food for the foragers." },
            new TechNode { id = "B2", name = "Spillway", lab = LabKind.Building, food = 50, days = 2f, requires = new[] { "M3" }, effect = "Low gravel dam that never wears." },
            new TechNode { id = "B3", name = "Base Mk2", lab = LabKind.Building, food = 60, days = 2.5f, requires = new[] { "M5", "M6" }, effect = "Base with a pump: +0.5 m water around it." },
            new TechNode { id = "B4", name = "Dam Mk2", lab = LabKind.Building, food = 70, days = 3f, requires = new[] { "M5", "M6" }, effect = "Strong dam that also works as a base." },
            new TechNode { id = "B5", name = "Big storage", lab = LabKind.Building, food = 40, days = 1.5f, effect = "Every storage holds 60 instead of 40." },
        };

        readonly HashSet<string> unlocked = new HashSet<string>();
        class Research { public TechNode node; public float done; }
        readonly Dictionary<LabKind, Research> research = new Dictionary<LabKind, Research>();
        /// <summary>Operator of each machine, with the last time it showed it is still working there (stale ones are freed).</summary>
        readonly Dictionary<Beaver, (BuildingSite m, float t)> operators = new Dictionary<Beaver, (BuildingSite m, float t)>();
        const float OperatorTimeout = 90f;

        bool Taken(BuildingSite m)
        {
            foreach (var kv in operators)
                if (kv.Value.m == m && kv.Key != null && Time.time - kv.Value.t < OperatorTimeout + BatchSeconds(m)) return true;
            return false;
        }

        BuildOrders Orders => colony.orders;
        float Day => colony.dayNight != null ? colony.dayNight.dayLength : 240f;

        // ------------------------------------------------------------------ data lookups

        public TechNode Node(string id) => System.Array.Find(tech, n => n.id == id);
        public MachineDef Machine(string id) => System.Array.Find(machines, m => m.id == id);
        public bool Unlocked(string id) => string.IsNullOrEmpty(id) || unlocked.Contains(id);
        public bool Automation => Unlocked("M7");

        public BuildingDef DefFor(BuildingKind k) =>
            k == BuildingKind.MaterialLab ? materialLabDef : k == BuildingKind.BuildingLab ? buildingLabDef : k == BuildingKind.WaterWheel ? wheelDef : null;

        /// <summary>The tech node a tool needs, or null when it is open from the start.</summary>
        public string TechFor(BuildTool t, string machineId)
        {
            switch (t)
            {
                case BuildTool.WaterWheel: return "M1";
                case BuildTool.FoodFarm: return "B1";
                case BuildTool.Spillway: return "B2";
                case BuildTool.PumpBase: return "B3";
                case BuildTool.DamMk2: return "B4";
                case BuildTool.Machine: return Machine(machineId)?.tech;
                case BuildTool.Sandbag: return "M4"; // sandbags need sand
                default: return null;
            }
        }

        public bool ToolUnlocked(BuildTool t, string machineId, out string needs)
        {
            string id = TechFor(t, machineId);
            needs = id == null ? "" : $"{id} {Node(id)?.name}";
            return id == null || Unlocked(id);
        }

        // ------------------------------------------------------------------ research

        public BuildingSite LabBuilding(LabKind lab)
        {
            var k = lab == LabKind.Material ? BuildingKind.MaterialLab : BuildingKind.BuildingLab;
            foreach (var b in Orders.Buildings) if (b.Kind == k && b.Finished) return b;
            return null;
        }

        public bool Researching(TechNode n, out float progress01)
        {
            progress01 = 0f;
            if (!research.TryGetValue(n.lab, out var r) || r == null || r.node != n) return false;
            progress01 = Mathf.Clamp01(r.done / (n.days * Day));
            return true;
        }

        public TechNode CurrentResearch(LabKind lab) => research.TryGetValue(lab, out var r) && r != null ? r.node : null;

        public bool CanResearch(TechNode n, out string why)
        {
            if (Unlocked(n.id)) { why = "done"; return false; }
            if (Researching(n, out _)) { why = "researching"; return false; }
            foreach (var req in n.requires)
                if (!Unlocked(req)) { why = $"needs {req} {Node(req)?.name}"; return false; }
            if (LabBuilding(n.lab) == null) { why = $"build a {(n.lab == LabKind.Material ? "material" : "building")} lab first"; return false; }
            if (CurrentResearch(n.lab) != null) { why = $"the lab is busy with {CurrentResearch(n.lab).name}"; return false; }
            int food = colony.Stock(Item.Food);
            if (food < n.food) { why = $"needs {n.food} food (you have {food})"; return false; }
            why = $"{n.food} food, {n.days:0.#} day(s)";
            return true;
        }

        public void StartResearch(TechNode n)
        {
            if (!CanResearch(n, out _)) return;
            for (int i = 0; i < n.food; i++) colony.TakeAny(Item.Food);
            colony.Stat("food spent on research", n.food);
            research[n.lab] = new Research { node = n };
            colony.Log($"Research started: {n.name} ({n.food} food, {n.days:0.#} day(s))");
        }

        void UpdateResearch(float dt)
        {
            foreach (LabKind lab in new[] { LabKind.Material, LabKind.Building })
            {
                if (!research.TryGetValue(lab, out var r) || r == null) continue;
                var labB = LabBuilding(lab);
                if (labB == null || labB.flooded) continue; // the lab is gone or flooded: research waits
                r.done += dt;
                if (r.done < r.node.days * Day) continue;
                unlocked.Add(r.node.id);
                research[lab] = null;
                if (r.node.id == "B5") Orders.storageCapacity = Mathf.Max(Orders.storageCapacity, bigStorageCapacity);
                colony.Log($"Research done: {r.node.name}. {r.node.effect}", true);
            }
        }

        /// <summary>Test and debug only: unlock a node at once.</summary>
        public void DebugUnlock(string id)
        {
            unlocked.Add(id);
            if (id == "B5") Orders.storageCapacity = Mathf.Max(Orders.storageCapacity, bigStorageCapacity);
        }

        // ------------------------------------------------------------------ water wheels and power

        IEnumerable<BuildingSite> OfKind(BuildingKind k) { foreach (var b in Orders.Buildings) if (b.Kind == k) yield return b; }

        /// <summary>How fast a wheel on this hex turns machines: 0 = stopped, 1 = reference speed.</summary>
        public float WheelFactor(HexCell c, out string why)
        {
            var w = colony.water;
            float depth = w.DepthAt(c.center.x, c.center.z);
            float speed = w.VelocityAt(c.center.x, c.center.z).magnitude;
            if (colony.IsWinter && colony.seasons.Ice > 0.05f) { why = "frozen"; return 0f; }
            if (depth < wheelMinDepth) { why = $"water too shallow ({depth:0.00} m, needs {wheelMinDepth:0.0} m)"; return 0f; }
            if (speed < wheelMinSpeed) { why = $"water too slow ({speed:0.00} m/s)"; return 0f; }
            float f = Mathf.Clamp(speed / wheelRefSpeed, wheelMinFactor, wheelMaxFactor);
            why = $"water {speed:0.0} m/s: machines at {f:0.00}x";
            return f;
        }

        /// <summary>Each finished wheel drives up to two finished machines next to it.</summary>
        void UpdatePower(float dt)
        {
            foreach (var m in OfKind(BuildingKind.Machine)) { m.power = 0f; m.powerWhy = "no water wheel next to it"; }
            foreach (var wh in OfKind(BuildingKind.WaterWheel))
            {
                if (!wh.Finished) continue;
                wh.power = WheelFactor(wh.hex, out wh.powerWhy);
                wh.wheelAngle += wh.power * 90f * dt;
                if (wh.visual != null)
                {
                    var spin = wh.visual.transform.Find("Spin");
                    if (spin != null) spin.localRotation = Quaternion.Euler(wh.wheelAngle, 0f, 0f);
                }
                int used = 0;
                foreach (var n in colony.grid.Neighbours(wh.hex))
                {
                    if (used >= machinesPerWheel) break;
                    foreach (var m in OfKind(BuildingKind.Machine))
                        if (m.hex == n && m.Finished && m.power <= 0f && used < machinesPerWheel)
                        {
                            used++;
                            m.power = m.flooded ? 0f : wh.power;
                            m.powerWhy = m.flooded ? "flooded" : wh.power > 0f ? $"wheel: {wh.powerWhy}" : $"wheel stopped: {wh.powerWhy}";
                        }
                }
            }
        }

        // ------------------------------------------------------------------ machines

        float assignTimer;

        void Update()
        {
            if (colony == null || colony.orders == null) return;
            float dt = Time.deltaTime;
            UpdateResearch(dt);
            UpdatePower(dt);
            assignTimer -= dt;
            if (assignTimer <= 0f) { assignTimer = 1f; AssignOperators(); }
        }

        /// <summary>Every machine with work and no operator calls one beaver: an idle worker first, else a random worker that is not busy eating or sleeping.</summary>
        void AssignOperators()
        {
            foreach (var m in OfKind(BuildingKind.Machine))
            {
                if (Taken(m) || !HasWork(m)) continue;
                Beaver pick = null;
                var options = new List<Beaver>();
                foreach (var b in colony.beavers)
                {
                    if (!b.CanWork || b.machineJob != null || b.Hungry || b.state == BeaverState.InLodge || operators.ContainsKey(b)) continue;
                    if (b.group == WorkGroup.Idle) { pick = b; break; }
                    options.Add(b);
                }
                if (pick == null && options.Count > 0) pick = options[Random.Range(0, options.Count)];
                if (pick == null) continue;
                operators[pick] = (m, Time.time);
                pick.machineJob = m;
                if (pick.carrying == Item.None && pick.state != BeaverState.Working) pick.Interrupt();
                colony.Log($"{pick.beaverName} goes to run the {m.Title.ToLower()}");
            }
        }

        public Beaver OperatorOf(BuildingSite m)
        {
            foreach (var kv in operators) if (kv.Value.m == m && kv.Key != null) return kv.Key;
            return null;
        }

        /// <summary>Batches to load in one trip: up to batchesPerTrip, and not more than ordered.</summary>
        public int WantBatches(BuildingSite m) => m.auto && Automation ? batchesPerTrip : Mathf.Min(batchesPerTrip, m.ordered);

        /// <summary>Full batches the inputs loaded in the machine are enough for.</summary>
        public int BatchesLoaded(BuildingSite m)
        {
            int n = int.MaxValue;
            foreach (var i in m.machine.recipe.inputs) n = Mathf.Min(n, m.Buffer(i.item) / Mathf.Max(1, i.count));
            return n == int.MaxValue ? 0 : n;
        }

        bool InputsAvailable(BuildingSite m)
        {
            foreach (var i in m.machine.recipe.inputs)
                if (m.Buffer(i.item) + colony.SupplyStock(i.item) < i.count) return false;
            return true;
        }

        /// <summary>Why a machine is not working now ("" = it has work).</summary>
        public string WaitReason(BuildingSite m)
        {
            if (!m.Finished) return "being built";
            if (!(m.auto && Automation) && m.ordered <= 0) return "no order: press +1 or +5";
            if (m.power <= 0f) return m.powerWhy;
            if (!InputsAvailable(m)) return $"waiting for {m.machine.recipe.Text().Split('-')[0].Trim()}";
            var outItem = m.machine.recipe.output.item;
            if (colony.FreeSpace(outItem) <= 0) return $"no storage room for {ItemInfo.Lower(outItem)}";
            return "";
        }

        public bool HasWork(BuildingSite m) => WaitReason(m).Length == 0;

        /// <summary>Nearest machine with work and no operator.</summary>
        public BuildingSite PickMachine(Beaver b)
        {
            Release(b);
            BuildingSite best = null; float bd = float.MaxValue;
            foreach (var m in OfKind(BuildingKind.Machine))
            {
                if (Taken(m) || !HasWork(m)) continue;
                float d = (m.hex.center - b.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = m; }
            }
            if (best != null) operators[b] = (best, Time.time);
            return best;
        }

        public bool StillOperating(Beaver b, BuildingSite m)
        {
            if (!operators.TryGetValue(b, out var x) || x.m != m || !Orders.sites.Contains(m)) return false;
            operators[b] = (m, Time.time);
            return true;
        }

        public void Release(Beaver b)
        {
            operators.Remove(b);
            if (b != null) b.machineJob = null;
        }

        /// <summary>First input the machine still needs for this many batches, or None when they are loaded.</summary>
        public Item MissingInput(BuildingSite m, int batches, out int count)
        {
            foreach (var i in m.machine.recipe.inputs)
            {
                int need = i.count * batches - m.Buffer(i.item);
                if (need > 0 && colony.SupplyStock(i.item) > 0) { count = need; return i.item; }
            }
            count = 0;
            return Item.None;
        }

        /// <summary>One batch is done: inputs used, the product waits in the machine until the operator carries it home.</summary>
        public void FinishBatch(BuildingSite m)
        {
            foreach (var i in m.machine.recipe.inputs) m.AddBuffer(i.item, -i.count);
            if (!(m.auto && Automation)) m.ordered = Mathf.Max(0, m.ordered - 1);
            int n = m.machine.recipe.output.count;
            m.made += n;
            m.outBuffer += n;
            colony.Stat($"{ItemInfo.Lower(m.machine.recipe.output.item)} made by machines", n);
        }

        /// <summary>Seconds one batch takes now (slower or faster with the water).</summary>
        public float BatchSeconds(BuildingSite m) => m.machine.recipe.seconds / Mathf.Max(0.05f, m.power);

        /// <summary>Drops what is loaded in a machine when it is removed or lost.</summary>
        public void DropBuffer(BuildingSite m)
        {
            if (m.outBuffer > 0) colony.DropPile(m.hex, m.machine.recipe.output.item, m.outBuffer);
            m.outBuffer = 0;
            if (m.buffer == null) return;
            foreach (var kv in m.buffer) colony.DropPile(m.hex, kv.Key, kv.Value);
            m.buffer.Clear();
        }

        public string IdleReason()
        {
            bool any = false;
            foreach (var m in OfKind(BuildingKind.Machine)) { any = true; break; }
            return any ? "No machine needs an operator now" : "No machines yet";
        }
    }
}
