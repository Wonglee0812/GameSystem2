using UnityEngine;

namespace DamRush.WaterCompare
{
    /// <summary>
    /// Shared part of the three water versions. All of them use the same virtual-pipe shallow
    /// water model (Mei et al., 2007): every cell keeps the water flowing to its four neighbours;
    /// the flow speeds up with the height difference, is slowed by friction, and a cell never
    /// sends out more water than it holds. Only where it runs differs:
    /// A = plain C# on the CPU (1 m cells), B = Burst jobs on the CPU (0.5 m cells),
    /// C = a compute shader on the GPU (0.25 m cells).
    /// The simulation runs in fixed steps; the renderer blends between the last two steps
    /// (Alpha), so the surface moves smoothly even when the steps are coarse.
    /// </summary>
    public abstract class WaterSimBase : MonoBehaviour
    {
        public string label = "A";
        public string description = "";
        [Tooltip("Cell size in metres.")]
        public float cellSize = 1f;
        [Tooltip("Inflow at the top of the stream (m3/s).")]
        public float sourceRate = 3f;
        [Tooltip("Flow lost per second to friction (0 = none).")]
        public float friction = 0.35f;
        [Tooltip("Most simulation steps per rendered frame (keeps the frame rate when the game runs fast).")]
        public int maxStepsPerFrame = 12;
        public const float Gravity = 9.81f;

        public int CellsX { get; protected set; }
        public int CellsZ { get; protected set; }
        /// <summary>Length of one fixed step (s): short enough for the cell size to stay stable.</summary>
        public float StepDt { get; protected set; }
        /// <summary>Real milliseconds the simulation took in the last frame (CPU side).</summary>
        public float FrameMs { get; private set; }
        public int StepsLastFrame { get; private set; }
        /// <summary>0-1: how far the time is between the last two steps (for smooth rendering).</summary>
        public float Alpha { get; private set; }
        public bool Breached { get; private set; }
        public float FloodFactor { get; set; } = 1f;

        /// <summary>A block dropped by the player: a box footprint (lane metres) and the height of its top.</summary>
        public struct Block { public Rect area; public float top; }
        protected readonly System.Collections.Generic.List<Block> blocks = new System.Collections.Generic.List<Block>();

        float acc;
        readonly System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

        protected virtual void Awake()
        {
            CellsX = Mathf.RoundToInt(CompareShape.LengthX / cellSize);
            CellsZ = Mathf.RoundToInt(CompareShape.WidthZ / cellSize);
            // wave speed sqrt(g h) with h up to ~3 m: about 0.3 cell per step keeps the pipes stable
            StepDt = Mathf.Min(0.05f, 0.3f * cellSize / Mathf.Sqrt(Gravity * 3f));
            Init();
        }

        protected abstract void Init();
        protected abstract void Step(float dt);
        /// <summary>Called once per frame after the steps (copy results for rendering, etc.).</summary>
        protected virtual void AfterSteps() { }
        protected abstract void ApplyWall();
        public abstract void ResetWater();

        public void SetBreach(bool open) { Breached = open; ApplyWall(); }

        /// <summary>Cell centre in the lane (local metres).</summary>
        public Vector2 CellCentre(int i, int j) => new Vector2((i + 0.5f) * cellSize, (j + 0.5f) * cellSize);

        protected float GroundAt(int i, int j) { var c = CellCentre(i, j); return GroundWithBlocks(c.x, c.y); }

        /// <summary>Ground height at (x, z) including the blocks the player dropped.</summary>
        public float GroundWithBlocks(float x, float z)
        {
            float g = CompareShape.Ground(x, z);
            foreach (var b in blocks) if (b.area.Contains(new Vector2(x, z))) g = Mathf.Max(g, b.top);
            return g;
        }

        /// <summary>Puts a solid block into the water (the water on top of it flows off).</summary>
        public void AddBlock(Rect area, float top) { blocks.Add(new Block { area = area, top = top }); RaiseSolid(area, top); }
        public int BlockCount => blocks.Count;

        /// <summary>Lifts the solid ground inside the area to at least top (only the cells under a new block).</summary>
        protected abstract void RaiseSolid(Rect area, float top);
        /// <summary>Raises (or lowers) the water in every wet cell by dh metres.</summary>
        public abstract void AddLevel(float dh);
        /// <summary>Ground (with walls and blocks), water depth and flow velocity at a point, for floating objects.</summary>
        public abstract bool Sample(Vector2 p, out float ground, out float depth, out Vector2 vel);

        /// <summary>Calls act(i, j) for every cell whose centre is inside the area.</summary>
        protected void ForCellsIn(Rect area, System.Action<int, int> act)
        {
            int i0 = Mathf.Max(0, Mathf.FloorToInt(area.xMin / cellSize)), i1 = Mathf.Min(CellsX - 1, Mathf.CeilToInt(area.xMax / cellSize));
            int j0 = Mathf.Max(0, Mathf.FloorToInt(area.yMin / cellSize)), j1 = Mathf.Min(CellsZ - 1, Mathf.CeilToInt(area.yMax / cellSize));
            for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                    if (area.Contains(CellCentre(i, j))) act(i, j);
        }

        /// <summary>Cell that holds point p, clamped to the grid.</summary>
        protected void CellOf(Vector2 p, out int i, out int j)
        {
            i = Mathf.Clamp(Mathf.FloorToInt(p.x / cellSize), 0, CellsX - 1);
            j = Mathf.Clamp(Mathf.FloorToInt(p.y / cellSize), 0, CellsZ - 1);
        }
        public void ClearBlocks() { blocks.Clear(); ApplyWall(); }

        /// <summary>Pushes the water inside a circle towards vel (m/s), like a hand moving through it.</summary>
        public abstract void Push(Vector2 centre, float radius, Vector2 vel);
        /// <summary>Drops extra water into a circle (a bump that runs out as a ring wave).</summary>
        public abstract void Splash(Vector2 centre, float radius, float height);

        /// <summary>Weight of a brush at distance d from its centre: 1 in the middle, 0 at the edge.</summary>
        protected static float BrushWeight(float d, float radius) { float t = d / radius; return t >= 1f ? 0f : 1f - t * t; }
        protected float WallAt(int i, int j) { var c = CellCentre(i, j); return CompareShape.Wall(c.x, c.y, Breached); }

        void Update()
        {
            watch.Restart();
            acc += Time.deltaTime;
            int n = 0;
            while (acc >= StepDt && n < maxStepsPerFrame) { Step(StepDt); acc -= StepDt; n++; }
            if (acc > StepDt) acc = StepDt; // too slow: drop time instead of spiralling
            Alpha = Mathf.Clamp01(acc / StepDt);
            AfterSteps();
            watch.Stop();
            FrameMs = (float)watch.Elapsed.TotalMilliseconds;
            StepsLastFrame = n;
        }
    }
}
