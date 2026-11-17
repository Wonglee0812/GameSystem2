using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// A part breaks into debris when one hit changes its speed by more than the
    /// material's toughness (stone is tough, mud is soft). Bumps in the first moment
    /// after the flood starts are ignored, because the parts are still settling.
    /// </summary>
    [RequireComponent(typeof(Part))]
    public class Breakable : MonoBehaviour
    {
        Part part;
        bool broken;

        public static int BrokenCount;
        public static int JointsSnapped;

        void Awake() { part = GetComponent<Part>(); }

        void OnCollisionEnter(Collision c)
        {
            if (broken || !part.IsActive || Time.time - part.ActiveSince < Part.SettleTime) return;
            float deltaV = c.impulse.magnitude / Mathf.Max(0.01f, part.Body.mass);
            if (deltaV < part.material.toughness) return;
            broken = true;
            BrokenCount++;
            PartFactory.Shatter(part);
        }

    }
}
