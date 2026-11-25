using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>A wall of wave bricks comes from one side and is pushed across the island. Each wave brick
    /// starts with 100% power and loses part of it on every hit (see WaveBlock).</summary>
    public class Tsunami : Disaster
    {
        [Header("References")]
        public MapGenerator map;
        public PhysicsActivator activator;
        [Tooltip("Mesh of one wave brick (2x4 brick).")]
        public Mesh waveMesh;
        [Tooltip("See-through blue material for the wave bricks (M_BP_Water).")]
        public Material waterMaterial;

        [Header("Wave shape")]
        [Tooltip("Size of one wave brick compared with a normal 2x4 brick. 2 = a 4 m x 2 m brick, 1.2 m high. Default 2.")]
        public float brickScale = 2f;
        [Tooltip("Wave height in (big) wave brick layers above the ground at the coast. 5 x 1.2 m = 6 m. Default 5.")]
        public int layers = 5;
        [Tooltip("Rows of wave bricks behind each other (how much water). Default 3.")]
        public int rows = 3;
        [Tooltip("Side the wave comes from: 0 = west, 1 = east, 2 = south, 3 = north, -1 = random. Default -1.")]
        [Range(-1, 3)] public int side = -1;

        [Header("Wave brick")]
        [Tooltip("Top speed of a wave brick at 100% power, in m/s. Slow but heavy. Default 5.")]
        public float speed = 5f;
        [Tooltip("Push in m/s2 at 100% power. Default 18.")]
        public float pushAcceleration = 18f;
        [Tooltip("Mass of one wave brick in kg (heavy water = more momentum). Default 160.")]
        public float mass = 160f;
        [Tooltip("Part of gravity cancelled, times power: at 100% power the wave keeps its height; weaker wave bricks fall like crashing water. Default 1.")]
        [Range(0f, 1f)] public float buoyancy = 1f;
        [Tooltip("Upward push in m/s2 when a wave brick is blocked, so the water climbs over steps. Default 14.")]
        public float climbAcceleration = 14f;
        [Tooltip("How strongly up-down speed is slowed while floating (stops bricks flying away). Default 3.")]
        public float verticalDamping = 3f;
        [Tooltip("Highest upward speed of a wave brick in m/s. Default 3.")]
        public float maxRiseSpeed = 3f;
        [Tooltip("Power left after each hit (0.5 = half). Default 0.5.")]
        [Range(0.1f, 0.95f)] public float powerAfterHit = 0.5f;
        [Tooltip("Below this power the wave brick fades away. Default 0.08.")]
        public float minPower = 0.08f;
        [Tooltip("Seconds a weak wave brick takes to fade away. Default 0.8.")]
        public float fadeTime = 0.8f;
        [Tooltip("Speed change given to a hit brick at 100% power, in m/s. Default 6.")]
        public float hitSpeed = 6f;
        [Tooltip("Joint damage of a hit at 100% power, in seconds of wear (plastic lasts 10, steel 60). Default 12.")]
        public float hitDamage = 12f;
        [Tooltip("Push given to a person at 100% power, in m/s. Default 6.")]
        public float personPush = 6f;
        [Tooltip("A wave brick is removed after this many seconds anyway. Default 20.")]
        public float lifetime = 20f;

        public Vector3 Direction { get; private set; }
        public override bool Running => waves.Count > 0;

        readonly List<WaveBlock> waves = new List<WaveBlock>();
        int startCount;

        void Reset() { displayName = "Tsunami"; }

        /// <summary>Physics layer of the wave bricks. They do not hit each other, so the wave stays a wall.</summary>
        public const string WaveLayer = "BrickPanicWave";
        int waveLayer = -1;

        void Awake()
        {
            waveLayer = LayerMask.NameToLayer(WaveLayer);
            if (waveLayer >= 0) Physics.IgnoreLayerCollision(waveLayer, waveLayer, true);
            else Debug.LogWarning("[BrickPanic] layer '" + WaveLayer + "' is missing: wave bricks will bump into each other.");
        }

        /// <summary>Chooses the side now, so the forecast can show it before the wave starts.</summary>
        public int PickSide() => side >= 0 ? side : Random.Range(0, 4);

        public override void Begin() => Begin(PickSide());

        public void Begin(int fromSide)
        {
            Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };   // from W, E, S, N
            Direction = dirs[fromSide];
            Vector3 lane = Vector3.Cross(Vector3.up, Direction);
            int n = map.size;
            var root = new GameObject("Tsunami").transform;

            // one lane every 2 m across the island; the wave starts just outside the first island cell of the lane
            int step = Mathf.Max(1, Mathf.RoundToInt(2f * brickScale));   // lane width = wave brick length
            float h = 0.62f * brickScale, depth = 1.1f * brickScale;
            for (int u = 0; u < n; u += step)
            {
                if (!FirstCell(u, fromSide, out int ci, out int cj)) continue;
                Vector3 cell = map.transform.TransformPoint(map.CellCentre(ci, cj));
                float ground = map.TopY(ci, cj);
                Vector3 laneCentre = cell + lane * (step * 0.5f - 0.5f);
                for (int r = 0; r < rows; r++)
                    for (int k = 0; k < layers; k++)
                    {
                        Vector3 p = laneCentre - Direction * (0.6f * brickScale + 1f + r * depth) + Vector3.up * (ground + 0.05f + k * h);
                        Spawn(p, root);
                    }
            }
            startCount = waves.Count;
            Debug.Log($"[BrickPanic] tsunami from side {fromSide}: {startCount} wave bricks");
        }

        /// <summary>First island cell of lane u, seen from the side the wave comes from.</summary>
        bool FirstCell(int u, int fromSide, out int ci, out int cj)
        {
            int n = map.size;
            for (int s = 0; s < n; s++)
            {
                switch (fromSide)
                {
                    case 0: ci = s; cj = u; break;
                    case 1: ci = n - 1 - s; cj = u; break;
                    case 2: ci = u; cj = s; break;
                    default: ci = u; cj = n - 1 - s; break;
                }
                if (map.Inside(ci, cj)) return true;
            }
            ci = cj = 0;
            return false;
        }

        void Spawn(Vector3 p, Transform root)
        {
            var go = new GameObject("WaveBrick");
            if (waveLayer >= 0) go.layer = waveLayer;
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(p, Quaternion.LookRotation(Direction));   // long side across the wave
            go.transform.localScale = Vector3.one * brickScale;
            go.AddComponent<MeshFilter>().sharedMesh = waveMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = waterMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(1.96f, 0.6f, 0.96f);
            box.center = new Vector3(0f, 0.3f, 0f);
            var w = go.AddComponent<WaveBlock>();
            w.Init(this, Direction);
            waves.Add(w);
        }

        public void Removed(WaveBlock w) { waves.Remove(w); }

        void FixedUpdate()
        {
            if (Running) DisasterState.Intensity = 0.5f + 0.5f * waves.Count / Mathf.Max(1f, startCount);
        }

        void LateUpdate()
        {
            if (!Running && startCount > 0) { startCount = 0; DisasterState.Intensity = 0f; }
        }
    }
}
