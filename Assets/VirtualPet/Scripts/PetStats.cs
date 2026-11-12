using System;
using UnityEngine;

namespace WindPet
{
    public enum PetBar { Energy, Fullness, Mood }

    /// <summary>
    /// The pet's four bars, each kept between 0 and 100. They are only changed
    /// through the methods below, by the rules in PetStateMachine (fullness slowly
    /// draining, bars affecting each other) and by actions (feeding, moving, storms).
    /// </summary>
    public class PetStats : MonoBehaviour
    {
        public PetConfig config;

        public float Energy { get; private set; }
        /// <summary>100 = full, 0 = starving.</summary>
        public float Fullness { get; private set; }
        public float Mood { get; private set; }
        public float Temperature { get; private set; }

        /// <summary>Raised after any bar changes.</summary>
        public event Action Changed;

        void Awake() => ResetToStart();

        public void ResetToStart()
        {
            Energy = config.startEnergy;
            Fullness = config.startFullness;
            Mood = config.startMood;
            Temperature = config.startTemperature;
            Changed?.Invoke();
        }

        public void ChangeEnergy(float amount) { Energy = Clamp(Energy + Boost(PetBar.Energy, amount)); Changed?.Invoke(); }
        public void ChangeFullness(float amount) { Fullness = Clamp(Fullness + Boost(PetBar.Fullness, amount)); Changed?.Invoke(); }
        public void ChangeMood(float amount) { Mood = Clamp(Mood + Boost(PetBar.Mood, amount)); Changed?.Invoke(); }

        // ---------- timed potion buffs: gains of one bar are multiplied for a while ----------

        readonly float[] buffEnd = new float[3];
        readonly float[] buffMul = { 1f, 1f, 1f };

        public void AddBuff(PetBar bar, float multiplier, float seconds)
        {
            buffMul[(int)bar] = multiplier;
            buffEnd[(int)bar] = Time.time + seconds;
        }

        /// <summary>Seconds left on the buff of this bar (0 = none).</summary>
        public float BuffLeft(PetBar bar) => Mathf.Max(0f, buffEnd[(int)bar] - Time.time);
        public float BuffMultiplier(PetBar bar) => BuffLeft(bar) > 0f ? buffMul[(int)bar] : 1f;

        float Boost(PetBar bar, float amount) => amount > 0f ? amount * BuffMultiplier(bar) : amount;
        public void ChangeTemperature(float amount) { Temperature = Clamp(Temperature + amount); Changed?.Invoke(); }

        /// <summary>Sets all bars at once (debug panel, save system).</summary>
        public void SetAll(float energy, float fullness, float mood, float temperature)
        {
            Energy = Clamp(energy);
            Fullness = Clamp(fullness);
            Mood = Clamp(mood);
            Temperature = Clamp(temperature);
            Changed?.Invoke();
        }

        static float Clamp(float value) => Mathf.Clamp(value, 0f, 100f);
    }
}
