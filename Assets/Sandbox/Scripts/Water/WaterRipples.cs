using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Ripple rings on the water around things that touch it: swimming beavers (every frame
    /// while they swim) and short splashes (a builder dropping a stone into a dam).
    /// The water shader draws the rings; this class only collects the points and sends the
    /// nearest ones to the shader once per frame (called by WaterGrid).
    /// </summary>
    public static class WaterRipples
    {
        public const int Max = 24;
        struct Ripple { public Vector4 data; public float until; }
        static readonly List<Ripple> ripples = new List<Ripple>();
        static readonly Vector4[] upload = new Vector4[Max];
        static readonly int RipplesId = Shader.PropertyToID("_DR_Ripples");
        static readonly int CountId = Shader.PropertyToID("_DR_RippleCount");

        /// <summary>Ripples at a point. seconds = 0: only this frame (call it every frame for a swimmer).</summary>
        public static void Emit(Vector3 pos, float strength, float radius, float seconds = 0f)
        {
            ripples.Add(new Ripple { data = new Vector4(pos.x, pos.z, strength, radius), until = Time.time + seconds });
        }

        public static void Splash(Vector3 pos, float strength = 1f) => Emit(pos, strength, 2.5f, 1.2f);

        /// <summary>Sends the ripples nearest to the camera to the shader, then drops the old ones.</summary>
        public static void Upload(Vector3 camPos)
        {
            if (ripples.Count > Max)
                ripples.Sort((a, b) => Dist(a.data, camPos).CompareTo(Dist(b.data, camPos)));
            int n = Mathf.Min(Max, ripples.Count);
            for (int i = 0; i < Max; i++) upload[i] = i < n ? ripples[i].data : Vector4.zero;
            Shader.SetGlobalVectorArray(RipplesId, upload);
            Shader.SetGlobalFloat(CountId, n);
            float now = Time.time;
            ripples.RemoveAll(r => r.until <= now);
        }

        static float Dist(Vector4 d, Vector3 c) => (d.x - c.x) * (d.x - c.x) + (d.y - c.z) * (d.y - c.z);
    }
}
