using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// A holdable prop that can also be used. While it is in the item slot, each
    /// left click makes a puff of wind food. The puff's temperature change also
    /// sets its colour: cold puffs are blue, warm puffs orange.
    /// </summary>
    public class ToolItem : HoldableItem
    {
        [Header("Use")]
        [Tooltip("Electricity used per puff while there is some stored (0 = needs none).")]
        public float powerPerPuff = 0f;
        [Tooltip("Money paid per puff when there is no electricity (0 = free).")]
        public float costPerPuff = 0f;

        [Header("Food puff")]
        [Tooltip("Fullness the pet gains when it eats one puff.")]
        public float food = 18f;
        [Tooltip("Temperature change per puff eaten. Negative = cools the pet.")]
        public float temperatureChange = 0f;

        public override bool Use(ToolBelt belt, Vector2 screen)
        {
            belt.MakePuff(this, screen);
            return true;
        }

        /// <summary>White for neutral, blue for cold, orange for warm.</summary>
        public Color PuffColor
        {
            get
            {
                float k = Mathf.Clamp01(Mathf.Abs(temperatureChange) / 8f);
                Color tint = temperatureChange < 0f ? new Color(0.45f, 0.8f, 1f) : new Color(1f, 0.6f, 0.3f);
                return Color.Lerp(new Color(0.95f, 0.97f, 1f), tint, k);
            }
        }
    }
}
