using UnityEngine;

namespace BrickPanic
{
    /// <summary>One brick of the tsunami. Gets one start speed, then pure physics: gravity, mass and collisions.
    /// No pushing forces and no scripted damage. When it has lost its momentum (slow for a short time) or gets
    /// too old, it drains away (fades out).</summary>
    public class WaveBlock : Hazard
    {
        Tsunami owner;
        float age, still, fade = -1f;
        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        Color baseColor;

        /// <summary>True while the brick still flows (used for the panic level).</summary>
        public bool Moving => fade < 0f && still < 0.5f;

        public void Init(Tsunami t)
        {
            owner = t;
            Body = gameObject.AddComponent<Rigidbody>();
            Body.mass = t.mass;
            Body.useGravity = true;
            Body.linearDamping = t.drag;
            Body.angularDamping = t.angularDrag;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;   // fast water must not pass through thin walls
            Body.linearVelocity = t.LaunchVelocity;   // the only push: one start speed
            mr = GetComponent<MeshRenderer>();
            mpb = new MaterialPropertyBlock();
            baseColor = mr.sharedMaterial.GetColor("_BaseColor");
            UpdateLook();
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            age += dt;
            if (fade >= 0f)
            {
                fade += dt;
                UpdateLook();
                if (fade >= owner.fadeTime) Destroy(gameObject);
                return;
            }
            still = Body.linearVelocity.magnitude < owner.settleSpeed ? still + dt : 0f;
            if (still > owner.settleTime || age > owner.lifetime || transform.position.y < -25f) fade = 0f;
        }

        void OnCollisionEnter(Collision c)
        {
            if (fade >= 0f) return;
            // the push comes from the collision itself; this only stops the person's steering (falls over)
            var person = c.collider.GetComponentInParent<Person>();
            if (person != null && c.relativeVelocity.magnitude > owner.sweepSpeed) person.Swept(Vector3.zero);
        }

        void UpdateLook()
        {
            float a = baseColor.a;
            if (fade >= 0f) a *= 1f - Mathf.Clamp01(fade / owner.fadeTime);
            mpb.SetColor("_BaseColor", new Color(baseColor.r, baseColor.g, baseColor.b, a));
            mr.SetPropertyBlock(mpb);
        }

        void OnDestroy() { if (owner != null) owner.Removed(this); }
    }
}
