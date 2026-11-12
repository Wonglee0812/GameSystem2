using System;
using UnityEngine;

namespace WindPet
{
    public enum EnergyState { Sleep, Tired, Normal, Energetic }
    public enum MoodState { Angry, Sad, Normal, Happy }
    public enum HungerState { Hungry, Full }
    public enum TempState { Cold, Normal, Hot }

    /// <summary>
    /// Four small state machines that run side by side, one per bar (concurrent
    /// state machines). Each one only knows which band its bar is in. Keeping
    /// them separate avoids one huge machine with 4 x 4 x 2 x 3 = 96 states.
    ///
    /// Fullness drains slowly by itself (a stomach empties over time). Apart from
    /// that, the bars affect each other:
    /// - restoring energy while awake uses up extra fullness (1 energy costs 2 fullness);
    /// - temperature changes how much energy every action costs;
    /// - energy and fullness push the mood up or down.
    /// Behaviour per state (sleeping, slow flying, angry wind) comes later and
    /// will listen to the events below.
    /// </summary>
    public class PetStateMachine : MonoBehaviour
    {
        public PetStats stats;

        public EnergyState Energy { get; private set; } = EnergyState.Normal;
        public MoodState Mood { get; private set; } = MoodState.Normal;
        public HungerState Hunger { get; private set; } = HungerState.Full;
        public TempState Temp { get; private set; } = TempState.Normal;

        /// <summary>Mood change per second right now, and which rules caused it (for the debug panel).</summary>
        /// <summary>Set by the behaviour: the pet is awake and standing still.</summary>
        public bool Resting { get; set; }
        public float MoodRate { get; private set; }
        public string MoodReason { get; private set; } = "";

        // old band, new band
        public event Action<EnergyState, EnergyState> EnergyChanged;
        public event Action<MoodState, MoodState> MoodChanged;
        public event Action<HungerState, HungerState> HungerChanged;
        public event Action<TempState, TempState> TempChanged;

        PetConfig Config => stats.config;
        public float DeltaTime => Time.deltaTime * Config.timeScale;

        void Start() => UpdateBands(true);

        void Update()
        {
            float dt = DeltaTime;
            // the stomach empties slowly all the time, awake or asleep
            stats.ChangeFullness(-Config.fullnessDecay * dt);
            if (Energy == EnergyState.Sleep)
            {
                // sleeping is free: energy and mood come back, fullness is not used
                stats.ChangeEnergy(Config.sleepRegen * dt);
                stats.ChangeMood(Config.sleepMoodGain * dt);
                MoodRate = Config.sleepMoodGain;
                MoodReason = "asleep";
            }
            else
            {
                // awake: a fed pet digests its food into energy all the time, more when the stomach is
                // fuller; resting adds a little more. Both are paid with fullness (1 energy = 2 fullness)
                if (stats.Energy < Config.restRegenUntil)
                {
                    float fed = Mathf.Clamp01((stats.Fullness - Config.hungryBelow) / Mathf.Max(1f, 100f - Config.hungryBelow));
                    float regen = Config.fedRegen * fed + (Resting ? Config.restRegen : 0f);
                    if (regen > 0f) RestoreEnergy(regen * dt);
                }
                ApplyMoodRules(dt);
            }
            UpdateBands(false);
        }

        // ---------- Rules between the bars ----------

        /// <summary>
        /// Restores energy, paid with fullness (Config.fullnessPerEnergy per point).
        /// A pet with an empty stomach cannot recover. Returns the energy restored.
        /// </summary>
        public float RestoreEnergy(float amount)
        {
            float room = 100f - stats.Energy;
            float affordable = stats.Fullness / Config.fullnessPerEnergy;
            float restored = Mathf.Max(0f, Mathf.Min(amount, room, affordable));
            if (restored <= 0f) return 0f;
            stats.ChangeEnergy(restored);
            stats.ChangeFullness(-restored * Config.fullnessPerEnergy);
            return restored;
        }

