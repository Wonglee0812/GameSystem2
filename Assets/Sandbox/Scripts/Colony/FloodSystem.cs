using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Flood v2 (plan v4): what a flood does to the colony, and what it leaves behind.
    /// - Swept away: a beaver outside the lodge in deep, fast water during a flood can be carried
    ///   off and dies. Kits are at the most risk, yearlings less, adults the least. The leader
    ///   protects the family by ringing the alarm bell (everybody swims home).
    /// - Flooded buildings: water above the floor of a storage spoils its stock (food fastest),
    ///   stops a machine or a lab, and stops a nursery.
    /// - Gifts: each wave that falls leaves piles of duckweed (food), mud and driftwood (wood)
    ///   on the shallow banks near the lodge.
    /// - Forecast: during the warning day, buildings that may flood are outlined in red.
    /// Numbers are a first version, to tune in playtests.
    /// </summary>
    public class FloodSystem : MonoBehaviour
    {
        public ColonyManager colony;
        public MapController map;

        [Header("Swept away")]
        [Tooltip("Water faster than this (m/s) and deeper than sweepDepth can carry a beaver off.")]
        public float sweepSpeed = 1.2f;
        public float sweepDepth = 0.3f;
        [Tooltip("Chance per second at sweepSpeed for a kit, a yearling and an adult (grows with the speed).")]
        public float kitRisk = 0.04f, yearlingRisk = 0.015f, adultRisk = 0.004f;

        [Header("Flooded buildings")]
        [Tooltip("Water this far above a floor floods the building (m).")]
        public float floodMargin = 0.1f;
        [Tooltip("Seconds per item a flooded storage loses: food spoils fast, other materials slowly.")]
        public float foodLossSeconds = 6f, otherLossSeconds = 20f;

        [Header("Gifts of a wave")]
        public int giftPilesBase = 3;
        public float giftPilesPerStrength = 0.6f;
        public float giftRange = 35f;

        [Header("Forecast")]
        [Tooltip("Expected rise of the water per x of flood strength (m), for the red outlines.")]
        public float risePerStrength = 0.12f;

        float tick;
        readonly Dictionary<BuildingSite, float> lossTimer = new Dictionary<BuildingSite, float>();
        readonly Dictionary<BuildingSite, float> lastWarn = new Dictionary<BuildingSite, float>();

        bool Active => map != null && map.Flooding && map.Phase != FloodPhase.Warning;

        void Start()
        {
            if (map == null) map = FindFirstObjectByType<MapController>();
            if (map != null) map.WaveFell += LeaveGifts;
        }

        void OnDestroy() { if (map != null) map.WaveFell -= LeaveGifts; }

        void Update()
        {
            if (colony == null || colony.orders == null) return;
            float dt = Time.deltaTime;
            if (map != null && map.Phase == FloodPhase.Warning) DrawForecast();
            tick -= dt;
            if (tick > 0f) return;
            float step = 0.5f - tick;
            tick = 0.5f;
            UpdateBuildings(step);
            if (Active) SweepBeavers(step);
        }

        // ------------------------------------------------------------------ swept away

        void SweepBeavers(float step)
        {
            var w = colony.water;
            foreach (var b in new List<Beaver>(colony.beavers))
            {
                if (b == null || b.state == BeaverState.InLodge) continue;
                Vector3 p = b.transform.position;
                if (w.DepthAt(p.x, p.z) < sweepDepth) continue;
                float speed = w.VelocityAt(p.x, p.z).magnitude;
                if (speed < sweepSpeed) continue;
                float risk = b.age == BeaverAge.Kit ? kitRisk : b.age == BeaverAge.Yearling ? yearlingRisk : adultRisk;
                risk *= speed / sweepSpeed;
                if (Random.value < risk * step)
                {
                    colony.Stat("beavers swept away");
                    colony.Die(b, $"swept away by the flood, a {b.age.ToString().ToLower()}");
                }
            }
        }

        // ------------------------------------------------------------------ flooded buildings

        /// <summary>Water surface on the hex is above the building's floor.</summary>
        public bool IsFlooded(BuildingSite b)
        {
            if (b.IsBase || b.Kind == BuildingKind.FoodFarm || b.Kind == BuildingKind.WaterWheel || b.Kind == BuildingKind.AlarmBell || !b.Finished) return false;
            float s = colony.water.SurfaceAt(b.hex.center.x, b.hex.center.z, out bool wet);
            if (!wet) foreach (var n in colony.grid.Neighbours(b.hex)) { s = Mathf.Max(s, colony.water.SurfaceAt(n.center.x, n.center.z, out bool nw)); wet |= nw; }
            // a little hysteresis, so a building on the edge does not flip on and off with every ripple
            return wet && s > b.Floor + (b.flooded ? floodMargin - 0.08f : floodMargin);
        }

        void UpdateBuildings(float step)
        {
            foreach (var b in colony.orders.Buildings)
            {
                bool was = b.flooded;
                b.flooded = IsFlooded(b);
                if (b.flooded && !was && (!lastWarn.TryGetValue(b, out float at) || Time.time - at > 120f))
                {
                    lastWarn[b] = Time.time;
                    colony.Log($"The {b.Title.ToLower()} at {b.hex.axial} is flooded!", true);
                }
                if (!b.flooded || b.Kind != BuildingKind.Storage || b.stock <= 0) continue;
                lossTimer.TryGetValue(b, out float t);
                t += step;
                float per = b.storeType == Item.Food ? foodLossSeconds : otherLossSeconds;
                while (t >= per && b.stock > 0) { t -= per; b.Add(b.storeType, -1); colony.Stat($"{ItemInfo.Lower(b.storeType)} lost in floods"); }
                lossTimer[b] = t;
            }
        }

        // ------------------------------------------------------------------ forecast

        public float ExpectedRise => map != null ? risePerStrength * map.floodStrength : 0f;

        /// <summary>A building may flood when the nearby water plus the expected rise reaches its floor.</summary>
        public bool AtRisk(BuildingSite b)
        {
            if (b.IsBase || b.Kind == BuildingKind.FoodFarm || b.Kind == BuildingKind.WaterWheel) return false;
            float top = float.MinValue;
            foreach (var n in colony.grid.Neighbours(b.hex))
            {
                float s = colony.water.SurfaceAt(n.center.x, n.center.z, out bool wet);
                if (wet) top = Mathf.Max(top, s);
            }
            return b.Kind != BuildingKind.AlarmBell && top > float.MinValue && top + ExpectedRise > b.Floor + floodMargin;
        }

        void DrawForecast()
        {
            float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
            foreach (var b in colony.orders.Buildings)
                if (AtRisk(b)) colony.grid.DrawHighlight(b.hex, new Color(1f, 0.2f * p, 0.1f));
        }

        // ------------------------------------------------------------------ gifts

        /// <summary>A wave fell: duckweed, mud and driftwood are left on shallow banks near the lodge.</summary>
        void LeaveGifts(float strength)
        {
            var options = new List<HexCell>();
            foreach (var c in colony.grid.Cells)
            {
                if (c.blocked || (c.center - colony.LodgeCenter).magnitude > giftRange) continue;
                float d = colony.water.DepthAt(c.center.x, c.center.z);
                if (d > 0.02f && d < 0.5f) options.Add(c);
            }
            if (options.Count == 0) return;
            int piles = Mathf.RoundToInt(giftPilesBase + giftPilesPerStrength * strength);
            int food = 0, mud = 0, wood = 0;
            for (int i = 0; i < piles; i++)
            {
                var hex = options[Random.Range(0, options.Count)];
                float r = Random.value;
                Item it = r < 0.4f ? Item.Food : r < 0.75f ? Item.Mud : Item.Wood;
                int n = Random.Range(2, 5);
                colony.DropPile(hex, it, n);
                if (it == Item.Food) food += n; else if (it == Item.Mud) mud += n; else wood += n;
            }
            colony.Stat("flood gifts", food + mud + wood);
            colony.Log($"The wave left duckweed ({food} food), mud ({mud}) and driftwood ({wood} wood) on the banks", true);
        }
    }
}
