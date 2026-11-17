using UnityEngine;

namespace DamRush.WaterCompare
{
    /// <summary>
    /// The test valley used by all three water versions, so they can be compared fairly:
    /// a winding stream that falls 3 m over 64 m, a reservoir behind a dam wall, a wide pond
    /// further down and an open end where the water leaves. Heights come from one formula, so
    /// every simulation grid (1 m, 0.5 m, 0.25 m) samples the same ground.
    /// </summary>
    public static class CompareShape
    {
        public const float LengthX = 64f, WidthZ = 48f;
        public const float WallX = 26f, WallHalfThickness = 0.8f, WallHeight = 1.8f;
        public const float BreachHalfWidth = 3.5f;

        public static float ChannelZ(float x) => WidthZ * 0.5f + 7f * Mathf.Sin(x * 0.11f);

        /// <summary>0 outside the pond, 1 in the middle of the wide pond (x 38-52).</summary>
        public static float Pond(float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(34f, 40f, x)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(50f, 56f, x)));

        public static float Ground(float x, float z)
        {
            float slope = 3f * (1f - x / LengthX);
            float pond = Pond(x);
            float halfWidth = 2.5f + 6f * pond;
            float d = Mathf.Abs(z - ChannelZ(x));
            float bank = Mathf.SmoothStep(0f, 1f, (d - halfWidth) / 6f);
            float h = slope + bank * 2.6f - (1f - bank) * (0.6f + 0.6f * pond);
            h += 0.3f * Mathf.PerlinNoise(x * 0.17f + 3.1f, z * 0.17f + 7.7f) - 0.15f;
            float edge = Mathf.Max(0f, 5f - Mathf.Min(z, WidthZ - z));
            return h + edge * 0.6f;
        }

        /// <summary>Top of the dam wall (one level across the valley).</summary>
        public static float WallTop => Ground(WallX, ChannelZ(WallX)) + WallHeight;

        /// <summary>Height of the dam wall above the ground at (x, z); 0 off the wall or in the breach.</summary>
        public static float Wall(float x, float z, bool breached)
        {
            if (Mathf.Abs(x - WallX) > WallHalfThickness) return 0f;
            if (breached && Mathf.Abs(z - ChannelZ(WallX)) < BreachHalfWidth) return 0f;
            return Mathf.Max(0f, WallTop - Ground(x, z));
        }

        /// <summary>Water at the start: the reservoir behind the wall is filled to 0.4 m below its top.</summary>
        public static float StartDepth(float x, float z)
        {
            if (x >= WallX - WallHalfThickness) return 0f;
            return Mathf.Max(0f, WallTop - 0.4f - Ground(x, z));
        }

        /// <summary>Inflow area at the upper end of the stream.</summary>
        public static bool IsSource(float x, float z) => x < 3f && Mathf.Abs(z - ChannelZ(x)) < 2.5f;
    }
}
