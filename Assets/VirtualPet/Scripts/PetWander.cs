using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Basic idle behaviour: the pet picks a random spot on the screen, floats
    /// there, hovers for a moment and then picks a new spot.
    /// It steers by adding acceleration to PetDrag instead of setting the
    /// position, so all the soft motion (bend, stretch, smoke trail) still works.
    /// The eyes look ahead in the direction of travel.
    /// Other systems (for example the FSM) can change SpeedMultiplier or pause it.
    /// </summary>
    public class PetWander : MonoBehaviour
    {
        public PetDrag drag;

        [Header("Area (viewport 0-1, for the pet's feet)")]
        [Tooltip("Keeps the pet away from the UI in the top corners.")]
        public Vector2 areaMin = new Vector2(0.15f, 0.05f);
        public Vector2 areaMax = new Vector2(0.85f, 0.5f);

        [Header("Movement")]
        public float moveSpeed = 1.4f;
        [Tooltip("How quickly the pet turns towards its target. Low = lazy, floaty.")]
        public float steering = 2.5f;
        [Tooltip("Starts slowing down inside this distance.")]
        public float slowDownRadius = 1.5f;
        public float arriveRadius = 0.35f;

        [Header("Resting")]
        public Vector2 restTime = new Vector2(1f, 3f);
        public float bobAmount = 0.1f;
        public float bobSpeed = 1.6f;
        [Tooltip("Wait after being thrown before wandering again.")]
        public float delayAfterRelease = 1.2f;

        [Header("Eyes")]
        public Transform eyes;
        [Tooltip("How far the eyes move per unit of speed. 0 = off.")]
        public float eyeLook = 0.04f;
        public float maxEyeOffset = 0.12f;

        /// <summary>Scales the wander speed (for example lower when tired).</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        /// <summary>When true the pet stays where it is.</summary>
        public bool Paused { get; set; }
        /// <summary>Set by PetFeeding: a food puff to fly to. Null = wander as usual.</summary>
        public Vector3? FoodTarget { get; set; }
        /// <summary>Asleep: no flying, the pet sinks down onto the desk and lies still.</summary>
        public bool Asleep { get; set; }
        /// <summary>Optional: chooses the next target (for example a prop to wreck in a storm).</summary>
        public System.Func<Vector3?> PickOverride { get; set; }
        /// <summary>Multiplies the rest time between flights (low = restless).</summary>
        public float RestScale { get; set; } = 1f;
        /// <summary>True while flying somewhere (to a spot or to food).</summary>
        public bool IsMoving => moving || FoodTarget.HasValue;
        [Tooltip("Chance that a normal flight goes to the back of the room (near the shelves).")]
        [Range(0f, 1f)] public float backChance = 0.35f;
        [Tooltip("Speed multiplier when flying to food.")]
        public float foodSpeedMultiplier = 1.5f;

        Vector3 target;
        Vector3 restPoint;
        bool moving;
        float restTimer;
        float releaseTimer;
        float moveTimer;
        Vector3 eyesBase;
        Vector3 eyeOffset;

        void Start()
        {
            if (eyes != null) eyesBase = eyes.localPosition;
            drag.Released += () => releaseTimer = delayAfterRelease;
            BeginRest(Random.Range(0.3f, 1f));
        }

        void Update()
        {
            float dt = Time.deltaTime;
            UpdateEyes(dt);

            if (drag.IsHeld)
            {
                moving = false;
                return;
            }
            if (releaseTimer > 0f)
            {
                releaseTimer -= dt;
                if (releaseTimer <= 0f) BeginRest(0.2f);
                return;
            }

            if (drag.Anchor.HasValue)
            {
                // inside the turbine: PetDrag holds the pet in place
                moving = false;
                restPoint = transform.position;
                return;
            }

            if (Asleep)
            {
                // sink slowly down to the desk, like a breeze that died down
                Steer(new Vector3(-drag.Velocity.x, -2f, 0f), dt);
                moving = false;
                restPoint = transform.position;
                restTimer = 0.5f;
                return;
            }

            if (FoodTarget.HasValue)
            {
                drag.TargetZ = FoodTarget.Value.z;
                Vector3 toFood = FoodTarget.Value - transform.position;
                toFood.z = 0f;
                float d = toFood.magnitude;
                float s = moveSpeed * SpeedMultiplier * foodSpeedMultiplier * Mathf.Clamp01(d / 0.6f);
                Steer(d > 0.001f ? toFood / d * s : Vector3.zero, dt);
                moving = false;
                restPoint = transform.position;
                restTimer = Random.Range(restTime.x, restTime.y);
                return;
            }

            if (Paused || !moving)
            {
                // Hover around the rest point with a gentle bob.
                Vector3 bob = new Vector3(0f, Mathf.Sin(Time.time * bobSpeed) * bobAmount, 0f);
                Steer((restPoint + bob - transform.position) * 2f, dt);

                if (Paused) return;
                restTimer -= dt;
                if (restTimer <= 0f) PickTarget();
                return;
            }

            // safety: a target that cannot be reached is given up after a while
            moveTimer += dt;
            if (moveTimer > 4f) { PickTarget(); return; }

            Vector3 toTarget = target - transform.position;
            toTarget.z = 0f;
            float distance = toTarget.magnitude;
            if (distance < arriveRadius)
            {
                BeginRest(Random.Range(restTime.x, restTime.y) * RestScale);
                return;
            }

            float speed = moveSpeed * SpeedMultiplier * Mathf.Clamp01(distance / slowDownRadius);
            Steer(toTarget / distance * speed, dt);
        }

        void Steer(Vector3 desiredVelocity, float dt)
        {
            Vector3 acceleration = (desiredVelocity - drag.Velocity) * steering;
            drag.AddAcceleration(acceleration, dt);
        }

        void PickTarget()
        {
            moveTimer = 0f;
            Vector3? picked = PickOverride != null ? PickOverride() : null;
            if (picked.HasValue)
            {
                target = picked.Value;
                drag.TargetZ = target.z;
                moving = true;
                return;
            }
            // most flights stay in front, some go to the back of the room
            float depth = Random.value < backChance
                ? Random.Range(Mathf.Lerp(drag.minDepthZ, drag.maxDepthZ, 0.45f), drag.maxDepthZ)
                : Random.Range(drag.minDepthZ, Mathf.Lerp(drag.minDepthZ, drag.maxDepthZ, 0.3f));
            var viewport = new Vector2(Random.Range(areaMin.x, areaMax.x), Random.Range(areaMin.y, areaMax.y));
            target = drag.ViewportToWorld(viewport, depth);
            drag.TargetZ = depth;
            moving = true;
        }

        void BeginRest(float duration)
        {
            moving = false;
            restTimer = duration;
            restPoint = transform.position;
        }

        void UpdateEyes(float dt)
        {
            if (eyes == null) return;
            Vector3 v = drag.Velocity;
            var wanted = Vector3.ClampMagnitude(new Vector3(v.x, v.y * 0.5f, 0f) * eyeLook, maxEyeOffset);
            eyeOffset = Vector3.Lerp(eyeOffset, wanted, 1f - Mathf.Exp(-8f * dt));
            eyes.localPosition = eyesBase + eyeOffset;
        }
    }
}
