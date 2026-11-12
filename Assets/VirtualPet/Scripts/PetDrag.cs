using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace WindPet
{
    /// <summary>
    /// Lets the player grab the pet and drag it around the screen.
    /// Game feel idea from Mix and Jam (2024), "Recreating Balatro's Game Feel":
    /// the logic target follows the pointer at once, while the pet follows the
    /// target through a spring, so it lags, overshoots and settles. A separate
    /// visual pivot tilts and stretches with the speed.
    /// On release the pet keeps its speed, slows down and bounces off the screen edges.
    /// Works with mouse and touch (Pointer), so it also runs in WebGL.
    /// </summary>
    public class PetDrag : MonoBehaviour
    {
        public Camera cam;
        [Tooltip("Child that tilts and stretches. Its pivot should be the centre of the body.")]
        public Transform visual;
        public Collider grabCollider;

        [Header("Follow (spring)")]
        [Tooltip("Higher = follows the pointer more tightly.")]
        public float stiffness = 140f;
        [Tooltip("Higher = less overshoot and wobble.")]
        public float damping = 13f;
        public float maxSpeed = 25f;

        [Header("After release")]
        [Tooltip("How fast the pet slows down after a throw.")]
        public float releaseDrag = 2.2f;
        [Range(0f, 1f)] public float edgeBounce = 0.6f;

        [Header("World limits")]
        [Tooltip("The pet's feet never go below this height (top of the desk).")]
        public float floorY = -1f;
        [Tooltip("Props on these layers block the pet (it is pushed out of them).")]
        public LayerMask obstacleLayers = 0;
        [Tooltip("On: the pet flows through props like wind (see WindPhase). Off: it bumps into them.")]
        public bool passThrough = true;

        [Header("Depth")]
        [Tooltip("The pet can fly from the front of the desk (min) back to the shelves (max), in world Z.")]
        public float minDepthZ = 0f;
        public float maxDepthZ = 3.8f;
        [Tooltip("How fast the pet moves forwards or backwards (metres per second).")]
        public float depthSpeed = 2f;
        /// <summary>World Z the pet is flying towards. Set by wandering, feeding and the storm.</summary>
        public float TargetZ { get; set; }
        /// <summary>Feet position the pet is held at (inside the turbine). Null = free.</summary>
        public Vector3? Anchor { get; set; }
        [Tooltip("Capsule used to test against obstacles.")]
        public CapsuleCollider body;

        [Header("Screen bounds")]
        [Tooltip("Height of the pet, so the top stays on screen.")]
        public float petHeight = 2.2f;
        public float sidePadding = 0.7f;

        [Header("Tilt")]
        public float tiltPerSpeed = 2f;
        public float maxTilt = 12f;
        public float tiltSmoothing = 12f;

        [Header("Squash and stretch")]
        public float stretchPerSpeed = 0.025f;
        public float maxStretch = 0.12f;

        [Header("Squash against the screen edge")]
        [Tooltip("How much the pet flattens per metre the pointer pushes past the edge.")]
        public float pressSquash = 0.5f;
        [Tooltip("Flatten amount per unit of impact speed when thrown into an edge.")]
        public float impactSquash = 0.035f;
        [Range(0f, 0.8f)] public float maxSquash = 0.55f;
        [Tooltip("Half the width of the body, used to keep the flat side on the wall.")]
        public float halfWidth = 0.6f;
        [Header("Jelly spring (after an impact)")]
        public float jellyStiffness = 220f;
        public float jellyDamping = 9f;

        [Header("Shape limits")]
        [Tooltip("Smallest allowed scale on any axis.")]
        public float minScale = 0.6f;
        [Tooltip("Largest allowed scale on any axis.")]
        public float maxScale = 1.3f;
        [Tooltip("Largest jelly wobble after an impact.")]
        public float maxJelly = 0.3f;

        [Header("Hover")]
        public float hoverScale = 1.08f;
        public float hoverSpeed = 12f;

        public bool IsHeld { get; private set; }
        public bool IsHovered { get; private set; }
        public Vector3 Velocity => velocity;

        public event Action Grabbed;
        public event Action Released;

        Vector3 velocity;
        Vector3 grabOffset;
        Vector3 visualBaseScale;
        float tilt;
        float hover01;
        Vector3 desired;          // where the pet wants to be before the screen clamp
        Vector2 press;            // squash from pushing into an edge (x = side, y = floor/top)
        Vector2 jelly;            // wobble after an impact
        Vector2 jellyVelocity;
        Vector3 visualBasePosition;

        void Awake()
        {
            TargetZ = transform.position.z;
            if (cam == null) cam = Camera.main;
            if (visual != null)
            {
                visualBaseScale = visual.localScale;
                visualBasePosition = visual.localPosition;
            }
        }

        void Update()
        {
            var pointer = Pointer.current;
            float dt = Time.deltaTime;

            if (pointer != null)
            {
                Ray ray = cam.ScreenPointToRay(pointer.position.ReadValue());
                bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                IsHovered = !overUI && grabCollider != null && grabCollider.Raycast(ray, out _, 200f);

                if (pointer.press.wasPressedThisFrame && IsHovered && !ItemGrabber.IsBusyWithItem && !ToolBelt.HasTool)
                {
                    IsHeld = true;
                    grabOffset = transform.position - PointOnPetPlane(ray);
                    Grabbed?.Invoke();
                }
                else if (IsHeld && pointer.press.wasReleasedThisFrame)
                {
                    IsHeld = false;
                    Released?.Invoke();
                }

                if (IsHeld)
                {
                    // Spring towards the pointer: pull + damping gives lag and a small overshoot.
                    Vector3 target = PointOnPetPlane(ray) + grabOffset;
                    desired = target;
                    Vector3 force = (target - transform.position) * stiffness - velocity * damping;
                    velocity += force * dt;
                }
            }

            if (!IsHeld)
                velocity *= Mathf.Exp(-releaseDrag * dt);
            if (!IsHeld && Anchor.HasValue)
            {
                // held in place inside the turbine
                Vector3 toAnchor = Anchor.Value - transform.position;
                toAnchor.z = 0f;
                velocity = Vector3.Lerp(velocity, toAnchor * 8f, 1f - Mathf.Exp(-12f * dt));
                TargetZ = Anchor.Value.z;
            }

            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
            velocity.z = 0f;
            transform.position += velocity * dt;
            if (!IsHeld) MoveDepth(dt);
            if (!IsHeld) desired = transform.position;

            KeepOnScreen(dt);
            if (!passThrough) PushOutOfObstacles();
            UpdateVisual(dt);
        }

        /// <summary>Lets other behaviours (wandering, reactions) push the pet.
        /// Ignored while the player is holding it.</summary>
        public void AddAcceleration(Vector3 acceleration, float dt)
        {
            if (IsHeld) return;
            acceleration.z = 0f;
            velocity += acceleration * dt;
        }

        void MoveDepth(float dt)
        {
            Vector3 p = transform.position;
            float goal = Mathf.Clamp(TargetZ, minDepthZ, maxDepthZ);
            float step = Mathf.Clamp((goal - p.z) * 2f, -depthSpeed, depthSpeed) * dt;   // eases in near the goal
            p.z = Mathf.Clamp(p.z + step, minDepthZ, maxDepthZ);
            transform.position = p;
        }

        /// <summary>A viewport point (0-1) projected onto a plane at depth z, in world space.</summary>
        public Vector3 ViewportToWorld(Vector2 viewport, float z)
        {
            var plane = new Plane(Vector3.back, new Vector3(0f, 0f, z));
            return ViewportOnPlane(plane, viewport);
        }

        /// <summary>A viewport point (0-1) projected onto the pet's plane, in world space.</summary>
        public Vector3 ViewportToWorld(Vector2 viewport)
        {
            var plane = new Plane(Vector3.back, new Vector3(0f, 0f, transform.position.z));
            return ViewportOnPlane(plane, viewport);
        }

        Vector3 PointOnPetPlane(Ray ray)
        {
            var plane = new Plane(Vector3.back, new Vector3(0f, 0f, transform.position.z));
            return plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : transform.position;
        }

        void KeepOnScreen(float dt)
        {
            var plane = new Plane(Vector3.back, new Vector3(0f, 0f, transform.position.z));
            Vector3 min = ViewportOnPlane(plane, new Vector2(0f, 0f));
            Vector3 max = ViewportOnPlane(plane, new Vector2(1f, 1f));

            Vector3 p = transform.position;
            float minX = min.x + sidePadding, maxX = max.x - sidePadding;
            float minY = Mathf.Max(min.y, floorY), maxY = max.y - petHeight;

            // Hitting an edge: bounce, and squash in proportion to the impact speed.
            if (p.x < minX) { Impact(new Vector2(1f, 0f), velocity.x); p.x = minX; velocity.x = Mathf.Abs(velocity.x) * edgeBounce; }
            if (p.x > maxX) { Impact(new Vector2(1f, 0f), velocity.x); p.x = maxX; velocity.x = -Mathf.Abs(velocity.x) * edgeBounce; }
            if (p.y < minY) { Impact(new Vector2(0f, 1f), velocity.y); p.y = minY; velocity.y = Mathf.Abs(velocity.y) * edgeBounce; }
            if (p.y > maxY) { Impact(new Vector2(0f, 1f), velocity.y); p.y = maxY; velocity.y = -Mathf.Abs(velocity.y) * edgeBounce; }
            transform.position = p;

            // Pushing past an edge while holding: flatten against it. Sign = which wall.
            Vector2 targetPress = Vector2.zero;
            if (IsHeld)
            {
                if (desired.x < minX) targetPress.x = -(minX - desired.x);
                else if (desired.x > maxX) targetPress.x = desired.x - maxX;
                if (desired.y < minY) targetPress.y = -(minY - desired.y);
                else if (desired.y > maxY) targetPress.y = desired.y - maxY;
            }
            press = Vector2.Lerp(press, targetPress, 1f - Mathf.Exp(-18f * dt));

            // Jelly spring returns the squash to normal with a small wobble.
            Vector2 force = -jelly * jellyStiffness - jellyVelocity * jellyDamping;
            jellyVelocity += force * dt;
            jelly += jellyVelocity * dt;
            jelly = Vector2.ClampMagnitude(jelly, maxJelly);
        }

        readonly Collider[] overlaps = new Collider[8];

        /// <summary>
        /// Props are solid in the pet's 2D play plane. If the pet overlaps a prop,
        /// it is moved out sideways or upwards (whichever is shorter) and the speed
        /// going into the prop is removed, which reads as a soft bump.
        /// Depth (Z) is ignored on purpose: the pet only moves in X and Y.
        /// </summary>
        void PushOutOfObstacles()
        {
            if (body == null || obstacleLayers == 0) return;
            Vector3 c = transform.TransformPoint(body.center);
            float half = Mathf.Max(0f, body.height * 0.5f - body.radius);
            Vector3 a = c + Vector3.up * half, b = c - Vector3.up * half;
            int n = Physics.OverlapCapsuleNonAlloc(a, b, body.radius, overlaps, obstacleLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Bounds pet = body.bounds;
                Bounds prop = overlaps[i].bounds;
                float pushRight = prop.max.x - pet.min.x;   // move right to clear
                float pushLeft = pet.max.x - prop.min.x;    // move left to clear
                float pushUp = prop.max.y - pet.min.y;      // move up to sit on top
                Vector3 dir;
                float dist;
                if (pushUp <= Mathf.Min(pushRight, pushLeft)) { dir = Vector3.up; dist = pushUp; }
                else if (pushRight < pushLeft) { dir = Vector3.right; dist = pushRight; }
                else { dir = Vector3.left; dist = pushLeft; }
                if (dist <= 0f) continue;
                transform.position += dir * dist;
                Physics.SyncTransforms();
                float into = Vector3.Dot(velocity, dir);
                if (into < 0f)
                {
                    velocity -= dir * into * (1f + edgeBounce * 0.5f);
                    Impact(new Vector2(Mathf.Abs(dir.x), Mathf.Abs(dir.y)), into);
                }
            }
        }

        /// <summary>A jelly wobble for reactions, for example a happy gulp when eating.</summary>
        public void Wobble(float amount) => jellyVelocity += new Vector2(-amount, amount);

        void Impact(Vector2 axis, float speedAlongAxis)
        {
            // Only fast hits wobble; a gentle touch does nothing.
            float speed = Mathf.Abs(speedAlongAxis);
            if (speed < 1.5f) return;
            jellyVelocity += axis * (-speed * impactSquash * 20f);
        }

        Vector3 ViewportOnPlane(Plane plane, Vector2 viewport)
        {
            Ray ray = cam.ViewportPointToRay(viewport);
            return plane.Raycast(ray, out float d) ? ray.GetPoint(d) : transform.position;
        }

        void UpdateVisual(float dt)
        {
            if (visual == null) return;

            // Lean against the movement, like a balloon pulled on a string.
            float targetTilt = Mathf.Clamp(-velocity.x * tiltPerSpeed, -maxTilt, maxTilt);
            tilt = Mathf.Lerp(tilt, targetTilt, 1f - Mathf.Exp(-tiltSmoothing * dt));
            visual.localRotation = Quaternion.Euler(0f, 0f, tilt);

            // Stretch with speed, keep the volume roughly the same.
            float stretch = Mathf.Min(velocity.magnitude * stretchPerSpeed, maxStretch);
            var squash = new Vector3(1f - stretch * 0.5f, 1f + stretch, 1f - stretch * 0.5f);

            // Flatten against walls: squeeze along the push, bulge the other way.
            float sideSquash = Mathf.Clamp(Mathf.Abs(press.x) * pressSquash, 0f, maxSquash);
            float floorSquash = Mathf.Clamp(Mathf.Abs(press.y) * pressSquash, 0f, maxSquash);
            squash.x *= (1f - sideSquash + floorSquash * 0.6f) * (1f + jelly.x);
            squash.y *= (1f - floorSquash + sideSquash * 0.6f) * (1f + jelly.y);
            squash.x /= 1f + jelly.y * 0.5f;
            squash.y /= 1f + jelly.x * 0.5f;

            squash.x = Mathf.Clamp(squash.x, minScale, maxScale);
            squash.y = Mathf.Clamp(squash.y, minScale, maxScale);

            // Keep the flat side on the wall instead of shrinking towards the middle.
            Vector3 offset = new Vector3(Mathf.Sign(press.x) * sideSquash * halfWidth,
                                         Mathf.Sign(press.y) * floorSquash * petHeight * 0.5f, 0f);
            visual.localPosition = visualBasePosition + offset;

            hover01 = Mathf.Lerp(hover01, (IsHovered || IsHeld) ? 1f : 0f, 1f - Mathf.Exp(-hoverSpeed * dt));
            float hover = Mathf.Lerp(1f, hoverScale, hover01);

            visual.localScale = Vector3.Scale(visualBaseScale, squash) * hover;
        }
    }
}
