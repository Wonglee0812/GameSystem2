using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// An enemy walking the path from waypoint to waypoint. It carries the card marks
    /// (CardMarks) and a health bar above its head. Reaching the base costs lives;
    /// dying gives gold. Boom enemies stun nearby towers when they die.
    /// Look: a Kenney UFO (CC0) that bobs and spins; graybox capsule when the art asset is missing.
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
        float slowFactor = 1f, slowTime, freezeTime;

        /// <summary>Slow item: speed x factor for some seconds (the stronger slow wins).</summary>
        public void Slow(float factor, float seconds)
        {
            if (slowTime <= 0f || factor < slowFactor) slowFactor = factor;
            slowTime = Mathf.Max(slowTime, seconds);
        }

        /// <summary>Freeze item: stops walking for some seconds.</summary>
        public void Freeze(float seconds)
        {
            freezeTime = Mathf.Max(freezeTime, seconds);
            FloatText.Show("Frozen", transform.position + Vector3.up * 2f, new Color(0.6f, 0.9f, 1f));
        }
        Transform head;
        Transform barFill;
        Transform body;      // Kenney model (null in graybox)
        float bobPhase;
        /// <summary>Plain material for the health bar and mark timer quads.</summary>
        public Material BarMaterial { get; private set; }

        public static Enemy Spawn(TDConfig.EnemyType type, float health, Material material)
        {
            var route = PathRoute.Instance;
            var model = PTDArt.I != null ? PTDArt.I.Ufo(type.name) : null;
            GameObject go;
            Transform body = null;
            if (model != null)
            {
                go = new GameObject("Enemy_" + type.name);
                go.transform.position = route.points[0].position + Vector3.up * type.size;
                body = PTDArt.Place(model, go.transform, Vector3.down * 0.5f * type.size, type.size * 1.6f);
                PTDArt.Tint(body, Color.Lerp(Color.white, type.color, 0.35f));
                foreach (var mr in body.GetComponentsInChildren<Renderer>()) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(go.GetComponent<Collider>());
                go.name = "Enemy_" + type.name;
                go.transform.position = route.points[0].position + Vector3.up * type.size;
                go.transform.localScale = Vector3.one * type.size;
                var r = go.GetComponent<Renderer>();
                r.sharedMaterial = material;
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", type.color);
                r.SetPropertyBlock(block);
            }

            var e = go.AddComponent<Enemy>();
            e.body = body;
            e.bobPhase = Random.value * 6.28f;
            e.BarMaterial = material;
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
            float k = transform.localScale.x;   // graybox: scaled by size; Kenney: 1
            head.localPosition = Vector3.up * (body != null ? 1.15f : 1.6f) * Type.size / k;
            head.localScale = Vector3.one / k;   // same size for every enemy
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
            r.sharedMaterial = BarMaterial;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            r.SetPropertyBlock(block);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return q;
        }

        void Update()
        {
            if (body != null)   // UFO hover: bob up and down, spin (not while frozen)
            {
                body.localPosition = Vector3.up * (-0.5f * Type.size + Mathf.Sin(Time.time * 3f + bobPhase) * 0.08f);
                if (freezeTime <= 0f) body.Rotate(0f, 120f * Time.deltaTime, 0f, Space.Self);
            }
            var route = PathRoute.Instance;
            if (next >= route.points.Length) { ReachBase(); return; }
            Vector3 target = route.points[next].position + Vector3.up * Type.size;
            Vector3 to = target - transform.position;
            slowTime -= Time.deltaTime;
            freezeTime -= Time.deltaTime;
            float speed = freezeTime > 0f ? 0f : slowTime > 0f ? Type.speed * slowFactor : Type.speed;
            float step = speed * Time.deltaTime;
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
