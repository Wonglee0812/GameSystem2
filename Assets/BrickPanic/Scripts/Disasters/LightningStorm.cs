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
        [Tooltip("Big dark bricks of the storm cloud over the island. Default 90.")]
        public int cloudBricks = 90;
        [Tooltip("Height of the storm cloud above the island base in metres. Default 34.")]
        public float cloudHeight = 34f;
        [Tooltip("Mesh and material of the cloud bricks (a big Kenney brick, M_BP_Fx).")]
        public Mesh cloudMesh;
        public Material cloudMaterial;
        public Color cloudColor = new Color(0.22f, 0.22f, 0.3f);
        [Tooltip("Mesh and material of the burn mark a strike leaves on the ground (a flat plate, dark).")]
        public Mesh scorchMesh;
        [Tooltip("Seconds a burn mark stays. Default 8.")]
        public float scorchTime = 8f;
        [Tooltip("Seconds between the strike and the thunder (camera rumble). Default 0.35.")]
        public float thunderDelay = 0.35f;

        float time = -1f, nextStrike;
        Transform cloudRoot;
        readonly List<(Transform t, MeshRenderer r, Vector3 home, float phase, float size)> cloud = new List<(Transform, MeshRenderer, Vector3, float, float)>();
        float cloudFlash;
        Vector3 lastStrike;
        MaterialPropertyBlock mpb;
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
            BuildCloud();
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
            UpdateCloud();
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
            BPJuice.AddTrauma(0.12f, at);
            BPJuice.Flash(grounded ? 0.6f : 1f);
            BPJuice.ScreenFlash(grounded ? 0.35f : 0.55f);
            Scorch(at, grounded);
            cloudFlash = 1f;
            lastStrike = at;
            StartCoroutine(Thunder(at));
        }

        System.Collections.IEnumerator Thunder(Vector3 at)
        {
            yield return new WaitForSeconds(thunderDelay);
            BPJuice.AddTrauma(0.3f, at);
        }

        /// <summary>A dark burnt plate on the ground (or on a steel rod's foot) that fades and shrinks away.</summary>
        void Scorch(Vector3 at, bool grounded)
        {
            if (scorchMesh == null || cloudMaterial == null) return;
            int ground = LayerMask.GetMask(BrickGrid.GroundLayerName);
            Vector3 p = at;
            if (ground != 0 && Physics.Raycast(at + Vector3.up * 0.5f, Vector3.down, out var h, 60f, ground, QueryTriggerInteraction.Ignore)) p = h.point;
            var go = new GameObject("Scorch");
            go.transform.position = p + Vector3.up * 0.02f;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            go.AddComponent<MeshFilter>().sharedMesh = scorchMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = cloudMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var b = new MaterialPropertyBlock();
            b.SetColor("_BaseColor", new Color(0.08f, 0.07f, 0.07f));
            b.SetColor("_EmissionColor", new Color(1f, 0.45f, 0.1f) * 2f);   // glows orange, then cools
            r.SetPropertyBlock(b);
            float size = grounded ? 0.8f : 1.3f;
            go.transform.localScale = new Vector3(size, 0.3f, size);
            StartCoroutine(Cool(go.transform, r, b, size));
            for (int i = 0; i < 4; i++) BrickFx.Smoke(p + Vector3.up * 0.3f, Random.Range(0.4f, 0.7f), 1.5f);
        }

        System.Collections.IEnumerator Cool(Transform t, MeshRenderer r, MaterialPropertyBlock b, float size)
        {
            for (float k = 0f; k < 1f && t != null; k += Time.deltaTime / scorchTime)
            {
                b.SetColor("_EmissionColor", new Color(1f, 0.45f, 0.1f) * (2f * Mathf.Clamp01(1f - k * 4f)));
                r.SetPropertyBlock(b);
                float s = size * (k < 0.8f ? 1f : 1f - (k - 0.8f) / 0.2f);
                t.localScale = new Vector3(s, 0.3f, s);
                yield return null;
            }
            if (t != null) Destroy(t.gameObject);
        }

        // ---------- storm cloud: a layer of big dark bricks that lights up from inside on every strike ----------

        void BuildCloud()
        {
            if (cloudRoot != null) Destroy(cloudRoot.gameObject);
            cloud.Clear();
            if (cloudMesh == null || cloudMaterial == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            cloudRoot = new GameObject("StormCloud").transform;
            cloudRoot.position = map.transform.position + Vector3.up * cloudHeight;
            float half = map.size * 0.5f * MapGenerator.Cell + 6f;
            for (int i = 0; i < cloudBricks; i++)
            {
                var go = new GameObject("CloudBrick");
                go.transform.SetParent(cloudRoot, false);
                go.AddComponent<MeshFilter>().sharedMesh = cloudMesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = cloudMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                Vector2 c = Random.insideUnitCircle * half;
                Vector3 home = new Vector3(c.x, Random.Range(-1.5f, 1.5f), c.y);
                go.transform.localRotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
                cloud.Add((go.transform, r, home, Random.value * 10f, Random.Range(3f, 6f)));
            }
        }

        void UpdateCloud()
        {
            if (cloudRoot == null) return;
            float ramp = time >= 0f ? Mathf.Clamp01(time / 2f) * Mathf.Clamp01((duration - time) / 2f) : 0f;
            cloudFlash = Mathf.MoveTowards(cloudFlash, 0f, Time.deltaTime * 3f);
            float t = Time.time;
            foreach (var c in cloud)
            {
                if (c.t == null) continue;
                Vector3 drift = new Vector3(Mathf.Sin(t * 0.15f + c.phase) * 2f, Mathf.Sin(t * 0.4f + c.phase) * 0.4f, Mathf.Cos(t * 0.12f + c.phase) * 2f);
                // the cloud comes down from above at the start and goes up at the end
                c.t.localPosition = c.home + drift + Vector3.up * ((1f - ramp) * 25f);
                c.t.localScale = Vector3.one * (c.size * Mathf.Max(0.05f, ramp));
                float near = 1f - Mathf.Clamp01(Vector3.Distance(new Vector3(c.t.position.x, 0f, c.t.position.z), new Vector3(lastStrike.x, 0f, lastStrike.z)) / 30f);
                mpb.SetColor("_BaseColor", cloudColor);
                mpb.SetColor("_EmissionColor", new Color(0.7f, 0.8f, 1f) * (cloudFlash * (0.3f + 1.5f * near)));
                c.r.SetPropertyBlock(mpb);
            }
            if (time < 0f && pending.Count == 0 && ramp <= 0f) { Destroy(cloudRoot.gameObject); cloudRoot = null; cloud.Clear(); }
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
            // two thin side forks from the upper half of the bolt
            for (int f = 0; f < 2; f++)
            {
                var fgo = new GameObject("Fork");
                fgo.transform.SetParent(go.transform, false);
                var fl = fgo.AddComponent<LineRenderer>();
                fl.sharedMaterial = boltMaterial;
                fl.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                int startI = Random.Range(2, n / 2 + 1);
                Vector3 p0 = lr.GetPosition(startI);
                Vector3 dir = new Vector3(Random.Range(-1f, 1f), -1.2f, Random.Range(-1f, 1f)).normalized;
                const int m = 6;
                fl.positionCount = m;
                for (int i = 0; i < m; i++)
                    fl.SetPosition(i, p0 + dir * (i * 2.2f) + (i > 0 ? new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f)) : Vector3.zero));
                fl.widthMultiplier = 0.15f;
                fl.startColor = boltColor;
                fl.endColor = new Color(boltColor.r, boltColor.g, boltColor.b, 0f);
            }
            // a second, quick re-strike flicker
            StartCoroutine(Flicker(lr));
            var light = new GameObject("BoltLight").AddComponent<Light>();
            light.transform.SetParent(go.transform, false);
            light.transform.position = at + Vector3.up * 2f;
            light.type = LightType.Point;
            light.color = boltColor;
            light.range = 18f;
            light.intensity = 8f;
            Destroy(go, 0.2f);
        }

        System.Collections.IEnumerator Flicker(LineRenderer lr)
        {
            yield return new WaitForSeconds(0.06f);
            if (lr != null) lr.enabled = false;
            yield return new WaitForSeconds(0.04f);
            if (lr != null) lr.enabled = true;
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
            if (cloudRoot != null) Destroy(cloudRoot.gameObject);
            cloud.Clear();
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
