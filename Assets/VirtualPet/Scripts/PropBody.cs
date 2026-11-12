using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Every physics prop in the lab. It starts asleep, so the shelves stay still
    /// until something touches them. It also remembers which way is "up" for the
    /// prop: some models are imported lying on their side, so world up is not
    /// always the model's own up.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PropBody : MonoBehaviour
    {
        /// <summary>The model's own axis that points up when it stands normally.</summary>
        public Vector3 LocalUp { get; private set; } = Vector3.up;
        /// <summary>Rotation when the scene started (standing normally).</summary>
        public Quaternion RestRotation { get; private set; } = Quaternion.identity;

        void Awake()
        {
            RestRotation = transform.rotation;
            LocalUp = Quaternion.Inverse(transform.rotation) * Vector3.up;
        }

        void Start() => GetComponent<Rigidbody>().Sleep();

        /// <summary>A rotation that stands the prop upright, keeping its current heading.</summary>
        public Quaternion Upright(Quaternion current)
        {
            Vector3 up = current * LocalUp;
            return Quaternion.FromToRotation(up, Vector3.up) * current;
        }
    }
}
