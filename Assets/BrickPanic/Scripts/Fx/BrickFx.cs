using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>Particle effects made of small Lego bricks (smoke, fire, sparks, explosions), so every effect
    /// matches the brick style. Visual only: no physics. Bricks pop in, fly, and shrink away at the end.
    /// Call BrickFx.Emit(...) from anywhere.</summary>
    public class BrickFx : MonoBehaviour
    {
        [Tooltip("Brick meshes used for particles (small bricks look best: 1x1, 1x2, 2x2).")]
        public Mesh[] meshes;
        [Tooltip("Material for the particles (URP Lit with emission on: M_BP_Fx). Colour and glow are set per brick.")]
        public Material material;
        [Tooltip("Most particle bricks alive at the same time. Older ones are reused. Default 700.")]
        public int maxParticles = 700;
        [Tooltip("Size of one particle brick at size 1, as a scale of the mesh. Default 1.")]
        public float baseScale = 1f;

        public static BrickFx Instance { get; private set; }

        class Particle
        {
            public Transform t;
            public MeshRenderer r;
            public Vector3 velocity, spin;
            public float age, life, size, gravity, drag, glow;
            public Color start, end;
            public bool alive;
        }

        readonly List<Particle> pool = new List<Particle>();
        int next;
        MaterialPropertyBlock mpb;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            Instance = this;
            mpb = new MaterialPropertyBlock();
        }

        /// <summary>One brick particle.</summary>
        /// <param name="gravity">1 = falls normally, 0 = floats, below 0 = rises (smoke, flames).</param>
        /// <param name="drag">Speed lost per second (0 = none). Smoke uses about 0.5.</param>
        /// <param name="glow">0 = no light, 2-3 = glowing lava or fire.</param>
        public static void Emit(Vector3 pos, Vector3 velocity, float life, float size, Color start, Color end,
                                float glow = 0f, float gravity = 1f, float drag = 0f)
        {
            if (Instance == null) return;
            Instance.Spawn(pos, velocity, life, size, start, end, glow, gravity, drag);
        }

        void Spawn(Vector3 pos, Vector3 velocity, float life, float size, Color start, Color end, float glow, float gravity, float drag)
        {
            Particle p;
            if (pool.Count < maxParticles)
            {
                var go = new GameObject("FxBrick");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = meshes[pool.Count % meshes.Length];
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                p = new Particle { t = go.transform, r = r };
                pool.Add(p);
            }
            else
            {
                p = pool[next];               // reuse the oldest slot
                next = (next + 1) % pool.Count;
            }
            p.t.gameObject.SetActive(true);
            p.t.SetPositionAndRotation(pos, Random.rotation);
            p.velocity = velocity;
            p.spin = Random.insideUnitSphere * 200f;
            p.age = 0f;
            p.life = Mathf.Max(0.05f, life);
            p.size = size * baseScale;
            p.gravity = gravity;
            p.drag = drag;
            p.glow = glow;
            p.start = start;
            p.end = end;
            p.alive = true;
            p.t.localScale = Vector3.zero;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 g = Physics.gravity;
            foreach (var p in pool)
            {
                if (!p.alive) continue;
                p.age += dt;
                float k = p.age / p.life;
                if (k >= 1f) { p.alive = false; p.t.gameObject.SetActive(false); continue; }
                p.velocity += g * (p.gravity * dt);
                p.velocity *= Mathf.Max(0f, 1f - p.drag * dt);
                p.t.position += p.velocity * dt;
                p.t.Rotate(p.spin * dt, Space.Self);
                // pop in fast, keep size, shrink away at the end
                float s = k < 0.12f ? k / 0.12f : 1f - Mathf.SmoothStep(0f, 1f, (k - 0.6f) / 0.4f);
                p.t.localScale = Vector3.one * (p.size * Mathf.Max(0f, s));
                Color c = Color.Lerp(p.start, p.end, k);
                mpb.SetColor(BaseColorId, c);
                mpb.SetColor(EmissionId, c * (p.glow * (1f - k * 0.7f)));
                p.r.SetPropertyBlock(mpb);
            }
        }

        // ---------- ready-made effects ----------

        static readonly Color Yellow = new Color(1f, 0.85f, 0.2f), Orange = new Color(1f, 0.45f, 0.05f), Red = new Color(0.85f, 0.12f, 0.05f);
        static readonly Color SmokeDark = new Color(0.08f, 0.07f, 0.07f), SmokeGrey = new Color(0.32f, 0.3f, 0.3f);

        /// <summary>One small flame brick (burning bricks, fire spots).</summary>
        public static void Flame(Vector3 pos, float size = 0.6f)
        {
            Emit(pos + Random.insideUnitSphere * 0.3f, new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(1.5f, 2.8f), Random.Range(-0.4f, 0.4f)),
                 Random.Range(0.5f, 0.9f), size * Random.Range(0.7f, 1.2f), Random.value < 0.5f ? Yellow : Orange, Red, 2.5f, -0.15f, 0.5f);
        }

        /// <summary>One dark smoke brick that rises and grows grey.</summary>
        public static void Smoke(Vector3 pos, float size, float upSpeed = 3f)
        {
            Emit(pos + Random.insideUnitSphere * 0.6f, new Vector3(Random.Range(-1f, 1f), upSpeed * Random.Range(0.7f, 1.3f), Random.Range(-1f, 1f)),
                 Random.Range(3.5f, 6f), size * Random.Range(0.7f, 1.3f), SmokeDark, SmokeGrey, 0f, -0.04f, 0.35f);
        }

        /// <summary>Glowing lava spark thrown out (crater, explosions).</summary>
        public static void Spark(Vector3 pos, Vector3 velocity, float size = 0.5f)
        {
            Emit(pos, velocity, Random.Range(0.8f, 1.5f), size * Random.Range(0.7f, 1.3f), Random.value < 0.5f ? Yellow : Orange, Red, 3f, 1f, 0.1f);
        }

        // ---------- brick break: the signature effect of Brick Panic (Poker TD has the dissolve) ----------

        static int snapFrame = -1, snapsThisFrame;
        /// <summary>Most small snap effects in one frame (a big collapse breaks hundreds of joints at once). Default 8.</summary>
        public static int SnapBudget = 8;

        /// <summary>Colour of a brick as the player sees it (its tint, or its material colour).</summary>
        public static Color ColorOf(Block b)
        {
            if (b == null) return Color.white;
            if (b.tint.HasValue) return b.tint.Value;
            return b.material != null ? b.material.color : Color.white;
        }

        /// <summary>A whole brick breaks apart: a white flash, chips in its own colour flying out with studs,
        /// and a ring of dust. strength 1 = normal, smaller for sell / undo, bigger for heavy hits.</summary>
        public static void Break(Block b, float strength = 1f)
        {
            if (b == null || Instance == null || !BPJuice.On) return;
            Vector3 size = b.shape != null ? b.shape.BodySize : Vector3.one * 0.6f;
            Vector3 c = b.transform.TransformPoint(new Vector3(0f, size.y * 0.5f, 0f));
            Break(c, size, ColorOf(b), strength);
        }

        /// <summary>Same as Break(Block), for a brick that is only a position, a size and a colour.</summary>
        public static void Break(Vector3 centre, Vector3 size, Color color, float strength = 1f)
        {
            if (Instance == null || !BPJuice.On) return;
            float vol = Mathf.Clamp(size.x * size.z * 2f, 0.5f, 4f);
            Color dark = Color.Lerp(color, Color.black, 0.35f);
            // flash
            Emit(centre, Vector3.zero, 0.1f, Mathf.Max(size.x, size.z) * 1.15f, Color.white, color, 4f, 0f, 0f);
            // two or three big chunks (the brick splits), slower and heavier
            int chunks = vol > 1.5f ? 3 : 2;
            for (int i = 0; i < chunks; i++)
                Emit(centre + Random.insideUnitSphere * 0.2f, (Random.onUnitSphere * 0.6f + Vector3.up) * Random.Range(2.5f, 4.5f) * strength,
                     Random.Range(1f, 1.4f), Random.Range(0.75f, 1f), color, dark, 0.1f, 1.2f, 0.1f);
            // chips and studs
            int chips = Mathf.RoundToInt(Mathf.Lerp(6f, 16f, vol / 4f) * Mathf.Clamp(strength, 0.4f, 1.6f));
            for (int i = 0; i < chips; i++)
            {
                Vector3 off = new Vector3(Random.Range(-0.5f, 0.5f) * size.x, Random.Range(-0.5f, 0.5f) * size.y, Random.Range(-0.5f, 0.5f) * size.z);
                Vector3 dir = (off.normalized + Vector3.up * 0.8f + Random.insideUnitSphere * 0.5f).normalized;
                Emit(centre + off, dir * Random.Range(4f, 8f) * strength, Random.Range(0.7f, 1.2f), Random.Range(0.35f, 0.62f),
                     Color.Lerp(color, Color.white, Random.value * 0.2f), dark, 0.15f, 1f, 0.2f);
            }
            // dust ring on the ground plane
            Color dust = new Color(0.95f, 0.9f, 0.8f);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f + Random.value;
                Emit(centre - Vector3.up * size.y * 0.4f, new Vector3(Mathf.Cos(a), 0.15f, Mathf.Sin(a)) * Random.Range(2.5f, 4f) * strength,
                     Random.Range(0.45f, 0.7f), Random.Range(0.35f, 0.55f), dust, new Color(dust.r, dust.g, dust.b, 0f), 0f, -0.05f, 3f);
            }
        }

        /// <summary>A joint snaps or a brick is hit hard: a tiny flash and a few chips (rate-limited per frame).</summary>
        public static void Snap(Vector3 at, Color color, float strength = 1f)
        {
            if (Instance == null || !BPJuice.On) return;
            if (Time.frameCount != snapFrame) { snapFrame = Time.frameCount; snapsThisFrame = 0; }
            if (++snapsThisFrame > SnapBudget) return;
            Emit(at, Vector3.zero, 0.08f, 0.5f, Color.white, color, 3f, 0f, 0f);
            int n = Random.Range(2, 4);
            for (int i = 0; i < n; i++)
                Emit(at, (Random.onUnitSphere + Vector3.up) * Random.Range(2f, 4.5f) * strength, Random.Range(0.4f, 0.7f), Random.Range(0.22f, 0.35f),
                     color, Color.Lerp(color, Color.black, 0.4f), 0.1f, 1f, 0.2f);
        }

        /// <summary>A brick lands where the player placed it: a small dust ring.</summary>
        public static void Land(Vector3 at, float width)
        {
            if (!BPJuice.On) return;
            Color dust = new Color(1f, 0.96f, 0.85f);
            int n = Mathf.Clamp(Mathf.RoundToInt(width * 4f), 4, 10);
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                Emit(at, new Vector3(Mathf.Cos(a), 0.2f, Mathf.Sin(a)) * Random.Range(1.5f, 2.5f), Random.Range(0.3f, 0.5f), Random.Range(0.2f, 0.32f),
                     dust, new Color(dust.r, dust.g, dust.b, 0f), 0f, -0.05f, 4f);
            }
        }

        /// <summary>A brick explosion: sparks out in all directions plus a smoke cloud.</summary>
        public static void Explosion(Vector3 pos, float radius)
        {
            int sparks = Mathf.RoundToInt(18 + radius * 8f);
            for (int i = 0; i < sparks; i++)
            {
                Vector3 d = Random.onUnitSphere;
                d.y = Mathf.Abs(d.y) + 0.2f;
                Spark(pos + Vector3.up * 0.3f, d.normalized * Random.Range(4f, 9f) * (radius / 3f), Random.Range(0.4f, 0.9f));
            }
            for (int i = 0; i < 8; i++) Smoke(pos + Vector3.up * 0.5f, Random.Range(0.8f, 1.6f), Random.Range(1.5f, 3f));
        }
    }
}
