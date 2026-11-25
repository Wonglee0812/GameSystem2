using UnityEngine;

namespace BrickPanic
{
    /// <summary>Fire left by a big lava bomb. Throws flame bricks, sets bricks in the radius on fire, and a person
    /// who stands in it too long dies.</summary>
    public class FireSpot : MonoBehaviour
    {
        public float radius = 2f;
        public float duration = 6f;
        public float personBurnSeconds = 1.5f;

        float age, nextTick, nextFlame;
        static readonly Collider[] hits = new Collider[64];

        public static void Create(Vector3 pos, float radius, float duration)
        {
            var go = new GameObject("FireSpot");
            go.transform.position = pos;
            var f = go.AddComponent<FireSpot>();
            f.radius = radius;
            f.duration = duration;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age > duration) { Destroy(gameObject); return; }
            float strength = 1f - age / duration;
            if (Time.time >= nextFlame)
            {
                nextFlame = Time.time + Mathf.Lerp(0.15f, 0.04f, strength);
                Vector2 r = Random.insideUnitCircle * radius;
                BrickFx.Flame(transform.position + new Vector3(r.x, 0.2f, r.y), 0.5f + 0.5f * strength);
            }
            if (Time.time < nextTick) return;
            nextTick = Time.time + 0.5f;
            int n = Physics.OverlapSphereNonAlloc(transform.position, radius, hits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var b = hits[i].GetComponent<Block>();
                if (b != null) { b.Ignite(); continue; }
                var p = hits[i].GetComponentInParent<Person>();
                if (p != null) p.Burn(0.5f, personBurnSeconds);
            }
        }
    }
}
