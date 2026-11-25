using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>All "juice" of Brick Panic that is not a particle: camera shake, hit stop, slow motion and the sky
    /// tint during a disaster. Small, fast feedback that does not change the rules (Jonasson &amp; Purho, 2012).
    /// Camera shake uses "trauma" (Eiserloh, 2016): hits add trauma, the shake is trauma squared, trauma fades.
    /// The shake only adds a small rotation on top of the fly camera and removes it again before the camera moves,
    /// so it never fights FlyCamera. The master switch turns everything off for the juice ON / OFF play test.</summary>
    [DefaultExecutionOrder(-100)]
    public class BPJuice : MonoBehaviour
    {
        public static BPJuice I { get; private set; }
        /// <summary>False when the master switch is off (effects check this).</summary>
        public static bool On => I == null || I.juiceOn;

        [Header("Master")]
        [Tooltip("Off = no juice at all (shake, hit stop, slow motion, sky tint, brick-break particles, emotes, cracks). Default on.")]
        public bool juiceOn = true;

        [Header("Camera shake (trauma)")]
        [Tooltip("Largest camera tilt in degrees at full trauma. Default 1.6.")]
        public float shakeMaxAngle = 1.6f;
        [Tooltip("Largest camera move in metres at full trauma. Default 0.25.")]
        public float shakeMaxOffset = 0.25f;
        [Tooltip("Trauma lost per second. Default 1.5.")]
        public float traumaDecay = 1.5f;
        [Tooltip("Shake speed (noise frequency). Default 18.")]
        public float shakeFrequency = 18f;
        [Tooltip("Trauma that an earthquake keeps up at intensity 1. Default 0.6.")]
        public float quakeTrauma = 0.6f;
        [Tooltip("Trauma that a tsunami keeps up at intensity 1. Default 0.18.")]
        public float tsunamiTrauma = 0.18f;
        [Tooltip("Trauma that a volcano keeps up at intensity 1 (bombs add their own). Default 0.12.")]
        public float volcanoTrauma = 0.12f;

        [Header("Hit stop (big collapse)")]
        [Tooltip("Joint breaks within 0.25 s that count as a big collapse. Default 18.")]
        public int collapseJoints = 18;
        [Tooltip("Freeze length in real seconds. Default 0.07.")]
        public float hitStopTime = 0.07f;
        [Tooltip("Seconds before the next hit stop can happen. Default 1.5.")]
        public float hitStopCooldown = 1.5f;

        [Header("Slow motion")]
        [Tooltip("Time speed in slow motion. Default 0.4.")]
        [Range(0.1f, 1f)] public float slowScale = 0.4f;
        [Tooltip("Real seconds of slow motion when the survivors drop below the goal. Default 1.2.")]
        public float belowGoalSlow = 1.2f;
        [Tooltip("Real seconds of slow motion when the disaster ends (the last collapse). Default 0.9.")]
        public float lastCollapseSlow = 0.9f;

        [Header("Sky and sun during a disaster")]
        [Tooltip("How much the sky takes the disaster colour at full intensity, 0-1. Default 0.55.")]
        [Range(0f, 1f)] public float skyTint = 0.55f;
        [Tooltip("How much darker the sun gets at full intensity, 0-1. Default 0.3.")]
        [Range(0f, 1f)] public float sunDim = 0.3f;
        public Color quakeSky = new Color(1f, 0.7f, 0.4f);
        public Color tsunamiSky = new Color(0.4f, 0.5f, 0.8f);
        public Color volcanoSky = new Color(1f, 0.35f, 0.2f);

        [Header("Scene links (found when empty)")]
        public RoundManager round;
        public Light sun;

        float trauma, noiseSeed, stopUntil, stopReady, slowUntil, slowLength, baseFixed, baseSun, tint;
        Quaternion lastRot = Quaternion.identity;
        Vector3 lastPos;
        bool shaken;
        Transform cam;
        Material sky;
        readonly Queue<(float time, int count)> breaks = new Queue<(float, int)>();
        int lastJoints;
        RoundManager.State lastState;
        bool belowGoalShown;
        static readonly int TintId = Shader.PropertyToID("_Tint"), TintColorId = Shader.PropertyToID("_TintColor");

        void Awake()
        {
            I = this;
            noiseSeed = Random.value * 100f;
            baseFixed = Time.fixedDeltaTime;
        }

        void Start()
        {
            if (Camera.main != null) cam = Camera.main.transform;
            if (round == null) round = FindFirstObjectByType<RoundManager>();
            if (sun == null) foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun != null) baseSun = sun.intensity;
            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty(TintId))
            {
                sky = new Material(RenderSettings.skybox);   // runtime copy: the asset is never changed
                RenderSettings.skybox = sky;
            }
            lastJoints = Block.JointsBroken;
            if (round != null) lastState = round.Current;
        }

        void OnDisable()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = baseFixed > 0f ? baseFixed : Time.fixedDeltaTime;
            RemoveShake();
            if (sun != null && baseSun > 0f) sun.intensity = baseSun;
            if (sky != null) sky.SetFloat(TintId, 0f);
        }

        void OnDestroy() { if (I == this) I = null; }

        /// <summary>Adds camera shake. With a position, the shake gets weaker with distance (full within 20 m, none past 90 m).</summary>
        public static void AddTrauma(float amount, Vector3? at = null)
        {
            if (I == null || !I.juiceOn) return;
            if (at.HasValue && I.cam != null)
            {
                float d = Vector3.Distance(at.Value, I.cam.position);
                amount *= 1f - Mathf.InverseLerp(20f, 90f, d);
            }
            I.trauma = Mathf.Clamp01(I.trauma + amount);
        }

        /// <summary>A short freeze (real seconds).</summary>
        public static void HitStop(float seconds)
        {
            if (I == null || !I.juiceOn || Time.unscaledTime < I.stopReady) return;
            I.stopUntil = Time.unscaledTime + seconds;
            I.stopReady = Time.unscaledTime + I.hitStopCooldown;
        }

        /// <summary>Slow motion for some real seconds (eases back to normal speed at the end).</summary>
        public static void SlowMotion(float seconds)
        {
            if (I == null || !I.juiceOn) return;
            I.slowLength = seconds;
            I.slowUntil = Time.unscaledTime + seconds;
        }

        void Update()
        {
            RemoveShake();   // before FlyCamera moves the camera
            if (round == null) return;
            float now = Time.unscaledTime;

            // big collapse: many joints break at once -> hit stop + trauma
            int j = Block.JointsBroken;
            if (j > lastJoints) breaks.Enqueue((now, j - lastJoints));
            lastJoints = j;
            int recent = 0;
            while (breaks.Count > 0 && now - breaks.Peek().time > 0.25f) breaks.Dequeue();
            foreach (var b in breaks) recent += b.count;
            if (juiceOn && recent >= collapseJoints && now >= stopReady)
            {
                HitStop(hitStopTime);
                AddTrauma(0.35f);
                breaks.Clear();
            }

            // phase events
            var st = round.Current;
            if (st != lastState)
            {
                if (st == RoundManager.State.Disaster) { AddTrauma(0.35f); belowGoalShown = false; }
                if (st == RoundManager.State.Settle && lastState == RoundManager.State.Disaster) SlowMotion(lastCollapseSlow);
                lastState = st;
            }
            if (st == RoundManager.State.Disaster && !belowGoalShown && Person.AliveCount < round.Goal && round.AliveAtStart >= round.Goal)
            {
                belowGoalShown = true;
                SlowMotion(belowGoalSlow);
                AddTrauma(0.3f);
            }

            // disaster strength: shake floor, sky tint, sun
            float intensity = st == RoundManager.State.Disaster ? Mathf.Clamp01(DisasterState.Intensity) : 0f;
            var d = round.Next;
            if (juiceOn && intensity > 0f)
            {
                float floor = intensity * (d is Earthquake ? quakeTrauma : d is Tsunami ? tsunamiTrauma : volcanoTrauma);
                if (trauma < floor) trauma = floor;
            }
            float target = juiceOn ? intensity * skyTint : 0f;
            tint = Mathf.MoveTowards(tint, target, Time.unscaledDeltaTime * 0.8f);
            if (sky != null)
            {
                sky.SetFloat(TintId, tint);
                sky.SetColor(TintColorId, d is Earthquake ? quakeSky : d is Tsunami ? tsunamiSky : volcanoSky);
            }
            if (sun != null && baseSun > 0f) sun.intensity = baseSun * (1f - sunDim * (skyTint > 0f ? tint / skyTint : 0f));

            // time: hit stop beats slow motion; physics steps scale with time, so slow motion stays smooth
            float scale = 1f;
            if (juiceOn && now < stopUntil) scale = 0.05f;
            else if (juiceOn && now < slowUntil)
            {
                float left = (slowUntil - now) / Mathf.Max(0.01f, slowLength);
                scale = Mathf.Lerp(1f, slowScale, Mathf.Clamp01(left * 3f));   // last third eases back to normal
            }
            Time.timeScale = scale;
            Time.fixedDeltaTime = baseFixed * (scale < 0.1f ? 1f : scale);   // a hit stop is a real freeze (no steps)
        }

        void LateUpdate()
        {
            trauma = Mathf.Max(0f, trauma - traumaDecay * Time.unscaledDeltaTime);
            if (cam == null || !juiceOn || trauma <= 0f) return;
            float s = trauma * trauma;
            float t = Time.unscaledTime * shakeFrequency;
            float N(float o) => Mathf.PerlinNoise(noiseSeed + o, t) * 2f - 1f;
            lastRot = Quaternion.Euler(shakeMaxAngle * s * N(0f), shakeMaxAngle * s * N(1f), shakeMaxAngle * 0.5f * s * N(2f));
            lastPos = new Vector3(N(3f), N(4f), N(5f)) * (shakeMaxOffset * s);
            cam.rotation = cam.rotation * lastRot;
            cam.position += lastPos;
            shaken = true;
        }

        void RemoveShake()
        {
            if (cam == null) return;
            if (!shaken) return;
            // (not "lastRot != identity": Unity's == treats tiny rotations as equal, so small shakes would never be removed and the camera would drift)
            cam.rotation = cam.rotation * Quaternion.Inverse(lastRot);
            cam.position -= lastPos;
            shaken = false;
            lastRot = Quaternion.identity;
            lastPos = Vector3.zero;
        }
    }
}
