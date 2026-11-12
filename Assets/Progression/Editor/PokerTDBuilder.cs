using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PokerTD.EditorTools
{
    /// <summary>
    /// Builds the graybox Poker TD scene from primitives: a tile grid with a winding
    /// path (like the Brackeys tower defense series), waypoints, the base, camera,
    /// managers and a plain HUD. Menu: Poker TD/Build Prototype Scene.
    /// </summary>
    public static class PokerTDBuilder
    {
        const string Root = "Assets/Progression";
        const string ScenePath = Root + "/Scenes/PokerTD.unity";
        const int Cols = 14, Rows = 9;
        const float Tile = 2f;
        // path corners in grid cells (column, row), from the spawn to the base
        static readonly Vector2Int[] Corners =
        {
            new Vector2Int(0, 7), new Vector2Int(10, 7), new Vector2Int(10, 4),
            new Vector2Int(3, 4), new Vector2Int(3, 1), new Vector2Int(13, 1),
        };

        [MenuItem("Poker TD/Build Prototype Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var config = Config();
            var gray = Mat("M_TD_Gray", Color.white);
            var grass = Mat("M_TD_Grass", new Color(0.55f, 0.7f, 0.45f));
            var sand = Mat("M_TD_Path", new Color(0.86f, 0.76f, 0.56f));
            var baseMat = Mat("M_TD_Base", new Color(0.35f, 0.55f, 0.9f));

            // light and camera (2.5D: tilted perspective view)
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.2f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.13f, 0.12f);
            camGo.transform.position = new Vector3(0f, 25f, -19f);
            camGo.transform.LookAt(new Vector3(0f, 0f, -1.2f));

            // everything on the battle board sinks together in the shop turn
            var board = new GameObject("Board").transform;
            // tiles
            var path = PathCells();
            var map = new GameObject("Map").transform;
            map.SetParent(board);
            for (int c = 0; c < Cols; c++)
                for (int r = 0; r < Rows; r++)
                {
                    bool onPath = path.Contains(new Vector2Int(c, r));
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = (onPath ? "Path_" : "Node_") + c + "_" + r;
                    tile.transform.SetParent(map);
                    tile.transform.position = Cell(c, r) + Vector3.down * (onPath ? 0.2f : 0f);
                    tile.transform.localScale = new Vector3(Tile * 0.95f, onPath ? 0.2f : 0.4f, Tile * 0.95f);
                    tile.GetComponent<Renderer>().sharedMaterial = onPath ? sand : grass;
                    tile.AddComponent<BuildNode>().isPath = onPath;
                }

            // waypoints, spawn and base
            var routeGo = new GameObject("Route");
            routeGo.transform.SetParent(board);
            var route = routeGo.AddComponent<PathRoute>();
            var pts = new List<Transform>();
            foreach (var corner in Corners)
            {
                var wp = new GameObject("Waypoint_" + pts.Count).transform;
                wp.SetParent(routeGo.transform);
                wp.position = Cell(corner.x, corner.y);
                pts.Add(wp);
            }
            route.points = pts.ToArray();
            var spawn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawn.name = "Spawn"; Object.DestroyImmediate(spawn.GetComponent<Collider>());
            spawn.transform.SetParent(board);
            spawn.transform.position = Cell(Corners[0].x, Corners[0].y) + new Vector3(-1.2f, 0.5f, 0f);
            spawn.transform.localScale = new Vector3(0.4f, 1.2f, 1.9f);
            spawn.GetComponent<Renderer>().sharedMaterial = Mat("M_TD_Spawn", new Color(0.7f, 0.25f, 0.25f));
            var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseGo.name = "Base"; Object.DestroyImmediate(baseGo.GetComponent<Collider>());
            baseGo.transform.SetParent(board);
            var last = Corners[Corners.Length - 1];
            baseGo.transform.position = Cell(last.x, last.y) + new Vector3(0f, 0.9f, 0f);
            baseGo.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            baseGo.GetComponent<Renderer>().sharedMaterial = baseMat;

            // managers
            var managers = new GameObject("Game");
            var game = managers.AddComponent<TDGame>(); game.config = config;
            var spawner = managers.AddComponent<WaveSpawner>(); spawner.config = config; spawner.enemyMaterial = gray;
            var build = managers.AddComponent<BuildManager>(); build.config = config; build.material = gray;
            var shop = BuildShopFloor(gray);
            var move = managers.AddComponent<BoardTransition>(); move.board = board; move.shop = shop; move.config = config;

            BuildHud(config);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Debug.Log("Poker TD scene built: " + ScenePath);
        }

        /// <summary>Graybox shop floor: a wooden stage with pedestals for the items (filled in step 5).</summary>
        static Transform BuildShopFloor(Material gray)
        {
            var shop = new GameObject("ShopFloor").transform;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Stage"; Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.transform.SetParent(shop);
            floor.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            floor.transform.localScale = new Vector3(Cols * Tile, 0.4f, Rows * Tile);
            floor.GetComponent<Renderer>().sharedMaterial = Mat("M_TD_ShopFloor", new Color(0.62f, 0.42f, 0.26f));
            var ped = Mat("M_TD_Pedestal", new Color(0.85f, 0.75f, 0.55f));
            for (int i = 0; i < 5; i++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                p.name = "Pedestal" + i; Object.DestroyImmediate(p.GetComponent<Collider>());
                p.transform.SetParent(shop);
                p.transform.localPosition = new Vector3((i - 2) * 4.5f, 0.5f, 1f);
                p.transform.localScale = new Vector3(2f, 0.5f, 2f);
                p.GetComponent<Renderer>().sharedMaterial = ped;
            }
            return shop;
        }

        static HashSet<Vector2Int> PathCells()
        {
            var set = new HashSet<Vector2Int>();
            for (int i = 1; i < Corners.Length; i++)
            {
                Vector2Int a = Corners[i - 1], b = Corners[i];
                Vector2Int step = new Vector2Int(System.Math.Sign(b.x - a.x), System.Math.Sign(b.y - a.y));
                for (var p = a; p != b; p += step) set.Add(p);
                set.Add(b);
            }
            return set;
        }

        static Vector3 Cell(int c, int r) => new Vector3((c - (Cols - 1) / 2f) * Tile, 0f, (r - (Rows - 1) / 2f) * Tile);

        // ---------- HUD ----------

        static void BuildHud(TDConfig config)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var hud = canvasGo.AddComponent<UI.TDHud>();

            hud.status = Text(canvasGo.transform, "Status", "", 34, new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(1200f, 110f), TextAlignmentOptions.TopLeft);
            var buttons = new List<Button>();
            for (int i = 0; i < config.towers.Length; i++)
                buttons.Add(MakeButton(canvasGo.transform, "Tower" + i, config.towers[i].name, new Vector2(0f, 0f), new Vector2(40f, 40f + (config.towers.Length - 1 - i) * 105f), new Vector2(230f, 90f)));
            hud.towerButtons = buttons.ToArray();
            hud.startWave = MakeButton(canvasGo.transform, "StartWave", "Start Wave", new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(300f, 90f));

            // hand of cards (bottom centre) with a card template the UI pass can restyle
            var handView = canvasGo.AddComponent<UI.HandView>();
            var area = new GameObject("Hand", typeof(RectTransform));
            area.transform.SetParent(canvasGo.transform, false);
            var art = (RectTransform)area.transform;
            art.anchorMin = art.anchorMax = art.pivot = new Vector2(0.5f, 0f);
            art.anchoredPosition = new Vector2(0f, 110f);
            art.sizeDelta = new Vector2(1000f, 200f);
            handView.area = art;
            var card = new GameObject("CardTemplate", typeof(RectTransform), typeof(Image), typeof(Outline));
            card.transform.SetParent(area.transform, false);
            var crt = (RectTransform)card.transform;
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(110f, 155f);
            card.GetComponent<Outline>().effectColor = new Color(0.17f, 0.1f, 0.06f);
            card.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            var cv = card.AddComponent<UI.CardView>();
            cv.face = card.GetComponent<Image>();
            cv.rank = Text(card.transform, "Rank", "7", 52, new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), new Vector2(110f, 70f), TextAlignmentOptions.Center);
            cv.rank.fontStyle = FontStyles.Bold;
            cv.suit = Text(card.transform, "Suit", "\u2665", 56, new Vector2(0.5f, 0.5f), new Vector2(0f, -32f), new Vector2(110f, 70f), TextAlignmentOptions.Center);
            handView.template = cv;
            handView.discardButton = MakeButton(canvasGo.transform, "Discard", "Discard", new Vector2(1f, 0f), new Vector2(-40f, 150f), new Vector2(300f, 80f));
            handView.discardLabel = handView.discardButton.GetComponentInChildren<TMP_Text>();

            // shop turn panel (placeholder until step 5)
            var shopView = canvasGo.AddComponent<UI.ShopView>();
            var sp = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
            sp.transform.SetParent(canvasGo.transform, false);
            var srt = (RectTransform)sp.transform;
            srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 1f);
            srt.anchoredPosition = new Vector2(0f, -150f);
            srt.sizeDelta = new Vector2(900f, 260f);
            sp.GetComponent<Image>().color = new Color(0.1f, 0.07f, 0.05f, 0.75f);
            shopView.panel = sp;
            shopView.title = Text(sp.transform, "Title", "SHOP", 64, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(860f, 200f), TextAlignmentOptions.Top);
            shopView.continueButton = MakeButton(sp.transform, "Continue", "Continue", new Vector2(0.5f, 0f), new Vector2(0f, -110f), new Vector2(300f, 90f));
            var over = new GameObject("GameOver", typeof(RectTransform), typeof(Image));
            over.transform.SetParent(canvasGo.transform, false);
            var ort = (RectTransform)over.transform; ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one; ort.offsetMin = ort.offsetMax = Vector2.zero;
            over.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            hud.gameOver = over;
            hud.gameOverText = Text(over.transform, "Text", "GAME OVER", 90, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 400f), TextAlignmentOptions.Center);
        }

        static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Vector2 anchor, Vector2 pos, Vector2 box, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.alignment = align; t.color = Color.white; t.raycastTarget = false;
            return t;
        }

        static Button MakeButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var t = Text(go.transform, "Label", label, 30, new Vector2(0.5f, 0.5f), Vector2.zero, size, TextAlignmentOptions.Center);
            t.color = new Color(0.17f, 0.1f, 0.06f);
            return go.GetComponent<Button>();
        }

        // ---------- assets ----------

        static TDConfig Config()
        {
            string path = Root + "/Data/TDConfig.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<TDConfig>(path);
            if (cfg == null) { cfg = ScriptableObject.CreateInstance<TDConfig>(); AssetDatabase.CreateAsset(cfg, path); }
            return cfg;
        }

        static Material Mat(string name, Color color)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void AddToBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (list.Exists(s => s.path == ScenePath)) return;
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
