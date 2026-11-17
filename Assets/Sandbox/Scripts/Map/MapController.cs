using UnityEngine;
using UnityEngine.InputSystem;

namespace DamRush
{
    public enum FloodPhase { None, Warning, Rising, Peak, Falling }

    /// <summary>
    /// Main map controls: floods, water debug keys and the hovered hex (for the HUD).
    /// Flood v2 (plan v4): a snowmelt flood lasts about a week of game days.
    /// - Warning: a forecast one day before; the HUD shows its strength and which buildings may flood.
    ///   Nothing happens on its own: the leader must ring the alarm bell to send the family home.
    /// - Rising, then waves: the water comes in waves (high for half a day, lower for half a day),
    ///   fast and strong the whole week. Each wave that falls leaves duckweed, mud and driftwood.
    /// - Falling back to normal.
    /// Flood water turns muddy brown, because floods carry a lot of sediment.
    /// </summary>
    public class MapController : MonoBehaviour
    {
        public WaterGrid water;
        public HexGrid hex;
        public Camera cam;
        public WaterSource[] sources;
        public ColonyManager colony;

        [Header("Flood v2 (times in game days)")]
        [Tooltip("Sources give this many times their normal flow at the top of a wave.")]
        public float floodStrength = 15f;
        public float warningDays = 1f, riseDays = 0.5f, wavesDays = 6f, fallDays = 0.5f;
        [Tooltip("One wave: high water, then lower water, in game days.")]
        public float wavePeriodDays = 1f;
        [Tooltip("Flow between two waves, as a share of the top of a wave.")]
        public float waveLow = 0.45f;
        [Tooltip("The F key starts a short test flood of this many days of waves.")]
        public float testFloodWaveDays = 1f;
        float runWavesDays;

        [Tooltip("Set by the seasons: streams carry less water in winter.")]
        public float seasonFlow = 1f;

        float[] baseRates;
        float floodTime = -1f;
        float baselineLevel = float.NaN;

        public bool Flooding => floodTime >= 0f;
        public FloodPhase Phase { get; private set; }
        /// <summary>Seconds left in the current flood phase.</summary>
        public float PhaseLeft { get; private set; }
        /// <summary>Current flow multiplier of the sources.</summary>
        public float FlowFactor { get; private set; } = 1f;
        public HexCell Hovered { get; private set; }
        /// <summary>The water is near the top of a wave now.</summary>
        public bool WaveHigh { get; private set; }
        /// <summary>Called when a wave starts to fall (the flood leaves its gifts then).</summary>
        public event System.Action<float> WaveFell;
        float Day => colony != null && colony.dayNight != null ? colony.dayNight.dayLength : 240f;
        /// <summary>Game days left until the water is normal again.</summary>
        public float DaysLeft => Flooding ? Mathf.Max(0f, (warningDays + riseDays + runWavesDays + fallDays) * Day - floodTime) / Day : 0f;
        /// <summary>Water surface around the main lodge, and how far it is above the normal level.</summary>
        public float LodgeLevel { get; private set; }
        public float LodgeRise => float.IsNaN(baselineLevel) ? 0f : LodgeLevel - baselineLevel;

        void Start()
        {
            baseRates = new float[sources.Length];
            for (int i = 0; i < sources.Length; i++) baseRates[i] = sources[i].rate;
        }

        /// <summary>Test flood (F): short, so it can be tried quickly.</summary>
        public void StartFlood() => StartFlood(5f, "test flood", testFloodWaveDays);

        /// <summary>
        /// Starts a flood: one day of warning, then a week of waves of the given strength (x normal flow).
        /// No automatic alarm: the leader rings the bell.
        /// </summary>
        public void StartFlood(float strength, string reason, float waveDays = -1f)
        {
            if (Flooding) return;
            floodStrength = Mathf.Max(1f, strength);
            runWavesDays = waveDays > 0f ? waveDays : wavesDays;
            floodTime = 0f;
            if (colony != null)
                colony.Log($"FLOOD FORECAST: {reason}. It starts in {warningDays:0.#} day and lasts {riseDays + runWavesDays + fallDays:0.#} days. Ring the alarm bell before the waves!", true);
        }

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null)
            {
                if (kb.fKey.wasPressedThisFrame) StartFlood();
                if (kb.cKey.wasPressedThisFrame) water.colorMode = (WaterColorMode)(((int)water.colorMode + 1) % 3);
            }

