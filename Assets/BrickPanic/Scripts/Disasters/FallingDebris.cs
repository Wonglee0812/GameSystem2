using UnityEngine;

namespace BrickPanic
{
    /// <summary>One falling rock or ash bomb of the shelter rule. Hurts a person it hits while it is still falling fast,
    /// wears the joints of a brick it hits a little, then breaks apart (brick-break effect) shortly after it lands.</summary>
    public class FallingDebris : Hazard
    {
        float damage, landedAt = -1f;
        string cause;
        bool hot, hitSomeone;

        public void Init(float damage, string cause, bool hot)
        {
            this.damage = damage;
            this.cause = cause;
            this.hot = hot;
            Body = gameObject.AddComponent<Rigidbody>();
            Body.mass = 12f;
            Body.linearVelocity = Vector3.down * 6f;
            Body.angularVelocity = Random.insideUnitSphere * 6f;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Destroy(gameObject, 12f);
        }

        void OnCollisionEnter(Collision c)
        {
            var person = c.collider.GetComponentInParent<Person>();
            if (person != null && !hitSomeone && c.relativeVelocity.magnitude > 4f)
            {
                hitSomeone = true;
                person.Damage(damage, cause);
                if (person.Alive) person.Swept(Vector3.down * 0.5f + Random.insideUnitSphere * 1.5f);
            }
            var block = c.collider.GetComponent<Block>();
            if (block != null && landedAt < 0f && PhysicsActivator.Instance != null) PhysicsActivator.Instance.DamageBlock(block, hot ? 4f : 2f);
            if (landedAt < 0f)
            {
                landedAt = Time.time;
                BPJuice.AddTrauma(0.05f, transform.position);
                if (hot && block != null) block.Ignite();
            }
        }

        void Update()
        {
            if (landedAt >= 0f && Time.time - landedAt > 0.8f)
            {
                var r = GetComponent<MeshRenderer>();
                Color col = r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.grey;
                BrickFx.Break(transform.position, Vector3.one * 0.6f * transform.localScale.x, col, 0.6f);
                Destroy(gameObject);
            }
        }
    }
}
