using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Makes the whirlwind bend like a soft column of air instead of a stiff stick.
    /// When the pet moves sideways, the middle follows and both ends trail behind
    /// (a C-shape). The bend runs on a spring with low damping, so the ends keep
    /// swinging for a moment after the pet stops (secondary motion).
    /// WindVortex and WindArcs read OffsetAt() to move their particles and arcs.
    /// </summary>
    public class PetBend : MonoBehaviour
    {
        public PetDrag drag;

        [Tooltip("How far the ends trail behind per unit of speed.")]
        public float bendPerSpeed = 0.06f;
        [Tooltip("Maximum sideways offset of the ends, in metres.")]
        public float maxBend = 0.45f;

        [Header("Spring")]
        public float stiffness = 60f;
        [Tooltip("Lower = the ends keep swinging longer after a stop.")]
        public float damping = 5f;

        [Header("Shape")]
        [Tooltip("Height (0-1) that stays in place, usually where the player grabs.")]
        [Range(0f, 1f)] public float anchorHeight = 0.5f;
        [Tooltip("Small idle sway so the pet never looks frozen.")]
        public float idleSway = 0.05f;
        public float idleSwaySpeed = 1.2f;

        /// <summary>Current bend in metres. Positive = ends to the right.</summary>
        public float Bend { get; private set; }

        /// <summary>Pet velocity, smoothed so trailing layers move without jumps.</summary>
        public Vector3 SmoothedVelocity { get; private set; }
        [Tooltip("How quickly SmoothedVelocity follows the real velocity.")]
        public float velocitySmoothing = 6f;

        float bendVelocity;

        void Update()
        {
            float dt = Time.deltaTime;
            float target = 0f;
            if (drag != null)
            {
                target = Mathf.Clamp(-drag.Velocity.x * bendPerSpeed, -maxBend, maxBend);
                SmoothedVelocity = Vector3.Lerp(SmoothedVelocity, drag.Velocity, 1f - Mathf.Exp(-velocitySmoothing * dt));
            }

            float force = (target - Bend) * stiffness - bendVelocity * damping;
            bendVelocity += force * dt;
            Bend += bendVelocity * dt;
            // The spring may overshoot; never let it go far past the limit.
            Bend = Mathf.Clamp(Bend, -maxBend * 1.3f, maxBend * 1.3f);
        }

        /// <summary>Sideways offset for a point at height01 (0 = bottom, 1 = top).</summary>
        public Vector3 OffsetAt(float height01)
        {
            float fromAnchor = (height01 - anchorHeight) / Mathf.Max(anchorHeight, 1f - anchorHeight);
            float shape = fromAnchor * fromAnchor;           // 0 at the anchor, 1 at the ends
            float sway = Mathf.Sin(Time.time * idleSwaySpeed + height01 * 3f) * idleSway * shape;
            return new Vector3(Bend * shape + sway, 0f, 0f);
        }
    }
}
