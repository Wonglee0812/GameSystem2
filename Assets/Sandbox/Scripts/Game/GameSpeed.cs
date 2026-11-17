using UnityEngine;
using UnityEngine.InputSystem;

namespace DamRush
{
    /// <summary>
    /// Game speed for the leader: pause, 1x, 2x, 3x and 5x (5x is mainly for playtests over many seasons). It changes Time.timeScale,
    /// so the beavers, the day and the water all run faster together. The camera uses unscaled
    /// time, so it still moves while the game is paused.
    /// Keys: Space = pause / continue, minus = slower, plus (=) = faster.
    /// </summary>
    public class GameSpeed : MonoBehaviour
    {
        public static readonly float[] Speeds = { 0f, 1f, 2f, 3f, 5f };

        [Tooltip("Index in Speeds at the start (1 = normal).")]
        public int index = 1;
        int beforePause = 1;

        public static GameSpeed Instance { get; private set; }
        public float Speed => Speeds[index];
        public bool Paused => index == 0;

        void Awake()
        {
            Instance = this;
            Apply();
        }

        void OnDestroy()
        {
            if (Instance == this) Time.timeScale = 1f;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame) TogglePause();
            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) Set(Mathf.Max(1, index - 1));
            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) Set(Mathf.Min(Speeds.Length - 1, index + 1));
        }

        public void TogglePause() => Set(Paused ? beforePause : 0);

        public void Set(int i)
        {
            i = Mathf.Clamp(i, 0, Speeds.Length - 1);
            if (i == 0 && index != 0) beforePause = index;
            index = i;
            Apply();
        }

        void Apply() => Time.timeScale = Speeds[index];
    }
}
