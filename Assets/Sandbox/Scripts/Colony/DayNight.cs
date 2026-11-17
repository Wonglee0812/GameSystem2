using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Day and night. Beavers are mainly active at night: they leave the lodge at dusk and
    /// come back at dawn (Bartra Cabré et al., 2020). In the game, work at night costs less
    /// energy than work in daylight. The sun turns and the light gets dark at night.
    /// </summary>
    public class DayNight : MonoBehaviour
    {
        public Light sun;
        public Camera cam;
        [Tooltip("Real seconds for one full day.")]
        public float dayLength = 240f;
        [Range(0f, 1f), Tooltip("0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset.")]
        public float time01 = 0.72f; // start just before sunset, when beavers wake up

        public Color dayAmbient = new Color(0.55f, 0.6f, 0.62f);
        public Color nightAmbient = new Color(0.3f, 0.34f, 0.48f);
        public Color daySky = new Color(0.55f, 0.7f, 0.82f);
        public Color nightSky = new Color(0.1f, 0.13f, 0.24f);

        float sunIntensity, sunYaw;

        /// <summary>Night between about 20:00 and 06:00.</summary>
        public bool IsNight => time01 < 0.25f || time01 > 0.83f;
        /// <summary>0 at night, 1 at noon.</summary>
        public float Daylight => Mathf.Clamp01(Mathf.Sin((time01 - 0.25f) * Mathf.PI * 2f) * 1.6f + 0.2f);
        public string Clock
        {
            get
            {
                float h = time01 * 24f;
                return $"{Mathf.FloorToInt(h):00}:{Mathf.FloorToInt((h % 1f) * 60f):00}";
            }
        }

        void Start()
        {
            sunIntensity = sun != null ? sun.intensity : 1f;
            sunYaw = sun != null ? sun.transform.eulerAngles.y : 0f;
        }

        void Update()
        {
            time01 = (time01 + Time.deltaTime / Mathf.Max(1f, dayLength)) % 1f;
            float light = Daylight;
            if (sun != null)
            {
                float elevation = Mathf.Lerp(-10f, 60f, light);
                sun.transform.rotation = Quaternion.Euler(Mathf.Max(5f, elevation), sunYaw + time01 * 180f, 0f);
                sun.intensity = Mathf.Lerp(0.35f, sunIntensity, light); // a little moonlight at night
                sun.color = Color.Lerp(new Color(0.6f, 0.7f, 1f), new Color(1f, 0.96f, 0.88f), light);
            }
            RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, light);
            if (cam != null) cam.backgroundColor = Color.Lerp(nightSky, daySky, light);
        }
    }
}
