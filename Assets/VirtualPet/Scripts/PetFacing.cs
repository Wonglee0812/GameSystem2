using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Turns the pet's face towards where it is going, or where it is being dragged.
    /// The whirlwind body looks the same from every side, so only the face turns:
    /// the eyes sit on a pivot in the centre of the body and slide around it.
    /// Moving right turns the face right, moving up tilts it up. When the pet is
    /// still for a while, the face slowly turns back to the player.
    /// </summary>
    public class PetFacing : MonoBehaviour
    {
        public PetDrag drag;
        [Tooltip("Pivot in the centre of the body. The eyes are its children.")]
        public Transform facePivot;

        [Header("Turning")]
        [Tooltip("Degrees of turn per unit of sideways speed.")]
        public float yawPerSpeed = 30f;
        public float maxYaw = 55f;
        public float pitchPerSpeed = 10f;
        public float maxPitch = 18f;
        [Tooltip("Seconds to reach the new direction.")]
        public float turnTime = 0.18f;

        [Header("Look back at the player")]
        [Tooltip("Below this speed the pet counts as still.")]
        public float stillSpeed = 0.25f;
        [Tooltip("Seconds of being still before turning back to the front.")]
        public float lookBackDelay = 0.8f;
        public float lookBackTime = 0.6f;

        float yaw, pitch;
        float yawVelocity, pitchVelocity;
        float targetYaw, targetPitch;
        float stillTimer;

        void Update()
        {
            if (drag == null || facePivot == null) return;
            Vector3 v = drag.Velocity;
            float time = turnTime;

            if (v.magnitude > stillSpeed)
            {
                stillTimer = 0f;
                // A point on the front (-Z) moves right when rotated by a negative yaw.
                targetYaw = Mathf.Clamp(-v.x * yawPerSpeed, -maxYaw, maxYaw);
                targetPitch = Mathf.Clamp(v.y * pitchPerSpeed, -maxPitch, maxPitch);
            }
            else
            {
                stillTimer += Time.deltaTime;
                if (stillTimer > lookBackDelay)
                {
                    targetYaw = 0f;
                    targetPitch = 0f;
                    time = lookBackTime;
                }
            }

            yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, time);
            pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref pitchVelocity, time);
            facePivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
