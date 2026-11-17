using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>One building stage of a dam: the materials it needs and what it gives.</summary>
    [System.Serializable]
    public class DamStage : BuildStage
    {
        [Tooltip("Top of the dam above the lowest ground of the hex after this stage (m).")]
        public float height;
        [Tooltip("0 = watertight, 1 = very leaky.")]
        public float leakiness;
    }

    public enum DamKind { Mk1, Mk2, Spillway, Levee, Floodgate }

    /// <summary>
    /// Settings of one dam type (Inspector, BuildOrders). The shared rules (water, wear, repair)
    /// are the same for every type; each type has its own stages, height, leak and wear values.
    /// </summary>
    [System.Serializable]
    public class DamDef
    {
        public DamKind kind;
        public string name;
        [TextArea] public string rule;
        [Tooltip("Wear from water running over the top: 1 = normal, 0 = never wears (spillway).")]
        public float wearFactor = 1f;
        [Tooltip("A finished dam of this type works as a base: a nursery or storage can stand on it.")]
        public bool platform;
        public DamStage[] stages;
    }

    /// <summary>
    /// A dam ordered on one hex. It is built in the order real beavers use: a base of mud and
    /// stones, then sticks, then mud plastered on to close the gaps (Beaver Trust, n.d.).
    /// Every finished stage raises the dam in the water grid and makes it less leaky.
    /// </summary>
    public class DamSite : Site
    {
        /// <summary>1 = solid. Water running over the top wears it down; at 0 the dam loses a stage.</summary>
        public float condition = 1f;
        /// <summary>How "loud" the dam is: leaking or overflowing water (used by the repair instinct).</summary>
        public float leakNoise;
        public float overtop;
        /// <summary>The dam lost a stage: builders and idle beavers come to build it up again.</summary>
        public bool damaged;

        readonly DamStage[] stages;
        readonly List<int> cells;
        readonly WaterGrid water;
        public readonly DamKind kind;
        public readonly float wearFactor;
        public readonly bool platform;
        readonly string title;

        public DamSite(HexCell hex, DamStage[] stages, List<int> cells, WaterGrid water) : base(hex)
        {
            this.stages = stages; this.cells = cells; this.water = water;
            kind = DamKind.Mk1; wearFactor = 1f; title = "Dam Mk1";
        }

        public DamSite(HexCell hex, DamDef def, List<int> cells, WaterGrid water) : base(hex)
        {
            stages = def.stages; this.cells = cells; this.water = water;
            kind = def.kind; wearFactor = def.wearFactor; platform = def.platform; title = def.name;
        }

        public override int StageCount => stages.Length;
        public override BuildStage StageAt(int index) => stages[index];
        public override string Title => title;
        // flood defences (plan v4)
        /// <summary>Floodgate: open = the water runs through over the low sill.</summary>
        public bool gateOpen;
        /// <summary>Sandbags on top (+height, less wear) and the order on the way.</summary>
        public bool bags, bagging;
        public float bagTimer;
        public float bagHeight = 0.3f;
        public bool GateOpen => kind == DamKind.Floodgate && gateOpen && stage >= 1;

        /// <summary>A finished platform dam (Dam Mk2) can carry a building.</summary>
        public bool IsPlatformNow => platform && Finished;
        public override float Urgency => leakNoise;
        public DamStage CurrentStage => stage >= 0 ? stages[stage] : null;
        public float Crest => hex.minGround + (stage < 0 ? 0f : GateOpen ? Mathf.Min(0.15f, stages[0].height) : stages[stage].height) + (bags && stage >= 0 ? bagHeight : 0f);

        protected override void OnStageDone()
        {
            ApplyToWater();
            if (Finished) damaged = false;
        }

        protected override void OnRepairDone()
        {
            if (bagging) { bagging = false; bags = true; ApplyToWater(); return; } // the sandbags are on
            condition = 1f;
        }

        /// <summary>
        /// Repair before the dam breaks: a few items of the main material of the top stage
        /// (sticks for the body, mud for the seal and the base). The cost grows with the wear,
        /// but stays far below the cost of building the stage again:
        /// items = stage items x share x wear, at least 1.
        /// </summary>
        public BuildStage RepairCost(float share)
        {
            var st = CurrentStage;
            float wear = 1f - condition;
            if (st == null || wear < 0.05f) return null;
            int n = Mathf.Max(1, Mathf.CeilToInt(st.Total * share * wear));
            var cost = new BuildStage { name = "repair" };
            cost.Set(st.Main, n);
            return cost;
        }

        /// <summary>The top stage is washed away (seal first, then sticks, then the base). Returns the stage lost.</summary>
        public DamStage Degrade()
        {
            if (stage < 0) return null;
            CancelRepair();
            var lost = stages[stage];
            stage--;
            condition = 1f;
            damaged = true;
            ApplyToWater();
            return lost;
        }

        public IReadOnlyList<int> Cells => cells;

        public void ApplyToWater()
        {
            var st = CurrentStage;
            foreach (int id in cells)
            {
                float h = st == null ? 0f : Mathf.Max(0f, Crest - water.CellGround(id));
                water.SetStatic(id, h, st == null ? 0f : st.leakiness);
            }
        }

        public override void Remove()
        {
            foreach (int id in cells) water.SetStatic(id, 0f, 0f);
            base.Remove();
        }

        public string Status()
        {
            string leak = leakNoise > 0.25f ? "  LEAK!" : "";
            string wear = condition < 0.99f ? $"  {condition * 100f:0}%" : "";
            if (repair != null) return $"Dam - repair: {MissingText()} left{leak}";
            if (Finished) return $"Dam {Crest - hex.minGround:0.0} m{wear}{leak}";
            return $"Dam - {Progress()}{wear}{leak}";
        }
    }
}
