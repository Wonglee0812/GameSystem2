using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace WindPet
{
    /// <summary>
    /// Prototype UI, built in code so it can be thrown away later without
    /// touching the scene: action buttons, debug bars, money, state, mood and a
    /// speech line. The final UI will replace this.
    /// </summary>
    public class PetHUD : MonoBehaviour
    {
        public PetStats stats;
        public PetStateMachine fsm;
        public Wallet wallet;
        public PetActions actions;
        public PetSave save;
        public bool showDebugBars = true;

        Font font;
        RectTransform hungerFill, temperatureFill, energyFill, funFill, bondFill;
        Text hungerValue, temperatureValue, energyValue, funValue, bondValue;
        Text infoText, speechText, turbineLabel, speedLabel;
        float speechTimer;

        void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildUI();
            fsm.Said += ShowSpeech;
        }

        void OnDestroy()
        {
            if (fsm != null) fsm.Said -= ShowSpeech;
        }

        void Update()
        {
            SetBar(hungerFill, hungerValue, stats.Hunger);
            SetBar(temperatureFill, temperatureValue, stats.Temperature);
            SetBar(energyFill, energyValue, stats.Energy);
            SetBar(funFill, funValue, stats.Fun);
            SetBar(bondFill, bondValue, stats.Bond);

            infoText.text = $"Money: {wallet.Money}\nState: {fsm.State}\nMood: {fsm.Mood}";
            turbineLabel.text = fsm.State == PetState.Working ? "Stop turbine" : "Turbine";
            speedLabel.text = $"Speed x{stats.config.timeScale:0}";

            if (speechTimer > 0f)
            {
                speechTimer -= Time.deltaTime;
                var c = speechText.color;
                c.a = Mathf.Clamp01(speechTimer);
                speechText.color = c;
            }
        }

        void ShowSpeech(string line)
        {
            speechText.text = line;
            speechTimer = 2.5f;
        }

        // ---------- Building ----------

        void BuildUI()
        {
            var canvasGo = new GameObject("PetHUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.GetComponent<RectTransform>();

            // Debug bars, top left.
            var panel = Panel(root, "Bars", new Vector2(0, 1), new Vector2(24, -24), new Vector2(460, 250));
            panel.gameObject.SetActive(showDebugBars);
            hungerFill = Bar(panel, "Hunger", 0, new Color(0.95f, 0.6f, 0.3f), out hungerValue);
            temperatureFill = Bar(panel, "Temperature", 1, new Color(0.9f, 0.35f, 0.3f), out temperatureValue);
            energyFill = Bar(panel, "Energy", 2, new Color(0.95f, 0.85f, 0.3f), out energyValue);
            funFill = Bar(panel, "Fun", 3, new Color(0.5f, 0.85f, 0.5f), out funValue);
            bondFill = Bar(panel, "Bond", 4, new Color(0.55f, 0.6f, 1f), out bondValue);

            // Info, top right.
            var info = Panel(root, "Info", new Vector2(1, 1), new Vector2(-24, -24), new Vector2(300, 130));
            infoText = Label(info, "Info", 26, TextAnchor.UpperLeft, new Vector2(16, -12), new Vector2(270, 110));

            // Speech line, above the pet.
            speechText = Label(root, "Speech", 40, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(900, 80));
            var speechRect = speechText.rectTransform;
            speechRect.anchorMin = speechRect.anchorMax = new Vector2(0.5f, 0.82f);
            speechRect.pivot = new Vector2(0.5f, 0.5f);
            speechRect.anchoredPosition = Vector2.zero;
            speechText.color = new Color(1, 1, 1, 0);

            // Action buttons, bottom centre.
            var bar = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            bar.SetParent(root, false);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0, 32);
            bar.sizeDelta = new Vector2(1200, 110);
            var layout = bar.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            Button(bar, "Fan (free)", actions.UseFan);
            Button(bar, $"Hair dryer ({stats.config.dryerCost})", actions.UseHairDryer);
            Button(bar, "Pinwheel", actions.PlayPinwheel);
            turbineLabel = Button(bar, "Turbine", actions.ToggleTurbine);

            // Debug buttons, bottom right.
            var debug = new GameObject("Debug", typeof(RectTransform), typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
            debug.SetParent(root, false);
            debug.anchorMin = debug.anchorMax = new Vector2(1f, 0f);
            debug.pivot = new Vector2(1f, 0f);
            debug.anchoredPosition = new Vector2(-24, 32);
            debug.sizeDelta = new Vector2(200, 130);
            var vLayout = debug.GetComponent<VerticalLayoutGroup>();
            vLayout.spacing = 10;
            vLayout.childControlWidth = vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = vLayout.childForceExpandHeight = true;
            speedLabel = Button(debug, "Speed x1", ToggleSpeed);
            if (save != null) Button(debug, "Reset pet", save.ResetSave);
        }

        void ToggleSpeed()
        {
            stats.config.timeScale = stats.config.timeScale < 2f ? 5f : 1f;
        }

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        static void SetBar(RectTransform fill, Text value, float amount)
        {
            fill.anchorMax = new Vector2(amount / 100f, 1f);
            value.text = Mathf.RoundToInt(amount).ToString();
        }

        RectTransform Panel(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
            return rect;
        }

        RectTransform Bar(RectTransform panel, string name, int row, Color color, out Text value)
        {
            float y = -20 - row * 44;
            Label(panel, name, 24, TextAnchor.MiddleLeft, new Vector2(16, y), new Vector2(150, 34));

            var back = new GameObject(name + "_Back", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            back.SetParent(panel, false);
            back.anchorMin = back.anchorMax = back.pivot = new Vector2(0, 1);
            back.anchoredPosition = new Vector2(170, y - 6);
            back.sizeDelta = new Vector2(220, 22);
            back.GetComponent<Image>().color = new Color(1, 1, 1, 0.15f);

            var fill = new GameObject(name + "_Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            fill.SetParent(back, false);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.5f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = color;

            value = Label(panel, name + "_Value", 22, TextAnchor.MiddleRight, new Vector2(390, y), new Vector2(54, 34));
            return fill;
        }

        Text Label(RectTransform parent, string name, int size, TextAnchor anchor, Vector2 position, Vector2 boxSize)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.rectTransform.SetParent(parent, false);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = new Vector2(0, 1);
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = boxSize;
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.text = name;
            return text;
        }

        Text Button(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.15f, 0.2f, 0.3f, 0.9f);
            go.GetComponent<Button>().onClick.AddListener(onClick);

            var text = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.rectTransform.SetParent(go.transform, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.font = font;
            text.fontSize = 30;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            return text;
        }
    }
}