        /// <summary>
        /// Spends energy for an action. The real cost depends on temperature:
        /// a cold pet burns more, a warm pet less. Returns the energy spent.
        /// </summary>
        public float SpendEnergy(float baseCost)
        {
            float cost = baseCost * CostMultiplier;
            stats.ChangeEnergy(-cost);
            return cost;
        }

        public float CostMultiplier =>
            Temp == TempState.Cold ? Config.coldCostMultiplier :
            Temp == TempState.Hot ? Config.hotCostMultiplier : Config.normalCostMultiplier;

        void ApplyMoodRules(float dt)
        {
            bool hungry = Hunger == HungerState.Hungry;
            bool tired = Energy == EnergyState.Tired;          // a sleeping pet does not feel tired
            float rate = 0f;
            string reason = "";

            if (Energy == EnergyState.Energetic && !hungry) { rate += Config.energeticAndFullMood; reason += "energetic + full, "; }
            if (hungry) { rate += Config.hungryMood; reason += "hungry, "; }
            if (tired) { rate += Config.tiredMood; reason += "tired, "; }
            if (hungry && tired) { rate += Config.hungryAndTiredExtra; reason += "hungry AND tired, "; }

            if (reason.Length == 0)
            {
                // nothing pushes the mood: drift slowly back to Normal (50)
                float toNormal = 50f - stats.Mood;
                rate = Mathf.Clamp(toNormal / Mathf.Max(dt, 0.0001f), -Config.moodDriftToNormal, Config.moodDriftToNormal);
                reason = "drift to normal";
            }
            else reason = reason.TrimEnd(',', ' ');

            MoodRate = rate;
            MoodReason = reason;
            stats.ChangeMood(rate * dt);
        }

        // ---------- The four band machines ----------

        void UpdateBands(bool force)
        {
            var c = Config;
            float m = force ? 0f : c.bandMargin;

            var e = (EnergyState)Band(stats.Energy, (int)Energy, m, c.sleepBelow, c.tiredBelow, c.energeticAbove);
            // a sleeping pet keeps sleeping until it has rested enough
            // and has calmed down (out of Angry), so it never wakes into a storm
            if (!force && Energy == EnergyState.Sleep && (stats.Energy < c.wakeAbove || stats.Mood < c.wakeMoodAbove))
                e = EnergyState.Sleep;
            if (force) Energy = e; else if (e != Energy) { var old = Energy; Energy = e; EnergyChanged?.Invoke(old, e); }

            var mo = (MoodState)Band(stats.Mood, (int)Mood, m, c.angryBelow, c.sadBelow, c.happyAbove);
            if (force) Mood = mo; else if (mo != Mood) { var old = Mood; Mood = mo; MoodChanged?.Invoke(old, mo); }

            var h = (HungerState)Band(stats.Fullness, (int)Hunger, m, c.hungryBelow);
            if (force) Hunger = h; else if (h != Hunger) { var old = Hunger; Hunger = h; HungerChanged?.Invoke(old, h); }

            var t = (TempState)Band(stats.Temperature, (int)Temp, m, c.coldBelow, c.hotAbove);
            if (force) Temp = t; else if (t != Temp) { var old = Temp; Temp = t; TempChanged?.Invoke(old, t); }
        }

        /// <summary>
        /// Which band a value is in. Borders are in rising order: band 0 is below
        /// borders[0], band 1 is between borders[0] and borders[1], and so on.
        /// The value must pass a border by 'margin' before the band changes.
        /// </summary>
        static int Band(float value, int current, float margin, params float[] borders)
        {
            int raw = 0;
            while (raw < borders.Length && value >= borders[raw]) raw++;
            int b = raw;
            // going up: each border must be passed by the margin
            while (b > current && value < borders[b - 1] + margin) b--;
            // going down: same, the other way
            while (b < current && value > borders[b] - margin) b++;
            return b;
        }
    }
}
