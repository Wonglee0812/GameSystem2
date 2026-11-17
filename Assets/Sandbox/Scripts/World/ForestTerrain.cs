using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>One stream drawn as a line of points from its source to its mouth.</summary>
    [System.Serializable]
    public class StreamPath
    {
        public string name = "Stream";
        public Vector2[] points;          // world x, z from source to mouth
        public float halfWidth = 1.5f;    // half the width of the stream bed (m)
        public float valleyHalfWidth = 8f;// how far the valley sides reach from the bed (m)
        public float sourceDepth = 1f;    // bed depth below the ground at the source (m)
        [Tooltip("Index of the stream this one flows into, or -1 when it leaves the map.")]
        public int joins = -1;

        [System.NonSerialized] public float[] bedAtPoint; // bed height at each point (filled by ForestTerrain)
    }

    /// <summary>
    /// Forest map: rolling ground that falls from west (upstream) to east (downstream),
    /// with a main stream and side streams cut into it. Beavers prefer low-gradient streams
    /// in wide valleys (USDA Forest Service, n.d.), so the bed falls about 3-4 % and every
    /// stream has a wide, flat valley next to it.
    /// 2026-11-14: the streams can be made at random for every game (Randomize, called by
    /// MapRandomizer): smooth meandering curves sampled every 2 m, so there are no sharp corners.
    /// The banks are soft U shapes over a few metres, and the small bumps are gone, so the water
    /// runs smoothly.
    /// </summary>
    public class ForestTerrain : GroundHeights
    {
        public float baseHeight = 6f;     // ground height at the west edge
        public float fall = 4f;           // the ground drops this much from west to east
        public float hillHeight = 2f;
        public int seed = 11;
        public List<StreamPath> streams = new List<StreamPath>();

        [Tooltip("Width of the soft bank between the stream bed and the valley floor (m).")]
        public float bankWidth = 2.5f;
        [Tooltip("Small bumps on the ground (m). 0 = smooth ground, best for the water.")]
        public float bumpHeight = 0f;

        bool prepared;

        float BaseGround(float x, float z)
        {
            float slope = baseHeight - fall * Mathf.Clamp01(x / LengthX);
            float hills = (Mathf.PerlinNoise(x * 0.035f + seed, z * 0.035f + seed * 0.5f) - 0.45f) * hillHeight * 2f;
            // a second, wider layer of gentle hills instead of the old sharp bumps
            hills += (Mathf.PerlinNoise(x * 0.012f + seed * 1.7f, z * 0.012f + 4.2f) - 0.5f) * hillHeight;
            float bumps = bumpHeight > 0f ? (Mathf.PerlinNoise(x * 0.15f + seed * 2f, z * 0.15f) - 0.5f) * bumpHeight : 0f;
            return slope + hills + bumps;
        }

        // ------------------------------------------------------------ random streams

        /// <summary>
        /// Makes a new set of streams from a seed: a main stream that meanders from west to east
        /// and three side streams that come from the north and south edges and join it.
        /// Every stream is a smooth curve with a point every 2 m. Index 0 is the main stream.
        /// </summary>
        public void Randomize(int newSeed)
        {
            var rng = new System.Random(newSeed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            seed = rng.Next(1, 997);
            prepared = false;
            streams.Clear();
            float L = LengthX, W = WidthZ;

            // main stream: two waves of different length make a natural meander
            float zc = W * R(0.38f, 0.62f), a1 = R(5f, 9f), a2 = R(1.5f, 3f);
            float k1 = Mathf.PI * 2f / R(55f, 85f), k2 = Mathf.PI * 2f / R(20f, 32f), p1 = R(0f, 6.3f), p2 = R(0f, 6.3f);
            var main = new List<Vector2>();
            for (float x = 0f; x <= L + 0.01f; x += 2f)
                main.Add(new Vector2(x, Mathf.Clamp(zc + a1 * Mathf.Sin(x * k1 + p1) + a2 * Mathf.Sin(x * k2 + p2), W * 0.2f, W * 0.8f)));
            streams.Add(new StreamPath { name = "Main stream", points = main.ToArray(), halfWidth = R(1.4f, 1.8f), valleyHalfWidth = R(9f, 12f), sourceDepth = 1f, joins = -1 });

            // side streams: north, south, and one more on a random side; their joins are spread along the main stream
            string[] names = { "North stream", "South stream", "Side brook" };
            bool[] fromNorth = { true, false, rng.NextDouble() < 0.5 };
            float[] joinAt = { R(0.22f, 0.38f), R(0.42f, 0.58f), R(0.64f, 0.8f) };
            // shuffle the join places, so the north stream is not always the first one
            for (int i = 2; i > 0; i--) { int j = rng.Next(i + 1); (joinAt[i], joinAt[j]) = (joinAt[j], joinAt[i]); }
            var starts = new List<Vector2>();
            for (int k = 0; k < 3; k++)
            {
                float jx = joinAt[k] * L;
                Vector2 end = main[Mathf.Clamp(Mathf.RoundToInt(jx / 2f), 0, main.Count - 1)];
                // the stream comes in at an angle from upstream, like real tributaries
                float sx = Mathf.Clamp(jx - R(4f, 18f), 8f, L - 8f);
                Vector2 start = new Vector2(sx, fromNorth[k] ? W - 0.5f : 0.5f);
                // two streams on the same edge start at least 20 m apart
                foreach (var o in starts)
                    if (Mathf.Abs(o.y - start.y) < 1f && Mathf.Abs(o.x - start.x) < 20f)
                        start.x = Mathf.Clamp(o.x + (start.x >= o.x ? 20f : -20f), 8f, L - 8f);
                starts.Add(start);
                Vector2 ctrl = (start + end) * 0.5f + new Vector2(R(-10f, 10f), 0f);
                float wob = R(0.8f, 2f), wk = Mathf.PI * 2f / R(14f, 24f), wp = R(0f, 6.3f);
                float len = Vector2.Distance(start, ctrl) + Vector2.Distance(ctrl, end);
                int n = Mathf.Max(4, Mathf.CeilToInt(len / 2f));
                var pts = new Vector2[n + 1];
                for (int i = 0; i <= n; i++)
                {
                    float t = i / (float)n;
                    Vector2 q = (1 - t) * (1 - t) * start + 2 * (1 - t) * t * ctrl + t * t * end; // quadratic Bezier
                    float fade = Mathf.Sin(t * Mathf.PI);                                     // no wobble at the ends
                    q.x += wob * fade * Mathf.Sin(t * len * wk + wp);
                    pts[i] = q;
                }
                pts[n] = end;
                float hw = k == 2 ? R(0.75f, 0.9f) : R(0.9f, 1.1f);
                streams.Add(new StreamPath { name = names[k], points = pts, halfWidth = hw, valleyHalfWidth = R(5f, 7f), sourceDepth = R(0.7f, 0.8f), joins = 0 });
            }
        }

        /// <summary>A point a little way into a stream from its source (for the water source).</summary>
        public Vector2 SourcePoint(int stream)
        {
            var p = streams[stream].points;
            Vector2 dir = (p[Mathf.Min(1, p.Length - 1)] - p[0]).normalized;
            return p[0] + dir * 1.5f;
        }

        /// <summary>A point on the main stream at a fraction of the map length (for the lodge).</summary>
        public Vector2 MainStreamAt(float fraction)
        {
            var p = streams[0].points;
            return p[Mathf.Clamp(Mathf.RoundToInt(fraction * (p.Length - 1)), 0, p.Length - 1)];
        }

        /// <summary>Works out the bed height along each stream so water always runs downhill.</summary>
        void Prepare()
        {
            if (prepared) return;
            prepared = true;
            // main streams first (joins = -1), then the side streams that flow into them
            var order = new List<int>();
            for (int i = 0; i < streams.Count; i++) if (streams[i].joins < 0) order.Add(i);
            for (int i = 0; i < streams.Count; i++) if (streams[i].joins >= 0) order.Add(i);

            foreach (int s in order)
            {
                var st = streams[s];
                int n = st.points.Length;
                st.bedAtPoint = new float[n];
                float start = BaseGround(st.points[0].x, st.points[0].y) - st.sourceDepth;
                float end;
                if (st.joins >= 0)
                {
                    NearestOnStream(streams[st.joins], st.points[n - 1], out _, out end);
                    end += 0.05f;
                }
                else end = BaseGround(st.points[n - 1].x, st.points[n - 1].y) - st.sourceDepth - 0.3f;

                float total = 0f;
                var cum = new float[n];
                for (int i = 1; i < n; i++) { total += Vector2.Distance(st.points[i - 1], st.points[i]); cum[i] = total; }
                for (int i = 0; i < n; i++) st.bedAtPoint[i] = Mathf.Lerp(start, end, total > 0f ? cum[i] / total : 0f);
            }
        }

        /// <summary>Distance from a point to a stream line, and the bed height at the closest spot.</summary>
        static void NearestOnStream(StreamPath st, Vector2 p, out float dist, out float bed)
        {
            dist = float.MaxValue; bed = 0f;
            for (int i = 0; i < st.points.Length - 1; i++)
            {
                Vector2 a = st.points[i], b = st.points[i + 1];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
                float d = Vector2.Distance(p, a + ab * t);
                if (d < dist) { dist = d; bed = Mathf.Lerp(st.bedAtPoint[i], st.bedAtPoint[i + 1], t); }
            }
        }

        /// <summary>The stream that shapes this point most (smallest distance relative to its valley).</summary>
        public bool NearestStream(float x, float z, out StreamPath stream, out float dist, out float bed)
        {
            Prepare();
            stream = null; dist = float.MaxValue; bed = 0f;
            float best = float.MaxValue;
            var p = new Vector2(x, z);
            foreach (var st in streams)
            {
                NearestOnStream(st, p, out float d, out float b);
                float score = (d - st.halfWidth) / st.valleyHalfWidth;
                if (score < best) { best = score; stream = st; dist = d; bed = b; }
            }
            return stream != null;
        }

        protected override float HeightFormula(float x, float z)
        {
            Prepare();
            float h = BaseGround(x, z);
            var p = new Vector2(x, z);
            foreach (var st in streams)
            {
                NearestOnStream(st, p, out float d, out float bed);
                // bed (a soft U) -> low bank (0.6 m) over bankWidth -> valley floor rising slowly to the forest ground
                float bank = Smooth(st.halfWidth * 0.3f, st.halfWidth + bankWidth, d);
                float valley = Smooth(st.halfWidth + bankWidth, st.halfWidth + bankWidth + st.valleyHalfWidth, d);
                float carved = bed + 0.6f * bank + Mathf.Max(0f, BaseGround(x, z) - bed - 0.6f) * valley;
                h = Mathf.Min(h, carved);
            }
            // Keep the banks above the bed: where the hills dip lower than the stream, the ground
            // next to the stream is raised to the bank height, so the water stays in its bed and
            // does not run off into the forest. Only the stream that shapes this point counts,
            // so a side stream never blocks the main stream where they join.
            if (NearestStream(x, z, out var near, out float nd, out float nbed))
            {
                float edge = near.halfWidth + bankWidth + near.valleyHalfWidth;
                if (nd < edge)
                {
                    float bankUp = Smooth(near.halfWidth * 0.3f, near.halfWidth + bankWidth, nd);
                    float floor = nbed + 0.6f * bankUp + 0.3f * Smooth(near.halfWidth + bankWidth, edge, nd);
                    float keep = 1f - Smooth(edge - 2f, edge, nd); // fade out at the valley edge
                    h = Mathf.Max(h, Mathf.Lerp(h, floor, keep));
                }
            }
            return h;
        }

        static float Smooth(float a, float b, float v)
        {
            float t = Mathf.Clamp01((v - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        protected override Color ColorFormula(float x, float z, float h)
        {
            NearestStream(x, z, out var st, out float d, out float bed);
            float above = h - bed;
            var sand = new Color(0.62f, 0.54f, 0.38f);
            var mud = new Color(0.40f, 0.33f, 0.22f);
            var grass = new Color(0.36f, 0.52f, 0.24f);
            var forest = new Color(0.22f, 0.36f, 0.17f);
            float n = Mathf.PerlinNoise(x * 0.3f, z * 0.3f);
            if (st != null && d < st.halfWidth + 0.3f) return Color.Lerp(sand, mud, n * 0.5f);
            // mud only on the banks; low hollows far from a stream stay green
            bool nearStream = st != null && d < st.halfWidth + bankWidth + 1.5f;
            if (nearStream && above < 0.3f) return Color.Lerp(mud, grass, above / 0.3f);
            return Color.Lerp(grass, forest, Mathf.Clamp01((above - 0.8f) / 2.5f) * 0.6f + n * 0.4f);
        }

        /// <summary>Streams start with a little water in them.</summary>
        public override float InitialWaterLevel(float x, float z)
        {
            if (!NearestStream(x, z, out var st, out float d, out float bed)) return float.NegativeInfinity;
            return d < st.halfWidth + 0.5f ? bed + 0.3f : float.NegativeInfinity;
        }

        /// <summary>Height above the nearest stream bed (used to keep trees out of the water).</summary>
        public float HeightAboveStream(float x, float z, out float distToStream)
        {
            NearestStream(x, z, out var st, out distToStream, out float bed);
            return GroundAt(x, z) - bed;
        }
    }
}
