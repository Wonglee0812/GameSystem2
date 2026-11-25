using UnityEngine;

namespace BrickPanic
{
    /// <summary>Base for all disasters (earthquake, tsunami, volcano). The round system picks one, shows it in the
    /// forecast, and calls Begin().</summary>
    public abstract class Disaster : MonoBehaviour
    {
        [Tooltip("Name shown to the player in the forecast.")]
        public string displayName = "Disaster";

        public abstract bool Running { get; }
        public abstract void Begin();
    }

    /// <summary>Anything a disaster throws around (wave bricks, lava bombs). People run away from these.</summary>
    public abstract class Hazard : MonoBehaviour
    {
        public Rigidbody Body { get; protected set; }
    }
}
