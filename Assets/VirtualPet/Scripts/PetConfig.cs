using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// All tuning values for the pet's four bars in one asset, so balance can be
    /// changed in the Inspector without touching code. All bars run from 0 to 100.
    /// Rates are per second of game time.
    /// </summary>
    [CreateAssetMenu(menuName = "Wind Pet/Pet Config", fileName = "PetConfig")]
    public class PetConfig : ScriptableObject
    {
        [Header("Start values (0-100)")]
        public float startEnergy = 80f;
        [Tooltip("100 = completely full, 0 = starving.")]
        public float startFullness = 70f;
        public float startMood = 50f;
        public float startTemperature = 50f;
        public int startMoney = 20;

        [Header("Energy bands")]
        public float sleepBelow = 10f;
        public float tiredBelow = 40f;
        public float energeticAbove = 75f;
        [Tooltip("A sleeping pet only wakes up again above this energy.")]
        public float wakeAbove = 40f;
        [Tooltip("...and only when its mood is above this, so it never wakes up straight into a storm.")]
        public float wakeMoodAbove = 40f;

        [Header("Mood bands (Normal is the middle)")]
        public float angryBelow = 10f;
        public float sadBelow = 35f;
        public float happyAbove = 75f;

        [Header("Fullness bands")]
        public float hungryBelow = 40f;

        [Header("Temperature bands")]
        public float coldBelow = 35f;
        public float hotAbove = 65f;

        [Header("Band switching")]
        [Tooltip("A value must cross a border by this much before the band changes, so states do not flicker.")]
        public float bandMargin = 2f;

        [Header("Fullness over time")]
        [Tooltip("Fullness lost every second, always (awake or asleep), like a real stomach.")]
        public float fullnessDecay = 0.12f;

        [Header("Energy <-> Fullness")]
        [Tooltip("Fullness used up for every point of energy that is restored.")]
        public float fullnessPerEnergy = 2f;
        [Tooltip("Energy restored per second while the pet sleeps. Sleep is free: no fullness is used.")]
        public float sleepRegen = 1f;
        [Tooltip("Mood gained per second while asleep (a nap calms the pet down).")]
        public float sleepMoodGain = 1f;
        [Tooltip("Energy restored per second while awake and resting. Paid with fullness. 0 = off.")]
        public float restRegen = 0.12f;
        [Tooltip("Resting restores energy up to this level (above Energetic, so a fed pet can become happy).")]
        public float restRegenUntil = 90f;
        [Tooltip("Energy restored per second by digestion when the stomach is full, awake and doing anything " +
                 "(also working). It scales down to 0 at the Hungry border. Paid with fullness like resting.")]
        public float fedRegen = 0.3f;

        [Header("Eating")]
        [Tooltip("Mood gained for every food puff eaten: feeding cheers the pet up.")]
        public float eatMoodBonus = 5f;

        [Header("Storm")]
        [Tooltip("Energy used per second by a full storm, so an angry pet tires itself out.")]
        public float stormEnergyDrain = 0.8f;

        [Header("Temperature -> energy cost")]
        [Tooltip("Multiplier on every energy cost: cold wind burns more energy, warm wind less.")]
        public float coldCostMultiplier = 1.5f;
        public float normalCostMultiplier = 1f;
        public float hotCostMultiplier = 0.7f;

        [Header("Mood rules (points per second)")]
        public float energeticAndFullMood = 0.6f;
        public float hungryMood = -0.3f;
        public float tiredMood = -0.2f;
        [Tooltip("Extra drop when the pet is hungry AND tired at the same time.")]
        public float hungryAndTiredExtra = -0.2f;
        [Tooltip("When no rule is active, mood slowly moves back to Normal (50).")]
        public float moodDriftToNormal = 0.15f;

        [Header("Debug")]
        [Tooltip("Speeds up the whole simulation for testing.")]
        public float timeScale = 1f;
    }
}
