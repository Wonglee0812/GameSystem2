using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DamRush
{
    public enum WaterColorMode { Natural, Depth, Speed }

    /// <summary>
    /// Shallow water on a height grid with "virtual pipes" between neighbour cells
    /// (Mei, Decaudin, &amp; Hu, 2007). Each cell stores a water depth and an outflow to its
    /// four neighbours; water flows from a high surface to a low surface.
    /// Additions for the game:
    /// - bed friction with the Manning formula (de Almeida et al., 2012), or the old fixed damping,
    /// - parts written into the grid as extra ground, and the water they push away is moved
    ///   to the neighbour cells, so no water is lost or created (idea from Kellomäki, 2013),
    /// - an open edge at the far x side where water leaves the map.
    /// Version B (2026-11-14, chosen in the water compare scene): the water grid is finer than the
    /// map (subdivide = 2 gives 0.5 m cells on the 1 m map), the pipe flows and the new depths are
    /// computed with Burst jobs on all CPU cores, the surface mesh is built by a Burst job too, and
    /// the surface blends between the last two steps, so it moves smoothly between steps.
    /// On a web build Burst is not used and jobs run on one thread, so it is slower there.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class WaterGrid : MonoBehaviour
    {
        public SandboxConfig config;
        public GroundHeights terrain;

        [Header("Resolution")]
        [Tooltip("Water cells per map cell along each side. 2 = 0.5 m water cells on the 1 m map (4x the cells).")]
        [Range(1, 3)] public int subdivide = 2;

        [Header("Debug")]
        public bool paused;
        public WaterColorMode colorMode = WaterColorMode.Natural;
        public Color plainColor = new Color(0.2f, 0.45f, 0.75f, 0.72f); // not used by the natural look any more

        [Header("Natural look")]
        public Color shallowColor = new Color(0.25f, 0.5f, 0.55f, 0.7f);
        public Color deepColor = new Color(0.04f, 0.16f, 0.32f, 0.93f);
        [Tooltip("Depth (m) where the water reaches the deep colour.")]
        public float deepAt = 1.3f;
        public Color foamColor = new Color(0.9f, 0.95f, 0.97f, 0.9f);
        [Tooltip("Water faster than this (m/s) starts to get white foam.")]
        public float foamFrom = 4f, foamFull = 8f;
        public Color floodColor = new Color(0.45f, 0.35f, 0.22f, 0.9f);
        [Tooltip("Very shallow water near the bank shows the sandy bed.")]
        public Color shoreColor = new Color(0.86f, 0.84f, 0.68f, 0.45f);
        [Tooltip("Water shallower than this (m) blends to the shore colour.")]
        public float shoreDepth = 0.18f;

        [Header("Stylised look")]
        [Tooltip("Material with the DamRush/StylizedWater shader, used for the natural colour mode. Empty = old look.")]
        public Material stylizedMaterial;
        [Tooltip("Also show the old white flow streaks.")]
        public bool flowStreaks;
        Material plainMaterial;
        [Range(0f, 1f), Tooltip("Set by the flood: muddy brown flood water.")]
        public float floodTint;

        int nx, nz;
        float cell, area;
        // simulation arrays (Burst jobs work on these; the main thread reads them between steps)
        NativeArray<float> ground, obstacle, depth, prevDepth, fL, fR, fB, fT, velX, velZ;
        float[] newObstacle;
        // fixed structures (dams): height above the ground, how leaky they are, and the water inside them
        float[] staticObstacle, porosity, pore;
        readonly HashSet<int> staticCells = new HashSet<int>();
        float obstacleTimer;
        Mesh mesh;
        NativeArray<Vector3> verts, normals;
        NativeArray<Color> colors;
        NativeArray<Vector2> flowUv, foamUv;
        float lastStepTime, lastStepLength = 0.02f;

        public int CellsX => nx;
        public int CellsZ => nz;
        public float CellSize => cell;
        /// <summary>Water that left the map over the open edge in the last physics step (m3/s).</summary>
        public float OutflowRate { get; private set; }
        public float InflowRate { get; private set; }
        public float StepMilliseconds { get; private set; }

        void Start()
        {
            Init();
            var r = GetComponent<MeshRenderer>();
            plainMaterial = r.sharedMaterial;
            // the old white streaks are only used without the stylised water shader
            if ((stylizedMaterial == null || flowStreaks) && GetComponent<FlowStreaks>() == null && Application.isPlaying)
                gameObject.AddComponent<FlowStreaks>().water = this;
        }

        void OnDestroy() => Dispose();

        void Dispose()
        {
            if (!depth.IsCreated) return;
            ground.Dispose(); obstacle.Dispose(); depth.Dispose(); prevDepth.Dispose();
            fL.Dispose(); fR.Dispose(); fB.Dispose(); fT.Dispose(); velX.Dispose(); velZ.Dispose();
            if (verts.IsCreated) { verts.Dispose(); normals.Dispose(); colors.Dispose(); flowUv.Dispose(); foamUv.Dispose(); }
        }

        /// <summary>Cell has water deeper than d (used by the flow streaks).</summary>
        public bool CellDeeper(int id, float d) => depth.IsCreated && id >= 0 && id < depth.Length && depth[id] > d;

        public void Init()
        {
            Dispose();
            int s = Mathf.Max(1, subdivide);
            nx = config.cellsX * s; nz = config.cellsZ * s;
            cell = config.cellSize / s; area = cell * cell;
            int n = nx * nz;
            var a = Allocator.Persistent;
            ground = new NativeArray<float>(n, a); obstacle = new NativeArray<float>(n, a);
            depth = new NativeArray<float>(n, a); prevDepth = new NativeArray<float>(n, a);
            fL = new NativeArray<float>(n, a); fR = new NativeArray<float>(n, a); fB = new NativeArray<float>(n, a); fT = new NativeArray<float>(n, a);
            velX = new NativeArray<float>(n, a); velZ = new NativeArray<float>(n, a);
            newObstacle = new float[n];
            staticObstacle = new float[n]; porosity = new float[n]; pore = new float[n];
            staticCells.Clear(); touched.Clear();
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                    ground[i * nz + j] = GroundOfCell(i, j);
            ResetWater();
            BuildMesh();
        }

        float GroundOfCell(int i, int j) => terrain.GroundAt((i + 0.5f) * cell, (j + 0.5f) * cell);

        /// <summary>Back to the start state of the map (and clears all flow).</summary>
        public void ResetWater()
        {
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    int id = i * nz + j;
                    float level = terrain.InitialWaterLevel((i + 0.5f) * cell, (j + 0.5f) * cell);
                    depth[id] = float.IsNegativeInfinity(level) ? 0f : Mathf.Max(0f, level - ground[id] - obstacle[id]);
                    prevDepth[id] = depth[id];
                    fL[id] = fR[id] = fB[id] = fT[id] = velX[id] = velZ[id] = 0f;
                }
        }

        int tickCounter;

        [Tooltip("Fast game speed (3x, 5x): how many times more water steps per real second than at 1x. " +
                 "1 = the same number of steps as at 1x, each step longer (cheap, needed for 5x on the big map). " +
                 "Higher = more exact water but slower.")]
        [Range(1f, 5f)] public float fastSpeedStepBudget = 1f;
        [Tooltip("Longest allowed water step at fast game speed (game seconds). " +
                 "Longer steps are cheaper but can make the water jumpy.")]
        public float maxStepSeconds = 0.2f;

        void FixedUpdate()
        {
            if (!depth.IsCreated || paused) return;
            // same step rate as the 1 m grid (25 per second): the pipe model stays stable on 0.5 m
            // cells at 0.04 s steps, and the surface blends between steps, so it still looks smooth
            int every = Mathf.Max(1, config.tickEvery);
            // fast speed: step less often with a longer time step, so the water keeps up with the game
            if (Time.timeScale > 1.01f)
            {
                int fast = Mathf.CeilToInt(every * Time.timeScale / Mathf.Max(1f, fastSpeedStepBudget));
                int limit = Mathf.Max(every, Mathf.FloorToInt(maxStepSeconds / Time.fixedDeltaTime));
                every = Mathf.Clamp(fast, every, limit);
            }
            if (++tickCounter < every) return;
            tickCounter = 0;
            StepOnce(Time.fixedDeltaTime * every);
        }

        /// <summary>One physics step of the water (also used by the lab's single-step button).</summary>
        public void StepOnce(float fixedDt)
        {
            if (!depth.IsCreated) return;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            NativeArray<float>.Copy(depth, prevDepth); // for the smooth blend in the surface mesh
            obstacleTimer -= fixedDt;
            if (obstacleTimer <= 0f) { obstacleTimer = config.obstacleRefresh; RasteriseObstacles(); }
            int steps = Mathf.Max(1, config.substeps);
            float dt = fixedDt / steps, outSum = 0f, inSum = 0f;
            for (int s = 0; s < steps; s++) Step(dt, ref inSum, ref outSum);
            InflowRate = inSum / fixedDt;
            OutflowRate = outSum / fixedDt;
            StepMilliseconds = (float)watch.Elapsed.TotalMilliseconds;
            lastStepTime = Time.time;
            lastStepLength = fixedDt;
        }

        static readonly int DrTime = Shader.PropertyToID("_DR_Time"), DrFlood = Shader.PropertyToID("_DR_Flood");

        void LateUpdate()
        {
            // the water look runs on real time, so it does not speed up at 3x or 5x
            Shader.SetGlobalFloat(DrTime, Time.unscaledTime);
            Shader.SetGlobalFloat(DrFlood, floodTint); // flood water is muddy in the clear-water look
            UpdateMesh();
            var c = Camera.main;
            WaterRipples.Upload(c != null ? c.transform.position : Vector3.zero);
        }

        // ---------------------------------------------------------------- simulation

        float Surface(int id) => ground[id] + obstacle[id] + depth[id];

        void Step(float dt, ref float inSum, ref float outSum)
        {
            // 1. sources (few cells, main thread)
            foreach (var src in WaterSource.All)
            {
                if (src == null || src.rate <= 0f) continue;
                Vector3 p = src.transform.position;
                int r = Mathf.CeilToInt(src.radius / cell);
                int ci = Mathf.FloorToInt(p.x / cell), cj = Mathf.FloorToInt(p.z / cell);
                int count = 0;
                for (int pass = 0; pass < 2; pass++)
                {
                    float add = pass == 1 && count > 0 ? src.rate * dt / (count * area) : 0f;
                    for (int i = ci - r; i <= ci + r; i++)
                        for (int j = cj - r; j <= cj + r; j++)
                        {
                            if (i < 0 || j < 0 || i >= nx || j >= nz) continue;
                            float dx = (i + 0.5f) * cell - p.x, dz = (j + 0.5f) * cell - p.z;
                            if (dx * dx + dz * dz > src.radius * src.radius) continue;
                            if (pass == 0) count++;
                            else depth[i * nz + j] += add;
                        }
                }
                if (count > 0) inSum += src.rate * dt;
            }

            // 2. pipe flows of every cell, then 3. new depths (Burst, all cores)
            var flux = new FluxJob
            {
                nx = nx, nz = nz, dt = dt, cell = cell, area = area,
                k = dt * config.pipeArea * config.gravity / cell,
                depthScaled = config.depthScaledPipes, pipeArea = config.pipeArea,
                damping = config.friction == FrictionMode.FluxDamping, fluxDamping = config.fluxDamping,
                gravity = config.gravity, n2 = config.manningN * config.manningN, openEnd = config.openEndX,
                ground = ground, obstacle = obstacle, depth = depth, fL = fL, fR = fR, fB = fB, fT = fT,
            };
            var h = flux.Schedule(nx * nz, 256);
            var dep = new DepthJob { nx = nx, nz = nz, dt = dt, cell = cell, area = area, depth = depth, fL = fL, fR = fR, fB = fB, fT = fT, velX = velX, velZ = velZ };
            dep.Schedule(nx * nz, 256, h).Complete();
            int last = (nx - 1) * nz;
            for (int j = 0; j < nz; j++) outSum += fR[last + j] * dt;

            // 4. seepage through leaky structures (few cells, main thread)
            if (staticCells.Count > 0) Seep(dt);
        }

        [BurstCompile]
        struct FluxJob : IJobParallelFor
        {
            public int nx, nz;
            public float dt, cell, area, k, pipeArea, fluxDamping, gravity, n2;
            public bool depthScaled, damping, openEnd;
            [ReadOnly] public NativeArray<float> ground, obstacle, depth;
            public NativeArray<float> fL, fR, fB, fT;

            float Surface(int id) => ground[id] + obstacle[id] + depth[id];

            /// <summary>
            /// Depth of the water that can flow between two cells: the higher surface minus the higher
            /// bottom (de Almeida et al., 2012). dh = surface A - surface B. Kept above a few cm so a dry
            /// cell can still start to fill.
            /// </summary>
            static float FlowDepth(float dh, float depthA, float depthB)
            {
                float hf = dh >= 0f ? math.min(depthA, depthB + dh) : math.min(depthB, depthA - dh);
                return math.clamp(hf, 0.03f, 4f);
            }

            /// <summary>
            /// New pipe flow towards one neighbour: gravity speeds it up by the surface difference,
            /// friction slows it down. Manning is applied semi-implicitly (divide instead of subtract),
            /// which stays stable in shallow water.
            /// </summary>
            float PipeFlow(float flow, float dh, float depthA, float depthB)
            {
                float kk = k;
                if (depthScaled) kk *= FlowDepth(dh, depthA, depthB) * cell / pipeArea;
                if (damping) return math.max(0f, flow * fluxDamping + kk * dh);
                float f = math.max(0f, flow + kk * dh);
                float h = math.max(0.02f, 0.5f * (depthA + depthB));
                float v = f / (cell * h);
                return f / (1f + dt * gravity * n2 * v / (h * math.pow(h, 1f / 3f))); // h^(4/3)
            }

            public void Execute(int id)
            {
                int i = id / nz, j = id - i * nz;
                float h = Surface(id), d = depth[id];
                float l = i > 0 ? PipeFlow(fL[id], h - Surface(id - nz), d, depth[id - nz]) : 0f;
                float r;
                if (i < nx - 1) r = PipeFlow(fR[id], h - Surface(id + nz), d, depth[id + nz]);
                else r = openEnd ? PipeFlow(fR[id], h - (ground[id] + obstacle[id] - 0.3f), d, d) : 0f;
                float b = j > 0 ? PipeFlow(fB[id], h - Surface(id - 1), d, depth[id - 1]) : 0f;
                float t = j < nz - 1 ? PipeFlow(fT[id], h - Surface(id + 1), d, depth[id + 1]) : 0f;
                // never send out more water than the cell holds
                float sum = l + r + b + t;
                if (sum > 0f)
                {
                    float scale = math.min(1f, d * area / (sum * dt));
                    l *= scale; r *= scale; b *= scale; t *= scale;
                }
                fL[id] = l; fR[id] = r; fB[id] = b; fT[id] = t;
            }
        }

        [BurstCompile]
        struct DepthJob : IJobParallelFor
        {
            public int nx, nz;
            public float dt, cell, area;
            public NativeArray<float> depth, velX, velZ;
            [ReadOnly] public NativeArray<float> fL, fR, fB, fT;

            public void Execute(int id)
            {
                int i = id / nz, j = id - i * nz;
                float inL = i > 0 ? fR[id - nz] : 0f, inR = i < nx - 1 ? fL[id + nz] : 0f;
                float inB = j > 0 ? fT[id - 1] : 0f, inT = j < nz - 1 ? fB[id + 1] : 0f;
                float outflow = fL[id] + fR[id] + fB[id] + fT[id];
                float d = math.max(0f, depth[id] + dt * (inL + inR + inB + inT - outflow) / area);
                depth[id] = d;
                // depth-averaged velocity (m/s), the same formula VelocityAt used before
                if (d < 0.02f) { velX[id] = 0f; velZ[id] = 0f; return; }
                float2 v = new float2(inL - fL[id] + fR[id] - inR, inB - fB[id] + fT[id] - inT) * 0.5f / (cell * math.max(d, 0.1f));
                float len = math.length(v);
                if (len > 8f) v *= 8f / len;
                velX[id] = v.x; velZ[id] = v.y;
            }
        }

        /// <summary>
        /// Water soaks slowly through a leaky dam (Darcy-like: flow grows with the height difference).
        /// The water inside the dam is kept in "pore" and is not drawn. A sealed dam has porosity 0.
        /// </summary>
        void Seep(float dt)
        {
            float k = config.seepRate * dt * cell / area; // metres of depth per metre of head per step
            foreach (int id in staticCells)
            {
                float p = porosity[id];
                if (p <= 0f) continue;
                int i = id / nz, j = id % nz;
                float inside = ground[id] + pore[id];
                float room = Mathf.Max(0f, obstacle[id] - pore[id]);
                for (int d = 0; d < 4; d++)
                {
                    int ni = i + (d == 0 ? -1 : d == 1 ? 1 : 0), nj = j + (d == 2 ? -1 : d == 3 ? 1 : 0);
                    if (ni < 0 || nj < 0 || ni >= nx || nj >= nz) continue;
                    int nb = ni * nz + nj;
                    if (staticCells.Contains(nb) && porosity[nb] > 0f)
                    {
                        // dam cell to dam cell: pore water spreads along and through the dam
                        float diff = (ground[id] + pore[id]) - (ground[nb] + pore[nb]);
                        if (diff <= 0f) continue;
                        float move = Mathf.Min(pore[id], diff * k * Mathf.Min(p, porosity[nb]) * 0.5f);
                        pore[id] -= move; pore[nb] = Mathf.Min(obstacle[nb], pore[nb] + move);
                        continue;
                    }
                    float outside = Surface(nb);
                    if (outside > inside && depth[nb] > 0.001f)
                    {
                        float move = Mathf.Min(depth[nb], Mathf.Min(room, (outside - inside) * k * p));
                        depth[nb] -= move; pore[id] += move; room -= move; inside += move;
                    }
                    else if (inside > outside && pore[id] > 0f)
                    {
                        float move = Mathf.Min(pore[id], (inside - outside) * k * p);
                        pore[id] -= move; depth[nb] += move; inside -= move;
                    }
                }
            }
        }

        // ---------------------------------------------------------------- fixed structures

        public int CellIndex(float x, float z) => CellAt(x, z, out int id) ? id : -1;
        public Vector3 CellCenter(int id) => new Vector3((id / nz + 0.5f) * cell, ground[id], (id % nz + 0.5f) * cell);
        public float CellGround(int id) => ground[id];

        /// <summary>
        /// Puts a fixed structure (a dam) on one cell: its top is at ground + height.
        /// Porosity 0 = watertight, 1 = very leaky. Height 0 removes it.
        /// </summary>
        public void SetStatic(int id, float height, float leakiness)
        {
            if (id < 0 || staticObstacle == null) return;
            staticObstacle[id] = Mathf.Max(0f, height);
            porosity[id] = Mathf.Clamp01(leakiness);
            if (height > 0f) staticCells.Add(id);
            else
            {
                staticCells.Remove(id);
                depth[id] += pore[id]; // the dam is gone: the water inside it comes out
                pore[id] = 0f;
            }
            obstacleTimer = 0f; // apply at the next step
        }

        /// <summary>How fast water is seeping through the cell (m3/s), for leak alarms later.</summary>
        public float PoreWater(int id) => id >= 0 ? pore[id] * area : 0f;

        // ---------------------------------------------------------------- earthworks and pumps (Phase B)

        /// <summary>Water surface of one cell (ground + obstacle + water).</summary>
        public float CellSurface(int id) => id >= 0 && depth.IsCreated ? Surface(id) : 0f;
        public float CellDepth(int id) => id >= 0 && depth.IsCreated ? depth[id] : 0f;

        /// <summary>Takes up to this much water (m3) out of a cell and returns what it took.</summary>
        public float TakeWater(int id, float volume)
        {
            if (id < 0 || !depth.IsCreated || volume <= 0f) return 0f;
            float h = Mathf.Min(depth[id], volume / area);
            depth[id] -= h;
            return h * area;
        }

        /// <summary>Puts water (m3) into a cell. Only the pump uses it: the water was taken somewhere else first.</summary>
        public void AddWater(int id, float volume)
        {
            if (id >= 0 && depth.IsCreated && volume > 0f) depth[id] += volume / area;
        }

        /// <summary>Reads the ground of these cells again after the terrain changed (digging).</summary>
        public void RefreshGround(IEnumerable<int> ids)
        {
            foreach (int id in ids) ground[id] = GroundOfCell(id / nz, id % nz);
            obstacleTimer = 0f;
        }

        // ---------------------------------------------------------------- parts in the grid

        readonly Dictionary<int, List<Vector2>> columns = new Dictionary<int, List<Vector2>>();
        readonly List<int> touched = new List<int>(), newTouched = new List<int>();
        readonly HashSet<int> newSet = new HashSet<int>();
        readonly Stack<List<Vector2>> listPool = new Stack<List<Vector2>>();

        /// <summary>
        /// Writes every part into the grid as extra ground. A part only counts when it stands
        /// on the ground or on another counted part, so floating parts let the water pass under.
        /// </summary>
        void RasteriseObstacles()
        {
            foreach (var list in columns.Values) { list.Clear(); listPool.Push(list); }
            columns.Clear();
            newTouched.Clear();
            newSet.Clear();

            foreach (var body in WaterBody.All)
            {
                if (body == null || !body.blocksWater || body.Col == null) continue;
                Bounds b = body.Col.bounds;
                int i0 = Mathf.Max(0, Mathf.FloorToInt(b.min.x / cell)), i1 = Mathf.Min(nx - 1, Mathf.FloorToInt(b.max.x / cell));
                int j0 = Mathf.Max(0, Mathf.FloorToInt(b.min.z / cell)), j1 = Mathf.Min(nz - 1, Mathf.FloorToInt(b.max.z / cell));
                float topY = b.max.y + 0.1f, botY = b.min.y - 0.1f, len = b.size.y + 0.2f;
                for (int i = i0; i <= i1; i++)
                    for (int j = j0; j <= j1; j++)
                    {
                        float x = (i + 0.5f) * cell, z = (j + 0.5f) * cell;
                        if (!body.Col.Raycast(new Ray(new Vector3(x, topY, z), Vector3.down), out RaycastHit top, len)) continue;
                        if (!body.Col.Raycast(new Ray(new Vector3(x, botY, z), Vector3.up), out RaycastHit bot, len)) continue;
                        int id = i * nz + j;
                        if (!columns.TryGetValue(id, out var list))
                        {
                            list = listPool.Count > 0 ? listPool.Pop() : new List<Vector2>();
                            columns[id] = list;
                            if (newSet.Add(id)) newTouched.Add(id);
                        }
                        list.Add(new Vector2(bot.point.y, top.point.y));
                    }
            }

            foreach (int id in touched) newObstacle[id] = 0f;
            foreach (int id in staticCells)
            {
                newObstacle[id] = staticObstacle[id];
                if (!columns.ContainsKey(id) && !newSet.Contains(id)) { newTouched.Add(id); newSet.Add(id); }
            }
            foreach (var pair in columns)
            {
                var list = pair.Value;
                list.Sort((a, c) => a.x.CompareTo(c.x));
                float column = ground[pair.Key];
                foreach (var span in list)
                    if (span.x <= column + 0.3f) column = Mathf.Max(column, span.y);
                newObstacle[pair.Key] = Mathf.Max(staticObstacle[pair.Key], column - ground[pair.Key]);
            }

            // apply: cells that grew push their water out, cells that shrank just get lower
            foreach (int id in touched) Apply(id);
            foreach (int id in newTouched) Apply(id);
            touched.Clear();
            touched.AddRange(newTouched);
        }

        void Apply(int id)
        {
            float before = obstacle[id], after = newObstacle[id];
            if (Mathf.Approximately(before, after)) return;
            if (after > before && config.displaceWater && depth[id] > 0f)
            {
                // water between the old and the new top is pushed to the neighbours that did not grow
                float moved = Mathf.Min(depth[id], after - before);
                int i = id / nz, j = id % nz;
                int count = 0;
                for (int k = 0; k < 4; k++) if (FreeNeighbour(i, j, k, out _)) count++;
                if (count > 0)
                {
                    depth[id] -= moved;
                    for (int k = 0; k < 4; k++)
                        if (FreeNeighbour(i, j, k, out int nb)) depth[nb] += moved / count;
                }
            }
            obstacle[id] = after;
        }

        bool FreeNeighbour(int i, int j, int dir, out int id)
        {
            int ni = i + (dir == 0 ? -1 : dir == 1 ? 1 : 0), nj = j + (dir == 2 ? -1 : dir == 3 ? 1 : 0);
            id = ni * nz + nj;
            if (ni < 0 || nj < 0 || ni >= nx || nj >= nz) return false;
            return newObstacle[id] <= obstacle[id] + 0.001f && newObstacle[id] < 0.01f;
        }

        // ---------------------------------------------------------------- queries

        bool CellAt(float x, float z, out int id)
        {
            int i = Mathf.FloorToInt(x / cell), j = Mathf.FloorToInt(z / cell);
            id = i * nz + j;
            return depth.IsCreated && i >= 0 && j >= 0 && i < nx && j < nz;
        }

        /// <summary>Water surface height at a point; wet is false when there is (almost) no water.</summary>
        public float SurfaceAt(float x, float z, out bool wet)
        {
            if (!CellAt(x, z, out int id)) { wet = false; return 0f; }
            wet = depth[id] > 0.02f;
            return Surface(id);
        }

        public float DepthAt(float x, float z) => CellAt(x, z, out int id) ? depth[id] : 0f;
        public float ObstacleAt(float x, float z) => CellAt(x, z, out int id) ? obstacle[id] : 0f;

        /// <summary>Depth-averaged flow velocity (m/s) on the x/z plane at a point (computed in the depth job).</summary>
        public Vector2 VelocityAt(float x, float z)
        {
            if (!CellAt(x, z, out int id) || depth[id] < 0.02f) return Vector2.zero;
            return new Vector2(velX[id], velZ[id]);
        }

        /// <summary>Mean water depth over a world rectangle (x/z).</summary>
        public float AverageDepth(Rect r)
        {
            if (!depth.IsCreated) return 0f;
            float sum = 0f; int count = 0;
            for (float x = r.xMin + cell * 0.5f; x < r.xMax; x += cell)
                for (float z = r.yMin + cell * 0.5f; z < r.yMax; z += cell)
                    if (CellAt(x, z, out int id)) { sum += depth[id]; count++; }
            return count > 0 ? sum / count : 0f;
        }

        public float TotalVolume()
        {
            if (!depth.IsCreated) return 0f;
            float v = 0f;
            for (int id = 0; id < depth.Length; id++) v += depth[id] + pore[id];
            return v * area;
        }

        public float MaxSpeed()
        {
            if (!depth.IsCreated) return 0f;
            float best = 0f;
            for (int id = 0; id < depth.Length; id++)
                if (depth[id] >= 0.02f) best = Mathf.Max(best, new Vector2(velX[id], velZ[id]).magnitude);
            return best;
        }

        // ---------------------------------------------------------------- rendering

        void BuildMesh()
        {
            int n = nx * nz;
            var a = Allocator.Persistent;
            verts = new NativeArray<Vector3>(n, a); normals = new NativeArray<Vector3>(n, a);
            colors = new NativeArray<Color>(n, a); flowUv = new NativeArray<Vector2>(n, a); foamUv = new NativeArray<Vector2>(n, a);
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                    verts[i * nz + j] = new Vector3((i + 0.5f) * cell, 0f, (j + 0.5f) * cell);
            var tris = new int[(nx - 1) * (nz - 1) * 6];
            int k = 0;
            for (int i = 0; i < nx - 1; i++)
                for (int j = 0; j < nz - 1; j++)
                {
                    int p = i * nz + j, b = p + 1, c = p + nz, d = c + 1;
                    tris[k++] = p; tris[k++] = b; tris[k++] = c;
                    tris[k++] = b; tris[k++] = d; tris[k++] = c;
                }
            mesh = new Mesh { name = "Water" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.MarkDynamic();
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.triangles = tris;
            mesh.bounds = new Bounds(new Vector3(nx * cell * 0.5f, 5f, nz * cell * 0.5f), new Vector3(nx * cell, 40f, nz * cell));
            GetComponent<MeshFilter>().sharedMesh = mesh;
            UpdateMesh();
        }

        static float4 F4(Color c) => new float4(c.r, c.g, c.b, c.a);

        void UpdateMesh()
        {
            if (mesh == null || !verts.IsCreated) return;
            bool stylized = colorMode == WaterColorMode.Natural && stylizedMaterial != null;
            var rend = GetComponent<MeshRenderer>();
            var want = stylized ? stylizedMaterial : plainMaterial;
            if (want != null && rend.sharedMaterial != want) rend.sharedMaterial = want;

            // blend between the depth before the last step and after it, so the surface moves smoothly
            float alpha = paused ? 1f : Mathf.Clamp01((Time.time - lastStepTime) / Mathf.Max(0.001f, lastStepLength));
            var surf = new SurfaceJob
            {
                nx = nx, nz = nz, alpha = alpha, mode = (int)colorMode, stylized = stylized,
                linearSpace = QualitySettings.activeColorSpace == ColorSpace.Linear,
                shallow = F4(shallowColor), deep = F4(deepColor), shore = F4(shoreColor), flood = F4(floodColor), foamCol = F4(foamColor),
                deepAt = deepAt, shoreDepth = shoreDepth, floodTint = floodTint, foamFrom = foamFrom, foamFull = foamFull,
                ground = ground, obstacle = obstacle, depth = depth, prevDepth = prevDepth, velX = velX, velZ = velZ,
                verts = verts, colors = colors, flowUv = flowUv, foamUv = foamUv,
            };
            var h = surf.Schedule(nx * nz, 512);
            new NormalJob { nx = nx, nz = nz, two = 2f * cell, verts = verts, normals = normals }.Schedule(nx * nz, 512, h).Complete();
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(1, flowUv);
            mesh.SetUVs(2, foamUv);
        }

        [BurstCompile]
        struct SurfaceJob : IJobParallelFor
        {
            public int nx, nz, mode;
            public bool stylized, linearSpace;
            public float alpha, deepAt, shoreDepth, floodTint, foamFrom, foamFull;
            public float4 shallow, deep, shore, flood, foamCol;
            [ReadOnly] public NativeArray<float> ground, obstacle, depth, prevDepth, velX, velZ;
            public NativeArray<Vector3> verts;
            public NativeArray<Color> colors;
            public NativeArray<Vector2> flowUv, foamUv;

            static float SmoothStep01(float t) { t = math.saturate(t); return t * t * (3f - 2f * t); }

            public void Execute(int id)
            {
                float d = math.lerp(prevDepth[id], depth[id], alpha);
                bool wet = d > 0.01f;
                float bed = ground[id] + obstacle[id];
                var v = verts[id];
                // smooth shoreline: a dry point next to water takes the water level of its wet
                // neighbours, so the flat water surface runs a little into the bank and the ground
                // cuts it along its real slope (a smooth line), not along the steps of the grid.
                // Its depth goes below zero there, and the shader fades the water out at depth 0.
                bool atShore = false; float shoreLevel = 0f;
                if (!wet)
                {
                    int i = id / nz, j = id - i * nz; float sum = 0f; int n = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        int ni = i + (k == 0 ? -1 : k == 1 ? 1 : 0), nj = j + (k == 2 ? -1 : k == 3 ? 1 : 0);
                        if (ni < 0 || nj < 0 || ni >= nx || nj >= nz) continue;
                        int nb = ni * nz + nj;
                        float dn = math.lerp(prevDepth[nb], depth[nb], alpha);
                        if (dn > 0.01f) { sum += ground[nb] + obstacle[nb] + dn; n++; }
                    }
                    if (n > 0) { atShore = true; shoreLevel = math.min(sum / n, bed + 0.05f); }
                }
                // dry cells away from the water sink under the ground and turn fully transparent
                v.y = wet ? bed + d : atShore ? shoreLevel : bed - 0.3f;
                verts[id] = v;
                float2 vel = depth[id] >= 0.02f ? new float2(velX[id], velZ[id]) : float2.zero;
                float speed = math.length(vel);
                float4 c;
                // debug colours: depth (light to dark blue) and speed (blue to red)
                if (mode == 1) c = math.lerp(new float4(0.55f, 0.85f, 1f, 0.6f), new float4(0.05f, 0.15f, 0.55f, 0.85f), math.saturate(d / 2f));
                else if (mode == 2) c = math.lerp(new float4(0.2f, 0.45f, 0.9f, 0.75f), new float4(1f, 0.25f, 0.15f, 0.85f), math.saturate(speed / 3f));
                else
                {
                    // deeper = darker, the sandy bed shows at the bank, flood water turns brown
                    c = math.lerp(shallow, deep, SmoothStep01(d / deepAt));
                    c = math.lerp(c, shore, 1f - SmoothStep01(d / shoreDepth));
                    if (floodTint > 0f) c = math.lerp(c, flood, floodTint * 0.75f);
                    if (!stylized)
                    {
                        // old look: fast water gets white foam in the vertex colour
                        float f = SmoothStep01((speed - foamFrom) / math.max(0.001f, foamFull - foamFrom)) * 0.45f;
                        c = math.lerp(c, foamCol, f);
                    }
                }
                if (!wet && !atShore) c.w = 0f;
                // vertex colours are read as linear values, so convert from the sRGB colours picked here
                if (linearSpace) c.xyz = math.pow(math.max(c.xyz, 0f), 2.2f);
                colors[id] = new Color(c.x, c.y, c.z, c.w);
                flowUv[id] = new Vector2(vel.x, vel.y);
                // shore points: depth = water level minus ground (below zero), so the fade lands on the real water line
                foamUv[id] = new Vector2(wet ? Foam(id, speed) : atShore ? 0.1f : 0f, atShore ? shoreLevel - bed : d);
            }

            /// <summary>
            /// How rough the water is in a cell (0-1), for the white foam of the stylised shader:
            /// fast water, water running over a dam or base, water pushing against a dam, and a thin
            /// line along the bank.
            /// </summary>
            float Foam(int id, float speed)
            {
                float foam = math.saturate((speed - foamFrom * 0.6f) / math.max(0.001f, foamFull * 0.75f - foamFrom * 0.6f));
                if (obstacle[id] > 0.05f) foam = math.max(foam, 0.55f + math.min(0.45f, speed * 0.3f)); // over the top of a dam
                int i = id / nz, j = id - i * nz;
                float surf = ground[id] + obstacle[id] + depth[id];
                for (int k = 0; k < 4; k++)
                {
                    int ni = i + (k == 0 ? -1 : k == 1 ? 1 : 0), nj = j + (k == 2 ? -1 : k == 3 ? 1 : 0);
                    if (ni < 0 || nj < 0 || ni >= nx || nj >= nz) continue;
                    int nb = ni * nz + nj;
                    if (depth[nb] > 0.01f) continue;
                    bool wall = ground[nb] + obstacle[nb] > surf && obstacle[nb] > 0.05f; // a dam or base next to the water
                    foam = math.max(foam, wall ? 0.35f + math.min(0.5f, speed * 0.4f) : 0.1f + math.min(0.15f, speed * 0.05f));
                }
                return math.saturate(foam);
            }
        }

        [BurstCompile]
        struct NormalJob : IJobParallelFor
        {
            public int nx, nz;
            public float two;
            [ReadOnly] public NativeArray<Vector3> verts;
            public NativeArray<Vector3> normals;

            public void Execute(int id)
            {
                int i = id / nz, j = id - i * nz;
                float l = verts[i > 0 ? id - nz : id].y, r = verts[i < nx - 1 ? id + nz : id].y;
                float b = verts[j > 0 ? id - 1 : id].y, t = verts[j < nz - 1 ? id + 1 : id].y;
                float3 n = math.normalize(new float3(l - r, two, b - t));
                normals[id] = new Vector3(n.x, n.y, n.z);
            }
        }
    }
}
