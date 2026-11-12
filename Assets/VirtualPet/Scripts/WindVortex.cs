using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Shapes a particle system into a small whirlwind. Each particle rises from
    /// the bottom to the top over its lifetime and circles the centre. The radius
    /// at each height comes from a curve: thin at both ends, wide in the middle.
    /// Other scripts can change spin speed, width and height at runtime, so the
    /// body can react to the pet's state.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(ParticleSystem))]
    public class WindVortex : MonoBehaviour
    {
        /// <summary>RiseFromBottom: particles travel up the column.
        /// OutFromCore: particles bloom out from the energy core to the body's edge.</summary>
        public enum Flow { RiseFromBottom, OutFromCore }

        public Flow flow = Flow.RiseFromBottom;

        [Tooltip("Height of the energy core (0 = bottom, 1 = top). Used by OutFromCore.")]
        [Range(0f, 1f)] public float coreHeight = 0.45f;

        [Tooltip("Height of the whirlwind in metres.")]
        public float height = 2.2f;

        [Tooltip("Radius along the height. X = 0 bottom, X = 1 top.")]
        public AnimationCurve radiusProfile = new AnimationCurve(
            new Keyframe(0f, 0.04f),
            new Keyframe(0.45f, 0.6f),
            new Keyframe(1f, 0.1f));

        public float radiusScale = 1f;

        [Tooltip("Degrees per second around the centre.")]
        public float spinSpeed = 300f;

        [Tooltip("Random spread around the curve, so the edge looks soft.")]
        [Range(0f, 0.5f)] public float radiusJitter = 0.2f;

        [Tooltip("Small side-to-side sway of the whole column.")]
        public float swayAmount = 0.08f;
        [Tooltip("Optional. Bends the column when the pet moves.")]
        public PetBend bend;
        [Tooltip("Multiplies the bend for this layer.")]
        public float bendStrength = 1f;
        [Tooltip("Older particles are left behind when the pet moves (seconds of lag at the end of life).")]
        public float motionTrail = 0f;
        [Tooltip("Maximum distance a particle can be left behind, in metres.")]
        public float maxTrail = 0.6f;
        public float swaySpeed = 1.3f;

        [Header("Thinning (set by WindPhase)")]
        [Tooltip("1 = normal. Above 1 the particles drift away from the body.")]
        public float spread = 1f;
        [Tooltip("Extra random scatter in metres, so the shape breaks up.")]
        public float scatter = 0f;
        [Range(0f, 1f)] public float fade = 1f;
        [Tooltip("Speed multiplier, set by the pet's behaviour.")]
        public float spinMultiplier = 1f;

        ParticleSystem ps;
        ParticleSystem.Particle[] buffer;

        void OnEnable()
        {
            ps = GetComponent<ParticleSystem>();
        }

        void LateUpdate()
        {
            if (ps == null) return;
            int max = ps.main.maxParticles;
            if (buffer == null || buffer.Length < max) buffer = new ParticleSystem.Particle[max];

            int count = ps.GetParticles(buffer);
            float time = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;

            for (int i = 0; i < count; i++)
            {
                var p = buffer[i];
                float age01 = 1f - p.remainingLifetime / p.startLifetime;
                float lived = p.startLifetime - p.remainingLifetime;

                // Stable random values per particle from its seed.
                float seedA = (p.randomSeed % 1000) / 1000f;
                float seedB = ((p.randomSeed / 1000) % 1000) / 1000f;

                float seedC = ((p.randomSeed / 1000000) % 1000) / 1000f;
                float angle = (seedA * 360f + lived * spinSpeed * spinMultiplier) * Mathf.Deg2Rad;
                float jitter = 1f - radiusJitter + 2f * radiusJitter * seedB;
                float height01;
                float radius;

                if (flow == Flow.OutFromCore)
                {
                    // Start in the core, ease out towards a random point on the body.
                    float targetHeight = Mathf.Lerp(0.08f, 0.92f, seedC);
                    float ease = 1f - (1f - age01) * (1f - age01);
                    height01 = Mathf.Lerp(coreHeight, targetHeight, ease);
                    radius = radiusProfile.Evaluate(height01) * radiusScale * jitter * ease;
                }
                else
                {
                    height01 = age01;
                    radius = radiusProfile.Evaluate(age01) * radiusScale * jitter;
                }

                float y = height01 * height;

                // The upper part sways more than the base, like a real whirlwind.
                float sway = Mathf.Sin(time * swaySpeed + y * 1.5f) * swayAmount * height01;

                radius *= spread;
                p.position = new Vector3(Mathf.Cos(angle) * radius + sway, y, Mathf.Sin(angle) * radius);
                if (scatter > 0f)
                {
                    // each particle drifts on its own slow wave, so the body tears into wisps
                    float t2 = time * 2.3f + seedA * 40f;
                    p.position += new Vector3(Mathf.Sin(t2) * (seedB - 0.5f), Mathf.Cos(t2 * 0.7f) * (seedC - 0.5f), Mathf.Sin(t2 * 1.3f) * (seedA - 0.5f)) * (2f * scatter);
                }
                Color32 c = p.startColor;
                c.a = (byte)(255f * fade);
                p.startColor = c;
                if (bend != null)
                {
                    p.position += bend.OffsetAt(height01) * bendStrength;
                    if (motionTrail > 0f)
                    {
                        Vector3 trail = -bend.SmoothedVelocity * motionTrail * age01;
                        p.position += Vector3.ClampMagnitude(new Vector3(trail.x, trail.y, 0f), maxTrail);
                    }
                }

                // Velocity along the circle. Not used for movement (we set the
                // position ourselves), but a Stretched Billboard renderer uses it
                // to draw curved wind streaks.
                float speed = spinSpeed * Mathf.Deg2Rad * radius;
                p.velocity = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * speed;
                buffer[i] = p;
            }

            ps.SetParticles(buffer, count);
        }
    }
}
