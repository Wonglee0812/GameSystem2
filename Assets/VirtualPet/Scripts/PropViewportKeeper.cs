using System.Collections.Generic;
using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Keeps every physics prop on screen. Each frame it checks where a moving
    /// prop's centre is in the camera view (0..1 on both axes). If the centre
    /// leaves the allowed area, the prop is put back on the edge and bounces off
    /// it, like hitting the side of a box. Props that already start partly off
    /// screen get a bigger area, so they are never pushed at the start.
    /// Works for any window size (also WebGL), because it uses view coordinates.
    /// Props made while playing (bills) call Register.
    /// Safety net: a prop that sinks below the desk top is lifted back onto it.
    /// </summary>
    public class PropViewportKeeper : MonoBehaviour
    {
        public static PropViewportKeeper Instance { get; private set; }

        public Camera cam;
        public LayerMask propLayers;
        [Range(0f, 0.2f)] public float margin = 0.04f;
        [Range(0f, 1f)] public float bounce = 0.35f;
        [Tooltip("Closest distance to the camera a prop may come.")]
        public float minDepth = 4.5f;
        public float maxDepth = 14f;
        [Header("Safety net")]
        [Tooltip("Height of the desk top. A prop that sinks below it is put back on the desk.")]
        public float floorY = -1f;
        public float recoverBelow = 0.4f;
        public float recoverHeight = 0.8f;

        readonly List<Rigidbody> bodies = new List<Rigidbody>();
        readonly List<Rect> allowed = new List<Rect>();
        readonly List<float> nearest = new List<float>();

        void Awake() => Instance = this;

        void Start()
        {
            if (cam == null) cam = Camera.main;
            foreach (var rb in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
                if ((propLayers.value & (1 << rb.gameObject.layer)) != 0) Register(rb);
        }

        /// <summary>Starts keeping this prop on screen.</summary>
        public void Register(Rigidbody rb)
        {
            if (rb == null || bodies.Contains(rb)) return;
            if (cam == null) cam = Camera.main;
            Vector3 vp = cam.WorldToViewportPoint(rb.worldCenterOfMass);
            bodies.Add(rb);
            nearest.Add(Mathf.Min(minDepth, vp.z - 0.1f));   // props that start closer may stay there
            allowed.Add(Rect.MinMaxRect(Mathf.Min(margin, vp.x), Mathf.Min(margin, vp.y),
                                        Mathf.Max(1f - margin, vp.x), Mathf.Max(1f - margin, vp.y)));
        }

        void FixedUpdate()
        {
            for (int i = bodies.Count - 1; i >= 0; i--)
            {
                Rigidbody rb = bodies[i];
                if (rb == null) { bodies.RemoveAt(i); allowed.RemoveAt(i); nearest.RemoveAt(i); continue; }
                if (rb.isKinematic || rb.IsSleeping() || !rb.gameObject.activeInHierarchy) continue;
                Vector3 com = rb.worldCenterOfMass;
                if (com.y < floorY - recoverBelow)
                {
                    // fell through the desk: drop it back on top, where the player can reach it
                    rb.position += Vector3.up * (floorY + recoverHeight - com.y);
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    continue;
                }
                Vector3 vp = cam.WorldToViewportPoint(com);
                Rect r = allowed[i];
                Vector3 c = new Vector3(Mathf.Clamp(vp.x, r.xMin, r.xMax), Mathf.Clamp(vp.y, r.yMin, r.yMax),
                                        Mathf.Clamp(vp.z, nearest[i], maxDepth));
                if (c == vp) continue;
                Vector3 delta = cam.ViewportToWorldPoint(c) - com;
                rb.position += delta;
                Vector3 n = delta.normalized;              // points back into the view
                float vn = Vector3.Dot(rb.linearVelocity, n);
                if (vn < 0f) rb.linearVelocity -= (1f + bounce) * vn * n;
            }
        }
    }
}
