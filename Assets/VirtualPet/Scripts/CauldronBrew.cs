using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Bubbles and steam above the cauldron. When the cauldron is knocked over
    /// (for example by an angry pet), the brew stops bubbling, and it starts
    /// again when the cauldron stands up.
    /// </summary>
    public class CauldronBrew : MonoBehaviour
    {
        public ParticleSystem[] effects;
        [Tooltip("Tilt in degrees at which the brew spills and stops bubbling.")]
        public float spillAngle = 50f;

        Vector3 restUp;
        bool running = true;

        void Start() => restUp = transform.up;

        void Update()
        {
            bool upright = Vector3.Angle(transform.up, restUp) < spillAngle;
            if (upright == running) return;
            running = upright;
            foreach (var fx in effects)
            {
                if (upright) fx.Play();
                else fx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}
