using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BrickPanic.EditorTools
{
    /// <summary>Menu "Brick Panic/Build HUD In Scene": makes the uGUI HUD (canvas "HUD"), the EventSystem, the UI theme
    /// asset and the card pictures (bricks rendered with their material). Safe to run twice: the old "HUD" object is
    /// replaced, the theme keeps the values Wong changed (only empty fields are filled), pictures are rendered again
    /// (so new brick colours show up). Run it again after changing materials or shapes.</summary>
    public static class BrickPanicHudBuilder
    {
        const string Root = "Assets/BrickPanic";
        const string UiDir = Root + "/Art/UI";
        const string RenderDir = UiDir + "/Renders";
        const string ThemePath = Root + "/Data/BP_UITheme.asset";
        const string FontDir = "Assets/Progression/Art/Resources/Fonts";

        static BPTheme theme;

        [MenuItem("Brick Panic/Build HUD In Scene")]
        public static void BuildMenu()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            Build();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        public static void Build()
        {
            ImportSprites();
            theme = EnsureTheme();
            var build = Object.FindFirstObjectByType<BuildController>();
            var round = Object.FindFirstObjectByType<RoundManager>();
            if (build == null || round == null) { Debug.LogError("[BrickPanic] Open BrickPanic_Main first (no BuildController / RoundManager)."); return; }
            var matIcons = new Sprite[build.materials.Length];
            var shapeIcons = new Sprite[build.shapes.Length];
            var brick24 = System.Array.Find(build.shapes, s => s != null && s.displayName.Contains("2X4") && s.kind == ShapeKind.Brick) ?? build.shapes[0];
            for (int i = 0; i < build.materials.Length; i++)
                matIcons[i] = RenderIcon(brick24.mesh, build.materials[i].RenderMaterial, "Mat_" + build.materials[i].displayName);
            // every shape in every material, so the size popup shows the bricks in the chosen material
            var shapeByMat = new Sprite[build.materials.Length * build.shapes.Length];
            for (int m = 0; m < build.materials.Length; m++)
                for (int i = 0; i < build.shapes.Length; i++)
                    shapeByMat[m * build.shapes.Length + i] = RenderIcon(build.shapes[i].mesh, build.materials[m].RenderMaterial, "Shape_" + build.materials[m].displayName + "_" + build.shapes[i].name);
            for (int i = 0; i < build.shapes.Length; i++) shapeIcons[i] = shapeByMat[i];

            var old = GameObject.Find("HUD");
            if (old != null) Object.DestroyImmediate(old);
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(es, "EventSystem");
            }

            var canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var hud = canvasGo.AddComponent<BrickPanicHud>();
            hud.round = round; hud.build = build; hud.activator = Object.FindFirstObjectByType<PhysicsActivator>(); hud.theme = theme;
            var c = canvasGo.transform;

            // Layout rules (Wong, 2026-11-25): every label sits in its own box with an inner margin and auto-sizes down
            // to fit, so no text touches a border or grows past its button. Titles (Action Comics) use about half the
            // box height, numbers (Super Chips) about 0.6, like Poker TD's buttons (max = 0.62 x size).
            var dark = new Color(0.13f, 0.1f, 0.17f, 0.92f);

            // ---------------- top left
            hud.roundTitle = Txt(c, "RoundTitle", true, true, 60f, theme.yellow, TextAlignmentOptions.MidlineLeft);
            Place(hud.roundTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -26f), new Vector2(520f, 84f));
            hud.roundTitle.text = "ROUND 1";
            hud.goalText = Txt(c, "Goal", false, true, 30f, theme.cream, TextAlignmentOptions.MidlineLeft);
            Place(hud.goalText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -112f), new Vector2(520f, 42f));
            hud.goalText.text = "GOAL: 5 SURVIVORS";

            // ---------------- stats top right
            hud.moneyPill = StatPill(c, "Money", 0, theme.yellow, theme.money, out hud.moneyText);
            hud.moneyText.text = "$100";
            hud.alivePill = StatPill(c, "Alive", 1, theme.green, theme.people, out hud.aliveText);
            hud.aliveText.text = "0 / 5";
            hud.wearPill = StatPill(c, "Wear", 2, theme.orange, theme.wear, out hud.wearText);
            hud.wearText.fontSizeMax = 20f;
            Stretch(hud.wearText.rectTransform, new Vector2(72f, 30f), new Vector2(-18f, -8f));
            hud.wearText.text = "JOINT WEAR 0%";
            var barBg = Img(hud.wearPill, "BarBack", theme.fill, new Color(1f, 1f, 1f, 0.15f), true);
            Stretch(barBg.rectTransform, new Vector2(72f, 12f), new Vector2(-18f, -36f));
            var barFill = Img(barBg.rectTransform, "Fill", theme.fill, theme.green, true);
            barFill.rectTransform.anchorMin = Vector2.zero;
            barFill.rectTransform.anchorMax = new Vector2(0.04f, 1f);
            barFill.rectTransform.offsetMin = Vector2.zero;
            barFill.rectTransform.offsetMax = Vector2.zero;
            hud.wearFill = barFill.rectTransform;

            // ---------------- forecast + START (build phase): icon, name, one line - nothing more
            var fc = Img(c, "Forecast", theme.panel, theme.cream, true, true).rectTransform;
            Place(fc, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(400f, 120f));
            hud.forecastRoot = fc;
            hud.forecastIcon = Img(fc, "Icon", theme.tsunami, theme.tsunamiColor, false);
            hud.forecastIcon.preserveAspect = true;
            Place(hud.forecastIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(76f, 76f));
            hud.forecastName = Txt(fc, "Name", true, true, 34f, theme.tsunamiColor, TextAlignmentOptions.BottomLeft);
            Box(hud.forecastName.rectTransform, new Vector2(112f, 58f), new Vector2(-20f, -14f));
            hud.forecastName.text = "TSUNAMI";
            hud.forecastDetail = Txt(fc, "Detail", false, false, 20f, theme.ink, TextAlignmentOptions.TopLeft);
            Box(hud.forecastDetail.rectTransform, new Vector2(114f, 16f), new Vector2(-20f, -72f));
            hud.forecastDetail.text = "FROM THE EAST  -  LEVEL 1";
            hud.startButton = Btn(fc, "StartButton", theme.orange, "START", 34f, true);
            Place((RectTransform)hud.startButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(240f, 76f));
            hud.peopleWarning = Txt(fc, "PeopleWarning", false, true, 22f, theme.red, TextAlignmentOptions.Center);
            Place(hud.peopleWarning.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(400f, 32f));
            hud.peopleWarning.text = "NEED 5 MORE PEOPLE";

            // ---------------- live counter (disaster)
            var live = Img(c, "Live", theme.panel, dark, true).rectTransform;
            Place(live, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(560f, 88f));
            hud.liveRoot = live;
            hud.liveIcon = Img(live, "Icon", theme.tsunami, theme.tsunamiColor, false);
            hud.liveIcon.preserveAspect = true;
            Place(hud.liveIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(60f, 60f));
            hud.liveName = Txt(live, "Name", true, true, 28f, theme.tsunamiColor, TextAlignmentOptions.BottomLeft);
            Place(hud.liveName.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(92f, -2f), new Vector2(240f, 38f));
            hud.liveDetail = Txt(live, "Detail", false, false, 17f, theme.cream, TextAlignmentOptions.TopLeft);
            Place(hud.liveDetail.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(94f, -4f), new Vector2(240f, 26f));
            var ai = Img(live, "AliveIcon", theme.people, theme.green, false);
            ai.preserveAspect = true;
            Place(ai.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(350f, 0f), new Vector2(40f, 40f));
            hud.liveAlive = Txt(live, "Alive", false, true, 30f, theme.green, TextAlignmentOptions.MidlineLeft);
            Place(hud.liveAlive.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(396f, 0f), new Vector2(56f, 48f));
            var di = Img(live, "DeadIcon", theme.dead, theme.red, false);
            di.preserveAspect = true;
            Place(di.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(456f, 0f), new Vector2(38f, 38f));
            hud.liveDead = Txt(live, "Dead", false, true, 30f, theme.red, TextAlignmentOptions.MidlineLeft);
            Place(hud.liveDead.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(500f, 0f), new Vector2(48f, 48f));

            // ---------------- shop (bottom left): SHOP button + current tool
            var shop = new GameObject("Shop", typeof(RectTransform)).GetComponent<RectTransform>();
            shop.SetParent(c, false);
            Place(shop, Vector2.zero, Vector2.zero, new Vector2(32f, 28f), new Vector2(500f, 140f));
            hud.shopRoot = shop;
            hud.shopButton = Btn(shop, "ShopButton", theme.orange, "SHOP", 26f, false);
            Place((RectTransform)hud.shopButton.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(140f, 140f));
            var shopVis = hud.shopButton.GetComponent<BPButton>().visual;
            var shopLabel = shopVis.Find("Label") as RectTransform;
            Box(shopLabel, new Vector2(12f, 16f), new Vector2(-12f, -96f));
            var shopIcon = Img(shopVis, "Icon", matIcons.Length > 0 ? matIcons[0] : null, Color.white, false);
            shopIcon.preserveAspect = true;
            Box(shopIcon.rectTransform, new Vector2(18f, 50f), new Vector2(-18f, -12f));
            Badge(shopVis, "B");
            var chip = Img(shop, "Tool", theme.panel, dark, true).rectTransform;
            Place(chip, Vector2.zero, Vector2.zero, new Vector2(156f, 0f), new Vector2(330f, 86f));
            hud.toolIcon = Img(chip, "Icon", matIcons.Length > 0 ? matIcons[0] : null, Color.white, false);
            hud.toolIcon.preserveAspect = true;
            Place(hud.toolIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(66f, 66f));
            hud.toolName = Txt(chip, "Name", true, true, 22f, theme.cream, TextAlignmentOptions.BottomLeft);
            Box(hud.toolName.rectTransform, new Vector2(90f, 42f), new Vector2(-16f, -12f));
            hud.toolName.text = "PLASTIC  2X4";
            hud.toolPrice = Txt(chip, "Price", false, true, 22f, theme.yellow, TextAlignmentOptions.TopLeft);
            Box(hud.toolPrice.rectTransform, new Vector2(92f, 12f), new Vector2(-16f, -46f));
            hud.toolPrice.text = "$8";
            hud.materialIcons = matIcons;
            hud.shapeIconsByMaterial = shapeByMat;

            // ---------------- popup 1: materials + person
            int nm = build.materials.Length;
            Vector2 cardSize = new Vector2(150f, 196f);
            float gap = 16f, pad = 24f;
            var cat = Popup(c, "ShopPopup", "SHOP", new Vector2(pad * 2f + (nm + 1) * cardSize.x + nm * gap, 92f + cardSize.y + 22f), false);
            hud.categoryPopup = cat;
            hud.materialCards = new BPCard[nm];
            for (int i = 0; i < nm; i++)
            {
                var m = build.materials[i];
                hud.materialCards[i] = Card(cat, "Mat_" + m.displayName, new Vector2(pad + cardSize.x * 0.5f + i * (cardSize.x + gap), 22f), cardSize, matIcons[i], m.displayName.ToUpper(), (i + 1).ToString());
            }
            hud.personCard = Card(cat, "Person", new Vector2(pad + cardSize.x * 0.5f + nm * (cardSize.x + gap), 22f), cardSize, theme.person, "PERSON", "TAB");
            hud.personCard.icon.color = theme.ink;
            Box(hud.personCard.icon.rectTransform, new Vector2(30f, 78f), new Vector2(-30f, -22f));

            // ---------------- popup 2: sizes of the chosen material
            int ns = build.shapes.Length;
            Vector2 sc = new Vector2(112f, 150f);
            float sgap = 12f;
            var sizeBox = Popup(c, "SizePopup", "PICK A SIZE", new Vector2(pad * 2f + ns * sc.x + (ns - 1) * sgap, 92f + sc.y + 22f), true);
            hud.sizePopup = sizeBox;
            hud.sizeTitle = sizeBox.Find("Title").GetComponent<TextMeshProUGUI>();
            hud.backButton = sizeBox.Find("Back").GetComponent<Button>();
            hud.shapeCards = new BPCard[ns];
            for (int i = 0; i < ns; i++)
            {
                var sd = build.shapes[i];
                string label = sd.kind == ShapeKind.Slope ? "SLOPE" : sd.kind == ShapeKind.Round ? "ROUND" :
                               sd.IsPlate ? (sd.StudCount > 8 ? "BIG PLATE" : "PLATE") : $"{sd.studs.y}X{sd.studs.x}";
                hud.shapeCards[i] = Card(sizeBox, "Shape_" + sd.name, new Vector2(pad + sc.x * 0.5f + i * (sc.x + sgap), 22f), sc, shapeIcons[i], label, "");
            }

            // ---------------- help: "?" button + controls popup (bottom right)
            hud.helpButton = Btn(c, "HelpButton", theme.blue, "?", 34f, false);
            Place((RectTransform)hud.helpButton.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 28f), new Vector2(72f, 72f));
            Box(hud.helpButton.GetComponent<BPButton>().visual.Find("Label") as RectTransform, new Vector2(12f, 16f), new Vector2(-12f, -10f));
            var help = Img(c, "HelpPopup", theme.panel, dark, true, true).rectTransform;
            Place(help, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 116f), new Vector2(560f, 372f));
            help.gameObject.AddComponent<CanvasGroup>();
            hud.helpPopup = help;
            var ht = Txt(help, "Title", true, true, 30f, theme.yellow, TextAlignmentOptions.MidlineLeft);
            Box(ht.rectTransform, new Vector2(28f, 312f), new Vector2(-28f, -16f));
            ht.text = "CONTROLS";
            var hb = Txt(help, "Body", false, false, 22f, theme.cream, TextAlignmentOptions.TopLeft);
            Box(hb.rectTransform, new Vector2(28f, 22f), new Vector2(-28f, -72f));
            hb.enableAutoSizing = false;
            hb.textWrappingMode = TextWrappingModes.Normal;
            hb.lineSpacing = 12f;
            hb.text = HelpText();

            BuildToolPanel(hud);

            // ---------------- banner
            hud.banner = Txt(c, "Banner", true, true, 120f, theme.yellow, TextAlignmentOptions.Center);
            Place(hud.banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1500f, 200f));
            hud.banner.text = "";

            // ---------------- round result
            var res = Img(c, "RoundResult", theme.panel, theme.cream, true, true).rectTransform;
            Place(res, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(600f, 520f));
            hud.resultRoot = res;
            var tro = Img(res, "Trophy", theme.trophy, theme.yellow, false);
            tro.preserveAspect = true;
            Place(tro.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(96f, 96f));
            Shadowed(tro);
            hud.resultTitle = Txt(res, "Title", true, true, 38f, theme.yellow, TextAlignmentOptions.Center);
            Place(hud.resultTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(480f, 60f));
            hud.resultTitle.text = "ROUND 1";
            hud.resultSubtitle = Txt(res, "Subtitle", true, true, 20f, theme.green, TextAlignmentOptions.Center);
            Place(hud.resultSubtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -106f), new Vector2(400f, 34f));
            hud.resultSubtitle.text = "SURVIVED!";
            hud.resultSurvivors = ResultLine(res, "Survivors", -166f, 32f, 46f);
            hud.resultDead = ResultLine(res, "Dead", -212f, 32f, 46f);
            hud.resultRubble = ResultLine(res, "Rubble", -254f, 26f, 38f);
            hud.resultReward = ResultLine(res, "Reward", -326f, 38f, 80f);
            hud.continueButton = Btn(res, "ContinueButton", theme.green, "CONTINUE", 34f, true);
            Place((RectTransform)hud.continueButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(280f, 78f));

            // ---------------- game over
            var go = Img(c, "GameOver", theme.panel, new Color(1f, 0.9f, 0.88f), true, true).rectTransform;
            Place(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(600f, 440f));
            hud.gameOverRoot = go;
            var sk = Img(go, "Skull", theme.dead, theme.red, false);
            sk.preserveAspect = true;
            Place(sk.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(96f, 96f));
            Shadowed(sk);
            hud.gameOverTitle = Txt(go, "Title", true, true, 56f, theme.red, TextAlignmentOptions.Center);
            Place(hud.gameOverTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(480f, 84f));
            hud.gameOverTitle.text = "GAME OVER";
            hud.gameOverStats = Txt(go, "Stats", false, false, 30f, theme.ink, TextAlignmentOptions.Center);
            Place(hud.gameOverStats.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(500f, 140f));
            hud.gameOverStats.textWrappingMode = TextWrappingModes.Normal;
            hud.restartButton = Btn(go, "RestartButton", theme.blue, "RESTART", 34f, true);
            Place((RectTransform)hud.restartButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(280f, 78f));

            // ---------------- tooltip (last, so it draws on top)
            var tipGo = new GameObject("Tooltip", typeof(RectTransform));
            tipGo.transform.SetParent(c, false);
            Stretch((RectTransform)tipGo.transform, Vector2.zero, Vector2.zero);
            var tip = tipGo.AddComponent<BPTooltip>();
            var card = Img(tipGo.transform, "Card", theme.panel, new Color(0.13f, 0.1f, 0.17f, 0.96f), true).rectTransform;
            card.anchorMin = card.anchorMax = Vector2.zero;
            card.pivot = Vector2.zero;
            card.sizeDelta = new Vector2(360f, 200f);
            tip.card = card;
            tip.titleText = Txt(card, "Title", true, true, 28f, theme.yellow, TextAlignmentOptions.TopLeft);
            var tr = tip.titleText.rectTransform;
            tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f); tr.pivot = new Vector2(0f, 1f);
            tr.offsetMin = new Vector2(18f, -60f); tr.offsetMax = new Vector2(-18f, -18f);
            tip.bodyText = Txt(card, "Body", false, false, 23f, theme.cream, TextAlignmentOptions.TopLeft);
            tip.bodyText.textWrappingMode = TextWrappingModes.Normal;
            tip.bodyText.enableAutoSizing = false;
            tip.titleText.enableAutoSizing = false;
            tip.bodyText.richText = true;
            Stretch(tip.bodyText.rectTransform, new Vector2(18f, 14f), new Vector2(-18f, -64f));

            round.showDebugHud = false;
            EditorUtility.SetDirty(round);
            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
            Debug.Log("[BrickPanic] HUD built into the scene.");
        }

        // ================================================================ tool panel (build / erase)

        [MenuItem("Brick Panic/Add Tool Panel (keeps your HUD edits)")]
        public static void AddToolPanelMenu()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            var hud = Object.FindFirstObjectByType<BrickPanicHud>();
            if (hud == null) { Debug.LogError("[BrickPanic] No HUD in the scene."); return; }
            theme = hud.theme != null ? hud.theme : EnsureTheme();
            ImportSprites();
            Undo.RecordObject(hud, "Tool panel");
            BuildToolPanel(hud);
            var body = hud.transform.Find("HelpPopup/Body");
            if (body != null) { var t = body.GetComponent<TextMeshProUGUI>(); Undo.RecordObject(t, "Help text"); t.text = HelpText(); }
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            EditorSceneManager.SaveScene(hud.gameObject.scene);
            Debug.Log("[BrickPanic] tool panel added (the rest of the HUD is untouched).");
        }

        /// <summary>Middle right: a dark panel with two square buttons under each other, hammer (build) and eraser (erase).
        /// Replaces an old ToolPanel; touches nothing else.</summary>
        public static void BuildToolPanel(BrickPanicHud hud)
        {
            var c = hud.transform;
            var old = c.Find("ToolPanel");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var panel = Img(c, "ToolPanel", theme.panel, new Color(0.13f, 0.1f, 0.17f, 0.92f), true, true).rectTransform;
            Place(panel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-32f, 0f), new Vector2(104f, 200f));
            int idx = c.Find("Tooltip") != null ? c.Find("Tooltip").GetSiblingIndex() : c.childCount - 1;
            panel.SetSiblingIndex(Mathf.Max(0, idx));
            hud.toolPanel = panel;
            hud.buildToolButton = ToolButton(panel, "BuildTool", 0, AssetDatabase.LoadAssetAtPath<Sprite>(UiDir + "/Icons/tool_build.png"), "V",
                "BUILD", "Click: place one brick.\nDrag: draw a line of bricks.\nDraw over a line to stack the next row.");
            hud.eraseToolButton = ToolButton(panel, "EraseTool", 1, AssetDatabase.LoadAssetAtPath<Sprite>(UiDir + "/Icons/tool_erase.png"), "C",
                "ERASE", "Click: remove one brick or person.\nDrag: remove everything in the area.\nBricks give back half their price, people the full price.");
        }

        static Button ToolButton(RectTransform panel, string name, int row, Sprite icon, string key, string title, string tip)
        {
            var b = Btn(panel, name, Color.white, "", 20f, false);
            var r = (RectTransform)b.transform;
            Place(r, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f - row * 88f), new Vector2(76f, 76f));
            var vis = b.GetComponent<BPButton>().visual;
            vis.GetComponent<Image>().color = row == 0 ? new Color(0.3f, 0.85f, 0.5f) : new Color(0.32f, 0.3f, 0.38f);
            var i = Img(vis, "Icon", icon, Color.white, false);
            i.preserveAspect = true;
            Box(i.rectTransform, new Vector2(14f, 18f), new Vector2(-14f, -10f));
            var kt = Badge(vis, key);
            kt.transform.parent.SetAsLastSibling();
            var tipComp = b.gameObject.AddComponent<BPHoverTip>();
            tipComp.title = title;
            tipComp.body = tip;
            return b;
        }

        static string HelpText()
        {
            string k(string key) => $"<color=#{ColorUtility.ToHtmlStringRGB(theme.yellow)}>{key}</color>";
            return $"{k("V")}  build tool        {k("C")}  erase tool\n" +
                   $"{k("DRAG")}  draw a line / erase an area        {k("R")}  rotate\n" +
                   $"{k("MIDDLE CLICK")}  move a brick        {k("CTRL+Z")}  undo\n" +
                   $"{k("1-4")}  material        {k("Z / X")}  size        {k("TAB")}  person\n" +
                   $"{k("B")}  shop        {k("HOLD RIGHT + WASD")}  fly        {k("Q / E")}  down / up\n" +
                   $"{k("ENTER")}  start        {k("F5")}  new island (round 1 only)";
        }

        // ================================================================ parts

        static RectTransform StatPill(Transform parent, string name, int row, Color badge, Sprite icon, out TextMeshProUGUI text)
        {
            var p = Img(parent, name, theme.panel, new Color(0.13f, 0.1f, 0.17f, 0.92f), true, true).rectTransform;
            Place(p, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -26f - row * 74f), new Vector2(290f, 62f));
            var b = Img(p, "Badge", theme.pill, badge, true);
            Place(b.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(48f, 46f));
            var i = Img(b.rectTransform, "Icon", icon, Color.white, false);
            i.preserveAspect = true;
            Box(i.rectTransform, new Vector2(10f, 9f), new Vector2(-10f, -9f));
            text = Txt(p, "Value", false, true, 28f, theme.cream, TextAlignmentOptions.MidlineLeft);
            Box(text.rectTransform, new Vector2(72f, 10f), new Vector2(-18f, -10f));
            return p;
        }

        /// <summary>A dark popup above the SHOP button: title at the top left, a close (or back) button at the top right.</summary>
        static RectTransform Popup(Transform parent, string name, string title, Vector2 size, bool back)
        {
            var p = Img(parent, name, theme.panel, new Color(0.13f, 0.1f, 0.17f, 0.94f), true, true).rectTransform;
            Place(p, Vector2.zero, Vector2.zero, new Vector2(32f, 186f), size);
            p.gameObject.AddComponent<CanvasGroup>();
            var t = Txt(p, "Title", true, true, 30f, theme.yellow, TextAlignmentOptions.MidlineLeft);
            Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(back ? 90f : 28f, -20f), new Vector2(size.x - 130f, 48f));
            t.text = title;
            if (back)
            {
                var bb = Btn(p, "Back", theme.blue, "<", 28f, false);
                Place((RectTransform)bb.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -18f), new Vector2(54f, 54f));
                var bv = bb.GetComponent<BPButton>().visual;
                bv.Find("Label").GetComponent<TextMeshProUGUI>().text = "";
                var arrow = Img(bv, "Icon", theme.restart, Color.white, false);
                arrow.preserveAspect = true;
                Box(arrow.rectTransform, new Vector2(12f, 14f), new Vector2(-12f, -8f));
            }
            return p;
        }

        static BPCard Card(Transform parent, string name, Vector2 bottomCentre, Vector2 size, Sprite icon, string label, string key)
        {
            bool small = size.x < 130f;
            var root = Img(parent, name, null, new Color(1f, 1f, 1f, 0f), false, true).rectTransform;   // invisible hit area
            Place(root, new Vector2(0f, 0f), new Vector2(0.5f, 0f), bottomCentre, size);
            var card = root.gameObject.AddComponent<BPCard>();
            var vis = new GameObject("Visual", typeof(RectTransform)).GetComponent<RectTransform>();
            vis.SetParent(root, false);
            Stretch(vis, Vector2.zero, Vector2.zero);
            card.visual = vis;
            card.glow = Img(vis, "Glow", theme.glow, theme.yellow, true);
            Stretch(card.glow.rectTransform, new Vector2(-22f, -22f), new Vector2(22f, 22f));
            card.face = Img(vis, "Face", theme.panel, theme.cream, true);
            Stretch(card.face.rectTransform, Vector2.zero, Vector2.zero);
            // picture: the top part, with a margin; name and price: two rows at the bottom, each with side margins
            float nameH = small ? 24f : 30f, priceH = small ? 26f : 30f, m = small ? 8f : 12f;
            card.icon = Img(vis, "Icon", icon, Color.white, false);
            card.icon.preserveAspect = true;
            Box(card.icon.rectTransform, new Vector2(m, m + priceH + nameH + 4f), new Vector2(-m, -m));
            card.nameText = Txt(vis, "Name", true, false, small ? 18f : 22f, theme.ink, TextAlignmentOptions.Center);
            Box(card.nameText.rectTransform, new Vector2(m, m + priceH), new Vector2(-m, -(size.y - m - priceH - nameH)));
            card.nameText.text = label;
            card.priceText = Txt(vis, "Price", false, false, small ? 21f : 23f, new Color(0.86f, 0.36f, 0.05f), TextAlignmentOptions.Center);
            Box(card.priceText.rectTransform, new Vector2(m, m), new Vector2(-m, -(size.y - m - priceH)));
            card.priceText.text = "$0";
            if (!string.IsNullOrEmpty(key)) card.keyText = Badge(vis, key);
            return card;
        }

        /// <summary>Small dark key badge on the top-left corner (keyboard shortcut).</summary>
        static TextMeshProUGUI Badge(RectTransform parent, string key)
        {
            var kb = Img(parent, "Key", theme.pill, theme.ink, true);
            Place(kb.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(10f, -8f), new Vector2(key.Length > 1 ? 54f : 36f, 30f));
            var t = Txt(kb.rectTransform, "Text", false, false, 16f, theme.cream, TextAlignmentOptions.Center);
            Box(t.rectTransform, new Vector2(6f, 5f), new Vector2(-6f, -5f));
            t.text = key;
            return t;
        }

        static TextMeshProUGUI ResultLine(RectTransform parent, string name, float y, float size, float height)
        {
            var t = Txt(parent, name, false, false, size, theme.ink, TextAlignmentOptions.Center);
            Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(500f, height));
            t.richText = true;
            return t;
        }

        static void Shadowed(Image img)
        {
            var s = img.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.8f);
            s.effectDistance = new Vector2(4f, -4f);
        }

        /// <summary>Chunky button (Poker TD style). The label fills the face minus a margin (more at the bottom: the
        /// sprite has a lip) and auto-sizes up to fontSize, so it never touches the edge.</summary>
        static Button Btn(Transform parent, string name, Color color, string label, float fontSize, bool breathe)
        {
            var root = Img(parent, name, null, new Color(1f, 1f, 1f, 0f), false, true);
            var b = root.gameObject.AddComponent<Button>();
            var vis = Img(root.rectTransform, "Visual", theme.button, Color.white, true);
            Stretch(vis.rectTransform, Vector2.zero, Vector2.zero);
            b.targetGraphic = vis;
            var colors = b.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.15f);
            colors.selectedColor = color;
            colors.disabledColor = Color.Lerp(color, Color.gray, 0.7f);
            b.colors = colors;
            var t = Txt(vis.rectTransform, "Label", true, true, fontSize, Color.white, TextAlignmentOptions.Center);
            Box(t.rectTransform, new Vector2(16f, 16f), new Vector2(-16f, -10f));
            t.text = label;
            var pb = root.gameObject.AddComponent<BPButton>();
            pb.visual = vis.rectTransform;
            pb.breathe = breathe;
            return b;
        }

        // ================================================================ helpers

        static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = pivot;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        /// <summary>Fills the parent minus margins: min = left/bottom margin, max = minus right/top margin.</summary>
        static void Box(RectTransform r, Vector2 min, Vector2 max) => Stretch(r, min, max);

        static void Stretch(RectTransform r, Vector2 min, Vector2 max)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = min; r.offsetMax = max;
        }

        static Image Img(Transform parent, string name, Sprite sprite, Color color, bool sliced, bool raycast = false)
        {
            var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.rectTransform.SetParent(parent, false);
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sliced && sprite != null) { img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1.6f; }
            return img;
        }

        /// <summary>A label that auto-sizes into its box (max = size). title = Action Comics (upper case), else Super Chips. outline = shared dark outline material
        /// (for text over the 3D world); without it the plain font material (for dark text on cream cards).</summary>
        static TextMeshProUGUI Txt(Transform parent, string name, bool title, bool outline, float size, Color color, TextAlignmentOptions align)
        {
            var t = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            t.rectTransform.SetParent(parent, false);
            t.font = title ? theme.title : theme.number;
            if (outline) { var m = title ? theme.titleOutline : theme.numberOutline; if (m != null) t.fontSharedMaterial = m; }
            t.fontSize = size;
            t.enableAutoSizing = true;          // shrinks to fit its box, never grows past size
            t.fontSizeMax = size;
            t.fontSizeMin = Mathf.Min(10f, size);
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            if (title) t.fontStyle = FontStyles.UpperCase;
            return t;
        }

        // ================================================================ assets

        static void ImportSprites()
        {
            void Set(string path, int border)
            {
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) return;
                bool change = imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single || imp.spriteBorder != new Vector4(border, border, border, border) || imp.mipmapEnabled;
                if (!change) return;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spriteBorder = new Vector4(border, border, border, border);
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            AssetDatabase.Refresh();
            Set(UiDir + "/Panel9.png", 28);
            Set(UiDir + "/Button9.png", 28);
            Set(UiDir + "/Pill9.png", 20);
            Set(UiDir + "/Glow9.png", 44);
            Set(UiDir + "/Fill9.png", 18);
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { UiDir + "/Icons" }))
                Set(AssetDatabase.GUIDToAssetPath(guid), 0);
        }

        static BPTheme EnsureTheme()
        {
            var t = AssetDatabase.LoadAssetAtPath<BPTheme>(ThemePath);
            if (t == null) { t = ScriptableObject.CreateInstance<BPTheme>(); AssetDatabase.CreateAsset(t, ThemePath); }
            T Load<T>(string p) where T : Object => AssetDatabase.LoadAssetAtPath<T>(p);
            Sprite Icon(string n) => Load<Sprite>(UiDir + "/Icons/" + n + ".png");
            if (t.title == null) t.title = Load<TMP_FontAsset>(FontDir + "/Action Comics Black SDF.asset");
            if (t.titleOutline == null) t.titleOutline = Load<Material>(FontDir + "/Action Comics Black SDF Outline.mat");
            if (t.number == null) t.number = Load<TMP_FontAsset>(FontDir + "/Super Chips SDF.asset");
            if (t.numberOutline == null) t.numberOutline = Load<Material>(FontDir + "/Super Chips SDF Outline.mat");
            if (t.panel == null) t.panel = Load<Sprite>(UiDir + "/Panel9.png");
            if (t.pill == null) t.pill = Load<Sprite>(UiDir + "/Pill9.png");
            if (t.button == null) t.button = Load<Sprite>(UiDir + "/Button9.png");
            if (t.glow == null) t.glow = Load<Sprite>(UiDir + "/Glow9.png");
            if (t.fill == null) t.fill = Load<Sprite>(UiDir + "/Fill9.png");
            if (t.money == null) t.money = Icon("dollar");
            if (t.person == null) t.person = Icon("character");
            if (t.goal == null) t.goal = Icon("flag_triangle");
            if (t.dead == null) t.dead = Icon("skull");
            if (t.wear == null) t.wear = Icon("wrench");
            if (t.people == null) t.people = Icon("pawns");
            if (t.rubble == null) t.rubble = Icon("structure_wall");
            if (t.trophy == null) t.trophy = Icon("trophy");
            if (t.warning == null) t.warning = Icon("warning");
            if (t.restart == null) t.restart = Icon("return");
            if (t.quake == null) t.quake = Icon("disaster_quake");
            if (t.tsunami == null) t.tsunami = Icon("disaster_tsunami");
            if (t.volcano == null) t.volcano = Icon("disaster_volcano");
            EditorUtility.SetDirty(t);
            AssetDatabase.SaveAssets();
            return t;
        }

        /// <summary>Renders one brick mesh (3/4 view, transparent background) to Art/UI/Renders/&lt;name&gt;.png and returns the sprite.</summary>
        static Sprite RenderIcon(Mesh mesh, Material mat, string name)
        {
            if (mesh == null || mat == null) return null;
            Directory.CreateDirectory(RenderDir);
            const int size = 256;
            const int layer = 31;
            var holder = new GameObject("IconRender") { hideFlags = HideFlags.HideAndDontSave };
            var obj = new GameObject("Mesh", typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.HideAndDontSave, layer = layer };
            obj.transform.SetParent(holder.transform, false);
            obj.transform.position = new Vector3(0f, -500f, 0f);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var b = obj.GetComponent<MeshRenderer>().bounds;
            var camGo = new GameObject("Cam", typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            camGo.transform.SetParent(holder.transform, false);
            var cam = camGo.GetComponent<Camera>();
            cam.cullingMask = 1 << layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.allowHDR = false;
            cam.allowMSAA = true;
            cam.fieldOfView = 22f;
            Vector3 dir = new Vector3(-1f, 0.95f, -1.25f).normalized;
            float dist = b.extents.magnitude / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.02f;
            cam.transform.position = b.center + dir * dist;
            cam.transform.LookAt(b.center);
            cam.nearClipPlane = 0.05f; cam.farClipPlane = dist * 3f;
            var data = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            var lightGo = new GameObject("Light", typeof(Light)) { hideFlags = HideFlags.HideAndDontSave };
            lightGo.transform.SetParent(holder.transform, false);
            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.cullingMask = 1 << layer;
            lightGo.transform.rotation = Quaternion.Euler(45f, 30f, 0f);

            var rt = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            var req = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
            else { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            string path = RenderDir + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(holder);

            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single || imp.mipmapEnabled)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
