using UnityEngine;
using UnityEngine.UI;

namespace WindPet.UI
{
    /// <summary>
    /// Thermometer icon. The liquid rises with the temperature and changes colour:
    /// low and blue when cold, white in the comfort zone, high and orange when hot.
    /// It pulses when the pet is outside the comfort zone.
    /// </summary>
    public class TemperatureIcon : MonoBehaviour
    {
        [Tooltip("Liquid image. Image Type = Filled, Vertical, Origin Bottom.")]
        public Image liquid;

        [Tooltip("Fill amount at 0 degrees. Keeps the bulb full so the icon never looks empty.")]
        [Range(0f, 1f)] public float minFill = 0.30f;
        [Tooltip("Fill amount at the top of the tube.")]
        [Range(0f, 1f)] public float maxFill = 0.88f;

        public Color coldColor = new Color(0.36f, 0.66f, 1f);
        public Color comfortColor = Color.white;
        public Color hotColor = new Color(1f, 0.55f, 0.2f);

        [Range(0f, 1f)] public float comfortMin = 0.4f;
        [Range(0f, 1f)] public float comfortMax = 0.6f;
        public float pulseSpeed = 6f;
        public float pulseScale = 0.08f;
        public float smoothSpeed = 4f;

        float target = 0.5f;
        float shown = 0.5f;
        Vector3 baseScale;

        void Awake() => baseScale = transform.localScale;

        public void SetValue(float value01) => target = Mathf.Clamp01(value01);

        void Update()
        {
            shown = Mathf.Lerp(shown, target, Time.deltaTime * smoothSpeed);

            if (liquid != null)
            {
                liquid.fillAmount = Mathf.Lerp(minFill, maxFill, shown);
                liquid.color = shown < 0.5f
                    ? Color.Lerp(coldColor, comfortColor, shown / 0.5f)
                    : Color.Lerp(comfortColor, hotColor, (shown - 0.5f) / 0.5f);
            }

            bool outside = shown < comfortMin || shown > comfortMax;
            float pulse = outside ? 1f + Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed)) * pulseScale : 1f;
            transform.localScale = baseScale * pulse;
        }
    }
}
