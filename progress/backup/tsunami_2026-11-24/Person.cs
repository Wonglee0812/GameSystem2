using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>One person. Moves with physics on the real island (so quakes push them and they can fall off),
    /// steers with simple steering behaviours (Reynolds, 1999) and panics; panic spreads between people
    /// (Helbing et al., 2000). When hit hard the person tumbles like a stiff toy figure and gets up again if alive.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class Person : MonoBehaviour
    {
        public enum State { Idle, Panic, Tumble, Dead }

        [Header("Look")]
        [Tooltip("Skins; one is picked at random for each person.")]
        public Material[] skins;
        [Tooltip("Colour of a dead person (multiplies the skin). Default grey 0.4.")]
        public Color deadTint = new Color(0.4f, 0.4f, 0.4f);

        [Header("Movement")]
        [Tooltip("Walk speed when calm, in m/s. Default 1.3.")]
        public float walkSpeed = 1.3f;
        [Tooltip("Run speed in full panic, in m/s. Default 4.5.")]
        public float runSpeed = 4.5f;
        [Tooltip("How fast a person can change speed, in m/s2. Low = slides more. Default 12.")]
        public float grip = 12f;
        [Tooltip("Turn speed in degrees per second. Default 540.")]
        public float turnSpeed = 540f;
        [Tooltip("Upward speed of a jump onto a step, in m/s (4.6 reaches a bit over one brick). Default 4.6.")]
        public float jumpSpeed = 4.6f;

        [Header("Senses")]
        [Tooltip("Radius in metres for seeing danger and other people. Default 6.")]
        public float senseRadius = 6f;
        [Tooltip("Seconds between AI decisions (the body still moves every physics step). Default 0.2.")]
        public float thinkEvery = 0.2f;

        [Header("Panic (Helbing et al., 2000)")]
        [Tooltip("Panic gained per second at disaster intensity 1 (scared people gain more). Default 0.8.")]
        public float panicGain = 0.8f;
        [Tooltip("Panic taken per second from panicking people nearby (panic spreads). Default 0.6.")]
        public float panicSpread = 0.6f;
        [Tooltip("Panic lost per second when nothing happens. Default 0.15.")]
        public float panicDecay = 0.15f;
        [Tooltip("Chance per second to lose balance and fall at disaster intensity 1 (more in panic). Default 0.08.")]
        public float stumbleChance = 0.08f;

        [Header("Steering weights (Reynolds, 1987, 1999)")]
        [Tooltip("Run away from falling or tall bricks. Default 2.5.")]
        public float fleeWeight = 2.5f;
        [Tooltip("Follow the crowd (same direction, stay near). Default 0.8.")]
        public float herdWeight = 0.8f;
        [Tooltip("Keep space from people very close. Default 1.5.")]
        public float separationWeight = 1.5f;
        [Tooltip("Random running around. Default 0.9.")]
        public float wanderWeight = 0.9f;
        [Tooltip("Turn back at a drop or the island edge. Default 4.")]
        public float edgeWeight = 4f;
        [Tooltip("Go around walls. Default 2.")]
        public float avoidWeight = 2f;

        [Header("Damage")]
        [Tooltip("Contact points higher than this above the feet count as a head hit, in metres. Default 1.35.")]
        public float headHeight = 1.35f;
        [Tooltip("Head hit by a moving brick with more impulse than this (N s = kg x m/s) -> fainted. Default 35.")]
        public float headFaintImpulse = 35f;
        [Tooltip("Head hit with more impulse than this -> dead. Default 110.")]
        public float headDeathImpulse = 110f;
        [Tooltip("Body hit by a moving brick with more impulse than this -> falls over. Default 60.")]
        public float bodyKnockImpulse = 60f;
        [Tooltip("Body hit with more impulse than this -> crushed (dead). Default 260.")]
        public float bodyDeathImpulse = 260f;
        [Tooltip("Landing that changes the speed by more than this (m/s) kills (a fall). Default 9.")]
        public float deathDeltaV = 9f;
        [Tooltip("Seconds a fainted person stays down. Default 6.")]
        public float faintTime = 6f;
        [Tooltip("Seconds on the ground after a knock before getting up. Default 2.5.")]
        public float tumbleTime = 2.5f;

        public static readonly List<Person> All = new List<Person>();
        public static int Dead;
        /// <summary>Deaths per cause ("hit", "fall", "fell off"), for balancing and the report.</summary>
        public static readonly Dictionary<string, int> Causes = new Dictionary<string, int>();
        public static int AliveCount { get { int n = 0; foreach (var p in All) if (p.Alive) n++; return n; } }

        public State Current { get; private set; }
        public bool Alive => Current != State.Dead;
        public float PanicLevel { get; private set; }
        public Vector3 Velocity => body.linearVelocity;

        // personal traits, rolled once (this is what makes people different)
        float bravery, speedFactor, recklessness, herdiness, noiseSeed;

        Rigidbody body;
        Animator anim;
        Renderer[] renderers;
        Vector3 desiredDir, home, wanderTarget;
        float desiredSpeed, nextThink, tumbleUntil, wanderUntil, jumpReady;
        bool grounded;
        Rigidbody groundBody;

        static readonly Collider[] hits = new Collider[64];
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int JumpId = Animator.StringToHash("Jump");

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            anim = GetComponentInChildren<Animator>();
            renderers = GetComponentsInChildren<Renderer>();
            body.mass = 70f;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            bravery = Random.Range(0f, 1f);
            speedFactor = Random.Range(0.8f, 1.2f);
            recklessness = Random.Range(0f, 1f);
            herdiness = Random.Range(0.3f, 1f);
            noiseSeed = Random.Range(0f, 1000f);
            if (skins != null && skins.Length > 0)
            {
                var skin = skins[Random.Range(0, skins.Length)];
                foreach (var r in renderers) r.sharedMaterial = skin;
            }
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        void Start() { home = transform.position; wanderTarget = home; }

        void Update()
        {
            if (anim == null || !anim.enabled) return;
            Vector3 v = body.linearVelocity - GroundVelocity();
            float hv = new Vector3(v.x, 0f, v.z).magnitude;
            anim.SetFloat(SpeedId, Mathf.Clamp01(hv / 2.5f));
            anim.speed = hv > 0.3f ? Mathf.Clamp(hv / 3f, 0.7f, 1.6f) : 1f;
        }

        void FixedUpdate()
        {
            if (Current == State.Dead) return;
            if (Current == State.Tumble)
            {
                if (Time.time > tumbleUntil && body.linearVelocity.sqrMagnitude < 0.5f) GetUp();
                return;
            }
            CheckGround();
            if (Time.time >= nextThink)
            {
                nextThink = Time.time + thinkEvery * Random.Range(0.8f, 1.2f);
                Think();
            }
            Move();
        }

        // ---------------- thinking ----------------

        void Think()
        {
            float intensity = DisasterState.Intensity;
            Vector3 pos = transform.position;
            Vector3 flee = Vector3.zero, sep = Vector3.zero, align = Vector3.zero, centre = Vector3.zero;
            int peers = 0;
            float peerPanic = 0f;

            int n = Physics.OverlapSphereNonAlloc(pos + Vector3.up * 0.9f, senseRadius, hits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = hits[i];
                if (c.attachedRigidbody == body) continue;
                var block = c.GetComponent<Block>();
                if (block != null)
                {
                    float speed = block.Body != null ? block.Body.linearVelocity.magnitude : 0f;
                    float height = c.bounds.max.y - pos.y;           // bricks above my feet can fall on me
                    if (speed < 0.5f && (intensity <= 0f || height < 1.2f)) continue;
                    float danger = Mathf.Max(0f, height - 0.6f) * 0.4f * intensity + speed;
                    Vector3 d = pos - c.ClosestPoint(pos);
                    d.y = 0f;
                    float dist = d.magnitude + 0.3f;
                    flee += d / dist * (danger / (dist * dist));
                    continue;
                }
                var hz = c.GetComponent<Hazard>();
                if (hz != null && hz.Body != null)
                {
                    Vector3 d = pos - c.transform.position;
                    d.y = 0f;
                    float dist = d.magnitude + 0.3f;
                    flee += d / dist * ((2f + hz.Body.linearVelocity.magnitude) / (dist * dist));
                    continue;
                }
                var p = c.GetComponent<Person>();
                if (p != null && p.Alive)
                {
                    Vector3 d = pos - p.transform.position;
                    d.y = 0f;
                    float dist = d.magnitude;
                    if (dist < 1.2f && dist > 0.01f) sep += d / (dist * dist);
                    align += p.Velocity;
                    centre += p.transform.position;
                    peerPanic += p.PanicLevel;
                    peers++;
                }
            }

            // panic: from the disaster (scared people more), from others (it spreads), from danger close by
            float avgPanic = peers > 0 ? peerPanic / peers : 0f;
            // others only pull my panic up to their own level, so the crowd calms down together after the disaster
            float spread = Mathf.Max(0f, avgPanic - PanicLevel) * panicSpread * herdiness;
            float gain = intensity * panicGain * (1.3f - bravery) + spread + Mathf.Min(1f, flee.magnitude) * 0.5f;
            float decay = intensity <= 0f ? panicDecay : 0f;
            PanicLevel = Mathf.Clamp01(PanicLevel + (gain - decay) * thinkEvery);
            Current = PanicLevel > 0.25f ? State.Panic : State.Idle;

            Vector3 dir;
            if (Current == State.Idle)
            {
                if (Time.time > wanderUntil || Flat(wanderTarget - pos).magnitude < 0.5f)
                {
                    Vector2 r = Random.insideUnitCircle * 4f;
                    wanderTarget = home + new Vector3(r.x, 0f, r.y);
                    wanderUntil = Time.time + Random.Range(3f, 7f);
                }
                Vector3 to = Flat(wanderTarget - pos);
                bool resting = Mathf.PerlinNoise(noiseSeed, Time.time * 0.2f) < 0.35f;  // people stop now and then
                dir = to.magnitude < 0.5f || resting ? Vector3.zero : to.normalized;
                desiredSpeed = walkSpeed * speedFactor;
                dir += flee * fleeWeight;
            }
            else
            {
                dir = flee * fleeWeight + Wander() * wanderWeight * (0.5f + PanicLevel);
                if (peers > 0)
                {
                    Vector3 herd = Flat(align / peers).normalized * 0.6f + Flat(centre / peers - pos).normalized * 0.4f;
                    dir += herd * herdWeight * herdiness;
                }
                desiredSpeed = Mathf.Lerp(walkSpeed, runSpeed, PanicLevel) * speedFactor;
            }
            dir += sep * separationWeight;
            dir += AvoidEdge(dir) * edgeWeight;
            dir += AvoidWalls(dir) * avoidWeight;
            dir = Flat(dir);
            desiredDir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.zero;

            // losing balance on a shaking island
            if (grounded && intensity > 0f && Random.value < stumbleChance * intensity * (1f + PanicLevel) * thinkEvery)
                Tumble(1.5f);
        }

        Vector3 Wander()
        {
            float a = (Mathf.PerlinNoise(noiseSeed, Time.time * 0.5f) - 0.5f) * 720f;
            return Quaternion.Euler(0f, a, 0f) * Vector3.forward;
        }

        /// <summary>Turn back when there is no ground ahead. Reckless people in panic sometimes run off anyway.</summary>
        Vector3 AvoidEdge(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.01f) return Vector3.zero;
            Vector3 f = Flat(dir).normalized;
            Vector3 ahead = transform.position + f * 1.6f + Vector3.up * 1f;
            if (Physics.Raycast(ahead, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore)) return Vector3.zero;
            if (Current == State.Panic && Random.value < recklessness * PanicLevel * 0.15f) return Vector3.zero;
            return -f;
        }

        /// <summary>Go around walls: pick the side with more space.</summary>
        Vector3 AvoidWalls(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.01f) return Vector3.zero;
            Vector3 f = Flat(dir).normalized;
            Vector3 chest = transform.position + Vector3.up * 1.1f;
            if (!Physics.SphereCast(chest, 0.25f, f, out _, 1.2f, ~0, QueryTriggerInteraction.Ignore)) return Vector3.zero;
            Vector3 right = Vector3.Cross(Vector3.up, f);
            float r = Physics.Raycast(chest, (f + right).normalized, out var hr, 3f) ? hr.distance : 3f;
            float l = Physics.Raycast(chest, (f - right).normalized, out var hl, 3f) ? hl.distance : 3f;
            return r >= l ? right : -right;
        }

        // ---------------- moving ----------------

        void CheckGround()
        {
            grounded = Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.25f, Vector3.down, out var hit, 0.35f, ~0, QueryTriggerInteraction.Ignore);
            groundBody = grounded ? hit.rigidbody : null;
        }

        Vector3 GroundVelocity() => groundBody != null && groundBody != body ? groundBody.GetPointVelocity(transform.position) : Vector3.zero;

        void Move()
        {
            float dt = Time.fixedDeltaTime;
            Vector3 v = body.linearVelocity;
            Vector3 ground = GroundVelocity();
            Vector3 rel = Flat(v - ground);
            if (grounded)
            {
                Vector3 target = desiredDir * desiredSpeed;
                Vector3 acc = Vector3.ClampMagnitude((target - rel) / dt, grip);
                body.AddForce(acc, ForceMode.Acceleration);

                if (desiredSpeed > 0.5f && desiredDir != Vector3.zero && Time.time > jumpReady && StepAhead())
                {
                    body.linearVelocity = new Vector3(v.x, jumpSpeed, v.z);
                    if (anim != null) anim.SetTrigger(JumpId);
                    jumpReady = Time.time + 0.8f;
                }
            }
            if (rel.sqrMagnitude > 0.05f)
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(rel), turnSpeed * dt));
        }

        /// <summary>Something low in front (a one-brick step) and free space above it.</summary>
        bool StepAhead()
        {
            Vector3 pos = transform.position;
            Vector3 f = desiredDir;
            bool low = Physics.Raycast(pos + Vector3.up * 0.3f, f, 0.7f, ~0, QueryTriggerInteraction.Ignore);
            bool high = Physics.Raycast(pos + Vector3.up * 1.0f, f, 0.9f, ~0, QueryTriggerInteraction.Ignore);
            return low && !high;
        }

        // ---------------- damage ----------------

        void OnCollisionEnter(Collision c)
        {
            if (Current == State.Dead) return;
            if (c.rigidbody != null && c.rigidbody.GetComponent<Hazard>() != null) return;   // waves and lava handle their own hits

            float impulse = c.impulse.magnitude;
            var block = c.rigidbody != null ? c.rigidbody.GetComponent<Block>() : null;
            if (block != null)
            {
                // only a moving brick hurts (not walking against or landing on bricks)
                if (c.relativeVelocity.magnitude < 1.5f || c.rigidbody.linearVelocity.magnitude < 1.5f) return;
                bool head = c.contactCount > 0 && c.GetContact(0).point.y > transform.position.y + headHeight;
                if (head)
                {
                    if (impulse > headDeathImpulse) Die("head hit");
                    else if (impulse > headFaintImpulse) Faint();
                }
                else
                {
                    if (impulse > bodyDeathImpulse) Die("crushed");
                    else if (impulse > bodyKnockImpulse && Current != State.Tumble) Tumble(1f);
                }
                return;
            }
            // ground or a still brick: a hard landing
            if (impulse / body.mass > deathDeltaV) Die("fall");
        }

        /// <summary>Hit on the head: down for faintTime, then gets up calm-ish.</summary>
        public void Faint()
        {
            if (Current == State.Dead) return;
            Tumble(0.6f);
            Fainted++;
            tumbleUntil = Time.time + faintTime * Random.Range(0.8f, 1.2f);
        }

        /// <summary>Caught by a wave: falls over and is pushed along.</summary>
        public void Swept(Vector3 push)
        {
            if (Current == State.Dead) return;
            Tumble(1.2f);
            body.AddForce(push, ForceMode.VelocityChange);
        }

        public static int Fainted;

        float burnTime;

        /// <summary>Standing in fire: dies after `limit` seconds in total.</summary>
        public void Burn(float seconds, float limit)
        {
            if (Current == State.Dead) return;
            PanicLevel = 1f;
            burnTime += seconds;
            if (burnTime >= limit) Die("fire");
        }

        /// <summary>Fall over like a stiff toy figure.</summary>
        public void Tumble(float strength)
        {
            if (Current == State.Dead) return;
            Current = State.Tumble;
            PanicLevel = 1f;
            body.constraints = RigidbodyConstraints.None;
            if (anim != null) anim.enabled = false;
            body.AddTorque(Random.onUnitSphere * 4f * strength, ForceMode.VelocityChange);
            tumbleUntil = Time.time + tumbleTime * Random.Range(0.7f, 1.3f);
        }

        void GetUp()
        {
            Current = State.Panic;
            float yaw = transform.eulerAngles.y;
            body.position += Vector3.up * 0.15f;
            body.rotation = Quaternion.Euler(0f, yaw, 0f);
            body.angularVelocity = Vector3.zero;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            if (anim != null) anim.enabled = true;
        }

        public void Die(string cause)
        {
            if (Current == State.Dead) return;
            Causes.TryGetValue(cause, out int n);
            Causes[cause] = n + 1;
            if (Current != State.Tumble)
            {
                body.constraints = RigidbodyConstraints.None;
                if (anim != null) anim.enabled = false;
            }
            Current = State.Dead;
            Dead++;
            foreach (var r in renderers) r.material.color *= deadTint;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
