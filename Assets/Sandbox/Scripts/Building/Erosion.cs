using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Specific rule for mud: flowing water washes it away. The part shrinks with the
    /// flow speed and how deep it sits in the water, and crumbles when little is left.
    /// Mud is the strongest glue, but it does not last.
    /// </summary>
    [RequireComponent(typeof(Part), typeof(WaterBody))]
    public class Erosion : MonoBehaviour
    {
        const float CrumbleAt = 0.35f;
        public float Remaining { get; private set; } = 1f;

        Part part;
        WaterBody wb;
        Vector3 startScale;
        float startMass, startVolume;

        void Awake()
        {
            part = GetComponent<Part>();
            wb = GetComponent<WaterBody>();
            startScale = transform.localScale;
        }

        void Start()
        {
            startMass = part.Body.mass;
            startVolume = wb.volume;
        }

        void FixedUpdate()
        {
            if (!part.IsActive || wb.Submerged < 0.05f) return;
            float wear = part.material.erosionPerSecond * Mathf.Max(0.15f, wb.FlowSpeed) * wb.Submerged * Time.fixedDeltaTime;
            Remaining -= wear;
            if (Remaining <= CrumbleAt)
            {
                PartFactory.Shatter(part);
                return;
            }
            float linear = Mathf.Pow(Remaining, 1f / 3f);
            transform.localScale = startScale * linear;
            part.Body.mass = startMass * Remaining;
            wb.volume = startVolume * Remaining;
        }
    }
}
