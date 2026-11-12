using System;
using System.Collections.Generic;
using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// The lab's electricity store. The turbine fills it, the lights and the hair
    /// dryer use it, and stamped bills sell it. There is no upper limit.
    /// The lights burn electricity all the time; when it is gone they burn money,
    /// and when the money is gone too the room goes dark.
    /// </summary>
    public class PowerGrid : MonoBehaviour
    {
        public static PowerGrid Instance { get; private set; }

        public WorkConfig config;
        public Wallet wallet;

        public float Electricity { get; private set; }
        /// <summary>True while the lights have power (electricity or money).</summary>
        public bool LightsOn { get; private set; } = true;
        public event Action<float> Changed;

        readonly List<Light> lights = new List<Light>();
        readonly List<float> baseIntensity = new List<float>();
        Color baseAmbient;
        float baseAmbientIntensity, brightness = 1f;

        void Awake()
        {
            Instance = this;
            Electricity = config.startElectricity;
        }

        void Start()
        {
            // every light that is part of the room (not the pet's storm flash or the turbine bulb)
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.GetComponentInParent<PetDrag>() != null || l.GetComponentInParent<Turbine>() != null) continue;
                lights.Add(l);
                baseIntensity.Add(l.intensity);
            }
            baseAmbient = RenderSettings.ambientLight;
            baseAmbientIntensity = RenderSettings.ambientIntensity;
        }

        public void Add(float kwh)
        {
            if (kwh <= 0f) return;
            Electricity += kwh;
            Changed?.Invoke(Electricity);
        }

        /// <summary>Uses electricity if there is enough. Returns false (and uses nothing) otherwise.</summary>
        public bool TryUse(float kwh)
        {
            if (Electricity + 0.0001f < kwh) return false;
            Electricity = Mathf.Max(0f, Electricity - kwh);
            Changed?.Invoke(Electricity);
            return true;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float need = config.lightsKwhPerSecond * dt;
            if (Electricity >= need) { Electricity -= need; LightsOn = true; Changed?.Invoke(Electricity); }
            else
            {
                Electricity = 0f;
                LightsOn = wallet != null && wallet.PayRunningCost(config.lightsEuroPerSecond * dt);
            }

            float target = LightsOn ? 1f : config.darkLevel;
            brightness = Mathf.MoveTowards(brightness, target, dt * 0.8f);
            for (int i = 0; i < lights.Count; i++)
                if (lights[i] != null) lights[i].intensity = baseIntensity[i] * brightness;
            RenderSettings.ambientLight = baseAmbient * brightness;
            RenderSettings.ambientIntensity = baseAmbientIntensity * brightness;
        }
    }
}
