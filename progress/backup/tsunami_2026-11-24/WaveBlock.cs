using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>One brick of the tsunami. Starts with 100% power; every brick or person it hits takes power
    /// (100 -> 50 -> 25 -> 12 %). A weak wave brick fades out and disappears.</summary>
    public class WaveBlock : Hazard
    {
        public float Power { get; private set; } = 1f;

        Tsunami owner;
        Vector3 dir;
        float age, fade = -1f;
        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        Color baseColor;
        readonly HashSet<Object> hitAlready = new HashSet<Object>();

        public void Init(Tsunami t, Vector3 direction)
        {
            owner = t;
            dir = direction;
            Body = gameObject.AddComponent<Rigidbody>();
            Body.mass = t.mass;
            Body.useGravity = true;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.linearVelocity = dir * t.speed * 0.6f;
            mr = GetComponent<MeshRenderer>();
            mpb = new MaterialPropertyBlock();
            baseColor = mr.sharedMaterial.GetColor("_BaseColor");
            UpdateLook();
        }

        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            if (fade >= 0f)
            {
                fade += Time.fixedDeltaTime;
                UpdateLook();
                if (fade >= owner.fadeTime) Remove();
                return;
            }
            Vector3 v = Body.linearVelocity;
            float along = Vector3.Dot(v, dir);
            if (along < owner.speed * Power) Body.AddForce(dir * owner.pushAcceleration * Power, ForceMode.Acceleration);
            Body.AddForce(-Physics.gravity * owner.buoyancy * Power, ForceMode.Acceleration);
            // water climbs over a step when it is blocked, but does not fly away: vertical speed is damped
            if (along < owner.speed * 0.25f * Power) Body.AddForce(Vector3.up * owner.climbAcceleration * Power, ForceMode.Acceleration);
            Body.AddForce(Vector3.up * (-v.y * owner.verticalDamping * Power), ForceMode.Acceleration);
            if (v.y > owner.maxRiseSpeed) Body.linearVelocity = new Vector3(v.x, owner.maxRiseSpeed, v.z);
            if (age > owner.lifetime || transform.position.y < -25f) StartFade();
        }

        void OnCollisionEnter(Collision c)
        {
            if (fade >= 0f) return;
            var block = c.collider.GetComponent<Block>();
            if (block != null)
            {
                if (!hitAlready.Add(block)) return;
                owner.activator.DamageBlock(block, owner.hitDamage * Power);
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
            if (Power < owner.minPower) StartFade();
        }

        void StartFade()
        {
            if (fade >= 0f) return;
            fade = 0f;
            Body.useGravity = true;
        }

        void UpdateLook()
        {
            float a = baseColor.a * (0.45f + 0.55f * Power);
            if (fade >= 0f) a *= 1f - Mathf.Clamp01(fade / owner.fadeTime);
            mpb.SetColor("_BaseColor", new Color(baseColor.r, baseColor.g, baseColor.b, a));
            mr.SetPropertyBlock(mpb);
        }

        void Remove()
        {
            owner.Removed(this);
            Destroy(gameObject);
        }

        void OnDestroy() { if (owner != null) owner.Removed(this); }
    }
}
