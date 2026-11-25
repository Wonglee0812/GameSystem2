using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>Tsunami. A block of wave bricks starts on an invisible shelf at one coast. Every wave brick has its
    /// own drive (Power, 100% at the start): while it is over the island it keeps pushing itself forward, and every
    /// brick or person it hits takes part of that Power (100 -> 50 -> 25 %). Gravity is always on, so nothing floats;
    /// the water slides on a smooth invisible ground so the 0.6 m ground steps do not stop it. A wave brick that goes
    /// over the island edge, or loses its Power, breaks into a spray of small bricks and is gone.</summary>
    public class Tsunami : Disaster
    {
        [Header("References")]
        public MapGenerator map;
        [Tooltip("Used to wear the joints of a hit brick.")]
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
        [Tooltip("Speed in m/s of the wave bricks when they spawn. The drive then speeds them up to Top speed. Default 3.")]
        public float launchSpeed = 3f;
        [Tooltip("Slope of the shelf floor down to the island, in degrees (0 = flat). Default 0.")]
        [Range(0f, 40f)] public float floorTilt = 0f;
        [Tooltip("Gap in metres between the front of the water block and the first island cell. Default 0.3.")]
        public float coastGap = 0.3f;
        [Tooltip("Free space in metres between wave bricks when they spawn, so they never start inside each other (that makes them explode). Default 0.12.")]
        public float spawnGap = 0.12f;
        [Tooltip("Random tilt in degrees of each wave brick at spawn, so the block looks loose like water. Default 3.")]
        [Range(0f, 15f)] public float jitterAngle = 3f;
        [Tooltip("Thickness in metres of the invisible walls between two lanes. Must be smaller than the gap between wave bricks (lane width - brick length). Default 0.05.")]
        public float sideWall = 0.05f;
        [Tooltip("Seconds before the invisible shelf is removed. Wave bricks do not break apart over the sea before this. Default 3.")]
        public float tankTime = 3f;

        [Header("Drive (Power)")]
        [Tooltip("Top speed of a wave brick at 100% Power, in m/s. Lower Power = lower top speed. Default 7.")]
        public float speed = 7f;
        [Tooltip("Forward push in m/s2 at 100% Power, only while the brick is over the island. Default 25.")]
        public float pushAcceleration = 25f;
        [Tooltip("Power left after each hit on a brick or a person (0.7 = 70% left). Default 0.7.")]
        [Range(0.1f, 0.95f)] public float powerAfterHit = 0.7f;
        [Tooltip("Below this Power the wave brick breaks into spray and is gone. Default 0.05.")]
        public float minPower = 0.05f;

        [Header("Wave brick body")]
        [Tooltip("Mass of one wave brick in kg (heavy water = more momentum). A plastic 2x4 brick is 8 kg, steel 48 kg. Default 160.")]
        public float mass = 160f;
        [Tooltip("Friction of the wave bricks (0 = ice). Water slides, so keep it low. Default 0.02.")]
        [Range(0f, 1f)] public float friction = 0.02f;
        [Tooltip("Bounce of the wave bricks (0 = no bounce). Default 0.")]
        [Range(0f, 1f)] public float bounce = 0f;
        [Tooltip("Air/water drag on the wave bricks (Rigidbody linear damping). Default 0.05.")]
        public float drag = 0.05f;
        [Tooltip("Spin damping of the wave bricks (Rigidbody angular damping). Higher = the bricks stay flat. Default 2.")]
        public float angularDrag = 2f;

        [Header("Hits")]
        [Tooltip("Speed change given to a hit brick at 100% Power, in m/s (on top of the real collision). Default 6.")]
        public float hitSpeed = 6f;
        [Tooltip("Joint damage of a hit at 100% Power, in seconds of wear (plastic lasts 10, steel 60). Default 12.")]
        public float hitDamage = 12f;
        [Tooltip("Push given to a person at 100% Power, in m/s. Default 6.")]
        public float personPush = 6f;
        [Tooltip("Time-based wear on all joints while the water moves. 1 = like a level-1 earthquake, 0 = none. Default 1.")]
        [Range(0f, 2f)] public float wearStrength = 1f;

        [Header("Edge and clean up")]
        [Tooltip("A wave brick with no island ground within this many metres below it is over the edge: it breaks into spray. Default 8.")]
        public float edgeCheckDepth = 8f;
        [Tooltip("Number of small spray bricks a wave brick breaks into. Default 8.")]
        public int splashBricks = 8;
        [Tooltip("Size of one spray brick in metres. Default 0.5.")]
        public float splashSize = 0.5f;
        [Tooltip("A wave brick slower than this (m/s) counts as stuck. Default 0.5.")]
        public float settleSpeed = 0.5f;
        [Tooltip("Seconds a wave brick may stay stuck before it breaks into spray. Default 2.5.")]
        public float settleTime = 2.5f;
        [Tooltip("A wave brick is removed after this many seconds anyway. Default 20.")]
        public float lifetime = 20f;

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
                GroundMask = waveLayer >= 0 ? ~(1 << waveLayer) : ~0;
                return;
            }
            GroundMask = (1 << ground) | (1 << waveGroundLayer);
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
            WaveBlock.EndEdge = WaveBlock.EndPower = WaveBlock.EndStuck = 0;
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

        /// <summary>True while the invisible shelf still holds the water (no edge check yet).</summary>
        public bool ShelfActive => tank != null;

        /// <summary>Layers that count as island ground for the edge check.</summary>
        public int GroundMask { get; private set; }

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
