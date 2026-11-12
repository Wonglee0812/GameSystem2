using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Moving costs energy: the further the pet flies on its own, the more energy it
    /// uses. The cost goes through PetStateMachine.SpendEnergy, so temperature
    /// changes it (cold costs more, warm costs less). Being carried by the player
    /// costs nothing, and a sleeping pet does not pay either.
    /// </summary>
    public class PetEnergyUse : MonoBehaviour
    {
        public PetDrag drag;
        public PetStateMachine fsm;
        [Tooltip("Energy used per metre flown (before the temperature multiplier).")]
        public float costPerMetre = 0.15f;
        [Tooltip("Very small movements (bobbing in place) are free.")]
        public float minSpeed = 0.15f;

        Vector3 lastPosition;

        void Start()
        {
            if (drag == null) drag = GetComponent<PetDrag>();
            lastPosition = transform.position;
        }

        void Update()
        {
            Vector3 p = transform.position;
            float distance = Vector3.Distance(p, lastPosition);
            lastPosition = p;
            if (fsm == null || drag == null || drag.IsHeld || drag.Anchor.HasValue) return;
            if (fsm.Energy == EnergyState.Sleep) return;
            if (Time.deltaTime <= 0f || distance / Time.deltaTime < minSpeed) return;
            fsm.SpendEnergy(distance * costPerMetre * fsm.stats.config.timeScale);
        }
    }
}
