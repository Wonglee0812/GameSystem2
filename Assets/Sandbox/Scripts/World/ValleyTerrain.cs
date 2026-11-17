using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// The river valley: a winding channel, banks on both sides, a narrow gorge at the
    /// mouth and a low terrace downstream. Kept for the game map; the water lab uses LabTerrain.
    /// </summary>
    public class ValleyTerrain : GroundHeights
    {
        /// <summary>Low terrace downstream (x/z rectangle), e.g. for a settlement.</summary>
        public Rect TerraceRect
        {
            get
            {
                float x0 = LengthX * 0.72f, x1 = LengthX * 0.9f;
                float zc = ChannelCenterZ((x0 + x1) * 0.5f);
                float z0 = zc + config.channelHalfWidth + 0.5f;
                return new Rect(x0, z0, x1 - x0, 4.5f);
            }
        }

        public float TerraceHeight(float x) => BedHeight(x) + config.villageTerraceHeight;
        public float BedHeight(float x) => config.riverDrop * (1f - Mathf.Clamp01(x / LengthX));
        public float ChannelCenterZ(float x) => WidthZ * 0.5f + 1.6f * Mathf.Sin(x * 0.16f);

        public override float InitialWaterLevel(float x, float z) => BedHeight(x) + 0.35f;

        protected override float HeightFormula(float x, float z)
        {
            float bed = BedHeight(x);
            float gorge = Mathf.Clamp01((x / LengthX - 0.9f) / 0.05f);
            float halfWidth = config.channelHalfWidth * Mathf.Lerp(1f, config.gorgeWidthFactor, gorge);
            float dist = Mathf.Abs(z - ChannelCenterZ(x)) - halfWidth;
            float t = Mathf.Clamp01(dist / config.bankSlope);
            t = t * t * (3f - 2f * t);
            float h = bed + t * config.bankHeight;
            h += t * (Mathf.PerlinNoise(x * 0.25f + config.terrainSeed, z * 0.25f) - 0.5f) * 0.4f;

            Rect v = TerraceRect;
            float mx = Mathf.Clamp01(Mathf.Min(x - v.xMin, v.xMax - x) / 1.2f + 0.5f);
            float mz = Mathf.Clamp01(Mathf.Min(z - (v.yMin - 1f), v.yMax - z) / 1.2f + 0.5f);
            float mask = Mathf.Min(mx, mz);
            if (mask > 0f) h = Mathf.Lerp(h, Mathf.Min(h, TerraceHeight(x)), mask);
            return h;
        }

        protected override Color ColorFormula(float x, float z, float h)
        {
            float t = Mathf.Clamp01((h - BedHeight(x)) / 0.8f);
            return Color.Lerp(new Color(0.45f, 0.38f, 0.28f), new Color(0.42f, 0.58f, 0.3f), t);
        }
    }
}
