using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace WindPet
{
    /// <summary>
    /// The bell-jar turbine. Drop the pet on the glass and it is sealed inside: its
    /// wind spins the rotor and makes electricity. Working costs the pet energy,
    /// fullness and mood. An angry pet (a storm) makes up to x3 electricity but wears
    /// the turbine out many times faster. A broken turbine makes nothing until the
    /// player buys a new one (click it). Pick the pet up to let it out.
    /// Levels (bought in the pachinko shop) multiply the output; a new turbine keeps the level.
    /// </summary>
    public class Turbine : MonoBehaviour
    {
        public WorkConfig config;
        public PetDrag pet;
        public PetStateMachine fsm;
        public PetLook look;
        public Wallet wallet;
        public Transform rotor;
        public TextMeshPro counter;
        public TextMeshPro label;
        public Light bulb;
        public Material smokeMaterial;
        [Tooltip("Height of the brass base: the pet stands on top of it.")]
        public float baseHeight = 0.45f;
        public float glassRadius = 1f;

        public static Turbine Instance { get; private set; }
        /// <summary>1 to config.maxTurbineLevel.</summary>
        public int Level { get; private set; } = 1;
        /// <summary>Output and bill size multiplier of the current level.</summary>
        public float LevelMultiplier => Mathf.Pow(config.levelGrowth, Level - 1);
        /// <summary>Electricity multiplier of the current level (grows faster than the bills).</summary>
        public float OutputMultiplier => Mathf.Pow(config.outputGrowth, Level - 1);
        public bool MaxLevel => Level >= config.maxTurbineLevel;
        public float UpgradeCost => Mathf.Round(config.upgradeBaseCost * Mathf.Pow(config.upgradeCostGrowth, Level - 1));
        public float NewTurbinePrice => Mathf.Round(config.turbinePrice * LevelMultiplier);

        /// <summary>Static helper for scripts that only need the multiplier (1 when there is no turbine).</summary>
        public static float CurrentMultiplier => Instance != null ? Instance.LevelMultiplier : 1f;

        /// <summary>Pays for the next level. Returns false when maxed out or too expensive.</summary>
        public bool TryUpgrade()
        {
            if (MaxLevel || wallet == null || !wallet.TrySpend(UpgradeCost)) return false;
            Level++;
            FloatingText.Show("Turbine level " + Level + "!", Centre + Vector3.up * 2.6f, new Color(1f, 0.85f, 0.3f));
            return true;
        }

        void Awake() => Instance = this;

        /// <summary>0-100. At 0 the turbine is broken.</summary>
        public float Durability { get; private set; } = 100f;
        public bool Broken => Durability <= 0f;
        public bool HasPet => pet != null && pet.Anchor.HasValue && holding;
        /// <summary>Electricity made per second right now.</summary>
        public float Output { get; private set; }

        bool holding;
        float spin;
        ParticleSystem smoke, sparks;
        Bounds bounds;
        Vector3 axle;   // the rotor spins around this vertical line
        Vector3 Centre => new Vector3(bounds.center.x, bounds.min.y + baseHeight, bounds.center.z);

        void Start()
        {
            // the main mesh only: the glass dome dips below the desk inside the base
            bounds = GetComponent<Renderer>().bounds;
            // the round glass dome sits exactly on the axle; the base bounds are
            // pushed a little forward by the counter panel
            axle = bounds.center;
            foreach (var r in GetComponentsInChildren<Renderer>())
                if (r.name.EndsWith("_Glass")) axle = r.bounds.center;
            pet.Released += OnPetReleased;
            pet.Grabbed += OnPetGrabbed;
            smoke = MakeSmoke();
            sparks = MakeSparks();
        }

        void OnDestroy()
        {
            if (pet == null) return;
            pet.Released -= OnPetReleased;
            pet.Grabbed -= OnPetGrabbed;
        }

        // ---------- the pet goes in and out ----------

        void OnPetReleased()
        {
            var cam = Camera.main;
            Vector3 petCentre = pet.body.transform.TransformPoint(pet.body.center);
            Vector3 domeCentre = Centre + Vector3.up * glassRadius;
            Vector2 a = cam.WorldToScreenPoint(petCentre), b = cam.WorldToScreenPoint(domeCentre);
            float radius = Vector2.Distance(b, (Vector2)cam.WorldToScreenPoint(domeCentre + cam.transform.right * glassRadius * 1.1f));
            if (Vector2.Distance(a, b) > radius) return;   // dropped somewhere else
            holding = true;
            pet.Anchor = Centre;
            pet.TargetZ = Centre.z;
            if (look != null) look.Confined = true;
            FloatingText.Show("Working", domeCentre + Vector3.up * 1.2f, new Color(0.7f, 0.9f, 1f));
        }

        void OnPetGrabbed()
        {
            if (!holding) return;
            holding = false;
            pet.Anchor = null;
            if (look != null) look.Confined = false;
        }

        // ---------- work ----------

        void Update()
        {
            float dt = Time.deltaTime;
            Output = 0f;
            if (HasPet && !Broken && fsm.Energy != EnergyState.Sleep)
            {
                float energy = fsm.Energy == EnergyState.Tired ? config.tiredOutput : fsm.Energy == EnergyState.Energetic ? config.energeticOutput : 1f;
                bool storm = fsm.Mood == MoodState.Angry && look != null;
                float stormMul = storm ? Mathf.Lerp(1f, config.stormOutput, look.Storm) : 1f;
                Output = config.kwhPerSecond * energy * stormMul * OutputMultiplier;
                PowerGrid.Instance?.Add(Output * dt);
                Durability = Mathf.Max(0f, Durability - config.wearPerSecond * (storm ? config.stormWear : 1f) * dt);
                fsm.SpendEnergy(config.workEnergy * stormMul * dt);
                fsm.stats.ChangeFullness(-config.workFullness * dt);
                fsm.stats.ChangeMood(-config.workMood * dt);
                if (Broken) FloatingText.Show("Turbine broken!", Centre + Vector3.up * 2.6f, new Color(1f, 0.4f, 0.3f));
            }

            // rotor, bulb and counter
            float wanted = Mathf.Min(Output / Mathf.Max(0.0001f, config.kwhPerSecond) * 300f, 1200f);   // degrees per second
            spin = Mathf.Lerp(spin, wanted, 1f - Mathf.Exp(-2f * dt));
            if (rotor != null) rotor.RotateAround(new Vector3(axle.x, rotor.position.y, axle.z), Vector3.up, spin * dt);
            if (bulb != null) bulb.intensity = Mathf.Lerp(bulb.intensity, Output > 0f ? 1.5f + Output * 20f : 0f, 1f - Mathf.Exp(-4f * dt));
            if (counter != null && PowerGrid.Instance != null) counter.text = PowerGrid.Instance.Electricity.ToString("0000.0");

            // wear: smoke from 45 %, sparks from 15 %
            var e = smoke.emission;
            e.rateOverTime = Durability < 45f ? Mathf.Lerp(6f, 1f, Durability / 45f) : 0f;
            var se = sparks.emission;
            se.rateOverTime = Durability < 15f && Durability > 0f ? 10f : 0f;
            if (label != null)
            {
                label.gameObject.SetActive(Broken);
                if (Broken) label.text = "Broken\n<size=70%>click: new turbine €" + NewTurbinePrice.ToString("0") + "</size>";
            }

            if (Broken) TryBuyNew();
        }

        void TryBuyNew()
        {
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame || ToolBelt.HasTool) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            Ray ray = Camera.main.ScreenPointToRay(pointer.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f) || !hit.collider.transform.IsChildOf(transform)) return;
            if (wallet != null && wallet.TrySpend(NewTurbinePrice))
            {
                Durability = 100f;
                FloatingText.Show("New turbine  -€" + NewTurbinePrice.ToString("0"), Centre + Vector3.up * 2.6f, new Color(1f, 0.85f, 0.3f));
            }
            else FloatingText.Show("Not enough money", Centre + Vector3.up * 2.6f, new Color(1f, 0.45f, 0.35f));
        }

        // ---------- effects ----------

        ParticleSystem MakeSmoke()
        {
            var go = new GameObject("WearSmoke");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(bounds.center.x, bounds.max.y - 0.2f, bounds.center.z);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            m.startColor = Color.white;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            var e = ps.emission; e.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 15f; sh.radius = 0.1f;
            sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0.3f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = smokeMaterial;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", new Color(0.35f, 0.33f, 0.32f));
            block.SetColor("_ShadowColor", new Color(0.15f, 0.14f, 0.14f));
            block.SetColor("_EdgeColor", new Color(0.55f, 0.53f, 0.5f));
            r.SetPropertyBlock(block);
            ps.Play();
            return ps;
        }

        ParticleSystem MakeSparks()
        {
            var go = new GameObject("WearSparks");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(bounds.center.x, bounds.min.y + baseHeight, bounds.min.z + 0.2f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            m.startColor = new Color(1f, 0.9f, 0.4f);
            m.gravityModifier = 1f;
            var e = ps.emission; e.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.05f;
            r.lengthScale = 1f;
            r.sharedMaterial = smokeMaterial;
            ps.Play();
            return ps;
        }
    }
}
