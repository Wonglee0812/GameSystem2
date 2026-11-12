using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Sends FSM data to the Animator. Animation clips themselves are keyframed
    /// by hand in the Animation window, as the brief requires.
    /// Animator parameters: State (int), Mood (int), Temperature, Energy, Bond (floats 0-1).
    /// </summary>
    public class PetAnimatorBridge : MonoBehaviour
    {
        public PetStats stats;
        public PetStateMachine fsm;
        public Animator animator;

        static readonly int StateId = Animator.StringToHash("State");
        static readonly int MoodId = Animator.StringToHash("Mood");
        static readonly int TemperatureId = Animator.StringToHash("Temperature");
        static readonly int EnergyId = Animator.StringToHash("Energy");
        static readonly int BondId = Animator.StringToHash("Bond");

        void Update()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetInteger(StateId, (int)fsm.State);
            animator.SetInteger(MoodId, (int)fsm.Mood);
            animator.SetFloat(TemperatureId, stats.Temperature / 100f);
            animator.SetFloat(EnergyId, stats.Energy / 100f);
            animator.SetFloat(BondId, stats.Bond / 100f);
        }
    }
}
