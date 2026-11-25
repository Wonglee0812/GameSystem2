using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>The shelter rule (Wong, 2026-11-25). Earthquake part only: the volcano throws its bombs from the crater and
    /// aims a part of them at people with no roof (Eruption.aimAtExposed). Without it the best plan was to build nothing and keep an empty island:
    /// a dominant strategy, which takes away meaningful choices (Adams, 2014). Now, during an earthquake or an eruption,
    /// people with no roof above their head get things falling on them: rocks shaken loose in a quake, hot ash bombs in an
    /// eruption. The falling pieces are real bricks, so a roof (any brick above the person) stops them - and a roof can
    /// collapse too. Every second, each exposed person gets one piece with a chance of rate x disaster intensity.</summary>
    public class ShelterDebris : MonoBehaviour
    {
        [Header("References")]
        public RoundManager round;
        [Tooltip("Mesh of a falling piece (a small Kenney brick).")]
        public Mesh debrisMesh;
        [Tooltip("Material of quake rocks (grey).")]
        public Material rockMaterial;
        [Tooltip("Material of ash bombs (glowing lava).")]
        public Material ashMaterial;

        [Header("Rule")]
        [Tooltip("Pieces per second for one exposed person at full quake strength. Default 0.6.")]
        public float quakeRate = 0.6f;
        [Tooltip("Pieces per second for one exposed person at full eruption strength. Default 0.45.")]
        public float eruptionRate = 0.45f;
        [Tooltip("Damage of a quake rock hit. Default 35 (an adult survives two).")]
        public float quakeDamage = 35f;
        [Tooltip("Damage of an ash bomb hit. Default 40.")]
        public float ashDamage = 40f;
        [Tooltip("Height above the person where a piece starts, in metres. Default 16.")]
        public float dropHeight = 16f;
        [Tooltip("Random miss in metres (0 = always right on the head). Default 1.")]
        public float spread = 1f;
        [Tooltip("Size of a piece (x the mesh). Default 1.2.")]
        public float size = 1.2f;
        [Tooltip("How high above the head a roof must be found, in metres. Default 30.")]
        public float roofSearch = 30f;

        /// <summary>People that had no roof at the last check (for the HUD and the report).</summary>
        public static int ExposedCount { get; private set; }

        float nextCheck;
        readonly List<Person> exposed = new List<Person>();
        readonly RaycastHit[] roofHits = new RaycastHit[8];

        void Start() { if (round == null) round = FindFirstObjectByType<RoundManager>(); }

        /// <summary>True when there is a brick (or ground) above the person's head.</summary>
        public bool HasRoof(Person p)
        {
            Vector3 head = p.transform.position + Vector3.up * (1.9f * p.transform.localScale.y);
            int n = Physics.RaycastNonAlloc(head, Vector3.up, roofHits, roofSearch, BrickGrid.QueryMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = roofHits[i].collider;
                if (c.GetComponentInParent<Person>() != null || c.GetComponent<Hazard>() != null) continue;
                return true;
            }
            return false;
        }

        void Update()
        {
            if (round == null || round.Current != RoundManager.State.Disaster) { ExposedCount = 0; return; }
            var d = round.Next;
            // volcano: no sky drops (Wong, 2026-11-25: bombs must come from the crater); Eruption aims some bombs at exposed people
            bool quake = d is Earthquake, erupt = false;
            if (!quake) { ExposedCount = 0; return; }
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + 1f;

            exposed.Clear();
            foreach (var p in Person.All) if (p != null && p.Alive && !HasRoof(p)) exposed.Add(p);
            ExposedCount = exposed.Count;
            float chance = (quake ? quakeRate : eruptionRate) * Mathf.Clamp01(DisasterState.Intensity * 1.2f);
            foreach (var p in exposed)
                if (Random.value < chance) Drop(p, erupt);
        }

        void Drop(Person p, bool ash)
        {
            if (debrisMesh == null) return;
            Vector2 miss = Random.insideUnitCircle * spread;
            // aim where the person will be when the piece lands (about 1.8 s), but only half the way: running still helps
            Vector3 lead = p.Velocity * 0.9f;
            lead.y = 0f;
            Vector3 start = p.transform.position + lead + new Vector3(miss.x, dropHeight, miss.y);
            var go = new GameObject(ash ? "AshBomb" : "QuakeRock");
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(start, Random.rotation);
            go.transform.localScale = Vector3.one * size;
            go.AddComponent<MeshFilter>().sharedMesh = debrisMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = ash ? ashMaterial : rockMaterial;
            var box = go.AddComponent<BoxCollider>();
            box.center = debrisMesh.bounds.center;
            box.size = debrisMesh.bounds.size;
            var f = go.AddComponent<FallingDebris>();
            f.Init(ash ? ashDamage : quakeDamage, ash ? "ash" : "rock", ash);
        }
    }
}
