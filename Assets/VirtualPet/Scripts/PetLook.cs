using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Shows the pet's state in its body, so the player can read it without the bars.
    /// - Temperature: a hot pet turns red-orange, a cold pet deep blue.
    /// - Bad mood: the wind grows, darkens and gets cloudier. Below Angry it turns
    ///   into a storm, and the longer it stays angry the bigger and faster the
    ///   storm gets (Rage), up to a size limit so it never fills the screen.
    /// - Sleep: the storm is put to rest and the wind spins very slowly.
    /// - Eating: a short flash in the colour of the food.
    /// Colours go through a MaterialPropertyBlock, so the shared materials stay untouched.
    /// </summary>
    public class PetLook : MonoBehaviour
    {
        public PetStats stats;
        public PetStateMachine fsm;
        public Transform body;
        public Transform face;
        public WindArcs arcs;
        [Tooltip("Big outer wind bands around the pet, only visible in a storm.")]
        public WindArcs stormArcs;
        public WindVortex[] vortices;
        public ParticleSystem smoke;
        public ParticleSystem glow;

        [Header("Temperature colours")]
        public float warmFrom = 55f, hotAt = 90f, coolFrom = 45f, coldAt = 10f;
        public Color hotLight = new Color(1f, 0.62f, 0.45f), hotShadow = new Color(0.85f, 0.3f, 0.25f), hotArc = new Color(1f, 0.55f, 0.4f);
        public Color coldLight = new Color(0.55f, 0.78f, 1f), coldShadow = new Color(0.15f, 0.35f, 0.85f), coldArc = new Color(0.5f, 0.75f, 1f);

        [Header("Bad mood (storm)")]
        [Tooltip("Mood where the darkening starts; at 0 the storm is full.")]
        public float stormFromMood = 40f;
        public Color stormLight = new Color(0.2f, 0.21f, 0.25f), stormShadow = new Color(0.05f, 0.05f, 0.08f), stormArc = new Color(0.22f, 0.22f, 0.28f);
        public float stormScale = 1.4f;
        public float stormSpin = 2.2f;
        public float stormSmokeRate = 3f;
        public float stormSmokeSize = 1.5f;

        [Header("Rage (staying angry)")]
        [Tooltip("Seconds of being Angry until the rage is full.")]
        public float rageBuildTime = 20f;
        public float rageCalmTime = 6f;
        public float rageScale = 1.3f;
        public float rageSpin = 1.6f;
        [Tooltip("The body never grows past this, so the storm stays on screen.")]
        public float maxScale = 1.7f;

        [Header("Sleep")]
        public float sleepSpin = 0.12f;

        [Tooltip("How fast the look follows the bars.")]
        public float smoothing = 2f;

        /// <summary>Inside the turbine the pet is shown smaller, so it fits in the glass.</summary>
        public bool Confined { get; set; }
        [Tooltip("Size of the pet inside the turbine, and the most it may grow there in a storm.")]
        public float confinedScale = 0.6f, confinedMaxScale = 0.75f;
        float confinedLerp;

        /// <summary>0 = calm, 1 = full storm (0 while asleep). PetStorm reads this.</summary>
        public float Storm { get; private set; }
        /// <summary>0-1, grows while the pet stays Angry.</summary>
        public float Rage { get; private set; }
        public float Heat { get; private set; }
        public float Cold { get; private set; }
        public float BodyScale { get; private set; } = 1f;

        MaterialPropertyBlock block;
        ParticleSystemRenderer smokeR, glowR;
        Color baseLight, baseShadow, baseEdge, glowBase;
        float smokeRate, smokeSize, spin = 1f;
        Color flashColor;
        float flash;

        /// <summary>Short glow in a colour, for example the colour of the food just eaten.</summary>
        public void Flash(Color color)
        {
            flashColor = color;
            flash = 1f;
        }

        void Start()
        {
            block = new MaterialPropertyBlock();
            if (smoke != null)
            {
                smokeR = smoke.GetComponent<ParticleSystemRenderer>();
                var m = smokeR.sharedMaterial;
                baseLight = m.GetColor("_BaseColor");
                baseShadow = m.GetColor("_ShadowColor");
                baseEdge = m.GetColor("_EdgeColor");
                smokeRate = smoke.emission.rateOverTimeMultiplier;
                smokeSize = smoke.main.startSizeMultiplier;
            }
            if (glow != null)
            {
                glowR = glow.GetComponent<ParticleSystemRenderer>();
                glowBase = glowR.sharedMaterial.HasProperty("_BaseColor") ? glowR.sharedMaterial.GetColor("_BaseColor") : Color.white;
            }
            // particles must follow the body's scale when the storm makes it bigger
            if (body != null)
                foreach (var ps in body.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = ps.main;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                }
        }

        void Update()
        {
            if (stats == null) return;
            float dt = Time.deltaTime;
            float k = 1f - Mathf.Exp(-smoothing * dt);
            bool asleep = fsm != null && fsm.Energy == EnergyState.Sleep;
            bool angry = fsm != null && fsm.Mood == MoodState.Angry && !asleep;

            Heat = Mathf.Lerp(Heat, Smooth(Mathf.InverseLerp(warmFrom, hotAt, stats.Temperature)), k);
            Cold = Mathf.Lerp(Cold, Smooth(Mathf.InverseLerp(coolFrom, coldAt, stats.Temperature)), k);
            float stormTarget = asleep ? 0f : Smooth(Mathf.InverseLerp(stormFromMood, 0f, stats.Mood));
            Storm = Mathf.Lerp(Storm, stormTarget, k);
            Rage = Mathf.MoveTowards(Rage, angry ? 1f : 0f, dt / (angry ? rageBuildTime : rageCalmTime));
            flash = Mathf.MoveTowards(flash, 0f, dt * 2f);

            // colour: temperature first, then the storm darkens everything, then the food flash
            Color light = Flashed(Mix(baseLight, hotLight, coldLight, stormLight));
            Color shadow = Flashed(Mix(baseShadow, hotShadow, coldShadow, stormShadow));
            Color edge = Flashed(Color.Lerp(Mix(baseEdge, Color.Lerp(baseEdge, hotLight, 0.6f), Color.Lerp(baseEdge, coldLight, 0.6f), baseEdge),
                                            Color.Lerp(stormLight, Color.white, 0.15f), Storm));
            Color arc = Flashed(Mix(Color.white, hotArc, coldArc, stormArc));

            if (smokeR != null)
            {
                smokeR.GetPropertyBlock(block);
                block.SetColor("_BaseColor", light);
                block.SetColor("_ShadowColor", shadow);
                block.SetColor("_EdgeColor", edge);
                smokeR.SetPropertyBlock(block);
                var e = smoke.emission;
                e.rateOverTimeMultiplier = smokeRate * Mathf.Lerp(1f, stormSmokeRate, Storm);
                var main = smoke.main;
                main.startSizeMultiplier = smokeSize * Mathf.Lerp(1f, stormSmokeSize, Storm);
            }
            if (glowR != null)
            {
                glowR.GetPropertyBlock(block);
                Color g = glowBase * arc;
                g.a = glowBase.a * (1f - 0.6f * Storm);
                block.SetColor("_BaseColor", g);
                glowR.SetPropertyBlock(block);
            }

            // size and spin: storm, then rage on top, with a hard size limit
            BodyScale = Mathf.Min(maxScale, Mathf.Lerp(1f, stormScale, Storm) * Mathf.Lerp(1f, rageScale, Rage));
            confinedLerp = Mathf.MoveTowards(confinedLerp, Confined ? 1f : 0f, dt * 3f);
            if (confinedLerp > 0f)
                BodyScale = Mathf.Lerp(BodyScale, Mathf.Min(BodyScale * confinedScale, confinedMaxScale), confinedLerp);
            float targetSpin = asleep ? sleepSpin : Mathf.Lerp(1f, stormSpin, Storm) * Mathf.Lerp(1f, rageSpin, Rage);
            spin = Mathf.Lerp(spin, targetSpin, k);

            if (arcs != null) { arcs.tint = arc; arcs.spinMultiplier = spin; }
            if (stormArcs != null)
            {
                stormArcs.tint = arc;
                stormArcs.spinMultiplier = spin * 1.3f;
                stormArcs.fade = Mathf.Clamp01(Mathf.InverseLerp(0.35f, 0.8f, Storm));
            }
            if (vortices != null)
                foreach (var v in vortices) if (v != null) v.spinMultiplier = spin;
            if (body != null) body.localScale = Vector3.one * BodyScale;
            if (face != null) face.localScale = Vector3.one * BodyScale;   // the face pivot sits at the feet, so the eyes rise with the body
        }

        Color Mix(Color normal, Color hot, Color cold, Color storm)
        {
            Color c = Color.Lerp(normal, hot, Heat);
            c = Color.Lerp(c, cold, Cold);
            return Color.Lerp(c, storm, Storm);
        }

        Color Flashed(Color c) => flash > 0f ? Color.Lerp(c, flashColor, flash * 0.7f) : c;

        static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
