using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Procedural look of the pet: the wind inside the bag glows through the eye
    /// holes, and the glow colour follows Temperature (blue = cold, orange = hot).
    /// Only touches the glow objects, never the transforms that the Animator moves.
    /// </summary>
    public class PetView : MonoBehaviour
    {
        public PetStats stats;
        public PetStateMachine fsm;
        public SpriteRenderer[] glowRenderers;

        public Color coldColor = new Color(0.45f, 0.75f, 1f);
        public Color comfortColor = new Color(1f, 1f, 0.92f);
        public Color hotColor = new Color(1f, 0.5f, 0.15f);

        void Update()
        {
            float t = stats.Temperature / 100f;
            Color color = t < 0.5f
                ? Color.Lerp(coldColor, comfortColor, t / 0.5f)
                : Color.Lerp(comfortColor, hotColor, (t - 0.5f) / 0.5f);

            // Low energy makes the glow dim; sleeping almost turns it off.
            float brightness = Mathf.Lerp(0.35f, 1f, stats.Energy / 100f);
            if (fsm.State == PetState.Sleeping) brightness = 0.2f;
            color *= brightness;
            color.a = 1f;

            foreach (var r in glowRenderers)
                if (r != null) r.color = color;
        }
    }
}
