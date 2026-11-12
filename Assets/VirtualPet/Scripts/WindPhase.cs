using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// The pet is wind, so it does not bump into props: it flows through them.
    /// While its body is inside a prop or a shelf, the wind thins out. The arcs
    /// spread wider and fade, the particles tear into wisps, the eyes squint and
    /// the pet slows down a little. When it is free again, it gathers back into
    /// shape, more slowly than it broke apart, like a gust that re-forms.
    /// </summary>
    public class WindPhase : MonoBehaviour
    {
        public PetDrag drag;
        public CapsuleCollider body;
        public LayerMask solidLayers;
        public WindArcs arcs;
        public WindVortex[] particles;
        public Transform eyes;
        [Tooltip("In a storm the pet is not thinned by the props it spins around itself.")]
        public PetLook look;

        [Header("Feel")]
        [Tooltip("Overlap (m) that is ignored, so just touching the desk does nothing.")]
        public float ignoreDepth = 0.06f;
        [Tooltip("Overlap (m) at which the pet is fully thinned out.")]
        public float fullDepth = 0.6f;
        public float breakSpeed = 7f;
        public float gatherSpeed = 2.2f;

        [Header("At full thinning")]
        public float arcSpread = 1.6f;
        [Range(0f, 1f)] public float arcFade = 0.2f;
        public float particleSpread = 1.5f;
        public float particleScatter = 0.35f;
        [Range(0f, 1f)] public float particleFade = 0.3f;
        public Vector3 eyeSquint = new Vector3(1.15f, 0.35f, 1f);
        [Tooltip("Speed lost per second while inside a solid.")]
        public float slowDown = 2.5f;

        /// <summary>0 = solid wind, 1 = fully thinned out. Other systems can read this.</summary>
        public float Amount { get; private set; }
        public bool InsideSolid => target > 0.01f;

        readonly Collider[] overlaps = new Collider[16];
        float target;
        Vector3 eyesBaseScale = Vector3.one;

        void Start()
        {
            if (drag == null) drag = GetComponent<PetDrag>();
            if (body == null && drag != null) body = drag.body;
            if (eyes != null) eyesBaseScale = eyes.localScale;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            target = Mathf.InverseLerp(ignoreDepth, fullDepth, DeepestOverlap());
            if (look != null) target *= 1f - look.Storm;
            if (drag != null && drag.Anchor.HasValue) target = 0f;   // inside the turbine
            float speed = target > Amount ? breakSpeed : gatherSpeed;
            Amount = Mathf.MoveTowards(Amount, target, speed * dt);
            float a = Amount * Amount * (3f - 2f * Amount);   // smooth start and end

            if (arcs != null)
            {
                arcs.spread = Mathf.Lerp(1f, arcSpread, a);
                arcs.fade = Mathf.Lerp(1f, arcFade, a);
            }
            foreach (var v in particles)
            {
                if (v == null) continue;
                v.spread = Mathf.Lerp(1f, particleSpread, a);
                v.scatter = particleScatter * a;
                v.fade = Mathf.Lerp(1f, particleFade, a);
            }
            if (eyes != null) eyes.localScale = Vector3.Scale(eyesBaseScale, Vector3.Lerp(Vector3.one, eyeSquint, a));
            if (drag != null && target > 0f && !drag.IsHeld)
                drag.AddAcceleration(-drag.Velocity * slowDown * a, dt);
        }

        /// <summary>How deep (m) the pet's body is inside the deepest solid it touches.</summary>
        float DeepestOverlap()
        {
            if (body == null) return 0f;
            Vector3 c = body.transform.TransformPoint(body.center);
            float half = Mathf.Max(0f, body.height * 0.5f - body.radius);
            int n = Physics.OverlapCapsuleNonAlloc(c + Vector3.up * half, c - Vector3.up * half, body.radius,
                                                   overlaps, solidLayers, QueryTriggerInteraction.Ignore);
            float deepest = 0f;
            for (int i = 0; i < n; i++)
            {
                var other = overlaps[i];
                if (Physics.ComputePenetration(body, body.transform.position, body.transform.rotation,
                                               other, other.transform.position, other.transform.rotation,
                                               out _, out float depth))
                    deepest = Mathf.Max(deepest, depth);
            }
            return deepest;
        }
    }
}
