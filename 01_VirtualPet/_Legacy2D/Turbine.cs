using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// The pet blows into the turbine to earn money. A rested, happy pet blows
    /// harder. Work costs energy and heats the pet up; overwork hurts the Bond
    /// (handled in PetStateMachine).
    /// </summary>
    public class Turbine : MonoBehaviour
    {
        public PetStats stats;
        public PetStateMachine fsm;
        public Wallet wallet;
        [Tooltip("Visual rotor that spins with the wind. Optional.")]
        public Transform rotor;
        public float maxSpinSpeed = 720f;

        /// <summary>0..1 wind strength, also useful for sound and particles.</summary>
        public float WindStrength { get; private set; }

        PetConfig Config => stats.config;

        void Update()
        {
            float dt = fsm.DeltaTime;
            bool working = fsm.State == PetState.Working;

            float target = 0f;
            if (working)
            {
                target = Mathf.Lerp(0.3f, 1f, stats.Energy / 100f);
                if (fsm.Mood == PetMood.Happy) target *= 1.2f;
                if (fsm.Mood == PetMood.Grumpy || fsm.Mood == PetMood.Sad) target *= 0.7f;

                wallet.AddIncome(Config.turbineIncome * target * dt);
                stats.ChangeEnergy(-Config.turbineEnergyCost * dt);
                stats.ChangeTemperature(Config.turbineHeat * dt);
            }
            else if (fsm.State == PetState.Storm)
            {
                // A storm spins the rotor wildly, but nobody is collecting the power.
                target = 1.3f;
            }

            WindStrength = Mathf.MoveTowards(WindStrength, target, dt * 2f);
            if (rotor != null) rotor.Rotate(0f, 0f, -maxSpinSpeed * WindStrength * Time.deltaTime);
        }
    }
}
