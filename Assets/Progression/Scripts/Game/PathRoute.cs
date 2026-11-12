using UnityEngine;

namespace PokerTD
{
    /// <summary>The waypoints of the enemy path, from the spawn to the base, in order.</summary>
    public class PathRoute : MonoBehaviour
    {
        public static PathRoute Instance { get; private set; }
        public Transform[] points;

        void Awake() => Instance = this;

        /// <summary>Total length of the path, used to compare how far enemies got.</summary>
        public float Length
        {
            get
            {
                float d = 0f;
                for (int i = 1; i < points.Length; i++) d += Vector3.Distance(points[i - 1].position, points[i].position);
                return d;
            }
        }
    }
}
