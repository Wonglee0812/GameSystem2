using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrickPanic
{
    /// <summary>Builds a random floating island from brick columns with Perlin noise (Perlin, 1985).
    /// 1 cell = 2 x 2 studs = 1 m. The ground is locked: it is merged into a few meshes and box colliders on this
    /// object, which is a kinematic Rigidbody (so the earthquake can move it). Everything on top is normal bricks.</summary>
    public class MapGenerator : MonoBehaviour
    {
        public const float Cell = 1f;

        [Header("Island")]
        [Tooltip("Island size in cells (1 cell = 2 x 2 studs = 1 m). Default 48.")]
        public int size = 48;
        [Tooltip("Map seed. 0 = a new random seed every play. The seed is written to the Console so a map can be replayed. Default 0.")]
        public int seed = 0;
        [Tooltip("Hill size: smaller = wider hills. Default 0.07.")]
        public float noiseScale = 0.07f;
        [Tooltip("Highest normal ground in brick layers (0.6 m each). Default 5.")]
        public int maxLayers = 5;
        [Tooltip("How the coast drops: higher = flatter middle and a steeper edge. Default 2.5.")]
        public float edgeFalloff = 2.5f;
        [Tooltip("How ragged the coastline is, 0-1. Default 0.35.")]
        [Range(0f, 1f)] public float coastNoise = 0.35f;
        [Tooltip("Depth of the hanging rock under the island centre, in metres. Default 9.")]
        public float underDepth = 9f;

        [Header("Zones")]
        [Tooltip("Ground at or below this layer is beach (sand). Default 1.")]
        public int beachLayer = 1;
        [Tooltip("Ground at or above this layer is rock. Default 4.")]
        public int rockLayer = 4;

        [Header("Volcano (always on the map)")]
        [Tooltip("Volcano radius in cells. Default 7.")]
        public float volcanoRadius = 7f;
        [Tooltip("Volcano height in brick layers above the ground around it. Default 9.")]
        public int volcanoLayers = 9;
        [Tooltip("Crater radius in cells. Default 2.")]
        public float craterRadius = 2f;
        [Tooltip("Crater depth in brick layers. Default 2.")]
        public int craterDepth = 2;
        [Tooltip("Distance of the volcano from the centre, as a part of the island radius (0 = centre, 1 = edge). Default 0.55.")]
        [Range(0f, 1f)] public float volcanoDistance = 0.55f;

        [Header("Look")]
        [Tooltip("Mesh for the top brick of each column (the 2x2 brick, must be Read/Write enabled).")]
        public Mesh topBrick;
        public Material sandMat, grassMat, rockMat, volcanoMat, lavaMat, underMat;
        [Tooltip("Ground friction, 0-1. Default 0.8.")]
        [Range(0f, 1f)] public float friction = 0.8f;

        [Header("Map pieces")]
        [Tooltip("Places trees, bushes, rocks and huts made of bricks after the ground is built.")]
        public EnvironmentDresser dresser;

        public enum ZoneType : byte { None, Sand, Grass, Rock, Volcano, Crater }

        public int Seed { get; private set; }
        public int[,] Layers { get; private set; }
        public ZoneType[,] Zones { get; private set; }
        /// <summary>World position of the crater floor centre (for the eruption later).</summary>
        public Vector3 CraterCentre { get; private set; }

        Transform generated;
        const float TopScale = 1f / 0.99f;

        void Start() { Generate(); }

        [ContextMenu("Generate (Play mode)")]
        public void Generate()
        {
            Seed = seed != 0 ? seed : Random.Range(1, 1000000);
            Debug.Log($"[BrickPanic] map seed {Seed}");
            var rng = new System.Random(Seed);
            float ox = rng.Next(0, 10000), oz = rng.Next(0, 10000), cx = rng.Next(0, 10000), cz = rng.Next(0, 10000);

            if (generated != null) Destroy(generated.gameObject);
            generated = new GameObject("Generated").transform;
            generated.SetParent(transform, false);

            Layers = new int[size, size];
            Zones = new ZoneType[size, size];
            var mask = new float[size, size];
            float half = size * 0.5f;

            // height + island shape
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    float dx = (i + 0.5f - half) / half, dz = (j + 0.5f - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dz * dz);
                    float coast = Mathf.PerlinNoise(cx + i * 0.12f, cz + j * 0.12f);
                    float m = 1f - Mathf.Pow(d * (1f - coastNoise * 0.5f + coastNoise * coast), edgeFalloff);
                    mask[i, j] = m;
                    if (m <= 0.04f) continue;
                    float h = Fbm(ox + i * noiseScale, oz + j * noiseScale);
                    int layer = Mathf.Clamp(Mathf.RoundToInt(h * maxLayers * Mathf.Clamp01(m * 1.6f)), 1, maxLayers);
                    Layers[i, j] = layer;
                    Zones[i, j] = layer <= beachLayer ? ZoneType.Sand : layer >= rockLayer ? ZoneType.Rock : ZoneType.Grass;
                }

            // volcano: a cone with a crater, away from the centre
            float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
            Vector2 vc = new Vector2(half + Mathf.Cos(angle) * volcanoDistance * half, half + Mathf.Sin(angle) * volcanoDistance * half);
            int baseLayer = Mathf.Max(1, Layers[Mathf.Clamp((int)vc.x, 0, size - 1), Mathf.Clamp((int)vc.y, 0, size - 1)]);
            int rimLayer = baseLayer + Mathf.RoundToInt(volcanoLayers * (1f - craterRadius / volcanoRadius));
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    float r = Vector2.Distance(new Vector2(i + 0.5f, j + 0.5f), vc);
                    if (r > volcanoRadius) continue;
                    if (r < craterRadius)
                    {
                        Layers[i, j] = rimLayer - craterDepth;
                        Zones[i, j] = ZoneType.Crater;
                    }
                    else
                    {
                        int v = baseLayer + Mathf.RoundToInt(volcanoLayers * (1f - r / volcanoRadius));
                        Layers[i, j] = Mathf.Max(Mathf.Max(Layers[i, j], 1), v);
                        Zones[i, j] = ZoneType.Volcano;
                    }
                    mask[i, j] = Mathf.Max(mask[i, j], 0.3f);
                }
            CraterCentre = transform.TransformPoint(CellCentre(vc.x - 0.5f, vc.y - 0.5f) + Vector3.up * ((rimLayer - craterDepth) * BrickGrid.BrickHeight));

            BuildGround(mask, rng);
            if (dresser != null) dresser.Dress(this, rng);
        }

        void BuildGround(float[,] mask, System.Random rng)
        {
            var lists = new Dictionary<ZoneType, List<CombineInstance>>();
            var under = new List<CombineInstance>();
            var box = BoxMesh();
            float bh = BrickGrid.BrickHeight;
            var pm = new PhysicsMaterial("PM_Ground") { staticFriction = friction, dynamicFriction = friction, frictionCombine = PhysicsMaterialCombine.Average };
            var colliders = new GameObject("Colliders");
            colliders.transform.SetParent(generated, false);
            int groundLayer = LayerMask.NameToLayer(BrickGrid.GroundLayerName);
            if (groundLayer >= 0) colliders.layer = groundLayer;   // own layer so wave bricks can skip the steps
            float uo = rng.Next(0, 10000);

            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    int layer = Layers[i, j];
                    if (layer <= 0) continue;
                    Vector3 c = CellCentre(i, j);
                    float top = layer * bh;
                    float depth = underDepth * Mathf.Sqrt(Mathf.Clamp01(mask[i, j])) * (0.6f + 0.4f * Mathf.PerlinNoise(uo + i * 0.2f, j * 0.2f));
                    float bottom = -Mathf.Ceil(depth / bh) * bh - bh;

                    var zone = Zones[i, j];
                    if (!lists.TryGetValue(zone, out var list)) lists[zone] = list = new List<CombineInstance>();
                    // the Kenney brick is 0.99 m wide; scale it to 1 m so no thin gaps show between ground bricks
                    list.Add(new CombineInstance { mesh = topBrick, transform = Matrix4x4.TRS(new Vector3(c.x, top - bh, c.z), Quaternion.identity, new Vector3(TopScale, 1f, TopScale)) });

                    float bodyTop = top - bh;
                    if (bodyTop > bottom)
                        under.Add(new CombineInstance { mesh = box, transform = Matrix4x4.TRS(new Vector3(c.x, (bodyTop + bottom) * 0.5f, c.z), Quaternion.identity, new Vector3(Cell, bodyTop - bottom, Cell)) });

                    var bc = colliders.AddComponent<BoxCollider>();
                    bc.center = new Vector3(c.x, (top + bottom) * 0.5f, c.z);
                    bc.size = new Vector3(Cell, top - bottom, Cell);
                    bc.sharedMaterial = pm;
                }

            foreach (var kv in lists) MakeMesh("Ground_" + kv.Key, kv.Value, MaterialFor(kv.Key));
            MakeMesh("Ground_Under", under, underMat);
        }

        Material MaterialFor(ZoneType z) => z switch
        {
            ZoneType.Sand => sandMat,
            ZoneType.Rock => rockMat,
            ZoneType.Volcano => volcanoMat,
            ZoneType.Crater => lavaMat,
            _ => grassMat
        };

        void MakeMesh(string name, List<CombineInstance> list, Material mat)
        {
            if (list.Count == 0) return;
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(list.ToArray(), true, true);
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(generated, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        /// <summary>Centre of a cell in local space (y = 0).</summary>
        public Vector3 CellCentre(float i, float j)
        {
            float half = size * 0.5f;
            return new Vector3((i + 0.5f - half) * Cell, 0f, (j + 0.5f - half) * Cell);
        }

        /// <summary>World height of the top of a cell's ground (studs not counted).</summary>
        public float TopY(int i, int j) => transform.position.y + Layers[i, j] * BrickGrid.BrickHeight;

        public bool Inside(int i, int j) => i >= 0 && j >= 0 && i < size && j < size && Layers[i, j] > 0;

        static float Fbm(float x, float z)
        {
            float a = Mathf.PerlinNoise(x, z) * 0.65f + Mathf.PerlinNoise(x * 2.1f + 17f, z * 2.1f + 31f) * 0.35f;
            return Mathf.Clamp01(a);
        }

        static Mesh boxMesh;
        /// <summary>A unit cube (made here so it is always readable for CombineMeshes).</summary>
        static Mesh BoxMesh()
        {
            if (boxMesh != null) return boxMesh;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxMesh = Instantiate(go.GetComponent<MeshFilter>().sharedMesh);
            Destroy(go);
            return boxMesh;
        }
    }
}
