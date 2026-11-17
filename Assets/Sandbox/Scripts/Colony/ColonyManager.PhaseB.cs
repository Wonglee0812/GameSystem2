using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Phase B work targets: trees marked to fell and hexes ordered to dig.
    /// Unmarked trees are only gnawed for wood and stay. A marked tree is felled: it takes longer,
    /// gives a lot of wood at once and is gone for good.
    /// </summary>
    public partial class ColonyManager
    {
        [Header("Phase B: fell and dig")]
        [Tooltip("Wood a felled tree gives (carried home and the rest left as a pile).")]
        public int fellWood = 6;
        [Tooltip("Felling a tree takes this many times longer than gnawing one load.")]
        public float fellTimeFactor = 3f;
        [Tooltip("Most diggers on one dig order at the same time.")]
        public int diggersPerOrder = 2;

        readonly HashSet<Transform> fellMarks = new HashSet<Transform>();

        bool OnHex(Transform t, HexCell c) => t != null && grid.TryGet(t.position, out var h) && h == c;

        public int TreesOn(HexCell c, bool markedOnly)
        {
            int n = 0;
            foreach (var t in trees) if (OnHex(t, c) && (!markedOnly || fellMarks.Contains(t))) n++;
            return n;
        }

        public int MarkTrees(HexCell c)
        {
            int n = 0;
            foreach (var t in trees) if (OnHex(t, c) && fellMarks.Add(t)) n++;
            if (n > 0) Log($"{n} tree(s) at {c.axial} marked to fell");
            return n;
        }

        public void UnmarkTrees(HexCell c) => fellMarks.RemoveWhere(t => t == null || OnHex(t, c));

        public IEnumerable<HexCell> MarkedHexes()
        {
            var seen = new HashSet<HexCell>();
            foreach (var t in fellMarks)
                if (t != null && grid.TryGet(t.position, out var h) && seen.Add(h)) yield return h;
        }

        /// <summary>Nearest marked tree that no other cutter is felling.</summary>
        public bool PickFellTree(Beaver b, out Vector3 pos, out HexCell hex, out Transform tree)
        {
            Release(b);
            pos = Vector3.zero; hex = null; tree = null;
            float bd = float.MaxValue;
            foreach (var t in fellMarks)
            {
                if (t == null || reserved.ContainsValue(t)) continue;
                if (!grid.TryGet(t.position, out var h)) continue; // trees next to or on a building can be felled too
                float d = Flat(t.position - b.transform.position);
                if (d < bd) { bd = d; tree = t; hex = h; }
            }
            if (tree == null) return false;
            reserved[b] = tree;
            pos = tree.position;
            return true;
        }

        /// <summary>Fells a marked tree: it is removed for good. Returns the wood it gives (0 = already gone).</summary>
        public int FellTree(Transform t)
        {
            if (t == null || !trees.Contains(t)) return 0;
            trees.Remove(t);
            fellMarks.Remove(t);
            Destroy(t.gameObject);
            Stat("trees felled");
            return fellWood;
        }

        public bool HasDigOrders => orders != null && orders.HasDigOrders;

        /// <summary>Nearest dig order with room for one more digger.</summary>
        bool PickOrderedDig(Beaver b, out HexCell hex)
        {
            hex = null;
            if (orders == null) return false;
            float bd = float.MaxValue;
            foreach (var c in orders.DigHexes)
            {
                int n = 0;
                foreach (var v in reserved.Values) if (ReferenceEquals(v, c)) n++;
                if (n >= diggersPerOrder) continue;
                float d = Flat(c.center - b.transform.position);
                if (d < bd) { bd = d; hex = c; }
            }
            if (hex != null) reserved[b] = hex;
            return hex != null;
        }

        /// <summary>Highest free hex next to a dug hex: the bank where spare mud is piled up.</summary>
        public HexCell Bank(HexCell c)
        {
            HexCell best = c; float top = float.MinValue;
            foreach (var n in grid.Neighbours(c))
                if (!n.blocked && n.meanGround > top) { top = n.meanGround; best = n; }
            return best;
        }
    }
}
