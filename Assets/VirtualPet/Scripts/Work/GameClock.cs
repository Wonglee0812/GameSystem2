using TMPro;
using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// A normal day clock for the lab (hours and minutes), so the player can keep an
    /// eye on contract deadlines. It starts at config.startHour on day 1 and one game
    /// hour lasts config.secondsPerGameHour real seconds.
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        public static GameClock Instance { get; private set; }

        public WorkConfig config;
        [Tooltip("Optional on-screen text for the time.")]
        public TMP_Text display;

        /// <summary>Game minutes since day 1, 00:00.</summary>
        public float Minutes { get; private set; }
        public int Now => Mathf.FloorToInt(Minutes);

        void Awake()
        {
            Instance = this;
            Minutes = config.startHour * 60f;
        }

        void Update()
        {
            Minutes += Time.deltaTime * 60f / Mathf.Max(0.01f, config.secondsPerGameHour);
            if (display != null)
            {
                string text = Format(Now, true);
                var office = ContractOffice.Instance;
                if (office != null && office.Strikes > 0) text += $"\n<size=60%><color=#E07A5F>fines {office.Strikes}/{config.strikesBeforeBigFine}</color></size>";
                display.text = text;
            }
        }

        /// <summary>"14:30", or "Day 2  14:30" when asked for the day.</summary>
        public static string Format(int minutes, bool withDay)
        {
            int day = minutes / 1440 + 1;
            int m = minutes % 1440;
            string time = (m / 60).ToString("00") + ":" + (m % 60).ToString("00");
            return withDay ? "Day " + day + "  " + time : time;
        }

        /// <summary>A deadline as text: only the time when it is today.</summary>
        public string Deadline(int minutes) => minutes / 1440 == Now / 1440 ? Format(minutes, false) : Format(minutes, true);
    }
}
