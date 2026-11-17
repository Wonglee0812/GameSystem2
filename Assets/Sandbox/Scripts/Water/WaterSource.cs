using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>Adds water to the grid in a circle around this object (m3 per second).</summary>
    public class WaterSource : MonoBehaviour
    {
        public static readonly List<WaterSource> All = new List<WaterSource>();

        [Tooltip("Flow in cubic metres per second (Timberborn also measures sources in m3/s).")]
        public float rate = 2f;
        public float radius = 1.5f;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
