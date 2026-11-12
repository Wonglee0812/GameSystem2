using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// An enemy walking the path from waypoint to waypoint. It carries the card marks
    /// (CardMarks) and a health bar above its head. Reaching the base costs lives;
    /// dying gives gold. Boom enemies stun nearby towers when they die.
    /// Graybox: the body is a capsule primitive, coloured by type.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> All = new List<Enemy>();

        public TDConfig.EnemyType Type { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public CardMarks Marks { get; private set; }
        /// <summary>How far along the path it is (higher = closer to the base). Used for targeting.</summary>
        public float Progress { get; private set; }

        int next = 1;
        Transform head;
        Transform barFill;

        public static Enemy Spawn(TDConfig.EnemyType type, float health, Material material)
        {
            var route = PathRoute.Instance;
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(go.GetComponent<Collider>());
            go.name = "Enemy_" + type.name;
            go.transform.position = route.points[0].position + Vector3.up * type.size;
            go.transform.localScale = Vector3.one * type.size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = material;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", type.color);
            r.SetPropertyBlock(block);

            var e = go.AddComponent<Enemy>();
            e.Type = type;
            e.MaxHealth = e.Health = health;
            e.BuildHead();
            e.Marks = go.AddComponent<CardMarks>();
            e.Marks.Init(e, e.head);
            All.Add(e);
            return e;
        }

        /// <summary>Health bar (two quads, as in a world-space health bar tutorial) and room for the marks.</summary>
        void BuildHead()
        {
            head = new GameObject("Head").transform;
            head.SetParent(transform, false);
            head.localPosition = Vector3.up * 1.6f;
            head.localScale = Vector3.one / Type.size;   // same size for every enemy
            var back = Quad("BarBack", new Color(0.15f, 0.1f, 0.08f), 1.2f, 0.16f, 0.001f);
            barFill = Quad("BarFill", new Color(0.35f, 0.85f, 0.35f), 1.14f, 0.11f, 0f).transform;
        }

        GameObject Quad(string name, Color color, float w, float h, float z)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = name;
            q.transform.SetParent(head, false);
            q.transform.localPosition = new Vector3(0f, 0f, z);
            q.transform.localScale = new Vector3(w, h, 1f);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = GetComponent<Renderer>().sharedMaterial;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            r.SetPropertyBlock(block);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return q;
        }

        void Update()
        {
            var route = PathRoute.Instance;
            if (next >= route.points.Length) { ReachBase(); return; }
            Vector3 target = route.points[next].position + Vector3.up * Type.size;
            Vector3 to = target - transform.position;
            float step = Type.speed * Time.deltaTime;
            if (to.magnitude <= step) { transform.position = target; next++; }
            else transform.position += to.normalized * step;
            Progress += step;

            // health bar faces the camera and shrinks from the right
            var cam = Camera.main;
            if (cam != null) head.rotation = cam.transform.rotation;
            float k = Mathf.Clamp01(Health / MaxHealth);
            barFill.localScale = new Vector3(1.14f * k, 0.11f, 1f);
            barFill.localPosition = new Vector3(-1.14f * (1f - k) / 2f, 0f, 0f);
        }

        public void TakeDamage(float amount)
        {
            if (Health <= 0f) return;
            Health -= amount;
            if (Health <= 0f) Die();
        }

        void Die()
        {
            TDGame.Instance.AddGold(Type.gold);
            if (Type.stunRadius > 0f)
            {
                foreach (var t in Tower.All)
                    if (Vector3.Distance(t.transform.position, transform.position) <= Type.stunRadius) t.Stun(Type.stunSeconds);
                FloatText.Show("BOOM! towers stunned", transform.position + Vector3.up * 2f, new Color(1f, 0.5f, 0.2f));
            }
            Remove();
        }

        void ReachBase()
        {
            TDGame.Instance.LoseLives(Type.damage);
            Remove();
        }

        void Remove()
        {
            All.Remove(this);
            Destroy(gameObject);
        }

        void OnDestroy() => All.Remove(this);
    }
}
