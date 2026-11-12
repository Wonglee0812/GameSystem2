using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Turns the pet's states into movement:
    /// - Asleep: no flying, the pet sinks onto the desk (PetWander.Asleep).
    /// - Tired: flies slower.
    /// - Storm: flies much faster and more wildly, rests only briefly, and goes
    ///   for the props to wreck them. A storm also burns energy, so an angry pet
    ///   tires itself out, falls asleep, and the storm stops.
    /// </summary>
    public class PetMoodBehaviour : MonoBehaviour
    {
        public PetStateMachine fsm;
        public PetWander wander;
        public PetLook look;
        public PetStorm storm;
        public LayerMask propLayers;
        public float tiredSpeed = 0.6f;
        public float stormSpeed = 3.2f;
        [Tooltip("Steering is this much sharper in a storm, for quick, wild turns.")]
        public float stormSteering = 2f;
        [Tooltip("A storm aims this far past its target, so it blasts through instead of stopping.")]
        public float overshoot = 1.5f;
        [Tooltip("Chance that a storm picks a prop as its next target.")]
        [Range(0f, 1f)] public float wreckChance = 0.7f;

        Camera cam;
        float baseSteering;
        readonly Collider[] hits = new Collider[64];

        void Start()
        {
            cam = Camera.main;
            if (wander != null) { wander.PickOverride = PickTarget; baseSteering = wander.steering; }
        }

        void Update()
        {
            if (fsm == null || wander == null) return;
            bool asleep = fsm.Energy == EnergyState.Sleep;
            float rampage = Rampage;
            float rage = look != null ? look.Rage : 0f;

            wander.Asleep = asleep;
            // standing still while awake = resting (energy slowly comes back, paid with fullness)
            fsm.Resting = !asleep && !wander.IsMoving && !wander.drag.IsHeld && wander.drag.Velocity.magnitude < 0.6f;
            float speed = Mathf.Lerp(1f, stormSpeed, rampage) * Mathf.Lerp(1f, 1.4f, rage);
            if (fsm.Energy == EnergyState.Tired && rampage < 0.5f) speed *= tiredSpeed;
            wander.SpeedMultiplier = speed;
            wander.RestScale = Mathf.Lerp(1f, 0f, rampage);                 // no resting in a rampage
            wander.steering = baseSteering * Mathf.Lerp(1f, stormSteering, rampage);

            // a storm costs energy (the temperature multiplier applies too)
            if (rampage > 0f)
                fsm.SpendEnergy(fsm.stats.config.stormEnergyDrain * rampage * Time.deltaTime * fsm.stats.config.timeScale);
        }

        /// <summary>
        /// Keeps a target where the pet can really get to: on screen and not under
        /// the desk. A target it can never reach would leave it stuck at the edge.
        /// </summary>
        Vector3 InReach(Vector3 p)
        {
            var drag = wander.drag;
            p.z = Mathf.Clamp(p.z, drag.minDepthZ, drag.maxDepthZ);
            Vector3 min = drag.ViewportToWorld(new Vector2(0.1f, 0f), p.z);
            Vector3 max = drag.ViewportToWorld(new Vector2(0.9f, 0.6f), p.z);
            p.x = Mathf.Clamp(p.x, min.x + drag.sidePadding, max.x - drag.sidePadding);
            p.y = Mathf.Clamp(p.y, Mathf.Max(min.y, drag.floorY), max.y);
            return p;
        }

        /// <summary>0-1: how wild the pet is. Only an Angry pet goes on a rampage.</summary>
        float Rampage => fsm != null && fsm.Mood == MoodState.Angry && fsm.Energy != EnergyState.Sleep && look != null
            && !wander.drag.Anchor.HasValue
            ? Mathf.InverseLerp(0.4f, 0.8f, look.Storm) : 0f;

        /// <summary>
        /// In a rampage the next target is a prop that is not caught yet and not
        /// too close, so the storm keeps dashing across the room. The target is set
        /// past the prop, so the pet blasts through it.
        /// </summary>
        Vector3? PickTarget()
        {
            if (Rampage < 0.5f || cam == null) return null;
            Vector3 here = transform.position;
            if (Random.value < wreckChance)
            {
                int n = Physics.OverlapSphereNonAlloc(here, 14f, hits, propLayers, QueryTriggerInteraction.Ignore);
                for (int tries = 0; tries < 12 && n > 0; tries++)
                {
                    var c = hits[Random.Range(0, n)];
                    var rb = c.attachedRigidbody;
                    if (rb == null || (storm != null && storm.IsCaught(rb))) continue;
                    Vector3 p = c.bounds.center + Vector3.down * 0.8f;   // feet a bit under the prop, so the body hits it
                    Vector2 flat = new Vector2(p.x - here.x, p.y - here.y);
                    if (Vector3.Distance(p, here) < 2.5f) continue;     // too close: it is probably already spinning around us
                    Vector3 vp = cam.WorldToViewportPoint(c.bounds.center);
                    if (vp.z <= 0f || vp.x < 0.08f || vp.x > 0.92f || vp.y < 0.03f || vp.y > 0.75f) continue;
                    return InReach(p + (Vector3)(flat.normalized * overshoot));   // keeps the prop's depth (shelves too)
                }
            }
            // no good prop: dash to a random far spot on screen
            for (int tries = 0; tries < 6; tries++)
            {
                float z = Random.Range(wander.drag.minDepthZ, wander.drag.maxDepthZ);
                var spot = wander.drag.ViewportToWorld(new Vector2(Random.Range(0.12f, 0.88f), Random.Range(0.05f, 0.55f)), z);
                if (Vector3.Distance(spot, here) > 3f) return InReach(spot);
            }
            return null;
        }
    }
}
