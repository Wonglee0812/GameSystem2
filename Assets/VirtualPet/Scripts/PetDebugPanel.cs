using UnityEngine;
using UnityEngine.InputSystem;

namespace WindPet
{
    /// <summary>
    /// Test panel for the bar system (press F1 in play mode). Drag the bars, spend
    /// or restore energy and speed up time, and watch the bands and the mood rules
    /// react. For development and the explanation video only.
    /// </summary>
    public class PetDebugPanel : MonoBehaviour
    {
        public PetStats stats;
        public PetStateMachine fsm;
        [Tooltip("Folded = only a small button. Click it (or press F1) to open the panel.")]
        public bool visible = false;
        public float spendAmount = 10f;

        Rect window = new Rect(0, 110, 330, 400);

        // start on the right, under the money, so the status icons stay visible
        void Start() => window.x = Screen.width - window.width - 16;

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) visible = !visible;
        }

        void OnGUI()
        {
            if (stats == null || fsm == null) return;
            if (!visible)
            {
                if (GUI.Button(new Rect(Screen.width - 90, 110, 74, 24), "Debug")) visible = true;
                return;
            }
            window = GUILayout.Window(GetInstanceID(), window, Draw, "Pet bars (F1)");
        }

        void Draw(int id)
        {
            float e = Slider("Energy", stats.Energy, fsm.Energy.ToString());
            float f = Slider("Fullness", stats.Fullness, fsm.Hunger.ToString());
            float m = Slider("Mood", stats.Mood, fsm.Mood.ToString());
            float t = Slider("Temperature", stats.Temperature, fsm.Temp.ToString());
            if (e != stats.Energy || f != stats.Fullness || m != stats.Mood || t != stats.Temperature)
                stats.SetAll(e, f, m, t);

            GUILayout.Space(6);
            GUILayout.Label($"Mood {fsm.MoodRate:+0.0;-0.0;0}/s  ({fsm.MoodReason})");
            GUILayout.Label($"Energy cost x{fsm.CostMultiplier:0.0} ({fsm.Temp})");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Spend {spendAmount:0} energy")) fsm.SpendEnergy(spendAmount);
            if (GUILayout.Button($"Restore {spendAmount:0} energy")) fsm.RestoreEnergy(spendAmount);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Time x{stats.config.timeScale:0}", GUILayout.Width(70));
            foreach (float s in new[] { 1f, 5f, 20f })
                if (GUILayout.Button("x" + s)) stats.config.timeScale = s;
            if (GUILayout.Button("Reset")) stats.ResetToStart();
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Hide")) visible = false;

            GUI.DragWindow();
        }

        static float Slider(string label, float value, string band)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label} {value:0}", GUILayout.Width(110));
            float v = GUILayout.HorizontalSlider(value, 0f, 100f, GUILayout.Width(120));
            GUILayout.Label(band, GUILayout.Width(70));
            GUILayout.EndHorizontal();
            return v;
        }
    }
}
