using UnityEngine;

namespace BrickPanic
{
    /// <summary>Shakes the ground. The ground is a kinematic Rigidbody moved with MovePosition,
    /// so bricks feel the motion through friction and contacts (no fake forces on the bricks).</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Earthquake : Disaster
    {
        [Tooltip("Strength: peak side acceleration of the ground in g (1 g = 9.81 m/s2). Strong real quakes are about 0.3-1 g. Default 0.5.")]
        public float peakAcceleration = 0.5f;
        [Tooltip("Shakes per second. Default 2.2.")]
        public float frequency = 2.2f;
        [Tooltip("Up-down move as a part of the side move (0.25 = 25%). Default 0.25.")]
        [Range(0f, 1f)] public float vertical = 0.25f;
        [Tooltip("How much random noise is mixed into the regular wave, 0-1. Default 0.5.")]
        [Range(0f, 1f)] public float noise = 0.5f;
        [Tooltip("The island leans more and more in one random direction, up to this angle in degrees at the end. Shaking + lean makes things slide to the edge. Default 6.")]
        public float leanMax = 6f;
        [Tooltip("Extra rocking of the island in degrees on top of the lean. Default 1.5.")]
        public float rockAngle = 1.5f;
        [Tooltip("Wear strength sent to DisasterState while the quake runs (1 = level 1). Default 1.")]
        public float wearStrength = 1f;
        [Tooltip("Seconds to grow from 0 to full strength. Default 3.")]
        public float rampUp = 3f;
        [Tooltip("Seconds to calm down at the end. Default 3.")]
        public float rampDown = 3f;
        [Tooltip("Total length of the quake in seconds. Default 20.")]
        public float duration = 20f;

        [Header("Per level (level 1 = the values above)")]
        [Tooltip("Extra peak acceleration per level above 1, as a part of the base (0.2 = +20% per level). Default 0.2.")]
        public float peakPerLevel = 0.2f;
        [Tooltip("Extra wear per level above 1, as a part of the base (0.25 = +25% per level). Default 0.25.")]
        public float wearPerLevel = 0.25f;
        [Tooltip("Extra seconds of shaking per level above 1. Default 2.")]
        public float durationPerLevel = 2f;

        float basePeak = -1f, baseWear, baseDuration;

        public override void SetLevel(int level)
        {
            if (basePeak < 0f) { basePeak = peakAcceleration; baseWear = wearStrength; baseDuration = duration; }
            int k = Mathf.Max(0, level - 1);
            peakAcceleration = basePeak * (1f + peakPerLevel * k);
            wearStrength = baseWear * (1f + wearPerLevel * k);
            duration = baseDuration + durationPerLevel * k;
        }

        Rigidbody body;
        Vector3 origin;
        Quaternion baseRotation;
        Vector3 leanAxis = Vector3.right;
        float time = -1f;

        public override bool Running => time >= 0f;
        public float Time01 => Running ? time / duration : 0f;

        void Reset() { displayName = "Earthquake"; }

        void Awake()
        {
            if (displayName == "Disaster") displayName = "Earthquake";
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            origin = body.position;
            baseRotation = body.rotation;
        }

        public override void Begin()
        {
            time = 0f;
            Vector2 d = Random.insideUnitCircle.normalized;
            leanAxis = new Vector3(d.x, 0f, d.y);   // the island tips around this axis
        }

        void OnDisable() { DisasterState.Intensity = 0f; }

        void FixedUpdate()
        {
            if (time < 0f) return;
            time += Time.fixedDeltaTime;
            if (time > duration)
            {
                time = -1f;
                DisasterState.Intensity = 0f;
                body.MovePosition(origin);
                body.MoveRotation(baseRotation);
                return;
            }

            float k = Mathf.Clamp01(time / rampUp) * Mathf.Clamp01((duration - time) / rampDown);
            float w = 2f * Mathf.PI * frequency;
            float f = time * frequency;
            float x = Mathf.Lerp(Mathf.Sin(w * time), Mathf.PerlinNoise(f, 0.37f) * 2f - 1f, noise);
            float z = Mathf.Lerp(Mathf.Sin(w * time * 1.31f + 1.7f), Mathf.PerlinNoise(5.1f, f) * 2f - 1f, noise);
            float y = Mathf.Sin(w * time * 0.77f) * vertical;
            float amplitude = peakAcceleration * 9.81f / (w * w); // a = A * w^2 for a sine wave
            body.MovePosition(origin + new Vector3(x, y, z) * (amplitude * k));

            // lean grows during the quake, then the island levels out again in the last rampDown seconds
            float lean = leanMax * Mathf.SmoothStep(0f, 1f, time / duration) * Mathf.Clamp01((duration - time) / rampDown);
            float rock = rockAngle * k * Mathf.Sin(w * 0.5f * time);
            body.MoveRotation(Quaternion.AngleAxis(lean + rock, leanAxis) * baseRotation);
            DisasterState.Intensity = k * wearStrength;
        }
    }
}
