using UnityEngine;

namespace DamRush
{
    /// <summary>How the water loses speed on the river bed.</summary>
    public enum FrictionMode
    {
        /// <summary>Old method: every pipe flow is multiplied by a fixed factor each step.</summary>
        FluxDamping,
        /// <summary>Manning bed friction: shallow and fast water slows down more (used in flood models).</summary>
        Manning,
    }

    /// <summary>
    /// All tuning numbers for Dam Rush in one asset, so balance changes need no code.
    /// Units are metres, seconds, kilograms and cubic metres per second.
    /// </summary>
    [CreateAssetMenu(menuName = "Dam Rush/Sandbox Config")]
    public class SandboxConfig : ScriptableObject
    {
        [Header("Valley grid")]
        public int cellsX = 80;          // along the river
        public int cellsZ = 48;          // across the river
        public float cellSize = 0.5f;
        public float bankHeight = 3f;    // height of the valley sides above the river bed
        public float channelHalfWidth = 3f;
        public float bankSlope = 3f;     // horizontal distance of the bank slope
        public float riverDrop = 0.6f;   // the bed drops this much from source to mouth
        public int terrainSeed = 7;
        [Tooltip("Channel width at the mouth compared to the normal width.")]
        public float gorgeWidthFactor = 0.25f;
        [Tooltip("Height of the village terrace above the river bed.")]
        public float villageTerraceHeight = 0.6f;

        [Header("Water (virtual pipes, Mei et al., 2007)")]
        public float gravity = 9.81f;
        public float pipeArea = 0.25f;   // cross-section of a virtual pipe (m2)
        [Tooltip("Pipe cross-section = flow depth x cell width (de Almeida et al., 2012) instead of the fixed pipe area. " +
                 "With a fixed area, shallow water speeds up far too much and runs off thin and fast.")]
        public bool depthScaledPipes = false;
        public float fluxDamping = 0.993f;   // only used with FrictionMode.FluxDamping
        public FrictionMode friction = FrictionMode.Manning;
        [Tooltip("Manning roughness n (s/m^(1/3)). Smooth channel ~0.015, natural stream ~0.035, weedy ~0.07.")]
        public float manningN = 0.035f;
        [Tooltip("When a part lands in a wet cell, push its volume of water to the neighbours (keeps water volume).")]
        public bool displaceWater = true;
        [Tooltip("How fast water soaks through a fully leaky structure (m/s per m of height difference).")]
        public float seepRate = 0.4f;
        [Tooltip("Water leaves the map over the far x edge.")]
        public bool openEndX = true;
        public int substeps = 2;         // water steps per FixedUpdate
        [Tooltip("Run the water only every N physics steps (with an N times longer step) to save CPU on big maps.")]
        public int tickEvery = 1;
        public float baseInflow = 1.2f;  // normal river flow (m3/s)
        public float obstacleRefresh = 0.1f; // seconds between block rasterisations

        [Header("Flood waves")]
        public float buildTime = 75f;      // seconds to build before the flood starts by itself
        public float firstPeakInflow = 7f; // peak flow of wave 1 (m3/s)
        public float peakGrowth = 3.5f;    // added to the peak each wave
        public float waveRamp = 4f;        // seconds to reach the peak
        public float waveHold = 18f;       // seconds at the peak
        public float waveCalm = 14f;       // seconds after the inflow drops before the next build phase

        [Header("Village")]
        public float villageMaxHealth = 100f;
        public float villageSafeDepth = 0.15f; // water deeper than this on the village hurts it
        public float villageDamagePerMetre = 18f; // damage per second per metre above safe depth

        [Header("Building")]
        public int startBudget = 120;
        public int budgetPerWave = 50;
        public float snapStep = 0.5f;
        public float snapAngle = 15f;
        public int maxDebris = 250;
        public float debrisLifetime = 40f;

        [Header("Water forces on blocks")]
        public float waterDensity = 1000f;
        [Tooltip("Game tuning: share of the real side pressure that is applied (1 = real physics).")]
        public float pressureScale = 1f;
        public float dragCoefficient = 1.1f;
        public float waterLinearDamping = 0.6f;
        public float waterAngularDamping = 0.8f;
        [Tooltip("Size of the surface patches used to measure water pressure on a part (m).")]
        public float probeStep = 0.25f;
    }
}
