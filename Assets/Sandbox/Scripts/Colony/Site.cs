using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>One building step: the materials it needs. Dams and buildings both use it.</summary>
    [System.Serializable]
    public class BuildStage
    {
        /// <summary>Every material a stage can need: raw first, then the ones made by machines (Phase C).</summary>
        public static readonly Item[] Materials = { Item.Stone, Item.Mud, Item.Wood, Item.PressedWood, Item.Gravel, Item.Sand, Item.Cement };

        public string name;
        public int wood, mud, stone;
        [Tooltip("Materials from machines (Phase C).")]
        public int pressedWood, gravel, sand, cement;

        public int Need(Item item)
        {
            switch (item)
            {
                case Item.Wood: return wood;
                case Item.Mud: return mud;
                case Item.Stone: return stone;
                case Item.PressedWood: return pressedWood;
                case Item.Gravel: return gravel;
                case Item.Sand: return sand;
                case Item.Cement: return cement;
                default: return 0;
            }
        }

        public void Set(Item item, int n)
        {
            switch (item)
            {
                case Item.Wood: wood = n; break;
                case Item.Mud: mud = n; break;
                case Item.Stone: stone = n; break;
                case Item.PressedWood: pressedWood = n; break;
                case Item.Gravel: gravel = n; break;
                case Item.Sand: sand = n; break;
                case Item.Cement: cement = n; break;
            }
        }

        public int Total { get { int n = 0; foreach (var i in Materials) n += Need(i); return n; } }

        /// <summary>Wood-like (sticks, pressed wood), stone-like (stone, gravel, cement) or mud-like (mud, sand) share, for looks and wear.</summary>
        public int Woody => wood + pressedWood;
        public int Stony => stone + gravel + cement;
        public int Muddy => mud + sand;

        /// <summary>The item this stage needs most of (repairs use it).</summary>
        public Item Main { get { Item best = Item.Mud; int n = -1; foreach (var i in Materials) if (Need(i) > n) { n = Need(i); best = i; } return best; } }

        public string CostText()
        {
            var parts = new List<string>();
            foreach (var i in new[] { Item.Wood, Item.Mud, Item.Stone, Item.PressedWood, Item.Gravel, Item.Sand, Item.Cement })
                if (Need(i) > 0) parts.Add($"{Need(i)} {ItemInfo.Lower(i)}");
            return string.Join(", ", parts);
        }

        public static BuildStage Sum(IEnumerable<BuildStage> stages)
        {
            var t = new BuildStage();
            foreach (var st in stages) foreach (var i in Materials) t.Set(i, t.Need(i) + st.Need(i));
            return t;
        }
    }

    /// <summary>
    /// Anything the leader orders on a hex (dam, base, nursery, storage). Builders bring one item
    /// at a time; when a stage has all its items, the stage is done and the next one starts.
    /// </summary>
    public abstract class Site
    {
        static Item[] Items => BuildStage.Materials;

        public readonly HexCell hex;
        public int stage = -1; // -1 = planned, nothing built yet
        public readonly Dictionary<Item, int> delivered = new Dictionary<Item, int>();
        public readonly Dictionary<Item, int> incoming = new Dictionary<Item, int>();
        public GameObject visual;
        /// <summary>Priority flag set by the leader: builders serve flagged sites first.</summary>
        public bool priority;
        public GameObject flag;

        protected Site(HexCell hex)
        {
            this.hex = hex;
            foreach (var i in Items) { delivered[i] = 0; incoming[i] = 0; }
        }

        public abstract int StageCount { get; }
        public abstract BuildStage StageAt(int index);
        public abstract string Title { get; }
        /// <summary>Higher = builders go here first (dams use their leak noise).</summary>
        public virtual float Urgency => 0f;

        public bool Finished => stage >= StageCount - 1;

        /// <summary>
        /// A repair the leader ordered: a few items that restore the condition. It comes before
        /// the next stage. The items already delivered for the next stage are kept aside.
        /// </summary>
        public BuildStage repair;
        Dictionary<Item, int> stash;

        /// <summary>True while builders still have something to bring (a stage or a repair).</summary>
        public bool HasWork => repair != null || !Finished;
        public BuildStage Next => repair ?? (Finished ? null : StageAt(stage + 1));

        public void StartRepair(BuildStage cost)
        {
            if (repair != null || cost == null) return;
            repair = cost;
            stash = new Dictionary<Item, int>(delivered);
            foreach (var i in Items) delivered[i] = 0;
        }

        /// <summary>Stops a repair; what was brought for it counts for the next stage.</summary>
        public void CancelRepair()
        {
            if (repair == null) return;
            foreach (var i in Items) delivered[i] += stash[i];
            repair = null; stash = null;
        }

        protected virtual void OnRepairDone() { }

        /// <summary>All materials delivered so far that are not part of a finished stage.</summary>
        public int Loose(Item item) => delivered[item] + (stash != null ? stash[item] : 0);

        /// <summary>Material the next stage still needs that nobody is bringing yet.</summary>
        public bool NeedsItem(out Item item)
        {
            item = Item.None;
            var next = Next;
            if (next == null) return false;
            foreach (var i in Items)
                if (delivered[i] + incoming[i] < next.Need(i)) { item = i; return true; }
            return false;
        }

        /// <summary>How many more of this item the next stage needs that nobody is bringing yet.</summary>
        public int Missing(Item item)
        {
            var next = Next;
            return next == null ? 0 : Mathf.Max(0, next.Need(item) - delivered[item] - incoming[item]);
        }

        /// <summary>A builder dropped one item here. Returns true when this finished a stage.</summary>
        public bool Deliver(Item item)
        {
            incoming[item] = Mathf.Max(0, incoming[item] - 1);
            delivered[item]++;
            var next = Next;
            if (next == null) return false;
            foreach (var i in Items)
                if (delivered[i] < next.Need(i)) return false;
            if (repair != null)
            {
                // extra items brought for the repair stay for the next stage
                foreach (var i in Items) delivered[i] = stash[i] + delivered[i] - next.Need(i);
                repair = null; stash = null;
                OnRepairDone();
                return true;
            }
            foreach (var i in Items) delivered[i] -= next.Need(i);
            stage++;
            OnStageDone();
            return true;
        }

        protected virtual void OnStageDone() { }

        /// <summary>Short progress text, e.g. "frame: wood 2/6".</summary>
        public string Progress()
        {
            var next = Next;
            if (next == null) return "done";
            string need = "";
            foreach (var i in Items)
                if (next.Need(i) > 0) need += $" {i.ToString().ToLower()} {delivered[i]}/{next.Need(i)}";
            return $"{next.name}:{need}";
        }

        /// <summary>0-1 over all stages, for progress bars.</summary>
        public float Progress01()
        {
            int total = 0, done = 0;
            if (repair != null)
            {
                foreach (var i in Items) { total += repair.Need(i); done += Mathf.Min(delivered[i], repair.Need(i)); }
                return total > 0 ? (float)done / total : 1f;
            }
            for (int s = 0; s < StageCount; s++)
            {
                var st = StageAt(s);
                int n = st.Total;
                total += n;
                if (s <= stage) done += n;
                else if (s == stage + 1) foreach (var i in Items) done += Mathf.Min(delivered[i], st.Need(i));
            }
            return total > 0 ? (float)done / total : 1f;
        }

        public virtual void Remove()
        {
            if (visual != null) Object.Destroy(visual);
            if (flag != null) Object.Destroy(flag);
        }

        /// <summary>Materials still missing for the whole site (all stages left), e.g. "wood 4, mud 3".</summary>
        public string MissingText()
        {
            var left = new BuildStage();
            if (repair != null)
            {
                foreach (var i in Items) left.Set(i, Mathf.Max(0, repair.Need(i) - delivered[i]));
                string r = left.CostText();
                return r.Length > 0 ? r : "nothing";
            }
            for (int s = stage + 1; s < StageCount; s++)
            {
                var st = StageAt(s);
                foreach (var i in Items) left.Set(i, left.Need(i) + Mathf.Max(0, st.Need(i) - (s == stage + 1 ? delivered[i] : 0)));
            }
            string t = left.CostText();
            return t.Length > 0 ? t : "nothing";
        }
    }
}
