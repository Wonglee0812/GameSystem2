using System.Collections.Generic;
using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// The angry pet as a little tornado (strength from PetLook.Storm and Rage).
    /// - Purple lightning cracks around the body, with branches and a flash of light.
    /// - Nearby props are sucked in and then circle around the pet like the debris
    ///   of a tornado: each one gets its own height, distance and speed on a ring
    ///   around the pet, and is steered there every physics step. When the storm
    ///   calms down they are let go and fall.
    /// </summary>
    public class PetStorm : MonoBehaviour
    {
        public PetLook look;
        public PetDrag drag;
        [Tooltip("Lightning and the tornado only happen while the mood is Angry.")]
        public PetStateMachine fsm;
        public Material boltMaterial;
        public LayerMask propLayers;

        [Header("Lightning")]
        public float lightningFrom = 0.45f;
        public float maxBoltsPerSecond = 8f;
        public Color boltColor = new Color(0.78f, 0.45f, 1f);
        public float flashIntensity = 5f;

        [Header("Tornado")]
        [Tooltip("Storm level where props start to be caught, and where the pull is full.")]
        public float catchFrom = 0.6f, catchFull = 0.85f;
        [Tooltip("Props closer than this (times the body size) get caught.")]
        public float catchRadius = 2.4f;
        public int maxDebris = 18;
        [Tooltip("Ring around the pet (times the body size).")]
        public Vector2 orbitRadius = new Vector2(0.9f, 1.5f);
        public Vector2 orbitHeight = new Vector2(-0.2f, 1.5f);
        [Tooltip("Degrees per second at full storm (rage adds more).")]
        public float orbitSpeed = 200f;
        [Tooltip("Front-back size of the ring compared to its width (flat = less in the camera's face).")]
        public float ringDepth = 0.45f;
        public float follow = 6f;
        [Tooltip("Speed the caught props are flung out with when the storm ends.")]
        public float burstSpeed = 7f;

        /// <summary>True while this prop is caught in the tornado.</summary>
        public bool IsCaught(Rigidbody rb) => debris.ContainsKey(rb);
        bool Angry => fsm == null || fsm.Mood == MoodState.Angry;

        const int BoltCount = 6;
        const int BoltPoints = 9;
        LineRenderer[] bolts;
        float[] boltLife;
        Light flash;
        float nextBolt;
        readonly Collider[] hits = new Collider[64];

        class Debris { public float angle, radius, height, speed, wobble; }
        readonly Dictionary<Rigidbody, Debris> debris = new Dictionary<Rigidbody, Debris>();
        readonly List<Rigidbody> toRelease = new List<Rigidbody>();

        float Power => look != null ? look.Storm * Mathf.Lerp(1f, 1.6f, look.Rage) : 0f;
        float Size => look != null ? look.BodyScale : 1f;
        Vector3 Centre => drag != null && drag.body != null ? drag.body.transform.TransformPoint(drag.body.center) : transform.position;

        void Start()
        {
            bolts = new LineRenderer[BoltCount];
            boltLife = new float[BoltCount];
            for (int i = 0; i < BoltCount; i++)
            {
                var go = new GameObject("Bolt_" + i);
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = boltMaterial;
                lr.positionCount = BoltPoints;
                lr.startColor = lr.endColor = boltColor;
                lr.numCornerVertices = 1;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.enabled = false;
                bolts[i] = lr;
            }
            var lightGo = new GameObject("StormFlash");
            lightGo.transform.SetParent(transform, false);
            flash = lightGo.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = boltColor;
            flash.range = 8f;
            flash.intensity = 0f;
        }

        // ---------- Lightning ----------

        void Update()
        {
            float storm = look != null && Angry ? look.Storm : 0f;
            float dt = Time.deltaTime;
            float rate = maxBoltsPerSecond * Mathf.InverseLerp(lightningFrom, 1f, storm) * Mathf.Lerp(1f, 1.5f, look != null ? look.Rage : 0f);
            if (rate > 0f && Time.time >= nextBolt)
            {
                nextBolt = Time.time + Random.Range(0.3f, 1.7f) / rate;
                Vector3 start, end;
                Strike(Centre, Size, out start, out end);
                if (Random.value < 0.5f)   // a branch from the middle of the bolt
                    Strike((start + end) * 0.5f, Size * 0.6f, out _, out _);
            }
            for (int i = 0; i < BoltCount; i++)
            {
                if (boltLife[i] <= 0f) continue;
                boltLife[i] -= dt;
                bolts[i].enabled = boltLife[i] > 0f && Random.value > 0.25f;   // flicker
            }
            flash.intensity = Mathf.MoveTowards(flash.intensity, 0f, flashIntensity * 8f * dt);
        }

        void Strike(Vector3 from, float scale, out Vector3 start, out Vector3 end)
        {
            int i = System.Array.IndexOf(boltLife, 0f);
            if (i < 0) { i = 0; for (int j = 1; j < BoltCount; j++) if (boltLife[j] < boltLife[i]) i = j; }
            Vector3 dir = Random.insideUnitCircle.normalized;
            start = from + dir * 0.4f * scale + Vector3.up * Random.Range(-0.6f, 0.9f) * scale;
            end = start + (dir + (Vector3)Random.insideUnitCircle * 0.7f).normalized * Random.Range(1f, 2f) * scale;
            Vector3 side = Vector3.Cross(end - start, Vector3.forward).normalized;
            var lr = bolts[i];
            for (int p = 0; p < BoltPoints; p++)
            {
                float t = p / (float)(BoltPoints - 1);
                float jag = (p == 0 || p == BoltPoints - 1) ? 0f : Random.Range(-0.2f, 0.2f) * scale;
                lr.SetPosition(p, Vector3.Lerp(start, end, t) + side * jag);
            }
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.25f));
            lr.widthMultiplier = 0.06f * scale;
            lr.enabled = true;
            boltLife[i] = Random.Range(0.08f, 0.2f);
            flash.transform.position = start;
            flash.intensity = flashIntensity;
        }

        // ---------- Tornado ----------

        void FixedUpdate()
        {
            float power = Angry ? Mathf.InverseLerp(catchFrom, catchFull, look != null ? look.Storm : 0f) : 0f;
            float dt = Time.fixedDeltaTime;
            Vector3 c = Centre;
            float size = Size;
            var held = ItemGrabber.Instance != null ? ItemGrabber.Instance.Held : null;

            // a storm inside the turbine only makes electricity: it does not pull props in
            if (power <= 0.01f || (drag != null && drag.Anchor.HasValue)) { ReleaseAll(); return; }

            // catch new props near the pet; when full, fling the oldest one away to make room,
            // so a storm that moves on to the shelves picks up the things there
            {
                int n = Physics.OverlapSphereNonAlloc(c, catchRadius * size, hits, propLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var rb = hits[i].attachedRigidbody;
                    if (rb == null || rb.isKinematic || rb == held || debris.ContainsKey(rb) || rb.GetComponent<Joint>() != null) continue;
                    if (debris.Count >= maxDebris) FlingOldest(c);
                    Vector3 o = rb.worldCenterOfMass - c;
                    debris[rb] = new Debris
                    {
                        angle = Mathf.Atan2(o.z / ringDepth, o.x) * Mathf.Rad2Deg,
                        radius = Random.Range(orbitRadius.x, orbitRadius.y),
                        height = Random.Range(orbitHeight.x, orbitHeight.y),
                        speed = Random.Range(0.75f, 1.25f),
                        wobble = Random.value * 10f
                    };
                    caughtOrder.Enqueue(rb);
                }
            }

            // steer every caught prop along its ring around the pet
            float spin = orbitSpeed * power * Mathf.Lerp(1f, 1.6f, look.Rage);
            toRelease.Clear();
            foreach (var pair in debris)
            {
                var rb = pair.Key;
                var d = pair.Value;
                if (rb == null || !rb.gameObject.activeInHierarchy || rb == held) { toRelease.Add(rb); continue; }
                d.angle += spin * d.speed * dt;
                float a = d.angle * Mathf.Deg2Rad;
                float ring = Mathf.Sqrt(size);   // the ring grows with the body, but slower
                float r = d.radius * ring;
                float h = (d.height + Mathf.Sin(Time.time * 1.3f + d.wobble) * 0.25f) * ring;
                Vector3 target = c + new Vector3(Mathf.Cos(a) * r, h - 0.5f * ring, Mathf.Sin(a) * r * ringDepth);
                Vector3 tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a) * ringDepth) * (spin * Mathf.Deg2Rad * r * d.speed);
                Vector3 wanted = tangent + (target - rb.worldCenterOfMass) * follow;
                rb.WakeUp();
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, wanted, (1f - Mathf.Exp(-8f * dt)) * power);
                rb.AddTorque(Random.insideUnitSphere * 6f * power, ForceMode.Acceleration);
            }
            foreach (var rb in toRelease) debris.Remove(rb);
        }

        readonly Queue<Rigidbody> caughtOrder = new Queue<Rigidbody>();

        void FlingOldest(Vector3 c)
        {
            while (caughtOrder.Count > 0)
            {
                var old = caughtOrder.Dequeue();
                if (old == null || !debris.ContainsKey(old)) continue;
                debris.Remove(old);
                Vector3 dir = old.worldCenterOfMass - c;
                dir.z *= 0.3f;
                old.linearVelocity = dir.normalized * burstSpeed + Vector3.up * 2f;
                return;
            }
        }

        void ReleaseAll()
        {
            if (debris.Count == 0) return;
            // the storm bursts: everything it carried is flung outwards, away from the pet
            Vector3 c = Centre;
            foreach (var pair in debris)
            {
                var rb = pair.Key;
                if (rb == null) continue;
                Vector3 out2D = rb.worldCenterOfMass - c;
                out2D.z *= 0.3f;
                Vector3 dir = (out2D.sqrMagnitude > 0.01f ? out2D.normalized : (Vector3)Random.insideUnitCircle.normalized);
                rb.linearVelocity = dir * burstSpeed * Random.Range(0.7f, 1.2f) + Vector3.up * Random.Range(2f, 4f);
                rb.angularVelocity = Random.onUnitSphere * 10f;
            }
            debris.Clear();
            caughtOrder.Clear();
            if (flash != null) flash.intensity = flashIntensity;
        }
    }
}
