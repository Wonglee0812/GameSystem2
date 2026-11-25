using UnityEngine;

namespace BrickPanic
{
    /// <summary>A glowing lava brick thrown by the volcano. Falls slowly (lower gravity) with a smoke trail.
    /// Kills people it hits, damages bricks and sets plastic and wood on fire. A big bomb explodes where it lands
    /// (brick explosion + push + fire spot); a small one cools down and disappears.</summary>
    public class LavaBomb : Hazard
    {
        Eruption owner;
        bool big, exploded;
        float landedAt = -1f, nextTrail;
        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        Color baseColor, emission;
        static readonly Collider[] hits = new Collider[96];

        public bool Hot => landedAt < 0f || Time.time - landedAt < owner.coolTime * 0.5f;

        public void Init(Eruption e, Vector3 velocity, bool isBig, float mass)
        {
            owner = e;
            big = isBig;
            Body = gameObject.AddComponent<Rigidbody>();
            Body.mass = mass;
            Body.useGravity = false;   // own, lower gravity in FixedUpdate
            Body.linearVelocity = velocity;
            Body.angularVelocity = Random.insideUnitSphere * 4f;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            mr = GetComponent<MeshRenderer>();
            mpb = new MaterialPropertyBlock();
            baseColor = mr.sharedMaterial.GetColor("_BaseColor");
            emission = mr.sharedMaterial.GetColor("_EmissionColor");
        }

        void FixedUpdate()
        {
            float g = landedAt < 0f ? owner.bombGravity : 1f;   // normal gravity once it landed
            Body.AddForce(Physics.gravity * g, ForceMode.Acceleration);
        }

        void OnCollisionEnter(Collision c)
        {
            var person = c.collider.GetComponentInParent<Person>();
            if (person != null && Hot) person.Die("lava");
            var block = c.collider.GetComponent<Block>();
            if (block != null && Hot)
            {
                owner.activator.DamageBlock(block, owner.hitDamage);
                block.Ignite();
            }
            if (landedAt < 0f)
            {
                landedAt = Time.time;
                if (big) Explode(c.contactCount > 0 ? c.GetContact(0).point : transform.position);
            }
        }

        /// <summary>Big bomb: brick explosion, push, kill very close people, set bricks on fire, leave a fire spot.</summary>
        void Explode(Vector3 at)
        {
            if (exploded) return;
            exploded = true;
            float r = owner.explosionRadius;
            BrickFx.Explosion(at, r);
            int n = Physics.OverlapSphereNonAlloc(at, r, hits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var col = hits[i];
                if (col.attachedRigidbody == Body) continue;
                Vector3 d = col.ClosestPoint(at) - at;
                float near = 1f - Mathf.Clamp01(d.magnitude / r);
                Vector3 push = (d.sqrMagnitude > 0.01f ? d.normalized : Vector3.up) + Vector3.up * 0.6f;
                var p = col.GetComponentInParent<Person>();
                if (p != null)
                {
                    if (d.magnitude < owner.killRadius) p.Die("explosion");
                    else p.Swept(push.normalized * owner.explosionPush * near);
                    continue;
                }
                var b = col.GetComponent<Block>();
                if (b != null)
                {
                    owner.activator.DamageBlock(b, owner.hitDamage * near);
                    b.Ignite();
                    if (b.Body != null) b.Body.AddForce(push.normalized * owner.explosionPush * near, ForceMode.VelocityChange);
                }
            }
            FireSpot.Create(at, r * 0.7f, owner.fireTime);
            Destroy(gameObject);
        }

        void Update()
        {
            if (landedAt < 0f)
            {
                if (Time.time >= nextTrail)   // smoke trail while flying
                {
                    nextTrail = Time.time + (big ? 0.05f : 0.09f);
                    BrickFx.Smoke(transform.position, big ? 1f : 0.6f, 0.5f);
                }
                if (transform.position.y < -25f) Destroy(gameObject);
                return;
            }
            float k = Mathf.Clamp01((Time.time - landedAt) / owner.coolTime);
            mpb.SetColor("_BaseColor", Color.Lerp(baseColor, new Color(0.15f, 0.12f, 0.11f), k));
            mpb.SetColor("_EmissionColor", Color.Lerp(emission, Color.black, k));
            mr.SetPropertyBlock(mpb);
            if (k >= 1f || transform.position.y < -25f) Destroy(gameObject);
        }
    }
}
