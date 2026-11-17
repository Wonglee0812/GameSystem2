using UnityEngine;

namespace DamRush
{
    public enum Season { Spring, Summer, Autumn, Winter }

    /// <summary>
    /// The year of the beaver family: four seasons of a few game days each, endless.
    /// - Spring: snowmelt flood waves come from upstream; they get stronger every year
    ///   (each hazard season is harder than the one before, as in Timberborn).
    /// - Summer: aquatic plants grow, foragers bring more food per trip.
    /// - Autumn: foragers cut branches from trees; the HUD shows how much food winter needs.
    /// - Winter: ice covers the water and gets thicker. Beavers can only leave the lodge and
    ///   reach a food storage where the water is deeper than the ice. Beavers store branches
    ///   under water and live from this cache when the pond freezes (Dorset Wildlife Trust, 2024);
    ///   when the water level is too low, the ice can trap them or freeze the cache (WXPR, 2019).
    /// The streams carry less water in winter.
    /// </summary>
    public class Seasons : MonoBehaviour
    {
        public ColonyManager colony;
        public MapController map;

        [Tooltip("Game days per season.")]
        public float daysPerSeason = 10f;
        [Tooltip("Thickest ice in the middle of winter (m).")]
        public float maxIce = 0.5f;
        [Tooltip("Water must be this much deeper than the ice for beavers to swim under it (m).")]
        public float iceMargin = 0.2f;
        [Tooltip("Stream flow in winter (share of normal).")]
        public float winterFlow = 0.5f;

        [Header("Spring floods")]
        public int floodsPerSpring = 2;
        [Tooltip("Flood strength (x normal flow) in year 1.")]
        public float firstFloodStrength = 5f;
        [Tooltip("Extra strength per year, as a share of the first year (0.35 = +35% per year).")]
        public float floodGrowthPerYear = 0.35f;
        [Tooltip("Flood v2: one long flood per spring. The forecast comes on this day of spring (0 = first day); the waves start one day later.")]
        public float floodForecastDay = 1f;

        [Header("Food")]
        [Tooltip("Extra food per forage trip in summer.")]
        public int summerForageBonus = 1;

        /// <summary>Game days since the start.</summary>
        public float Days { get; private set; }
        public int Year => Mathf.FloorToInt(Days / (4f * daysPerSeason)) + 1;
        public Season Current => (Season)(Mathf.FloorToInt(Days / daysPerSeason) % 4);
        /// <summary>Day in the season, 1-based for the HUD.</summary>
        public int Day => Mathf.FloorToInt(Days % daysPerSeason) + 1;
        public float SeasonProgress => (Days % daysPerSeason) / daysPerSeason;
        public bool IsWinter => Current == Season.Winter;
        /// <summary>Ice thickness now (m): grows in the first third of winter, melts in the last fifth.</summary>
        public float Ice { get; private set; }
        public float FloodStrength => firstFloodStrength * (1f + floodGrowthPerYear * (Year - 1));
        public int FloodsDone { get; private set; }
        public int ForageBonus => Current == Season.Summer ? summerForageBonus : 0;

        Season last = (Season)(-1);
        float[] floodDays;
        static readonly int IceId = Shader.PropertyToID("_DR_Ice");

        float DayLength => colony != null && colony.dayNight != null ? colony.dayNight.dayLength : 240f;

        void Start()
        {
            if (colony == null) colony = GetComponent<ColonyManager>();
            if (map == null) map = FindFirstObjectByType<MapController>();
        }

        void Update()
        {
            Days += Time.deltaTime / DayLength;
            var s = Current;
            if (s != last) { last = s; StartSeason(s); }

            // ice
            float target = 0f;
            if (s == Season.Winter)
            {
                float p = SeasonProgress;
                target = maxIce * Mathf.Clamp01(Mathf.Min(p / 0.33f, (1f - p) / 0.2f));
            }
            Ice = target;
            Shader.SetGlobalFloat(IceId, maxIce > 0f ? Ice / maxIce : 0f);

            // stream flow and spring floods
            if (map != null)
            {
                map.seasonFlow = s == Season.Winter ? winterFlow : 1f;
                // flood v2: one long snowmelt flood per spring (forecast, then a week of waves)
                if (s == Season.Spring && FloodsDone == 0 && Days % daysPerSeason >= floodForecastDay && !map.Flooding)
                {
                    FloodsDone = 1;
                    map.StartFlood(FloodStrength, $"the snowmelt flood of year {Year} (x{FloodStrength:0.#} water)");
                }
            }
        }

