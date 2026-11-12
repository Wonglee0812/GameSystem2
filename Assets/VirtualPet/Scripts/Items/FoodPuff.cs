using System.Collections.Generic;
using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// A puff of wind food made by the fan or the hair dryer. It floats slowly
    /// upwards and wobbles. The pet flies to it and eats it; then the puff is
    /// sucked into the pet. Uneaten puffs fade away after a while.
    /// </summary>
    public class FoodPuff : MonoBehaviour
    {
        /// <summary>All puffs that can still be eaten.</summary>
        public static readonly List<FoodPuff> All = new List<FoodPuff>();

        public float Food { get; private set; }
        public float TemperatureChange { get; private set; }
        public Color PuffColor { get; private set; }
        Material material;

        const float RiseSpeed = 0.28f;
        const float Lifetime = 12f;
        ParticleSystem ps;
        float age, maxY, swaySeed;
        Transform eater;
        bool eaten;

        public static FoodPuff Create(Vector3 position, Color color, float food, float temperatureChange, Material material, float maxY)
        {
            var go = new GameObject("FoodPuff");
            go.transform.position = position;
            var puff = go.AddComponent<FoodPuff>();
            puff.Food = food;
            puff.TemperatureChange = temperatureChange;
            puff.PuffColor = color;
            puff.material = material;
            puff.maxY = maxY;
            puff.swaySeed = Random.value * 10f;
            puff.ps = BuildParticles(go, color, material);
            All.Add(puff);
            return puff;
        }

        static ParticleSystem BuildParticles(GameObject go, Color color, Material material)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main;
            m.duration = 1f;
            m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.6f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.5f);
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            m.startColor = Color.white;     // the smoke shader uses alpha to eat the puff away
            m.simulationSpace = ParticleSystemSimulationSpace.World;   // leaves soft wisps behind
            m.maxParticles = 40;
            var e = ps.emission; e.rateOverTime = 12f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.15f;
            var n = ps.noise; n.enabled = true; n.strength = 0.15f; n.frequency = 0.8f;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.4f), new Keyframe(0.3f, 1f), new Keyframe(1, 1.3f)));
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0.2f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            // the pet's stylised smoke, coloured by the food's temperature
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_ShadowColor", color * 0.72f);
            block.SetColor("_EdgeColor", Color.Lerp(color, Color.white, 0.6f));
            r.SetPropertyBlock(block);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        /// <summary>A small burst of food smoke, for the eating effect.</summary>
        public void Burst(Vector3 position)
        {
            var go = new GameObject("EatBurst");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main;
            m.loop = false;
            m.duration = 0.2f;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            m.startColor = Color.white;
            m.gravityModifier = -0.1f;
            var e = ps.emission; e.rateOverTime = 0f; e.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", PuffColor);
            block.SetColor("_ShadowColor", PuffColor * 0.72f);
            block.SetColor("_EdgeColor", Color.Lerp(PuffColor, Color.white, 0.6f));
            r.SetPropertyBlock(block);
            ps.Play();
            Destroy(go, 1f);
        }

        /// <summary>The pet eats this puff. Returns false if someone was faster.</summary>
        public bool Eat(Transform pet)
        {
            if (eaten) return false;
            eaten = true;
            eater = pet;
            All.Remove(this);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(gameObject, 1.6f);
            return true;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            if (eaten)
            {
                // get sucked into the pet
                if (eater != null) transform.position = Vector3.Lerp(transform.position, eater.position, 1f - Mathf.Exp(-12f * dt));
                return;
            }
            Vector3 p = transform.position;
            p.y = Mathf.Min(p.y + RiseSpeed * dt, maxY);
            p.x += Mathf.Sin(age * 1.7f + swaySeed) * 0.15f * dt;
            transform.position = p;

            if (age > Lifetime)
            {
                All.Remove(this);
                eaten = true;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(gameObject, 1.6f);
            }
        }

        void OnDestroy() => All.Remove(this);
    }
}
