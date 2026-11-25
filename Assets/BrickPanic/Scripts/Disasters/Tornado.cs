using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>A tornado crosses the island from one side to the other (Wong, 2026-11-25). Inside its radius it tears at
    /// joints (local wear, so steel lasts six times longer than plastic) and pushes every loose body with a swirl, a pull
    /// to the centre and a lift. The push is a force, not an acceleration, so the test is MASS: light plastic and plates
    /// fly away, heavy stone and steel stay. People are thrown over unless they stand under a roof.
    /// The funnel is drawn with small spinning bricks (same brick style as BrickFx).</summary>
    public class Tornado : Disaster
    {
        [Header("References")]
        public MapGenerator map;
        public PhysicsActivator activator;
        [Tooltip("Small brick meshes for the funnel.")]
        public Mesh[] funnelMeshes;
        [Tooltip("Material of the funnel bricks (M_BP_Fx; colour set per brick).")]
        public Material funnelMaterial;

        [Header("Path")]
        [Tooltip("Seconds to cross the island. Default 26.")]
        public float duration = 26f;
        [Tooltip("Start and end this many metres outside the island edge. Default 8.")]
        public float margin = 8f;
        [Tooltip("Side-to-side wander of the path in metres. Default 9.")]
        public float wander = 9f;

        [Header("Strength (level 1)")]
        [Tooltip("Radius in metres. Default 7.")]
        public float radius = 7f;
        [Tooltip("Lift force in newton at the centre (a plastic 2x4 weighs 78 N, steel 470 N). Default 320.")]
        public float lift = 320f;
        [Tooltip("Swirl force in newton at the centre (around the funnel). Default 260.")]
        public float swirl = 260f;
        [Tooltip("Pull to the centre in newton. Default 140.")]
        public float pull = 140f;
        [Tooltip("No lift above this height over the ground (bricks circle and then fall). Default 18.")]
        public float maxLiftHeight = 18f;
        [Tooltip("Joint wear per second at the centre, in seconds of wear (plastic lasts 10, steel 60). Default 4.")]
        public float wearPerSecond = 4f;
        [Tooltip("Force on people is multiplied by this. Above about 2 people are lifted off the ground and thrown off the island. Default 1.8.")]
        public float peopleForceScale = 1.8f;
        [Tooltip("Damage per second to a person in the core (inner half) with no roof. Default 14.")]
        public float coreDamage = 14f;
        [Tooltip("Force on people under a roof is multiplied by this. Default 0.25.")]
        public float roofedForceScale = 0.25f;

        [Header("Per level (level 1 = the values above)")]
        [Tooltip("Extra radius per level above 1 (0.1 = +10%). Default 0.1.")]
        public float radiusPerLevel = 0.1f;
        [Tooltip("Extra forces and wear per level above 1 (0.15 = +15%). Default 0.15.")]
        public float strengthPerLevel = 0.15f;

        [Header("Look")]
        [Tooltip("Funnel bricks. Default 160.")]
        public int funnelBricks = 160;
        [Tooltip("Funnel height in metres. Default 30.")]
        public float funnelHeight = 30f;
        public Color funnelColorLow = new Color(0.45f, 0.42f, 0.4f);
        public Color funnelColorHigh = new Color(0.82f, 0.85f, 0.9f);
        [Tooltip("Big dark bricks of the storm cloud on top of the funnel. Default 46.")]
        public int cloudBricks = 46;
        [Tooltip("Radius of the cloud cap in metres. Default 13.")]
        public float cloudRadius = 13f;
        public Color cloudColor = new Color(0.28f, 0.29f, 0.33f);
        [Tooltip("Wind streaks (thin white bricks circling the funnel) per second. Default 30.")]
        public float windRate = 30f;
        [Tooltip("Dirt chips thrown up at the base per second. Default 40.")]
        public float dirtRate = 40f;

        float baseRadius = -1f, baseLift, baseSwirl, basePull, baseWear;
        float time = -1f;
        Vector3 from, to, side, centre;
        float noiseSeed;
        readonly Collider[] hits = new Collider[512];
        readonly HashSet<Rigidbody> pushed = new HashSet<Rigidbody>();
        readonly HashSet<Block> worn = new HashSet<Block>();

        class FunnelBrick { public Transform t; public MeshRenderer r; public float h, angle, speed, size; }
        readonly List<FunnelBrick> funnel = new List<FunnelBrick>();
        readonly List<FunnelBrick> cloud = new List<FunnelBrick>();
        readonly HashSet<Rigidbody> ripped = new HashSet<Rigidbody>();
        float windDebt, dirtDebt;
        Transform funnelRoot;
        MaterialPropertyBlock mpb;

        public override bool Running => time >= 0f;
        /// <summary>Centre of the tornado on the ground (for the camera, the HUD and tests).</summary>
        public Vector3 Centre => centre;

        void Reset() { displayName = "Tornado"; }

        public override void SetLevel(int level)
        {
            if (baseRadius < 0f) { baseRadius = radius; baseLift = lift; baseSwirl = swirl; basePull = pull; baseWear = wearPerSecond; }
            int k = Mathf.Max(0, level - 1);
            float s = 1f + strengthPerLevel * k;
            radius = baseRadius * (1f + radiusPerLevel * k);
            lift = baseLift * s; swirl = baseSwirl * s; pull = basePull * s; wearPerSecond = baseWear * s;
        }

        /// <summary>Picks the side the tornado comes from (same sides as the tsunami, so the forecast arrow can show it).</summary>
        public int PickSide() => Random.Range(0, 4);

        public override void Begin() => Begin(PickSide());

        public void Begin(int fromSide)
        {
            Vector3 dir = Tsunami.SideDirection(fromSide);
            float half = map.size * 0.5f * MapGenerator.Cell;
            Vector3 c = map.transform.position;
            side = Vector3.Cross(Vector3.up, dir);
            float offset = Random.Range(-0.25f, 0.25f) * half;   // not always right through the middle
            from = c - dir * (half + margin) + side * offset;
            to = c + dir * (half + margin) + side * offset;
            noiseSeed = Random.value * 100f;
            time = 0f;
            centre = from;
            DisasterState.WearScale = 0f;   // no island-wide wear: the tornado wears joints only where it is
            if (mpb == null) mpb = new MaterialPropertyBlock();
            BuildFunnel();
        }

        void FixedUpdate()
        {
            if (time < 0f) return;
            time += Time.fixedDeltaTime;
            float k = time / duration;
            if (k >= 1f) { End(); return; }
            float ramp = Mathf.Clamp01(time / 2f) * Mathf.Clamp01((duration - time) / 2f);
            float w = (Mathf.PerlinNoise(noiseSeed, time * 0.15f) * 2f - 1f) * wander;
            centre = Vector3.Lerp(from, to, k) + side * w;
            centre.y = GroundY(centre);
            DisasterState.Intensity = 0.7f * ramp;

            // forces on every loose body in range
            pushed.Clear();
            worn.Clear();
            int n = Physics.OverlapSphereNonAlloc(centre + Vector3.up * (radius * 0.5f), radius * 1.3f, hits, BrickGrid.QueryMask, QueryTriggerInteraction.Ignore);
            float dt = Time.fixedDeltaTime;
            for (int i = 0; i < n; i++)
            {
                var c = hits[i];
                var block = c.GetComponent<Block>();
                Vector3 p = c.bounds.center;
                Vector3 d = p - centre; d.y = 0f;
                float dist = d.magnitude;
                if (dist > radius) continue;
                float fall = (1f - dist / radius) * ramp;
                if (block != null && worn.Add(block) && activator != null) activator.DamageBlock(block, wearPerSecond * fall * dt);
                var rb = c.attachedRigidbody;
                if (rb == null || rb.isKinematic || !pushed.Add(rb)) continue;
                Vector3 radial = dist > 0.05f ? d / dist : Vector3.forward;
                Vector3 tangent = Vector3.Cross(Vector3.up, radial);
                float height = p.y - centre.y;
                float liftK = Mathf.Clamp01(1f - height / maxLiftHeight);
                Vector3 force = (tangent * swirl - radial * pull) * fall + Vector3.up * (lift * fall * liftK);
                var person = rb.GetComponent<Person>();
                if (person != null)
                {
                    if (!person.Alive) continue;
                    float scale = person.Roofed ? roofedForceScale : peopleForceScale;
                    force *= scale;
                    if (!person.Roofed && dist < radius * 0.5f)
                    {
                        person.Damage(coreDamage * dt, "tornado");
                        if (person.Alive && person.Current != Person.State.Tumble && Random.value < 0.8f * dt) person.Swept(Vector3.up * 2f + tangent * 2f);
                    }
                }
                rb.AddForce(force, ForceMode.Force);
                if (block != null && fall > 0.4f && ripped.Add(rb)) BrickFx.Snap(c.bounds.center, BrickFx.ColorOf(block), 1.2f);   // torn loose
            }
            BPJuice.AddTrauma(0.03f * ramp, centre);
        }

        void Update()
        {
            if (time < 0f || funnelRoot == null) return;
            float ramp = Mathf.Clamp01(time / 2f) * Mathf.Clamp01((duration - time) / 2f);
            funnelRoot.position = centre;
            float dt = Time.deltaTime;
            foreach (var f in funnel)
            {
                f.h += dt * (1.5f + f.h * 0.12f);                  // bricks climb the funnel
                if (f.h > funnelHeight) f.h -= funnelHeight;
                float hk = f.h / funnelHeight;
                float r = radius * (0.12f + 0.7f * hk * hk) + 0.4f;   // narrow at the ground, wide at the top
                f.angle += dt * f.speed * (1.4f - hk * 0.6f);
                f.t.localPosition = new Vector3(Mathf.Cos(f.angle) * r, f.h, Mathf.Sin(f.angle) * r);
                f.t.Rotate(Vector3.up, 400f * dt, Space.Self);
                f.t.localScale = Vector3.one * (f.size * ramp * (0.6f + hk));
                mpb.SetColor("_BaseColor", Color.Lerp(funnelColorLow, funnelColorHigh, hk));
                f.r.SetPropertyBlock(mpb);
            }
            // cloud cap: big dark bricks turning slowly on top of the funnel
            foreach (var c in cloud)
            {
                c.angle += dt * c.speed;
                float r = c.h;
                c.t.localPosition = new Vector3(Mathf.Cos(c.angle) * r, funnelHeight + Mathf.Sin(c.angle * 2f + r) * 0.8f, Mathf.Sin(c.angle) * r);
                c.t.Rotate(Vector3.up, 30f * dt, Space.Self);
                c.t.localScale = Vector3.one * (c.size * ramp);
            }

            // wind streaks: thin pale bricks thrown along the swirl, wider than the funnel
            windDebt += windRate * ramp * dt;
            while (windDebt >= 1f)
            {
                windDebt -= 1f;
                float a = Random.value * Mathf.PI * 2f, rr = radius * Random.Range(0.8f, 1.5f), hh = Random.Range(0.5f, funnelHeight * 0.6f);
                Vector3 at = centre + new Vector3(Mathf.Cos(a) * rr, hh, Mathf.Sin(a) * rr);
                Vector3 v = Vector3.Cross(Vector3.up, (at - centre).normalized) * Random.Range(14f, 20f) + Vector3.up * 2f;
                var wind = new Color(0.95f, 0.97f, 1f, 0.8f);
                BrickFx.Emit(at, v, Random.Range(0.4f, 0.7f), Random.Range(0.3f, 0.5f), wind, new Color(1f, 1f, 1f, 0f), 0.3f, 0f, 0.5f);
            }

            // dirt and grass chips thrown up at the base, and a dust ring
            dirtDebt += dirtRate * ramp * dt;
            while (dirtDebt >= 1f)
            {
                dirtDebt -= 1f;
                float a = Random.value * Mathf.PI * 2f, rr = radius * Random.Range(0.2f, 0.7f);
                Vector3 at = centre + new Vector3(Mathf.Cos(a) * rr, 0.3f, Mathf.Sin(a) * rr);
                Vector3 v = Vector3.Cross(Vector3.up, (at - centre).normalized) * Random.Range(5f, 9f) + Vector3.up * Random.Range(5f, 10f);
                bool grass = Random.value < 0.5f;
                Color col = grass ? new Color(0.3f, 0.65f, 0.25f) : new Color(0.55f, 0.4f, 0.25f);
                BrickFx.Emit(at, v, Random.Range(0.8f, 1.4f), Random.Range(0.35f, 0.6f), col, col * 0.7f, 0f, 0.6f, 0.3f);
            }
            if (Random.value < 0.8f)
            {
                var a = Random.value * Mathf.PI * 2f;
                Vector3 at = centre + new Vector3(Mathf.Cos(a), 0.2f, Mathf.Sin(a)) * radius * 0.6f;
                Vector3 v = Vector3.Cross(Vector3.up, (at - centre).normalized) * 7f + Vector3.up * 1.5f;
                var dust = new Color(0.85f, 0.78f, 0.62f);
                BrickFx.Emit(at, v, Random.Range(0.8f, 1.3f), Random.Range(0.5f, 0.9f), dust, new Color(dust.r, dust.g, dust.b, 0f), 0f, -0.15f, 1f);
            }
        }

        void BuildFunnel()
        {
            if (funnelRoot != null) Destroy(funnelRoot.gameObject);
            funnel.Clear();
            funnelRoot = new GameObject("TornadoFunnel").transform;
            cloud.Clear();
            ripped.Clear();
            if (funnelMeshes == null || funnelMeshes.Length == 0 || funnelMaterial == null) return;
            var cmpb = new MaterialPropertyBlock();
            for (int i = 0; i < cloudBricks; i++)
            {
                var go = new GameObject("CloudBrick");
                go.transform.SetParent(funnelRoot, false);
                go.AddComponent<MeshFilter>().sharedMesh = funnelMeshes[funnelMeshes.Length - 1];
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = funnelMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), Random.Range(-8f, 8f));
                cmpb.SetColor("_BaseColor", cloudColor * Random.Range(0.85f, 1.15f));
                r.SetPropertyBlock(cmpb);
                cloud.Add(new FunnelBrick { t = go.transform, r = r, h = Mathf.Sqrt(Random.value) * cloudRadius, angle = Random.value * Mathf.PI * 2f, speed = Random.Range(0.4f, 0.8f), size = Random.Range(2.5f, 4.5f) });
            }
            for (int i = 0; i < funnelBricks; i++)
            {
                var go = new GameObject("FunnelBrick");
                go.transform.SetParent(funnelRoot, false);
                go.AddComponent<MeshFilter>().sharedMesh = funnelMeshes[i % funnelMeshes.Length];
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = funnelMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.transform.rotation = Random.rotation;
                funnel.Add(new FunnelBrick { t = go.transform, r = r, h = Random.value * funnelHeight, angle = Random.value * Mathf.PI * 2f, speed = Random.Range(3.5f, 5.5f), size = Random.Range(0.8f, 1.5f) });
            }
        }

        float GroundY(Vector3 p)
        {
            int ground = LayerMask.GetMask(BrickGrid.GroundLayerName);   // the island only, not houses on it
            if (ground != 0 && Physics.Raycast(new Vector3(p.x, map.transform.position.y + 60f, p.z), Vector3.down, out var h, 120f, ground, QueryTriggerInteraction.Ignore))
                return h.point.y;
            return map.transform.position.y;
        }

        void End()
        {
            time = -1f;
            DisasterState.Intensity = 0f;
            DisasterState.WearScale = 1f;
            if (funnelRoot != null) Destroy(funnelRoot.gameObject);
            funnel.Clear();
        }

        void OnDisable() { if (time >= 0f) End(); }
    }
}
