using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Makes a new map at the start of every game: new streams (ForestTerrain.Randomize), the
    /// ground made again from them, the water sources moved to the new stream heads, a new forest
    /// and a new place for the lodge. It runs before every other script (execution order -1000),
    /// so the hex grid, the water and the colony set themselves up on the new map as usual.
    /// seed = 0 gives a random map every time; any other number gives the same map again
    /// (useful to replay a map or to test a change).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class MapRandomizer : MonoBehaviour
    {
        public ForestTerrain terrain;
        public MapController map;
        public ColonyManager colony;
        public CameraRig cameraRig;
        [Tooltip("0 = a new random map every game. Any other number = always that map.")]
        public int seed = 0;
        [Tooltip("Where the lodge goes, as a fraction of the main stream from west (0) to east (1).")]
        [Range(0.2f, 0.8f)] public float lodgeAlongStream = 0.47f;

        /// <summary>The seed of the map being played (shown in the log, so a good map can be played again).</summary>
        public static int CurrentSeed { get; private set; }

        void Awake()
        {
            if (terrain == null) return;
            CurrentSeed = seed != 0 ? seed : Random.Range(1, 1000000);
            terrain.Randomize(CurrentSeed);
            terrain.Generate();
            Debug.Log($"Dam Rush map seed: {CurrentSeed}");

            // water sources: main stream first, then the side streams (same order as in MapController)
            if (map != null && map.sources != null)
                for (int i = 0; i < map.sources.Length && i < terrain.streams.Count; i++)
                {
                    Vector2 p = terrain.SourcePoint(i);
                    map.sources[i].transform.position = new Vector3(p.x, terrain.GroundAt(p.x, p.y) + 0.5f, p.y);
                }

            // the lodge goes next to the main stream, a little off the middle of the map
            Vector2 lodge = terrain.MainStreamAt(lodgeAlongStream);
            if (colony != null)
            {
                colony.preferredLodgeSpot = lodge;
                if (colony.forestRoot != null) colony.forestRoot = ReplantForest(colony.forestRoot, CurrentSeed);
            }
            if (cameraRig != null) cameraRig.focus = new Vector3(lodge.x, terrain.GroundAt(lodge.x, lodge.y), lodge.y);
        }

        /// <summary>
        /// Plants a new forest with the same tree kinds as the old one (willow and birch near the water,
        /// pines further away, clearings where a noise is low), then removes the old forest.
        /// The colony reads the trees from the new root in its Start.
        /// </summary>
        Transform ReplantForest(Transform oldRoot, int mapSeed)
        {
            // one example of each kind of tree, copied for the new forest
            var protos = new Dictionary<string, GameObject>();
            foreach (Transform t in oldRoot)
                if (!protos.ContainsKey(t.name)) protos[t.name] = t.gameObject;
            if (protos.Count == 0) return oldRoot;
            protos.TryGetValue("Willow", out var willow);
            protos.TryGetValue("Birch", out var birch);
            protos.TryGetValue("Pine", out var pine);
            GameObject Any() { foreach (var g in protos.Values) return g; return null; }
            if (willow == null) willow = birch != null ? birch : Any();
            if (birch == null) birch = willow;
            if (pine == null) pine = Any();

            var root = new GameObject("Forest").transform;
            var rng = new System.Random(mapSeed * 7 + 3);
            float R() => (float)rng.NextDouble();
            float ox = R() * 100f, oz = R() * 100f;
            int count = 0;
            for (float x = 1.5f; x < terrain.LengthX - 1f; x += 2.5f)
                for (float z = 1.5f; z < terrain.WidthZ - 1f; z += 2.5f)
                {
                    float px = x + (R() - 0.5f) * 2f, pz = z + (R() - 0.5f) * 2f;
                    float above = terrain.HeightAboveStream(px, pz, out float dist);
                    if (dist < 4.5f || above < 0.7f) continue;                          // keep stream banks open
                    float density = Mathf.PerlinNoise(px * 0.06f + ox, pz * 0.06f + oz);
                    if (R() > density * 1.6f - 0.1f) continue;                           // clearings where noise is low
                    bool nearWater = dist < 12f;
                    var proto = nearWater && R() < 0.75f ? (R() < 0.4f ? birch : willow) : pine;
                    var tree = Instantiate(proto, root).transform;
                    tree.name = proto.name;
                    tree.position = new Vector3(px, terrain.GroundAt(px, pz), pz);
                    tree.rotation = Quaternion.Euler(0f, R() * 360f, 0f);
                    tree.localScale = Vector3.one * (0.85f + R() * 0.3f);
                    count++;
                }
            Destroy(oldRoot.gameObject);
            Debug.Log($"New forest: {count} trees");
            return root;
        }
    }
}
