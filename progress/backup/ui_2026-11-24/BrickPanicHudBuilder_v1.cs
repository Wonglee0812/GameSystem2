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
            var neutral = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            neutral.SetColor("_BaseColor", new Color(0.66f, 0.67f, 0.72f));
            neutral.SetFloat("_Smoothness", 0.4f);
            for (int i = 0; i < build.shapes.Length; i++)
                shapeIcons[i] = RenderIcon(build.shapes[i].mesh, neutral, "Shape_" + build.shapes[i].name);
            Object.DestroyImmediate(neutral);

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

            // ---------------- top left
            hud.roundTitle = Txt(c, "RoundTitle", true, true, 64f, theme.yellow, TextAlignmentOptions.TopLeft);
            Place(hud.roundTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(700f, 90f));
            hud.roundTitle.text = "ROUND 1";
            hud.goalText = Txt(c, "Goal", false, true, 34f, theme.cream, TextAlignmentOptions.TopLeft);
            Place(hud.goalText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -130f), new Vector2(700f, 50f));
            hud.goalText.text = "GOAL: 5 SURVIVORS";

            // ---------------- stats top right
            hud.moneyPill = StatPill(c, "Money", 0, theme.yellow, theme.money, out hud.moneyText);
            hud.moneyText.text = "$100";
            hud.alivePill = StatPill(c, "Alive", 1, theme.green, theme.people, out hud.aliveText);
            hud.aliveText.text = "0 / 5";
            hud.wearPill = StatPill(c, "Wear", 2, theme.orange, theme.wear, out hud.wearText);
            hud.wearText.fontSize = 24f;
            hud.wearText.alignment = TextAlignmentOptions.TopLeft;
            hud.wearText.rectTransform.offsetMax = new Vector2(-16f, -8f);
            hud.wearText.text = "WEAR 0%";
            var barBg = Img(hud.wearPill, "BarBack", theme.fill, new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.85f), true);
            Stretch(barBg.rectTransform, new Vector2(82f, 12f), new Vector2(-16f, -42f));
            var barFill = Img(barBg.rectTransform, "Fill", theme.fill, theme.green, true);
            barFill.rectTransform.anchorMin = Vector2.zero;
            barFill.rectTransform.anchorMax = new Vector2(0.04f, 1f);
            barFill.rectTransform.offsetMin = new Vector2(2f, 2f);
            barFill.rectTransform.offsetMax = new Vector2(-2f, -2f);
            hud.wearFill = barFill.rectTransform;

            // ---------------- forecast + START (build phase)
            var fc = Img(c, "Forecast", theme.panel, theme.cream, true, true).rectTransform;
            Place(fc, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(460f, 200f));
            hud.forecastRoot = fc;
            var head = Txt(fc, "Header", false, false, 22f, new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.7f), TextAlignmentOptions.Top);
            Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(400f, 30f));
            head.text = "NEXT DISASTER";
            hud.forecastIcon = Img(fc, "Icon", theme.tsunami, theme.tsunamiColor, false);
            Place(hud.forecastIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, -10f), new Vector2(118f, 118f));
            hud.forecastName = Txt(fc, "Name", true, true, 52f, theme.tsunamiColor, TextAlignmentOptions.Left);
            Place(hud.forecastName.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(160f, 6f), new Vector2(285f, 70f));
            hud.forecastName.enableAutoSizing = true; hud.forecastName.fontSizeMin = 20f; hud.forecastName.fontSizeMax = 52f;
            hud.forecastName.text = "TSUNAMI";
            hud.forecastDetail = Txt(fc, "Detail", false, false, 24f, theme.ink, TextAlignmentOptions.TopLeft);
            Place(hud.forecastDetail.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(162f, -56f), new Vector2(285f, 60f));
            hud.forecastDetail.textWrappingMode = TextWrappingModes.Normal;
            hud.forecastDetail.text = "FROM THE EAST  -  LEVEL 1";
            hud.startButton = Btn(fc, "StartButton", theme.orange, "START!", 50f, new Vector2(300f, 88f), true);
            Place((RectTransform)hud.startButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(300f, 88f));
            var enter = Txt(hud.startButton.transform, "Key", false, true, 18f, theme.cream, TextAlignmentOptions.Top);
            Place(enter.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(200f, 26f));
            enter.text = "ENTER";
            hud.peopleWarning = Txt(fc, "PeopleWarning", false, true, 28f, theme.red, TextAlignmentOptions.Top);
            Place(hud.peopleWarning.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(500f, 40f));
            hud.peopleWarning.text = "ONLY 0 / 5 PEOPLE!";
            hud.newMapHint = Txt(fc, "NewMapHint", false, true, 22f, theme.cream, TextAlignmentOptions.Top);
            Place(hud.newMapHint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(500f, 34f));
            hud.newMapHint.text = "F5: NEW ISLAND";

            // ---------------- live counter (disaster)
            var live = Img(c, "Live", theme.panel, new Color(0.13f, 0.1f, 0.17f, 0.9f), true).rectTransform;
            Place(live, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(640f, 96f));
            hud.liveRoot = live;
            hud.liveIcon = Img(live, "Icon", theme.tsunami, theme.tsunamiColor, false);
            Place(hud.liveIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(70f, 70f));
            hud.liveName = Txt(live, "Name", true, true, 36f, theme.tsunamiColor, TextAlignmentOptions.BottomLeft);
            Place(hud.liveName.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(100f, -6f), new Vector2(300f, 46f));
            hud.liveName.enableAutoSizing = true; hud.liveName.fontSizeMin = 14f; hud.liveName.fontSizeMax = 36f;
            hud.liveDetail = Txt(live, "Detail", false, true, 20f, theme.cream, TextAlignmentOptions.TopLeft);
            Place(hud.liveDetail.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(102f, -8f), new Vector2(300f, 30f));
            var ai = Img(live, "AliveIcon", theme.people, theme.green, false);
            Place(ai.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(412f, 0f), new Vector2(48f, 48f));
            hud.liveAlive = Txt(live, "Alive", false, true, 40f, theme.green, TextAlignmentOptions.Left);
            Place(hud.liveAlive.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(464f, 0f), new Vector2(70f, 60f));
            var di = Img(live, "DeadIcon", theme.dead, theme.red, false);
            Place(di.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(530f, 0f), new Vector2(44f, 44f));
            hud.liveDead = Txt(live, "Dead", false, true, 40f, theme.red, TextAlignmentOptions.Left);
            Place(hud.liveDead.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(580f, 0f), new Vector2(60f, 60f));

            // ---------------- hotbar
            var bar = Img(c, "Hotbar", theme.panel, theme.zone, true, true).rectTransform;
            Place(bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(1640f, 262f));
            hud.hotbarRoot = bar;
            float x = 30f;
            SectionLabel(bar, "MATERIALS", "1-4", x);
            hud.materialCards = new BPCard[build.materials.Length];
            for (int i = 0; i < build.materials.Length; i++)
            {
                var m = build.materials[i];
                hud.materialCards[i] = Card(bar, "Mat_" + m.displayName, new Vector2(x + 64f, 16f), new Vector2(128f, 168f), matIcons[i], m.displayName.ToUpper(), (i + 1).ToString(), false);
                x += 140f;
            }
            Divider(bar, x + 6f);
            x += 30f;
            SectionLabel(bar, "SHAPES", "Z / X", x);
            hud.shapeCards = new BPCard[build.shapes.Length];
            for (int i = 0; i < build.shapes.Length; i++)
            {
                var s = build.shapes[i];
                string label = s.kind == ShapeKind.Slope ? "SLOPE" : s.kind == ShapeKind.Round ? "ROUND" :
                               s.IsPlate ? (s.StudCount > 8 ? "BIG PLATE" : "PLATE") : $"{s.studs.y}X{s.studs.x}";
                hud.shapeCards[i] = Card(bar, "Shape_" + s.name, new Vector2(x + 46f, 16f), new Vector2(92f, 124f), shapeIcons[i], label, "", true);
                x += 102f;
            }
            Divider(bar, x + 6f);
            x += 30f;
            SectionLabel(bar, "PEOPLE", "TAB", x);
            hud.personCard = Card(bar, "Person", new Vector2(x + 64f, 16f), new Vector2(128f, 168f), theme.person, "PERSON", "TAB", false);
            hud.personCard.icon.color = theme.ink;
            hud.personCard.icon.rectTransform.sizeDelta *= 0.75f;
            hud.controlsText = Txt(bar, "Controls", false, true, 21f, theme.cream, TextAlignmentOptions.BottomLeft);
            Place(hud.controlsText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(20f, 8f), new Vector2(1600f, 34f));
            hud.controlsText.text = "LEFT CLICK  place    RIGHT CLICK  sell    MIDDLE CLICK  move    R  rotate    CTRL+Z  undo    HOLD RIGHT + WASD  fly";

            // ---------------- banner
            hud.banner = Txt(c, "Banner", true, true, 120f, theme.yellow, TextAlignmentOptions.Center);
            Place(hud.banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1700f, 220f));
            hud.banner.text = "";

            // ---------------- round result
            var res = Img(c, "RoundResult", theme.panel, theme.cream, true, true).rectTransform;
            Place(res, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(640f, 560f));
            hud.resultRoot = res;
            var tro = Img(res, "Trophy", theme.trophy, theme.yellow, false);
            Place(tro.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(110f, 110f));
            Shadowed(tro);
            hud.resultTitle = Txt(res, "Title", true, true, 44f, theme.yellow, TextAlignmentOptions.Center);
            hud.resultTitle.enableAutoSizing = true; hud.resultTitle.fontSizeMin = 20f; hud.resultTitle.fontSizeMax = 44f;
            Place(hud.resultTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(560f, 80f));
            hud.resultTitle.text = "ROUND 1 SURVIVED!";
            hud.resultSurvivors = ResultLine(res, "Survivors", -160f, 40f);
            hud.resultDead = ResultLine(res, "Dead", -222f, 40f);
            hud.resultRubble = ResultLine(res, "Rubble", -284f, 34f);
            hud.resultReward = ResultLine(res, "Reward", -370f, 52f);
            hud.resultReward.rectTransform.sizeDelta = new Vector2(600f, 110f);
            hud.continueButton = Btn(res, "ContinueButton", theme.green, "CONTINUE", 44f, new Vector2(320f, 88f), true);
            Place((RectTransform)hud.continueButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(320f, 88f));

            // ---------------- game over
            var go = Img(c, "GameOver", theme.panel, new Color(1f, 0.9f, 0.88f), true, true).rectTransform;
            Place(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(640f, 480f));
            hud.gameOverRoot = go;
            var sk = Img(go, "Skull", theme.dead, theme.red, false);
            Place(sk.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(110f, 110f));
            Shadowed(sk);
            hud.gameOverTitle = Txt(go, "Title", true, true, 66f, theme.red, TextAlignmentOptions.Center);
            Place(hud.gameOverTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(600f, 110f));
            hud.gameOverTitle.text = "GAME OVER";
            hud.gameOverStats = Txt(go, "Stats", false, false, 36f, theme.ink, TextAlignmentOptions.Center);
            Place(hud.gameOverStats.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(600f, 150f));
            hud.gameOverStats.textWrappingMode = TextWrappingModes.Normal;
            hud.restartButton = Btn(go, "RestartButton", theme.blue, "RESTART", 44f, new Vector2(320f, 88f), true);
            Place((RectTransform)hud.restartButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(320f, 88f));

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
            tip.bodyText.richText = true;
            Stretch(tip.bodyText.rectTransform, new Vector2(18f, 14f), new Vector2(-18f, -64f));

            round.showDebugHud = false;
            EditorUtility.SetDirty(round);
            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
            Debug.Log("[BrickPanic] HUD built into the scene.");
        }

        // ================================================================ parts

        static RectTransform StatPill(Transform parent, string name, int row, Color badge, Sprite icon, out TextMeshProUGUI text)
        {
            var p = Img(parent, name, theme.panel, new Color(0.13f, 0.1f, 0.17f, 0.9f), true, true).rectTransform;
            Place(p, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -24f - row * 84f), new Vector2(320f, 72f));
            var b = Img(p, "Badge", theme.pill, badge, true);
            Place(b.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(60f, 56f));
            var i = Img(b.rectTransform, "Icon", icon, Color.white, false);
            Place(i.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f));
            text = Txt(p, "Value", false, true, 38f, theme.cream, TextAlignmentOptions.Left);
            Stretch(text.rectTransform, new Vector2(84f, 4f), new Vector2(-16f, -4f));
            return p;
        }

        static BPCard Card(Transform parent, string name, Vector2 bottomCentre, Vector2 size, Sprite icon, string label, string key, bool small)
        {
            var root = Img(parent, name, null, new Color(1f, 1f, 1f, 0f), false, true).rectTransform;   // invisible hit area
            Place(root, new Vector2(0f, 0f), new Vector2(0.5f, 0f), bottomCentre, size);
            var card = root.gameObject.AddComponent<BPCard>();
            var vis = new GameObject("Visual", typeof(RectTransform)).GetComponent<RectTransform>();
            vis.SetParent(root, false);
            Stretch(vis, Vector2.zero, Vector2.zero);
            card.visual = vis;
            card.glow = Img(vis, "Glow", theme.glow, theme.yellow, true);
            Stretch(card.glow.rectTransform, new Vector2(-24f, -24f), new Vector2(24f, 24f));
            card.face = Img(vis, "Face", theme.panel, theme.cream, true);
            Stretch(card.face.rectTransform, Vector2.zero, Vector2.zero);
            float pic = small ? size.x - 14f : size.x - 18f;
            card.icon = Img(vis, "Icon", icon, Color.white, false);
            card.icon.preserveAspect = true;
            Place(card.icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, small ? -4f : -8f), new Vector2(pic, pic));
            card.nameText = Txt(vis, "Name", true, false, small ? 17f : 22f, theme.ink, TextAlignmentOptions.Center);
            Place(card.nameText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, small ? 30f : 40f), new Vector2(size.x - 10f, 28f));
            card.nameText.enableAutoSizing = true; card.nameText.fontSizeMin = 11f; card.nameText.fontSizeMax = small ? 17f : 22f;
            card.nameText.text = label;
            card.priceText = Txt(vis, "Price", false, true, small ? 22f : 28f, new Color(0.95f, 0.45f, 0.1f), TextAlignmentOptions.Center);
            Place(card.priceText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, small ? 4f : 8f), new Vector2(size.x - 10f, 30f));
            card.priceText.text = "$0";
            if (!string.IsNullOrEmpty(key))
            {
                var kb = Img(vis, "Key", theme.pill, theme.ink, true);
                Place(kb.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(8f, -6f), new Vector2(key.Length > 1 ? 58f : 40f, 34f));
                card.keyText = Txt(kb.rectTransform, "Text", false, false, 20f, theme.cream, TextAlignmentOptions.Center);
                Stretch(card.keyText.rectTransform, Vector2.zero, Vector2.zero);
                card.keyText.text = key;
            }
            return card;
        }

        static void SectionLabel(RectTransform bar, string title, string keys, float x)
        {
            var t = Txt(bar, title + "Label", true, true, 26f, theme.yellow, TextAlignmentOptions.TopLeft);
            Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -10f), new Vector2(400f, 40f));
            t.text = title + $"  <size=70%><color=#{ColorUtility.ToHtmlStringRGB(theme.cream)}>{keys}</color></size>";
        }

        static void Divider(RectTransform bar, float x)
        {
            var d = Img(bar, "Divider", theme.fill, new Color(1f, 1f, 1f, 0.18f), true);
            Place(d.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -6f), new Vector2(4f, 180f));
        }

        static TextMeshProUGUI ResultLine(RectTransform parent, string name, float y, float size)
        {
            var t = Txt(parent, name, false, false, size, theme.ink, TextAlignmentOptions.Center);
            Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(600f, 60f));
            t.richText = true;
            return t;
        }

        static void Shadowed(Image img)
        {
            var s = img.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.8f);
            s.effectDistance = new Vector2(4f, -4f);
        }

        static Button Btn(Transform parent, string name, Color color, string label, float fontSize, Vector2 size, bool breathe)
        {
            var root = Img(parent, name, null, new Color(1f, 1f, 1f, 0f), false, true);
            root.rectTransform.sizeDelta = size;
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
            Stretch(t.rectTransform, new Vector2(8f, 10f), new Vector2(-8f, -2f));
            t.enableAutoSizing = true; t.fontSizeMin = 12f; t.fontSizeMax = fontSize;
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

        /// <summary>A label. title = Action Comics (upper case), else Super Chips. outline = shared dark outline material
        /// (for text over the 3D world); without it the plain font material (for dark text on cream cards).</summary>
        static TextMeshProUGUI Txt(Transform parent, string name, bool title, bool outline, float size, Color color, TextAlignmentOptions align)
        {
            var t = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            t.rectTransform.SetParent(parent, false);
            t.font = title ? theme.title : theme.number;
            if (outline) { var m = title ? theme.titleOutline : theme.numberOutline; if (m != null) t.fontSharedMaterial = m; }
            t.fontSize = size;
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
