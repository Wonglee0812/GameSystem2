using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// One placed building part (material x shape). In the build phase it is frozen
    /// (kinematic) so the player can stack freely. When the flood starts it becomes a
    /// physics body and welds to the parts it touches with breakable joints.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(WaterBody))]
    public class Part : MonoBehaviour
    {
        public static readonly List<Part> All = new List<Part>();
        static readonly HashSet<long> weldedPairs = new HashSet<long>();

        public MaterialDef material;
        public ShapeDef shape;
        public int cost;
        public bool IsActive { get; private set; }
        /// <summary>Time the part became a physics body (used to ignore settling bumps).</summary>
        public float ActiveSince { get; private set; }

        public Rigidbody Body { get; private set; }

        void Awake() { Body = GetComponent<Rigidbody>(); }
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Freeze()
        {
            IsActive = false;
            Body.isKinematic = true;
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            ActiveSince = Time.time;
            Body.isKinematic = false;
            Body.WakeUp();
        }

        static long PairKey(Part a, Part b)
        {
            int x = a.GetInstanceID(), y = b.GetInstanceID();
            if (x > y) { int t = x; x = y; y = t; }
            return ((long)x << 32) ^ (uint)y;
        }

        /// <summary>
        /// Joins this part to the parts it touches when one of the two is mud.
        /// Mud works like mortar: it glues neighbours together and sticks to the ground.
        /// Wood and stone parts without mud stay loose (weight and friction only).
        /// </summary>
        public void WeldToNeighbours(Collider groundCollider)
        {
            var half = Vector3.Scale(GetComponent<Collider>().bounds.extents, Vector3.one) + Vector3.one * 0.06f;
            var hits = Physics.OverlapBox(GetComponent<Collider>().bounds.center, half, Quaternion.identity);
            foreach (var hit in hits)
            {
                if (hit == groundCollider)
                {
                    if (material.gluesToGround && !weldedPairs.Contains(GetInstanceID()))
                    {
                        weldedPairs.Add(GetInstanceID());
                        var g = gameObject.AddComponent<FixedJoint>();
                        Arm(g, material.jointStrength);
                    }
                    continue;
                }
                var other = hit.GetComponent<Part>();
                if (other == null || other == this) continue;
                // face neighbours only: skip parts that touch at an edge or corner (diagonal)
                Vector3 d = (hit.bounds.center - GetComponent<Collider>().bounds.center).normalized;
                float align = Mathf.Max(Mathf.Abs(Vector3.Dot(d, transform.right)), Mathf.Abs(Vector3.Dot(d, transform.up)), Mathf.Abs(Vector3.Dot(d, transform.forward)));
                if (align < 0.8f) continue;
                // only join real contacts, not boxes that just overlap at the corners
                if (!Physics.ComputePenetration(GetComponent<Collider>(), transform.position, transform.rotation,
                        hit, hit.transform.position, hit.transform.rotation, out _, out float dist) && !Touching(hit)) continue;
                // only mud glues: wood and stone lie loose and hold by weight and friction
                bool mud = material.gluesToGround || other.material.gluesToGround;
                if (!mud) continue;
                long key = PairKey(this, other);
                if (weldedPairs.Contains(key)) continue;
                weldedPairs.Add(key);
                float strength = Mathf.Max(material.jointStrength, other.material.jointStrength);
                var j = gameObject.AddComponent<FixedJoint>();
                j.connectedBody = other.Body;
                j.enablePreprocessing = false;
                Arm(j, strength);
            }
        }

        struct Weld { public Joint joint; public float strength; }
        readonly List<Weld> welds = new List<Weld>();

        /// <summary>
        /// PhysX breaks a joint on any force, also when the joint just carries the weight
        /// of the parts above it. So joints get infinite PhysX strength, and this script
        /// breaks them itself on pull (tension) and shear only. Pressing together never
        /// breaks a joint; that load is the stack's own weight.
        /// </summary>
        void Arm(Joint joint, float strength)
        {
            joint.breakForce = Mathf.Infinity;
            joint.breakTorque = Mathf.Infinity;
            welds.Add(new Weld { joint = joint, strength = strength });
        }

        void FixedUpdate()
        {
            if (!IsActive || welds.Count == 0 || Time.time - ActiveSince < SettleTime) return;
            for (int i = welds.Count - 1; i >= 0; i--)
            {
                var w = welds[i];
                if (w.joint == null) { welds.RemoveAt(i); continue; }
                Vector3 f = w.joint.currentForce; // force on this part
                Vector3 n = w.joint.connectedBody != null
                    ? (w.joint.connectedBody.worldCenterOfMass - Body.worldCenterOfMass).normalized
                    : Vector3.down; // glued to the ground below
                float along = Vector3.Dot(f, n);
                if (along < 0f) f -= n * along; // remove compression
                if (f.magnitude > w.strength || w.joint.currentTorque.magnitude > w.strength * 1.5f)
                {
                    if (LogSnaps) Debug.Log($"[DamRush] joint snap {name}: shear/pull {f.magnitude:0} N, torque {w.joint.currentTorque.magnitude:0} Nm, limit {w.strength:0}");
                    Destroy(w.joint);
                    welds.RemoveAt(i);
                    Breakable.JointsSnapped++;
                }
            }
        }

        public const float SettleTime = 0.75f;
        public static bool LogSnaps;

        bool Touching(Collider other)
        {
            var mine = GetComponent<Collider>();
            Vector3 p = other.ClosestPoint(mine.bounds.center);
            Vector3 q = mine.ClosestPoint(p);
            return (p - q).sqrMagnitude < 0.06f * 0.06f;
        }

        public static void ForgetWelds() { weldedPairs.Clear(); }
    }
}