            UpdateFlood(Time.deltaTime);
            for (int i = 0; i < sources.Length; i++) sources[i].rate = baseRates[i] * FlowFactor * seasonFlow;
            water.floodTint = Mathf.Clamp01((FlowFactor - 1f) / Mathf.Max(0.01f, floodStrength - 1f));
            MeasureLodgeLevel();

            Hovered = null;
            if (mouse != null && Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit h, 500f))
            {
                hex.TryGet(h.point, out var c);
                Hovered = c;
            }
        }

        void UpdateFlood(float dt)
        {
            if (!Flooding) { Phase = FloodPhase.None; FlowFactor = 1f; PhaseLeft = 0f; WaveHigh = false; return; }
            floodTime += dt;
            float D = Day, t = floodTime;
            float a = warningDays * D, b = a + riseDays * D, c = b + runWavesDays * D, d = c + fallDays * D;
            bool wasHigh = WaveHigh;
            if (t < a) { Phase = FloodPhase.Warning; FlowFactor = 1f; PhaseLeft = a - t; WaveHigh = false; }
            else if (t < b) { Phase = FloodPhase.Rising; FlowFactor = Mathf.Lerp(1f, floodStrength, Mathf.SmoothStep(0f, 1f, (t - a) / (b - a))); PhaseLeft = b - t; WaveHigh = true; }
            else if (t < c)
            {
                // waves: cosine between the top (1) and waveLow, starting at the top
                Phase = FloodPhase.Peak;
                float w = 0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * (t - b) / (wavePeriodDays * D));
                FlowFactor = Mathf.Max(1f, floodStrength * Mathf.Lerp(waveLow, 1f, w));
                WaveHigh = w > 0.5f;
                // next change between high and low water
                float inWave = Mathf.Repeat(t - b, wavePeriodDays * D);
                float half = wavePeriodDays * D * 0.25f;
                PhaseLeft = inWave < half ? half - inWave : inWave < 3f * half ? 3f * half - inWave : 5f * half - inWave;
                PhaseLeft = Mathf.Min(PhaseLeft, c - t);
            }
            else if (t < d) { Phase = FloodPhase.Falling; FlowFactor = Mathf.Lerp(floodStrength * waveLow, 1f, (t - c) / (d - c)); PhaseLeft = d - t; WaveHigh = false; }
            else
            {
                floodTime = -1f; Phase = FloodPhase.None; FlowFactor = 1f; WaveHigh = false;
                if (colony != null) colony.Log("The flood is over", true);
            }
            if (wasHigh && !WaveHigh && Phase != FloodPhase.None) WaveFell?.Invoke(floodStrength);
        }

        /// <summary>Mean water surface within 8 m of the lodge. The normal level follows it slowly when there is no flood.</summary>
        void MeasureLodgeLevel()
        {
            if (colony == null || colony.lodgeCells.Count == 0) return;
            Vector3 c = colony.LodgeCenter;
            float sum = 0f; int n = 0;
            for (float x = -8f; x <= 8f; x += 1f)
                for (float z = -8f; z <= 8f; z += 1f)
                {
                    float s = water.SurfaceAt(c.x + x, c.z + z, out bool wet);
                    if (wet && water.DepthAt(c.x + x, c.z + z) > 0.1f) { sum += s; n++; }
                }
            if (n == 0) return;
            LodgeLevel = sum / n;
            if (float.IsNaN(baselineLevel)) baselineLevel = LodgeLevel;
            else if (!Flooding) baselineLevel = Mathf.Lerp(baselineLevel, LodgeLevel, Time.deltaTime * 0.2f);
        }
    }
}
