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
        [Tooltip("Lava bombs per second at full strength. Default 2.")]
        public float bombsPerSecond = 2f;
        [Tooltip("Flight time of a bomb in seconds (random between x and y). Longer = higher arcs. Default 3.5 - 5.5.")]
        public Vector2 flightTime = new Vector2(3.5f, 5.5f);
        [Tooltip("Gravity on bombs as a part of normal gravity. Lower = bombs fly higher and fall slowly. Default 0.5.")]
        [Range(0.1f, 1f)] public float bombGravity = 0.5f;
        [Tooltip("Seconds to grow to full strength at the start. Default 2.")]
        public float rampUp = 2f;

        [Header("Lava bomb")]
        [Tooltip("Mass of a small lava bomb in kg (big ones weigh more by their size). Default 40.")]
        public float bombMass = 40f;
        [Tooltip("Size of a small bomb (x the brick mesh). Default 1.5.")]
        public float smallSize = 1.5f;
        [Tooltip("Size of a big bomb. Big bombs explode when they land. Default 2.5.")]
        public float bigSize = 2.5f;
        [Tooltip("Part of bombs that are big. Default 0.35.")]
        [Range(0f, 1f)] public float bigChance = 0.35f;
        [Tooltip("Explosion radius of a big bomb in metres. Default 3.")]
        public float explosionRadius = 3f;
        [Tooltip("People closer than this to an explosion die, in metres. Default 1.3.")]
        public float killRadius = 1.3f;
        [Tooltip("Push of an explosion on bricks and people (speed change in m/s at the centre). Default 9.")]
        public float explosionPush = 9f;
        [Tooltip("Seconds the fire spot of an explosion burns. Default 6.")]
        public float fireTime = 6f;

        [Header("Smoke and sparks (brick particles)")]
        [Tooltip("Smoke bricks per second from the crater at full strength. Default 14.")]
        public float smokePerSecond = 14f;
        [Tooltip("Lava sparks per second from the crater at full strength. Default 10.")]
        public float sparksPerSecond = 10f;
        [Tooltip("Joint damage of a bomb hit, in seconds of wear (plastic lasts 10, steel 60). Default 15.")]
        public float hitDamage = 15f;
        [Tooltip("Seconds a bomb glows on the ground before it is removed. Default 4.")]
        public float coolTime = 4f;

        public override bool Running => time >= 0f;

        float time = -1f, spawnDebt, smokeDebt, sparkDebt;
        Transform root;

        void Reset() { displayName = "Volcano"; }

        public override void Begin()
        {
            time = 0f;
            spawnDebt = 0f;
            root = new GameObject("Eruption").transform;
        }

        void FixedUpdate()
        {
            if (time < 0f) return;
            time += Time.fixedDeltaTime;
            if (time > duration) { time = -1f; DisasterState.Intensity = 0f; return; }
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

        void Throw()
        {
            // random target cell on the island
            Vector3 target = Vector3.zero;
            for (int tries = 0; tries < 30; tries++)
            {
                int i = Random.Range(0, map.size), j = Random.Range(0, map.size);
                if (!map.Inside(i, j)) continue;
                target = map.transform.TransformPoint(map.CellCentre(i, j));
                target.y = map.TopY(i, j);
                break;
            }
            Vector3 start = map.CraterCentre + Vector3.up * 1.2f + Random.insideUnitSphere * 0.6f;
            float t = Random.Range(flightTime.x, flightTime.y);
            Vector3 g = Physics.gravity * bombGravity;
            Vector3 v = (target - start) / t - 0.5f * g * t;   // ballistic launch speed with the lower bomb gravity
            bool big = Random.value < bigChance;
            float size = big ? bigSize : smallSize;

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
            var bomb = go.AddComponent<LavaBomb>();
            bomb.Init(this, v, big, bombMass * size * size);
        }
    }
}
