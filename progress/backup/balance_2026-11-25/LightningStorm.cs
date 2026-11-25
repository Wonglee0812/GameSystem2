using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>A thunderstorm (Wong, 2026-11-25). Lightning strikes again and again; each strike picks a random spot and
    /// hits the HIGHEST thing within strikeRadius of it - a brick, a person or the ground. Steel bricks count as higher than
    /// they are (they draw the lightning), so a tall steel pillar works as a lightning rod: it takes the strike and sends it
    /// safely into the ground. Other materials are damaged (wood also catches fire), a person who is hit loses a lot of
    /// health, and people close to a strike on the ground get a smaller side flash. The test is MATERIAL + HEIGHT.
    /// A short glow on the target warns the player just before each strike.</summary>
    public class LightningStorm : Disaster
    {
        [Header("References")]
        public MapGenerator map;
        public PhysicsActivator activator;
        [Tooltip("Material of the bolt line (vertex colours, unlit: M_BP_Bolt).")]
        public Material boltMaterial;
        [Tooltip("Material of the rain streaks (particles, unlit: M_BP_Rain). Empty = no rain.")]
        public Material rainMaterial;

        [Header("Storm (level 1)")]
        [Tooltip("Length of the storm in seconds. Default 22.")]
        public float duration = 22f;
        [Tooltip("Seconds between strikes (random between x and y). Default 0.8 - 1.6.")]
        public Vector2 interval = new Vector2(0.8f, 1.6f);
        [Tooltip("A strike hits the highest thing within this many metres of its random spot. Default 6.")]
        public float strikeRadius = 6f;
        [Tooltip("Steel counts this many metres higher than it is (lightning rod). Default 4.")]
        public float steelBonus = 4f;
        [Tooltip("Part of the strikes aimed near a person with no roof (the rest at random spots). Default 0.4.")]
        [Range(0f, 1f)] public float aimAtExposed = 0.4f;
        [Tooltip("Seconds of warning glow before a strike. Default 0.7.")]
        public float warnTime = 0.7f;

        [Header("Damage")]
        [Tooltip("Damage to a person hit directly. Default 75.")]
        public float personDamage = 75f;
        [Tooltip("Damage to people within sideFlashRadius of a strike that did not hit steel. Default 25.")]
        public float sideFlashDamage = 25f;
        [Tooltip("Side flash radius in metres. Default 2.5.")]
        public float sideFlashRadius = 2.5f;
        [Tooltip("Joint wear of a hit brick, in seconds (plastic lasts 10). Steel takes none. Default 8.")]
        public float brickWear = 8f;
        [Tooltip("Push on a hit brick (speed change in m/s, up and out). Default 4.")]
        public float brickKick = 4f;

        [Header("Per level (level 1 = the values above)")]
        [Tooltip("Strikes come this much faster per level above 1 (0.12 = 12% shorter waits). Default 0.12.")]
        public float fasterPerLevel = 0.12f;
        [Tooltip("Extra damage per level above 1 (0.1 = +10%). Default 0.1.")]
        public float damagePerLevel = 0.1f;

        [Header("Look")]
        public Color boltColor = new Color(0.85f, 0.92f, 1f);
        public Color glowColor = new Color(0.6f, 0.85f, 1f);
        [Tooltip("Rain drops per second. Default 600.")]
        public float rainRate = 600f;

        float time = -1f, nextStrike;
        Vector2 baseInterval = new Vector2(-1f, -1f);
        float basePersonDamage, baseSide, baseWear;
        ParticleSystem rain;

        class Pending { public Vector3 point; public Block block; public Person person; public float at; public Light glow; }
        readonly List<Pending> pending = new List<Pending>();

        public override bool Running => time >= 0f || pending.Count > 0;

        void Reset() { displayName = "Lightning"; }

        public override void SetLevel(int level)
        {
            if (baseInterval.x < 0f) { baseInterval = interval; basePersonDamage = personDamage; baseSide = sideFlashDamage; baseWear = brickWear; }
            int k = Mathf.Max(0, level - 1);
            interval = baseInterval / (1f + fasterPerLevel * k);
            float s = 1f + damagePerLevel * k;
            personDamage = basePersonDamage * s; sideFlashDamage = baseSide * s; brickWear = baseWear * s;
        }

        public override void Begin()
        {
            time = 0f;
            nextStrike = 1.5f;
            DisasterState.WearScale = 0f;    // no island-wide wear: only strikes hurt bricks
            StartRain();
        }

        void Update()
        {
            if (time < 0f && pending.Count == 0) return;
            if (time >= 0f)
            {
                time += Time.deltaTime;
                float ramp = Mathf.Clamp01(time / 2f) * Mathf.Clamp01((duration - time) / 2f);
                DisasterState.Intensity = 0.6f * ramp;
                if (time >= nextStrike && time < duration - 1f)
                {
                    nextStrike = time + Random.Range(interval.x, interval.y);
                    Plan();
                }
                if (time >= duration) End();
            }
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var p = pending[i];
                float left = p.at - Time.time;
                if (p.glow != null) p.glow.intensity = Mathf.Lerp(6f, 0.5f, left / warnTime) * (0.6f + 0.4f * Mathf.Sin(Time.time * 60f));
                if (left > 0f) continue;
                pending.RemoveAt(i);
                if (p.glow != null) Destroy(p.glow.gameObject);
                Strike(p);
            }
        }

        /// <summary>Chooses a target now and strikes it after the warning glow.</summary>
        void Plan()
        {
            Vector3 spot = RandomSpot();
            if (Random.value < aimAtExposed)
            {
                var open = new List<Person>();
                foreach (var p in Person.All) if (p != null && p.Alive && !p.Roofed) open.Add(p);
                if (open.Count > 0) { var who = open[Random.Range(0, open.Count)]; Vector2 m = Random.insideUnitCircle * 1.5f; spot = who.transform.position + new Vector3(m.x, 0f, m.y); }
            }
            var target = new Pending { point = spot };
            float best = GroundTop(spot);
            target.point = new Vector3(spot.x, best, spot.z);
            float r2 = strikeRadius * strikeRadius;
            foreach (var b in Block.All)
            {
                if (b == null || b.Box == null) continue;
                var bounds = b.Box.bounds;
                Vector3 d = bounds.center - spot; d.y = 0f;
                if (d.sqrMagnitude > r2) continue;
                float h = bounds.max.y + (IsSteel(b) ? steelBonus : 0f);
                if (h > best) { best = h; target.block = b; target.person = null; target.point = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z); }
            }
            foreach (var p in Person.All)
            {
                if (p == null || !p.Alive) continue;
                Vector3 d = p.transform.position - spot; d.y = 0f;
                if (d.sqrMagnitude > r2) continue;
                float h = p.transform.position.y + 1.9f * p.transform.localScale.y;
                if (h > best) { best = h; target.person = p; target.block = null; target.point = p.transform.position + Vector3.up * 1.9f * p.transform.localScale.y; }
            }
            target.at = Time.time + warnTime;
            var lg = new GameObject("StrikeWarning").AddComponent<Light>();
            lg.type = LightType.Point;
            lg.color = glowColor;
            lg.range = 4f;
            lg.intensity = 0.5f;
            lg.transform.position = target.point + Vector3.up * 0.6f;
            target.glow = lg;
            pending.Add(target);
            BrickFx.Spark(target.point + Vector3.up * 0.3f, Vector3.up * 2f, 0.3f);
        }

        void Strike(Pending p)
        {
            Vector3 at = p.point;
            if (p.person != null && p.person.Alive) at = p.person.transform.position + Vector3.up * 1.9f * p.person.transform.localScale.y;
            else if (p.block != null && p.block.Box != null) { var b = p.block.Box.bounds; at = new Vector3(b.center.x, b.max.y, b.center.z); }
            Bolt(at);
            bool grounded = false;
            if (p.person != null && p.person.Alive)
            {
                p.person.Damage(personDamage, "lightning");
                if (p.person.Alive) p.person.Swept(Vector3.up * 2f + Random.insideUnitSphere);
            }
            else if (p.block != null)
            {
                if (IsSteel(p.block)) grounded = true;   // the rod takes it safely into the ground
                else
                {
                    if (activator != null) activator.DamageBlock(p.block, brickWear);
                    if (p.block.material != null && p.block.material.heatSeconds > 0f) p.block.Ignite();
                    if (p.block.Body != null && !p.block.Body.isKinematic) p.block.Body.AddForce((Vector3.up + Random.insideUnitSphere * 0.5f) * brickKick, ForceMode.VelocityChange);
                    BrickFx.Break(p.block.Centre, p.block.shape.BodySize * 0.6f, BrickFx.ColorOf(p.block), 0.5f);
                }
            }
            // side flash for people near the strike (not when a rod took it)
            if (!grounded)
                foreach (var q in Person.All)
                {
                    if (q == null || !q.Alive || q == p.person) continue;
                    Vector3 d = q.transform.position - at; d.y = 0f;
                    if (d.sqrMagnitude < sideFlashRadius * sideFlashRadius && !q.Roofed) q.Damage(sideFlashDamage, "lightning");
                }
            for (int i = 0; i < 14; i++) BrickFx.Spark(at, (Random.onUnitSphere + Vector3.up) * Random.Range(3f, 7f), Random.Range(0.25f, 0.45f));
            BPJuice.AddTrauma(0.25f, at);
            BPJuice.Flash(grounded ? 0.6f : 1f);
        }

        /// <summary>A jagged white line from the clouds to the point, gone in a fifth of a second.</summary>
        void Bolt(Vector3 at)
        {
            var go = new GameObject("Bolt");
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = boltMaterial;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            const int n = 14;
            lr.positionCount = n;
            Vector3 top = at + new Vector3(Random.Range(-6f, 6f), 45f, Random.Range(-6f, 6f));
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)(n - 1);
                Vector3 p = Vector3.Lerp(top, at, k);
                if (i > 0 && i < n - 1) p += new Vector3(Random.Range(-1.4f, 1.4f), 0f, Random.Range(-1.4f, 1.4f)) * (1f - k * 0.5f);
                lr.SetPosition(i, p);
            }
            lr.widthMultiplier = 0.35f;
            lr.startColor = lr.endColor = boltColor;
            var light = new GameObject("BoltLight").AddComponent<Light>();
            light.transform.SetParent(go.transform, false);
            light.transform.position = at + Vector3.up * 2f;
            light.type = LightType.Point;
            light.color = boltColor;
            light.range = 18f;
            light.intensity = 8f;
            Destroy(go, 0.2f);
        }

        void StartRain()
        {
            if (rainMaterial == null || rain != null) return;
            var go = new GameObject("Rain");
            rain = go.AddComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = rain.main;
            main.loop = true;
            main.startLifetime = 1.2f;
            main.startSpeed = 0f;
            main.startSize3D = true;
            main.startSizeX = 0.05f; main.startSizeY = 1.2f; main.startSizeZ = 0.05f;
            main.startColor = new Color(0.75f, 0.85f, 1f, 0.5f);
            main.maxParticles = 3000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = rain.emission;
            em.rateOverTime = rainRate;
            var sh = rain.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(70f, 1f, 70f);
            var vel = rain.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-2f);
            vel.y = new ParticleSystem.MinMaxCurve(-32f);
            vel.z = new ParticleSystem.MinMaxCurve(0f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = rainMaterial;
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.03f;
            r.lengthScale = 2f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.transform.position = map.transform.position + Vector3.up * 30f;
            rain.Play();
        }

        void End()
        {
            time = -1f;
            DisasterState.Intensity = 0f;
            DisasterState.WearScale = 1f;
            if (rain != null) { rain.Stop(true, ParticleSystemStopBehavior.StopEmitting); Destroy(rain.gameObject, 2f); rain = null; }
        }

        void OnDisable()
        {
            if (time >= 0f) End();
            foreach (var p in pending) if (p.glow != null) Destroy(p.glow.gameObject);
            pending.Clear();
        }

        static bool IsSteel(Block b) => b.material != null && b.material.displayName == "Steel";

        Vector3 RandomSpot()
        {
            for (int t = 0; t < 30; t++)
            {
                int i = Random.Range(0, map.size), j = Random.Range(0, map.size);
                if (!map.Inside(i, j)) continue;
                var p = map.transform.TransformPoint(map.CellCentre(i, j));
                p.y = map.TopY(i, j);
                return p;
            }
            return map.transform.position;
        }

        float GroundTop(Vector3 p)
        {
            int ground = LayerMask.GetMask(BrickGrid.GroundLayerName);
            if (ground != 0 && Physics.Raycast(new Vector3(p.x, map.transform.position.y + 60f, p.z), Vector3.down, out var h, 120f, ground, QueryTriggerInteraction.Ignore))
                return h.point.y;
            return p.y;
        }
    }
}
