using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>Momentum tsunami. A block of water bricks is spawned on an invisible shelf at one coast and
    /// launched once towards the island (one start speed, like a wave rolling in). After that nothing is
    /// scripted: the water bricks only have gravity, mass, low friction and collisions. Every hit hands part of
    /// their momentum to the thing they hit, so a wave that hits a lot slows down, stops and drains away; a wave
    /// that hits little sweeps across the whole island. Joints break only from real contact forces.</summary>
    public class Tsunami : Disaster
    {
        [Header("References")]
        public MapGenerator map;
        [Tooltip("Not used for damage any more (the wave is pure physics). Kept so the scene builder still works.")]
        public PhysicsActivator activator;
        [Tooltip("Mesh of one wave brick (2x4 brick).")]
        public Mesh waveMesh;
        [Tooltip("See-through blue material for the wave bricks (M_BP_Water).")]
        public Material waterMaterial;

        [Header("Water block")]
        [Tooltip("Size of one wave brick compared with a normal 2x4 brick. 2 = a 4 m x 2 m brick, 1.2 m high. Default 2.")]
        public float brickScale = 2f;
        [Tooltip("Height of the water block in wave brick layers. 4 x 1.2 m = 4.8 m. Default 4.")]
        public int layers = 4;
        [Tooltip("Rows of wave bricks behind each other (how much water). Default 3.")]
        public int rows = 3;
        [Tooltip("Side the wave comes from: 0 = west, 1 = east, 2 = south, 3 = north, -1 = random. Default -1.")]
        [Range(-1, 3)] public int side = -1;
        [Tooltip("Start speed of the wave in m/s, given ONCE at spawn (all later movement is physics). Higher = more momentum = sweeps further and hits harder. Default 9.")]
        public float launchSpeed = 9f;
        [Tooltip("Slope of the shelf floor down to the island, in degrees (0 = flat). Default 0.")]
        [Range(0f, 40f)] public float floorTilt = 0f;
        [Tooltip("Gap in metres between the front of the water block and the first island cell. Default 0.3.")]
        public float coastGap = 0.3f;
        [Tooltip("Free space in metres between wave bricks when they spawn, so they never start inside each other (that makes them explode). Default 0.12.")]
        public float spawnGap = 0.12f;
        [Tooltip("Random tilt in degrees of each wave brick at spawn, so the block is a loose pile and collapses like water instead of standing like a wall. Default 5.")]
        [Range(0f, 15f)] public float jitterAngle = 5f;
        [Tooltip("Thickness in metres of the invisible walls between two lanes. Must be smaller than the gap between wave bricks (lane width - brick length). Default 0.05.")]
        public float sideWall = 0.05f;
        [Tooltip("Seconds before the invisible shelf is removed. Water still on it then falls into the sea. Default 1.5.")]
        public float tankTime = 1.5f;

        [Header("Wave brick")]
        [Tooltip("Mass of one wave brick in kg. A plastic 2x4 brick is 8 kg, steel 48 kg. Heavier water hits harder. Default 40.")]
        public float mass = 40f;
        [Tooltip("Friction of the wave bricks (0 = ice). Water slides, so keep it low. Default 0.02.")]
        [Range(0f, 1f)] public float friction = 0.02f;
        [Tooltip("Bounce of the wave bricks (0 = no bounce). Default 0.")]
        [Range(0f, 1f)] public float bounce = 0f;
        [Tooltip("Air/water drag on the wave bricks (Rigidbody linear damping). Higher = the wave stops sooner. Default 0.05.")]
        public float drag = 0.05f;
        [Tooltip("Spin damping of the wave bricks (Rigidbody angular damping). Default 2.")]
        public float angularDrag = 2f;

        [Header("Clean up")]
        [Tooltip("A wave brick slower than this (m/s) has lost its momentum. Default 1.")]
        public float settleSpeed = 1f;
        [Tooltip("Seconds a wave brick must stay that slow before it drains away (fades out). Default 0.75.")]
        public float settleTime = 0.75f;
        [Tooltip("Seconds a wave brick takes to fade out. Its collider stays on until it is gone. Default 0.8.")]
        public float fadeTime = 0.8f;
        [Tooltip("A wave brick is removed after this many seconds anyway. Default 15.")]
        public float lifetime = 25f;

        [Header("People and wear")]
        [Tooltip("A person hit by water moving faster than this (m/s) falls over (tumble). The push itself comes only from the collision. Default 2.")]
        public float sweepSpeed = 2f;
        [Tooltip("Extra time-based wear on joints while the water moves (0 = none: only real forces break joints = 100% physics). 1 = same wear as a level-1 earthquake. Default 0.")]
        [Range(0f, 2f)] public float wearStrength = 0f;

        public Vector3 Direction { get; private set; }
        public override bool Running => waves.Count > 0;

        readonly List<WaveBlock> waves = new List<WaveBlock>();
        int startCount;
        GameObject tank;
        PhysicsMaterial waterPhysics;

        void Reset() { displayName = "Tsunami"; }

        /// <summary>Physics layer of the wave bricks. They now DO hit each other (that is how the water pushes itself forward).</summary>
        public const string WaveLayer = "BrickPanicWave";
        int waveLayer = -1;

        int waveGroundLayer = -1;
        GameObject waveGround;

        void Awake()
        {
            waveLayer = LayerMask.NameToLayer(WaveLayer);
            int ground = LayerMask.NameToLayer(BrickGrid.GroundLayerName);
            waveGroundLayer = LayerMask.NameToLayer(BrickGrid.WaveGroundLayerName);
            if (waveLayer < 0 || ground < 0 || waveGroundLayer < 0)
            {
                Debug.LogWarning("[BrickPanic] tsunami layers missing (" + WaveLayer + ", " + BrickGrid.GroundLayerName + ", " + BrickGrid.WaveGroundLayerName + "): water will hit the ground steps.");
                waveGroundLayer = -1;
                return;
            }
            Physics.IgnoreLayerCollision(waveLayer, waveLayer, false);   // water pushes water
            Physics.IgnoreLayerCollision(waveLayer, ground, true);       // water skips the stepped ground...
            for (int l = 0; l < 32; l++)                                  // ...and slides on the smooth wave ground, which only water touches
                Physics.IgnoreLayerCollision(waveGroundLayer, l, l != waveLayer);
        }

        /// <summary>Builds a smooth invisible surface over the island (one vertex per cell corner, height = average of
        /// the cells around it). Only wave bricks touch it, so water flows up and over the 0.6 m ground steps instead
        /// of crashing into each step like a wall. People and bricks still stand on the real stepped ground.</summary>
        void BuildWaveGround()
        {
            if (waveGround != null) Destroy(waveGround);
            if (waveGroundLayer < 0) return;
            int n = map.size;
            float baseY = map.transform.position.y;
            var verts = new Vector3[(n + 1) * (n + 1)];
            var tris = new List<int>();
            for (int a = 0; a <= n; a++)
                for (int b = 0; b <= n; b++)
                {
                    float sum = 0f; int count = 0;
                    for (int di = -1; di <= 0; di++)
                        for (int dj = -1; dj <= 0; dj++)
                            if (map.Inside(a + di, b + dj)) { sum += map.TopY(a + di, b + dj) - baseY; count++; }
                    Vector3 c = map.CellCentre(a - 0.5f, b - 0.5f);
                    verts[a * (n + 1) + b] = new Vector3(c.x, count > 0 ? sum / count : 0f, c.z);
                }
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    if (!map.Inside(i, j)) continue;
                    int v00 = i * (n + 1) + j, v10 = (i + 1) * (n + 1) + j, v01 = v00 + 1, v11 = v10 + 1;
                    tris.Add(v00); tris.Add(v01); tris.Add(v11);
                    tris.Add(v00); tris.Add(v11); tris.Add(v10);
                }
            var mesh = new Mesh { name = "WaveGround", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = verts;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            waveGround = new GameObject("WaveGround") { layer = waveGroundLayer };
            waveGround.transform.SetParent(map.transform, false);
            var mc = waveGround.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.sharedMaterial = waterPhysics;
        }

        /// <summary>Chooses the side now, so the forecast can show it before the wave starts.</summary>
        public int PickSide() => side >= 0 ? side : Random.Range(0, 4);

        public override void Begin() => Begin(PickSide());

        public void Begin(int fromSide)
        {
            Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };   // from W, E, S, N
            Direction = dirs[fromSide];
            int n = map.size;
            if (waterPhysics == null)
                waterPhysics = new PhysicsMaterial("Water")
                {
                    dynamicFriction = friction, staticFriction = friction, bounciness = bounce,
                    frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum
                };

            BuildWaveGround();
            var root = new GameObject("Tsunami").transform;
            if (tank != null) Destroy(tank);
            tank = new GameObject("TsunamiTank");
            if (waveLayer >= 0) tank.layer = waveLayer;

            // wave brick size (scaled 2x4 brick: long side across the wave)
            float bw = 1.96f * brickScale, bh = 0.6f * brickScale, bd = 0.96f * brickScale;
            int step = Mathf.Max(1, Mathf.CeilToInt(bw + sideWall + 0.01f));   // lane width in cells (1 cell = 1 m)
            float laneW = step * MapGenerator.Cell;
            float tankLen = rows * (bd + spawnGap) + spawnGap;
            float tankH = layers * (bh + spawnGap) + 2f;
            Quaternion frame = Quaternion.LookRotation(Direction) * Quaternion.Euler(floorTilt, 0f, 0f);
            Vector3 f = frame * Vector3.forward, up = frame * Vector3.up, right = frame * Vector3.right;

            int lanes = (n + step - 1) / step;
            bool[] has = new bool[lanes];
            Vector3[] front = new Vector3[lanes];
            for (int l = 0; l < lanes; l++) has[l] = LaneCoast(l * step, step, fromSide, out front[l]);
            launch = f * launchSpeed;

            for (int l = 0; l < lanes; l++)
            {
                if (!has[l]) continue;
                Vector3 o = front[l] - Direction * coastGap;   // front edge of the tank floor, at ground height
                // shelf: floor (flat by default), back wall, thin side walls
                AddBox(o - f * (tankLen * 0.5f) - up * 0.25f, new Vector3(laneW, 0.5f, tankLen), frame);
                AddBox(o - f * (tankLen + 0.25f) + up * (tankH * 0.5f), new Vector3(laneW, tankH, 0.5f), frame);
                // thin side walls on both lane borders: they sit in the small gap between two wave bricks, so the
                // water cannot spill sideways out of a lane whose neighbour starts further in or out
                Vector3 sideCentre = o - f * (tankLen * 0.5f) + up * (tankH * 0.5f);
                Vector3 sideSize = new Vector3(sideWall, tankH, tankLen);
                Vector3 across = LaneStep(fromSide);
                AddBox(sideCentre - across * (laneW * 0.5f), sideSize, frame);
                AddBox(sideCentre + across * (laneW * 0.5f), sideSize, frame);

                for (int r = 0; r < rows; r++)
                    for (int k = 0; k < layers; k++)
                    {
                        Vector3 p = o - f * (spawnGap + bd * 0.5f + r * (bd + spawnGap)) + up * (0.02f + k * (bh + spawnGap));
                        Quaternion rot = frame * Quaternion.Euler(Random.Range(-jitterAngle, jitterAngle), 0f, 0f);
                        Spawn(p, rot, root);
                    }
            }
            Invoke(nameof(RemoveTank), tankTime);
            startCount = waves.Count;
            Debug.Log($"[BrickPanic] tsunami from side {fromSide}: {startCount} wave bricks, {layers} layers, launch {launchSpeed} m/s");
        }

        /// <summary>World step from one lane to the next (direction in which u grows).</summary>
        static Vector3 LaneStep(int fromSide) => fromSide <= 1 ? Vector3.forward : Vector3.right;

        static void ToCell(int fromSide, int n, int s, int u, out int i, out int j)
        {
            switch (fromSide)
            {
                case 0: i = s; j = u; break;
                case 1: i = n - 1 - s; j = u; break;
                case 2: i = u; j = s; break;
                default: i = u; j = n - 1 - s; break;
            }
        }

        /// <summary>Front point of a lane: the coast edge of the lane (the first island cell of any of its cell
        /// columns, seen from the sea), at the height of the highest of those first cells.</summary>
        bool LaneCoast(int u0, int step, int fromSide, out Vector3 point)
        {
            int n = map.size, sMin = int.MaxValue;
            float ground = float.MinValue;
            for (int u = u0; u < Mathf.Min(n, u0 + step); u++)
                for (int s = 0; s < n; s++)
                {
                    ToCell(fromSide, n, s, u, out int i, out int j);
                    if (!map.Inside(i, j)) continue;
                    sMin = Mathf.Min(sMin, s);
                    ground = Mathf.Max(ground, map.TopY(i, j));
                    break;
                }
            point = default;
            if (sMin == int.MaxValue) return false;
            float uMid = u0 + (step - 1) * 0.5f;
            Vector3 c;
            switch (fromSide)
            {
                case 0: c = map.CellCentre(sMin, uMid); break;
                case 1: c = map.CellCentre(n - 1 - sMin, uMid); break;
                case 2: c = map.CellCentre(uMid, sMin); break;
                default: c = map.CellCentre(uMid, n - 1 - sMin); break;
            }
            c = map.transform.TransformPoint(c) - Direction * (MapGenerator.Cell * 0.5f);
            point = new Vector3(c.x, ground, c.z);
            return true;
        }

        void AddBox(Vector3 centre, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("TankPart");
            go.layer = tank.layer;
            go.transform.SetParent(tank.transform, false);
            go.transform.SetPositionAndRotation(centre, rot);
            var b = go.AddComponent<BoxCollider>();
            b.size = size;
            b.sharedMaterial = waterPhysics;
        }

        Vector3 launch;
        /// <summary>Start velocity of a new wave brick (set once at spawn).</summary>
        public Vector3 LaunchVelocity => launch;

        void RemoveTank() { if (tank != null) Destroy(tank); }

        void Spawn(Vector3 p, Quaternion rot, Transform root)
        {
            var go = new GameObject("WaveBrick");
            if (waveLayer >= 0) go.layer = waveLayer;
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(p, rot);
            go.transform.localScale = Vector3.one * brickScale;
            go.AddComponent<MeshFilter>().sharedMesh = waveMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = waterMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(1.96f, 0.6f, 0.96f);
            box.center = new Vector3(0f, 0.3f, 0f);
            box.sharedMaterial = waterPhysics;
            var w = go.AddComponent<WaveBlock>();
            w.Init(this);
            waves.Add(w);
        }

        public void Removed(WaveBlock w) { waves.Remove(w); }

        void FixedUpdate()
        {
            if (!Running) return;
            int moving = 0;
            foreach (var w in waves) if (w.Moving) moving++;
            float k = (float)moving / Mathf.Max(1, startCount);
            DisasterState.Intensity = 0.5f + 0.5f * k;           // people panic while the water moves
            DisasterState.WearScale = wearStrength;              // 0 = no extra wear: only real forces break joints
        }

        void LateUpdate()
        {
            if (!Running && startCount > 0)
            {
                startCount = 0;
                DisasterState.Intensity = 0f;
                DisasterState.WearScale = 1f;
                CancelInvoke(nameof(RemoveTank));
                RemoveTank();
                if (waveGround != null) Destroy(waveGround);
            }
        }

        void OnDisable()
        {
            if (startCount > 0) { DisasterState.Intensity = 0f; DisasterState.WearScale = 1f; }
        }
    }
}
