using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>One brick of the tsunami. It has its own drive (Power, starts at 100%): over the island it pushes
    /// itself forward; every brick or person it hits takes Power (100 -> 50 -> 25 %). Gravity is always on.
    /// Over the island edge, when Power is too low, or when it is stuck, it breaks into a spray of small bricks.</summary>
    public class WaveBlock : Hazard
    {
        public float Power { get; private set; } = 1f;

        /// <summary>Debug counters: why wave bricks broke apart (edge, no Power left, stuck or too old).</summary>
        public static int EndEdge, EndPower, EndStuck;

        /// <summary>True while the brick still has drive (used for the panic level).</summary>
        public bool Moving => !gone && Power >= owner.minPower;

        Tsunami owner;
        Vector3 dir;
        float age, stuck, noGround;
        bool gone;
        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        Color baseColor;
        readonly HashSet<Object> hitAlready = new HashSet<Object>();

        public void Init(Tsunami t)
        {
            owner = t;
            dir = t.Direction;
            Body = gameObject.AddComponent<Rigidbody>();
            Body.mass = t.mass;
            Body.useGravity = true;
            Body.linearDamping = t.drag;
            Body.angularDamping = t.angularDrag;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.linearVelocity = t.LaunchVelocity;
            mr = GetComponent<MeshRenderer>();
            mpb = new MaterialPropertyBlock();
            baseColor = mr.sharedMaterial.GetColor("_BaseColor");
            UpdateLook();
        }

        void FixedUpdate()
        {
            if (gone) return;
            float dt = Time.fixedDeltaTime;
            age += dt;
            Vector3 v = Body.linearVelocity;

            // over the island? (ground or the smooth wave ground below the brick)
            bool overIsland = Physics.Raycast(Body.worldCenterOfMass, Vector3.down, owner.edgeCheckDepth, owner.GroundMask, QueryTriggerInteraction.Ignore);
            if (!overIsland && !owner.ShelfActive)
            {
                noGround += dt;
                if (noGround > 0.1f) { EndEdge++; Splash(); return; }   // over the edge: the water breaks apart
            }
            else noGround = 0f;

            // drive: push forward up to the top speed of the current Power (sideways and up-down are left to physics)
            if (overIsland || owner.ShelfActive)
            {
                float along = Vector3.Dot(v, dir);
                if (along < owner.speed * Power) Body.AddForce(dir * owner.pushAcceleration * Power, ForceMode.Acceleration);
            }

            stuck = v.magnitude < owner.settleSpeed ? stuck + dt : 0f;
            if (stuck > owner.settleTime || age > owner.lifetime || transform.position.y < -25f) { EndStuck++; Splash(); }
        }

        void OnCollisionEnter(Collision c)
        {
            if (gone) return;
            var block = c.collider.GetComponent<Block>();
            if (block != null)
            {
                if (!hitAlready.Add(block)) return;
                if (owner.activator != null) owner.activator.DamageBlock(block, owner.hitDamage * Power);
                if (block.Body != null) block.Body.AddForce(dir * owner.hitSpeed * Power, ForceMode.VelocityChange);
                LosePower();
                return;
            }
            var person = c.collider.GetComponentInParent<Person>();
            if (person != null)
            {
                if (!hitAlready.Add(person)) return;
                person.Swept(dir * owner.personPush * Power + Vector3.up * 1.5f * Power);
                LosePower();
            }
        }

        void LosePower()
        {
            Power *= owner.powerAfterHit;
            UpdateLook();
            if (Power < owner.minPower) { EndPower++; Splash(); }
        }

        /// <summary>Breaks the wave brick into small spray bricks that fly on, fall and fade (BrickFx), then removes it.</summary>
        void Splash()
        {
            if (gone) return;
            gone = true;
            Vector3 v = Body.linearVelocity;
            Vector3 c = Body.worldCenterOfMass;
            Vector3 half = Vector3.Scale(GetComponent<BoxCollider>().size, transform.lossyScale) * 0.5f;
            Color end = new Color(0.85f, 0.95f, 1f, 0f);
            for (int i = 0; i < owner.splashBricks; i++)
            {
                Vector3 p = c + transform.rotation * new Vector3(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y), Random.Range(-half.z, half.z));
                Vector3 vel = v * Random.Range(0.4f, 0.8f) + new Vector3(Random.Range(-2f, 2f), Random.Range(1f, 4f), Random.Range(-2f, 2f));
                BrickFx.Emit(p, vel, Random.Range(0.6f, 1.2f), owner.splashSize * Random.Range(0.7f, 1.3f), baseColor, end, 0f, 1f, 0.3f);
            }
            Destroy(gameObject);
        }

        void UpdateLook()
        {
            float a = baseColor.a * (0.45f + 0.55f * Power);
            mpb.SetColor("_BaseColor", new Color(baseColor.r, baseColor.g, baseColor.b, a));
            mr.SetPropertyBlock(mpb);
        }

        void OnDestroy() { if (owner != null) owner.Removed(this); }
    }
}
