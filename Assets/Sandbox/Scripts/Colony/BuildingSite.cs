using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    public enum BuildingKind { Base, Nursery, Storage, PumpBase, FoodFarm, MaterialLab, BuildingLab, WaterWheel, Machine, AlarmBell }

    /// <summary>Settings of one building type, editable in the Inspector (BuildOrders).</summary>
    [System.Serializable]
    public class BuildingDef
    {
        public BuildingKind kind;
        public string name;
        [TextArea] public string rule;
        public BuildStage[] stages;
    }

    /// <summary>
    /// A base, nursery or storage on one hex.
    /// - Base: a platform of stones and mud. Every building stands on a finished base. The base
    ///   is a solid block in the water grid, so it also holds back water a little.
    /// - Nursery: where kits are born. Like a real lodge it needs an entrance under water.
    /// - Storage: holds one material (or food), picked when the leader orders it. Separate
    ///   storages per material make the leader plan where each material goes.
    /// </summary>
    public class BuildingSite : Site, ISupply
    {
        public readonly BuildingDef def;
        public readonly Site baseSite;     // the base (or Dam Mk2) under a nursery or storage
        readonly List<int> cells;
        readonly WaterGrid water;
        readonly float baseHeight;

        // storage: holds one item type, picked by the leader when ordering
        public Item storeType = Item.Wood;
        public int stock;
        public bool stockChanged = true;
        // food farm (water plants)
        public float growth;
        public int farmStock;
        public int farmReserved;
        public bool farmDry;
        // machine (Phase C): its data, the leader's order and what is loaded in it
        public MachineDef machine;
        public int ordered;
        public bool auto;
        public int made;
        /// <summary>Products made but not carried home yet.</summary>
        public int outBuffer;
        public Dictionary<Item, int> buffer;
        public int Buffer(Item i) => buffer != null && buffer.TryGetValue(i, out int n) ? n : 0;
        public void AddBuffer(Item i, int n)
        {
            if (buffer == null) buffer = new Dictionary<Item, int>();
            buffer[i] = Mathf.Max(0, Buffer(i) + n);
        }
        // machine and water wheel: how fast it runs now (0 = stopped) and why
        public float power;
        public string powerWhy = "";
        public float wheelAngle;
        /// <summary>Water is above the floor (flood): storages lose stock, machines, labs and nurseries stop.</summary>
        public bool flooded;
        public Material look;
        // pump base
        public float pumped;
        public string pumpStatus = "";
        // nursery
        public float litterTimer;
        public bool entranceUnderWater;
        public float entranceDepth;
        /// <summary>Why the nursery makes no kits now ("" = it is working).</summary>
        public string waitReason = "";

        public HexCell Hex => hex;

        public BuildingSite(BuildingDef def, HexCell hex, Site baseSite, List<int> cells, WaterGrid water, float baseHeight) : base(hex)
        {
            this.def = def; this.baseSite = baseSite; this.cells = cells; this.water = water; this.baseHeight = baseHeight;
        }

        public BuildingKind Kind => def.kind;
        public override int StageCount => def.stages.Length;
        public override BuildStage StageAt(int index) => def.stages[index];

        /// <summary>Top of the base platform (floor of the building on it).</summary>
        /// <summary>Base and pump base are platforms; nurseries and storages stand on one.</summary>
        public bool IsBase => Kind == BuildingKind.Base || Kind == BuildingKind.PumpBase;

        public float Floor => IsBase ? hex.minGround + baseHeight
            : Kind == BuildingKind.FoodFarm || Kind == BuildingKind.WaterWheel ? hex.minGround
            : Kind == BuildingKind.AlarmBell ? hex.maxGround
            : baseSite is DamSite d ? d.Crest : baseSite is BuildingSite b ? b.Floor : hex.minGround;

        protected override void OnStageDone()
        {
            if (IsBase) ApplyToWater();
        }

        /// <summary>A base is a block in the water grid: stones are leaky, the mud top seals it.</summary>
        public void ApplyToWater()
        {
            if (cells == null) return;
            float leak = stage <= 0 ? 0.5f : 0.05f;
            foreach (int id in cells)
                water.SetStatic(id, stage >= 0 ? Mathf.Max(0f, Floor - water.CellGround(id)) : 0f, leak);
        }

        // ------------------------------------------------------------------ storage

        public int Stock(Item item) => Kind == BuildingKind.Storage && item == storeType ? stock : 0;

        public void Add(Item item, int n)
        {
            if (item != storeType) return;
            stock += n;
            stockChanged = true;
        }

        public override string Title => Kind == BuildingKind.Storage ? $"{ItemInfo.Name(storeType)} storage" : def.name;

        public override void Remove()
        {
            if (IsBase && cells != null)
                foreach (int id in cells) water.SetStatic(id, 0f, 0f);
            base.Remove();
        }

        /// <summary>Drop-off point on the edge of the building, on the side the beaver comes from.</summary>
        public Vector3 Door(Vector3 from)
        {
            Vector3 c = new Vector3(hex.center.x, Floor, hex.center.z);
            Vector3 d = from - c; d.y = 0f;
            if (d.sqrMagnitude < 0.01f) d = Vector3.forward;
            return c + d.normalized * 1.6f;
        }
    }
}
