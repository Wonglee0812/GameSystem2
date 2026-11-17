using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Test ground for the water lab, in two lanes side by side:
    /// - Pool (front lane): flat floor, walls all round, still water at the start.
    ///   Use it to check floating, sinking and buoyancy without any current.
    /// - Channel (back lane): sloped bed, water source at the top, open end at the bottom.
    ///   Use it to check flow speed, drag and the push of water against walls.
    /// </summary>
    public class LabTerrain : GroundHeights
    {
        public const float WallHeight = 3f;
        public float poolWaterLevel = 1.2f;
        public float channelTopBed = 1f; // bed height at the source end

        // lane borders in metres along z
        public float PoolZ0 => 0.5f;
        public float PoolZ1 => WidthZ * 0.42f;
        public float ChannelZ0 => WidthZ * 0.55f;
        public float ChannelZ1 => WidthZ - 0.5f;

        public bool InPool(float x, float z) => x > 0.5f && x < LengthX - 0.5f && z > PoolZ0 && z < PoolZ1;
        public bool InChannel(float x, float z) => x > 0.5f && z > ChannelZ0 && z < ChannelZ1;
        public float ChannelBed(float x) => channelTopBed * (1f - Mathf.Clamp01(x / LengthX));

        protected override float HeightFormula(float x, float z)
        {
            if (InPool(x, z)) return 0f;
            if (InChannel(x, z)) return ChannelBed(x);
            return WallHeight;
        }

        protected override Color ColorFormula(float x, float z, float h)
        {
            if (h >= WallHeight - 0.01f) return new Color(0.55f, 0.55f, 0.58f);
            // checker tiles every metre, so distances are easy to read
            bool odd = ((Mathf.FloorToInt(x) + Mathf.FloorToInt(z)) & 1) == 1;
            return odd ? new Color(0.82f, 0.76f, 0.6f) : new Color(0.74f, 0.68f, 0.52f);
        }

        public override float InitialWaterLevel(float x, float z) =>
            InPool(x, z) ? poolWaterLevel : float.NegativeInfinity;
    }
}
