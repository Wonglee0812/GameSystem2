using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>Turns built bricks into physics bodies. Bricks that sit on top of each other are joined like
    /// real toy bricks: the more studs overlap, the stronger the joint. Side neighbours are not joined.</summary>
    public class PhysicsActivator : MonoBehaviour
    {
        [Tooltip("Height in metres checked above a brick's top to find the brick sitting on it. Default 0.06.")]
        public float contactGap = 0.06f;
        [Tooltip("Solver iterations per brick. Higher = stiffer stacks but slower physics. Default 8.")]
        [Range(4, 20)] public int solverIterations = 8;
        [Tooltip("Overlap smaller than this (in studs) does not make a joint. Default 0.5.")]
        public float minOverlapStuds = 0.5f;

        [Tooltip("Seconds after physics turns on in which joints cannot break, so the first solver push does not break them. Default 0.5.")]
        public float graceSeconds = 0.5f;

        [Tooltip("Extra wear from load: at load = joint strength, wear goes this many times faster. Default 1 (so up to 2x, capped at 3x).")]
        public float loadWear = 1f;

        readonly Collider[] hits = new Collider[16];

        /// <summary>One joint between two bricks, with its full strength and how worn it is (0 = new, 1 = gone).</summary>
        class Link
        {
            public FixedJoint joint;
            public Block owner;
            public float force, torque, durability, wear;
        }
        readonly List<Link> links = new List<Link>();
        bool graceOver;

        public static PhysicsActivator Instance { get; private set; }

        void Awake() { Instance = this; }

        /// <summary>Turns physics on for a round. Bricks without a Rigidbody (new this round) get one; frozen bricks
        /// from earlier rounds wake up. Only pairs with at least one new brick get a new joint: old joints keep their
        /// wear (wear carries over between rounds), and broken joints or rubble never join again.</summary>
        public void Activate(IReadOnlyList<Block> blocks)
        {
            var fresh = new HashSet<Block>();
            foreach (var b in blocks)
            {
                if (b == null) continue;
                if (b.Body == null) fresh.Add(b);
                var body = b.MakeDynamic(solverIterations);
                body.isKinematic = false;
            }
            Physics.SyncTransforms();
            graceOver = false;
            foreach (var l in links)
            {
                if (l.joint == null) continue;
                l.joint.breakForce = Mathf.Infinity;   // grace for old joints too (waking up gives a small push)
                l.joint.breakTorque = Mathf.Infinity;
            }
            foreach (var b in blocks) if (b != null) ConnectUp(b, fresh);
            CancelInvoke(nameof(EndGrace));
            Invoke(nameof(EndGrace), graceSeconds);
        }

        /// <summary>Gives every joint its real strength after the grace time (minus its wear).</summary>
        void EndGrace()
        {
            foreach (var l in links)
            {
                if (l.joint == null) continue;
                l.joint.breakForce = l.force * (1f - l.wear);
                l.joint.breakTorque = l.torque * (1f - l.wear);
            }
            graceOver = true;
        }

        /// <summary>After a round: the standing bricks stop moving and wait for the next round (joints and wear stay).</summary>
        public void Freeze(IEnumerable<Block> standing)
        {
            foreach (var b in standing)
            {
                if (b == null || b.Body == null) continue;
                b.Body.linearVelocity = Vector3.zero;
                b.Body.angularVelocity = Vector3.zero;
                b.Body.isKinematic = true;
            }
            graceOver = false;   // no wear while frozen
        }

        /// <summary>Removes all joints of a brick that is being destroyed. Without this a joint on a neighbour would
        /// lose its connected body and pin the neighbour to the world.</summary>
        public void Forget(Block b)
        {
            for (int i = links.Count - 1; i >= 0; i--)
            {
                var l = links[i];
                if (l.joint == null) { links.RemoveAt(i); continue; }
                if (l.owner != b && (b.Body == null || l.joint.connectedBody != b.Body)) continue;
                if (l.owner != b) Destroy(l.joint);
                links.RemoveAt(i);
            }
        }

        /// <summary>Average wear of all joints (0 = new, 1 = gone), for the HUD.</summary>
        public float AverageWear
        {
            get
            {
                float sum = 0f; int n = 0;
                foreach (var l in links) if (l.joint != null) { sum += l.wear; n++; }
                return n > 0 ? sum / n : 0f;
            }
        }

        /// <summary>Direct damage to one brick (a wave or lava hit): every joint of the brick wears by
        /// seconds / durability, so the same hit hurts plastic more than steel. Returns how many joints broke.</summary>
        public int DamageBlock(Block b, float seconds)
        {
            if (b == null || seconds <= 0f) return 0;
            int broke = 0;
            for (int i = links.Count - 1; i >= 0; i--)
            {
                var l = links[i];
                if (l.joint == null) { links.RemoveAt(i); continue; }
                if (l.owner != b && l.joint.connectedBody != b.Body) continue;
                l.wear += seconds / Mathf.Max(0.1f, l.durability);
                if (l.wear >= 1f)
                {
                    l.owner.WornOut();
                    Destroy(l.joint);
                    links.RemoveAt(i);
                    broke++;
                }
                else if (graceOver)
                {
                    l.joint.breakForce = l.force * (1f - l.wear);
                    l.joint.breakTorque = l.torque * (1f - l.wear);
                }
            }
            return broke;
        }

        /// <summary>Wear: while a disaster runs, every joint loses strength. Speed = intensity x (1 + load) / durability.
        /// So no material is safe forever; stronger materials only last longer.</summary>
        void FixedUpdate()
        {
            float intensity = DisasterState.Intensity * DisasterState.WearScale;
            if (!graceOver || intensity <= 0f) return;
            float dt = Time.fixedDeltaTime;
            for (int i = links.Count - 1; i >= 0; i--)
            {
                var l = links[i];
                if (l.joint == null) { links.RemoveAt(i); continue; }
                float strength = l.force * (1f - l.wear);
                float load = strength > 0f ? l.joint.currentForce.magnitude / strength : 1f;
                float speed = Mathf.Min(3f, 1f + loadWear * load);
                l.wear += intensity * speed * dt / Mathf.Max(0.1f, l.durability);
                if (l.wear >= 1f)
                {
                    l.owner.WornOut();
                    Destroy(l.joint);
                    links.RemoveAt(i);
                    continue;
                }
                l.joint.breakForce = l.force * (1f - l.wear);
                l.joint.breakTorque = l.torque * (1f - l.wear);
            }
        }

        void ConnectUp(Block b, HashSet<Block> fresh)
        {
            var t = b.transform;
            Vector3 half = b.Box.size * 0.5f;
            Vector3 centre = t.TransformPoint(b.Box.center + Vector3.up * (half.y + contactGap * 0.5f));
            Vector3 probe = new Vector3(Mathf.Max(0.01f, half.x - 0.05f), contactGap * 0.5f, Mathf.Max(0.01f, half.z - 0.05f));
            int n = Physics.OverlapBoxNonAlloc(centre, probe, hits, t.rotation, BrickGrid.QueryMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var other = hits[i].GetComponent<Block>();
                if (other == null || other == b || other.Body == null) continue;
                if (!fresh.Contains(b) && !fresh.Contains(other)) continue;   // old pair: keep the old joint (or none if it broke)
                float studs = OverlapStuds(b.Box.bounds, other.Box.bounds);
                if (studs < minOverlapStuds) continue;

                var j = b.gameObject.AddComponent<FixedJoint>();
                j.connectedBody = other.Body;
                float force = Mathf.Min(b.material.breakForcePerStud, other.material.breakForcePerStud) * studs;
                float torque = Mathf.Min(b.material.breakTorquePerStud, other.material.breakTorquePerStud) * studs;
                j.breakForce = Mathf.Infinity;   // real values after the grace time
                j.breakTorque = Mathf.Infinity;
                links.Add(new Link { joint = j, owner = b, force = force, torque = torque,
                                     durability = Mathf.Min(b.material.durability, other.material.durability) });
                Block.JointsMade++;
            }
        }

        /// <summary>Overlap area of two footprints, counted in studs.</summary>
        static float OverlapStuds(Bounds a, Bounds b)
        {
            float x = Mathf.Max(0f, Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x));
            float z = Mathf.Max(0f, Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z));
            return x * z / (BrickGrid.Stud * BrickGrid.Stud);
        }
    }
}
