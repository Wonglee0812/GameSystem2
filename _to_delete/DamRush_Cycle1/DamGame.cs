using UnityEngine;
using UnityEngine.SceneManagement;

namespace DamRush
{
    public enum Phase { Build, Flood, GameOver }

    /// <summary>
    /// The game loop (like TABS: build, then watch): a build phase with a timer,
    /// then a flood wave that ramps up, holds and fades. Each wave is bigger.
    /// The village on the low terrace loses health while water stands on it.
    /// </summary>
    public class DamGame : MonoBehaviour
    {
        public SandboxConfig config;
        public WaterGrid water;
        public ValleyTerrain terrain;
        public Transform partsRoot, debrisRoot;

        public Phase Phase { get; private set; } = Phase.Build;
        public int Wave { get; private set; } = 1;
        public int Budget { get; private set; }
        public float TimeLeft { get; private set; }
        public float VillageHealth { get; private set; }
        public float VillageDepth { get; private set; }
        public float WaveProgress { get; private set; }
        /// <summary>Deepest water on the village during the current or last wave (for playtests).</summary>
        public float PeakVillageDepth { get; private set; }

        public bool CanBuild => Phase == Phase.Build;
        public float PeakInflow(int wave) => config.firstPeakInflow + config.peakGrowth * (wave - 1);

        float waveTime;
        Collider groundCollider;

        void Awake()
        {
            PartFactory.Config = config;
            PartFactory.Water = water;
            PartFactory.PartsRoot = partsRoot;
            PartFactory.DebrisRoot = debrisRoot;
            PartFactory.ClearDebris();
            Part.ForgetWelds();
            Breakable.BrokenCount = 0;
            Breakable.JointsSnapped = 0;
            groundCollider = terrain.GetComponent<Collider>();
        }

        void Start()
        {
            Budget = config.startBudget;
            VillageHealth = config.villageMaxHealth;
            TimeLeft = config.buildTime;
        }

        public bool TrySpend(int cost)
        {
            if (cost > Budget) return false;
            Budget -= cost;
            return true;
        }

        public void Refund(int amount) { Budget += amount; }

        public void StartFlood()
        {
            if (Phase != Phase.Build) return;
            Phase = Phase.Flood;
            waveTime = 0f;
            PeakVillageDepth = 0f;
            Physics.SyncTransforms();
            var fresh = new System.Collections.Generic.List<Part>();
            foreach (var p in Part.All) if (!p.IsActive) fresh.Add(p);
            foreach (var p in fresh) p.Activate();
            foreach (var p in fresh) p.WeldToNeighbours(groundCollider);
        }

        public void Restart() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }

        void Update()
        {
            float dt = Time.deltaTime;
            switch (Phase)
            {
                case Phase.Build:
                    TimeLeft -= dt;
                    if (TimeLeft <= 0f) StartFlood();
                    break;

                case Phase.Flood:
                    waveTime += dt;
                    float ramp = config.waveRamp, hold = config.waveHold, calm = config.waveCalm;
                    float total = ramp * 2f + hold + calm;
                    float peak = PeakInflow(Wave), inflow;
                    if (waveTime < ramp) inflow = peak * waveTime / ramp;
                    else if (waveTime < ramp + hold) inflow = peak;
                    else if (waveTime < ramp * 2f + hold) inflow = peak * (1f - (waveTime - ramp - hold) / ramp);
                    else inflow = 0f;
                    water.extraInflow = inflow;
                    WaveProgress = Mathf.Clamp01(waveTime / total);
                    TimeLeft = total - waveTime;
                    if (waveTime >= total) EndWave();
                    break;
            }

            if (Phase != Phase.GameOver)
            {
                VillageDepth = water.AverageDepth(terrain.VillageRect);
                PeakVillageDepth = Mathf.Max(PeakVillageDepth, VillageDepth);
                float over = VillageDepth - config.villageSafeDepth;
                if (over > 0f) VillageHealth -= over * config.villageDamagePerMetre * dt;
                if (VillageHealth <= 0f)
                {
                    VillageHealth = 0f;
                    Phase = Phase.GameOver;
                    water.extraInflow = 0f;
                }
            }
        }

        void EndWave()
        {
            water.extraInflow = 0f;
            Wave++;
            Budget += config.budgetPerWave;
            TimeLeft = config.buildTime;
            Phase = Phase.Build;
        }
    }
}
