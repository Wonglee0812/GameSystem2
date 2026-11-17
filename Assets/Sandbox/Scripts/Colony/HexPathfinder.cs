using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// A* search over the hex grid (Hart, Nilsson, &amp; Raphael, 1968; hex distance from Patel, 2013).
    /// Beavers swim faster than they walk, so wet hexes are cheaper than dry ones.
    /// Steep steps between two dry hexes cannot be climbed.
    /// </summary>
    public class HexPathfinder
    {
        readonly HexGrid grid;
        readonly WaterGrid water;
        public float swimCost = 1f, walkCost = 2f, maxStep = 1.4f, swimDepth = 0.3f;

        public HexPathfinder(HexGrid grid, WaterGrid water) { this.grid = grid; this.water = water; }

        public bool IsWet(HexCell c) => water.DepthAt(c.center.x, c.center.z) > swimDepth;

        public static int Distance(Vector2Int a, Vector2Int b)
        {
            int dq = a.x - b.x, dr = a.y - b.y;
            return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(dq + dr)) / 2;
        }

        float StepCost(HexCell from, HexCell to)
        {
            if (to.blocked) return -1f;
            bool wetFrom = IsWet(from), wetTo = IsWet(to);
            if (!wetFrom && !wetTo && Mathf.Abs(to.meanGround - from.meanGround) > maxStep) return -1f;
            return wetTo ? swimCost : walkCost;
        }

        /// <summary>Path from start to goal (both included). The goal may be a blocked hex (e.g. the lodge).</summary>
        public List<HexCell> FindPath(HexCell start, HexCell goal)
        {
            if (start == null || goal == null) return null;
            var open = new List<HexCell> { start };
            var came = new Dictionary<HexCell, HexCell>();
            var g = new Dictionary<HexCell, float> { [start] = 0f };
            var f = new Dictionary<HexCell, float> { [start] = Distance(start.axial, goal.axial) };
            var closed = new HashSet<HexCell>();
            int guard = 0;

            while (open.Count > 0 && guard++ < 4000)
            {
                int best = 0;
                for (int i = 1; i < open.Count; i++) if (f[open[i]] < f[open[best]]) best = i;
                var cur = open[best];
                if (cur == goal) return Rebuild(came, cur);
                open.RemoveAt(best);
                closed.Add(cur);

                foreach (var nb in grid.Neighbours(cur))
                {
                    if (closed.Contains(nb)) continue;
                    float cost = nb == goal ? (IsWet(nb) ? swimCost : walkCost) : StepCost(cur, nb);
                    if (cost < 0f) continue;
                    float tentative = g[cur] + cost;
                    if (g.TryGetValue(nb, out float old) && tentative >= old) continue;
                    came[nb] = cur;
                    g[nb] = tentative;
                    f[nb] = tentative + Distance(nb.axial, goal.axial) * swimCost;
                    if (!open.Contains(nb)) open.Add(nb);
                }
            }
            return null;
        }

        static List<HexCell> Rebuild(Dictionary<HexCell, HexCell> came, HexCell cur)
        {
            var path = new List<HexCell> { cur };
            while (came.TryGetValue(cur, out var prev)) { cur = prev; path.Add(cur); }
            path.Reverse();
            return path;
        }
    }
}
