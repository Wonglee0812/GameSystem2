using UnityEngine;
using UnityEngine.UI;

namespace WindPet.UI
{
    /// <summary>
    /// One need icon that fills up with colour from the bottom.
    /// Two layers, like a "Dark Souls" health bar: the front layer jumps to the
    /// new value, and the back layer follows smoothly after a short delay, so the
    /// player can see how much was lost or gained.
    /// When the value is in a bad range, the icon pulses to ask for attention.
    /// </summary>
    public class StatusIcon : MonoBehaviour
    {
        [Header("Images (Image Type = Filled, Vertical, Origin Bottom)")]
        public Image frontFill;
        public Image backFill;

        [Header("Colours")]
        public Color normalColor = new Color(0.55f, 0.85f, 1f);
        public Color criticalColor = new Color(1f, 0.45f, 0.35f);
        public Color lossColor = new Color(1f, 1f, 1f, 0.6f);
        public Color gainColor = new Color(0.6f, 1f, 0.7f, 0.8f);

        [Header("Critical warning")]
        [Tooltip("Pulse when the value is below this (0-1).")]
        [Range(0f, 1f)] public float criticalBelow = 0.25f;
        [Tooltip("Turn on for needs where a HIGH value is bad (for example Hunger).")]
        public bool highIsBad;
        public float pulseSpeed = 6f;
        public float pulseScale = 0.08f;

        [Header("Smoothing")]
        public float lagDelay = 0.35f;
        public float lagSpeed = 2.5f;

        float target = 1f;
        float delayTimer;
        Vector3 baseScale;

        void Awake()
        {
            baseScale = transform.localScale;
        }

        /// <summary>Value from 0 (empty) to 1 (full).</summary>
        public void SetValue(float value01)
        {
            value01 = Mathf.Clamp01(value01);
            if (Mathf.Approximately(value01, target)) return;
            target = value01;
            delayTimer = lagDelay;
        }

        void Update()
        {
            if (frontFill == null || backFill == null) return;

            bool losing = target < backFill.fillAmount;

            if (losing)
            {
                // Front drops at once, back follows after the delay.
                frontFill.fillAmount = target;
                backFill.color = lossColor;
                if (delayTimer > 0f) delayTimer -= Time.deltaTime;
                else backFill.fillAmount = Mathf.Lerp(backFill.fillAmount, target, Time.deltaTime * lagSpeed);
            }
            else
            {
                // Back jumps up at once (shows the gain), front fills up to it.
                backFill.fillAmount = target;
                backFill.color = gainColor;
                if (delayTimer > 0f) delayTimer -= Time.deltaTime;
                else frontFill.fillAmount = Mathf.Lerp(frontFill.fillAmount, target, Time.deltaTime * lagSpeed);
            }

            bool critical = highIsBad ? target > 1f - criticalBelow : target < criticalBelow;
            frontFill.color = critical ? criticalColor : normalColor;

            float pulse = critical ? 1f + Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed)) * pulseScale : 1f;
            transform.localScale = baseScale * pulse;
        }
    }
}