        void StartSeason(Season s)
        {
            FloodsDone = 0;
            floodDays = null;
            if (s == Season.Spring)
            {
                // flood days spread over the season, never on the first day
                floodDays = new float[floodsPerSpring];
                for (int i = 0; i < floodsPerSpring; i++)
                    floodDays[i] = daysPerSeason * (i + 0.5f) / floodsPerSpring + Random.Range(-0.15f, 0.15f) * daysPerSeason / floodsPerSpring;
            }
            if (colony != null) colony.Log($"{s.ToString().ToUpper()} begins (year {Year}): {Goal(s)}", true);
        }

        public static string Goal(Season s)
        {
            switch (s)
            {
                case Season.Spring: return "hold the snowmelt floods";
                case Season.Summer: return "build and grow, plants give extra food";
                case Season.Autumn: return "store food for winter";
                default: return "survive the ice";
            }
        }

        /// <summary>Food the whole family eats in one season (kits half).</summary>
        public float FoodNeededForSeason()
        {
            if (colony == null) return 0f;
            var n = colony.needs;
            float perBeaverDay = n.hungerPerSecond * DayLength / Mathf.Max(1f, n.foodPerMeal);
            float sum = 0f;
            foreach (var b in colony.beavers) sum += b.age == BeaverAge.Kit ? 0.5f : 1f;
            return sum * perBeaverDay * daysPerSeason;
        }

        /// <summary>One or two lines for the HUD: the goal of this season and how it is going.</summary>
        public string GoalText()
        {
            if (colony == null) return "";
            float lodge = colony.LodgeWaterDepth;
            float safe = maxIce + iceMargin;
            string lodgeTxt = lodge >= safe ? $"<color=#8fe08f>lodge water {lodge:0.00} m</color>" : $"<color=#ff8a70>lodge water {lodge:0.00} m (winter needs {safe:0.0} m)</color>";
            switch (Current)
            {
                case Season.Spring:
                    if (map != null && map.Flooding)
                        return map.Phase == FloodPhase.Warning
                            ? $"<color=#ffb070>Flood in {map.PhaseLeft / DayLength * 24f:0} h</color> (x{FloodStrength:0.#} water, about a week). Red outlines = buildings that may flood. Build levees, sandbags, open floodgates, and ring the alarm bell when the waves come."
                            : $"Flood: {map.DaysLeft:0.#} days left, x{map.FlowFactor:0.#} water now. {(map.WaveHigh ? "<color=#ff8a70>High wave: keep the family in the lodge</color>" : "<color=#8fe08f>Low water between waves: safe to work</color>")}";
                    return FloodsDone > 0 ? "The flood is over. Collect what it left (duckweed, mud, driftwood) and repair." : $"A snowmelt flood comes this spring (x{FloodStrength:0.#} water).";
                case Season.Summer:
                    return $"Build and grow. Foragers bring +{summerForageBonus} food per trip from water plants.\n{lodgeTxt}";
                case Season.Autumn:
                {
                    int food = colony.Stock(Item.Food);
                    int need = Mathf.CeilToInt(FoodNeededForSeason());
                    string col = food >= need ? "8fe08f" : "ffb070";
                    return $"Store food for winter: <color=#{col}><b>{food}/{need}</b></color> food. Foragers cut branches now.\n{lodgeTxt}. Food storages need {safe:0.0} m water next to them.";
                }
                default:
                {
                    int reach = colony.ReachableFood();
                    string frozen = colony.LodgeFrozenIn ? "  <color=#ff6050><b>FROZEN IN!</b></color>" : "";
                    return $"Survive the winter: ice {Ice:0.00} m. Food under the ice: {reach}.\n{lodgeTxt}{frozen}";
                }
            }
        }
    }
}
