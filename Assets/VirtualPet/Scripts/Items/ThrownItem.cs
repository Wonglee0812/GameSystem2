using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Added to a prop for a short time after the player throws it. If it hits the
    /// pet, the pet's mood drops and the pet is knocked back a little.
    /// </summary>
    public class ThrownItem : MonoBehaviour
    {
        public PetDrag pet;
        public PetStats stats;
        public float moodPenalty = 15f;
        public float knockBack = 6f;
        public float activeTime = 3f;

        Collider[] colliders;

        void Start() => colliders = GetComponentsInChildren<Collider>();

        void FixedUpdate()
        {
            activeTime -= Time.fixedDeltaTime;
            if (activeTime <= 0f || pet == null || pet.body == null) { Destroy(this); return; }
            Bounds petBounds = pet.body.bounds;
            petBounds.Expand(-0.2f);                     // only a clear hit counts
            foreach (var c in colliders)
            {
                if (c == null || !c.bounds.Intersects(petBounds)) continue;
                if (stats != null) stats.ChangeMood(-moodPenalty);
                var rb = GetComponent<Rigidbody>();
                Vector3 push = rb != null ? rb.linearVelocity : Vector3.zero;
                push.z = 0f;
                pet.AddAcceleration(push.normalized * knockBack, 1f);
                Destroy(this);
                return;
            }
        }
    }
}
