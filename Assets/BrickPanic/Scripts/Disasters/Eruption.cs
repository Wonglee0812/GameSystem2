using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>The volcano throws lava bombs from the crater to random places on the island.</summary>
    public class Eruption : Disaster
    {
        [Header("References")]
        public MapGenerator map;
        public PhysicsActivator activator;
        [Tooltip("Meshes used for lava bombs (one is picked per bomb).")]
        public Mesh[] bombMeshes;
        [Tooltip("Glowing lava material (M_BP_Lava).")]
        public Material lavaMaterial;

        [Header("Eruption")]
        [Tooltip("Length of the eruption in seconds. Default 20.")]
        public float duration = 20f;
        [Tooltip("Lava bombs per second at full strength. Default 4.5 (was 2; Wong 2026-11-25: too easy).")]
        public float bombsPerSecond = 4.5f;
        [Tooltip("Flight time of a bomb in seconds (random between x and y). Longer = higher arcs. Default 3.5 - 5.5.")]
        public Vector2 flightTime = new Vector2(3.5f, 5.5f);
        [Tooltip("Gravity on bombs as a part of normal gravity. Lower = bombs fly higher and fall slowly. Default 0.5.")]
        [Range(0.1f, 1f)] public float bombGravity = 0.5f;
        [Tooltip("Seconds to grow to full strength at the start. Default 2.")]
        public float rampUp = 2f;

        [Header("Lava bomb")]
        [Tooltip("Mass of a small lava bomb in kg (big ones weigh more by their size). Default 40.")]
        public float bombMass = 40f;
        [Tooltip("Size of a small bomb (x the brick mesh). Default 1.8.")]
        public float smallSize = 1.8f;
        [Tooltip("Size of a big bomb. Big bombs explode when they land. Default 3.2.")]
        public float bigSize = 3.2f;
        [Tooltip("Part of bombs that are big. Default 0.5.")]
        [Range(0f, 1f)] public float bigChance = 0.5f;
        [Tooltip("Explosion radius of a big bomb in metres. Default 3.6.")]
        public float explosionRadius = 3.6f;
        [Tooltip("People closer than this to an explosion die, in metres. Default 1.3.")]
        public float killRadius = 1.3f;
        [Tooltip("Push of an explosion on bricks and people (speed change in m/s at the centre). Default 9.")]
        public float explosionPush = 9f;
        [Tooltip("Seconds the fire spot of an explosion burns. Default 6.")]
        public float fireTime = 6f;

        [Header("Shelter rule")]
        [Tooltip("Part of the bombs aimed at a person with no roof above (the rest land at random). Default 0.5.")]
        [Range(0f, 1f)] public float aimAtExposed = 0.5f;
        [Tooltip("Random miss in metres around an aimed person. Default 0.8.")]
        public float aimSpread = 0.8f;
        [Tooltip("Damage of an explosion to a person outside the kill radius, at the centre (less further out). Default 70.")]
        public float explosionDamage = 70f;
        [Tooltip("Blast radius of a small bomb as a part of the big one (small bombs make no fire and never kill at once). Default 0.55.")]
        [Range(0f, 1f)] public float smallBlastScale = 0.55f;

        [Header("Smoke and sparks (brick particles)")]
        [Tooltip("Smoke bricks per second from the crater at full strength. Default 14.")]
        public float smokePerSecond = 14f;
        [Tooltip("Lava sparks per second from the crater at full strength. Default 10.")]
        public float sparksPerSecond = 10f;
        [Tooltip("Joint damage of a bomb hit, in seconds of wear (plastic lasts 10, steel 60). Default 15.")]
        public float hitDamage = 15f;
        [Tooltip("Seconds a bomb glows on the ground before it is removed. Default 4.")]
        public float coolTime = 4f;

        [Header("Per level (level 1 = the values above)")]
        [Tooltip("Extra bombs per second per level above 1, as a part of the base (0.25 = +25% per level). Default 0.25.")]
        public float bombsPerLevel = 0.25f;
        [Tooltip("Extra chance of a big (exploding) bomb per level above 1. Default 0.05.")]
        public float bigChancePerLevel = 0.05f;

        float baseBombs = -1f, baseBig;

        public override void SetLevel(int level)
        {
            if (baseBombs < 0f) { baseBombs = bombsPerSecond; baseBig = bigChance; }
            int k = Mathf.Max(0, level - 1);
            bombsPerSecond = baseBombs * (1f + bombsPerLevel * k);
            bigChance = Mathf.Min(0.9f, baseBig + bigChancePerLevel * k);
        }

        public override bool Running => time >= 0f;

        float time = -1f, spawnDebt, smokeDebt, sparkDebt;

        void OnDisable() { if (time >= 0f) { DisasterState.Intensity = 0f; DisasterState.WearScale = 1f; } }
        Transform root;

        void Reset() { displayName = "Volcano"; }

        public override void Begin()
        {
            // only bombs, explosions and fire hurt bricks: no island-wide wear like the earthquake (bug found 2026-11-25:
            // the volcano wore every joint on the island, even far from any bomb)
            DisasterState.WearScale = 0f;
            time = 0f;
            spawnDebt = 0f;
            root = new GameObject("Eruption").transform;
        }

        void FixedUpdate()
        {
            if (time < 0f) return;
            time += Time.fixedDeltaTime;
            if (time > duration) { time = -1f; DisasterState.Intensity = 0f; DisasterState.WearScale = 1f; return; }
            float k = Mathf.Clamp01(time / rampUp);
            DisasterState.Intensity = k;
            spawnDebt += bombsPerSecond * k * Time.fixedDeltaTime;
            while (spawnDebt >= 1f) { spawnDebt -= 1f; Throw(); }

            // black smoke and lava sparks from the crater, all made of bricks
            Vector3 c = map.CraterCentre + Vector3.up;
            smokeDebt += smokePerSecond * k * Time.fixedDeltaTime;
            while (smokeDebt >= 1f) { smokeDebt -= 1f; BrickFx.Smoke(c + Vector3.up * 1.5f, Random.Range(1.6f, 3.2f), Random.Range(3f, 5f)); }
            sparkDebt += sparksPerSecond * k * Time.fixedDeltaTime;
            while (sparkDebt >= 1f)
            {
                sparkDebt -= 1f;
                BrickFx.Spark(c, new Vector3(Random.Range(-3f, 3f), Random.Range(7f, 12f), Random.Range(-3f, 3f)), Random.Range(0.6f, 1.1f));
            }
        }

        readonly List<Collider> flying = new List<Collider>();
        float craterDepthMetres => map != null ? map.craterDepth * BrickGrid.BrickHeight : 1.2f;

        void Throw()
        {
            Vector3 target = Vector3.zero;
            float t = Random.Range(flightTime.x, flightTime.y);
            bool aimed = false;
            // shelter rule: some bombs are aimed at people with no roof (where they will be when the bomb lands, half lead)
            if (Random.value < aimAtExposed)
            {
                var open = new List<Person>();
                foreach (var p in Person.All) if (p != null && p.Alive && !p.Roofed) open.Add(p);
                if (open.Count > 0)
                {
                    var who = open[Random.Range(0, open.Count)];
                    Vector2 miss = Random.insideUnitCircle * aimSpread;
                    Vector3 lead = who.Velocity * t;   // where the person will be if they keep running
                    target = who.transform.position + new Vector3(lead.x + miss.x, 0f, lead.z + miss.y);
                    aimed = true;
                }
            }
            // random target cell on the island
            for (int tries = 0; tries < 30 && !aimed; tries++)
            {
                int i = Random.Range(0, map.size), j = Random.Range(0, map.size);
                if (!map.Inside(i, j)) continue;
                target = map.transform.TransformPoint(map.CellCentre(i, j));
                target.y = map.TopY(i, j);
                break;
            }
            bool big = Random.value < bigChance;
            float size = big ? bigSize : smallSize;
            // start above the crater rim, so a big bomb (wider than the crater) does not touch the walls and blow up there
            Vector3 start = map.CraterCentre + Vector3.up * (1.2f + size * 0.8f + craterDepthMetres) + Random.insideUnitSphere * 0.6f;
            Vector3 g = Physics.gravity * bombGravity;
            Vector3 v = (target - start) / t - 0.5f * g * t;   // ballistic launch speed with the lower bomb gravity

            var go = new GameObject("LavaBomb");
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(start, Random.rotation);
            go.transform.localScale = Vector3.one * size;
            var mesh = bombMeshes[Random.Range(0, bombMeshes.Length)];
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = lavaMaterial;
            var box = go.AddComponent<BoxCollider>();   // box, because the brick meshes are not readable for a MeshCollider in a build
            box.center = mesh.bounds.center;
            box.size = mesh.bounds.size;
            // bombs start close together in the crater: they must not hit each other there (they would blow up at the crater)
            flying.RemoveAll(c => c == null);
            foreach (var other in flying) Physics.IgnoreCollision(box, other);
            flying.Add(box);
            var bomb = go.AddComponent<LavaBomb>();
            bomb.Init(this, v, big, bombMass * size * size);
        }
    }
}
