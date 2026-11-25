using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PokerTD.UI;

namespace PokerTD.EditorTools
{
    /// <summary>
    /// Builds the 3D shop floor (Wong, 2026-11-24, like the stations in Unrailed): Kenney floor tiles,
    /// and zones on mats: MACHINES (square pedestals), ITEMS and UPGRADES (card slots), plus the 3D
    /// REROLL and CONTINUE buttons. Every zone is one piece for the tile-by-tile transition.
    /// The old UI shop panel is switched off. Menu: Poker TD/Build 3D Shop. Safe to run twice:
    /// it only adds missing parts and never moves or restyles what is already in the scene.
    /// </summary>
    public static class PokerTDShopBuilder
    {
        const string Kit = "Assets/Progression/Art/Kenney_TD/";
        const string Res = "Assets/Progression/Art/Resources/";
        const int Cols = 14, Rows = 9;
        const float Tile = 2f;
        const float ZoneWidth = 10.5f;

        [MenuItem("Poker TD/Build 3D Shop")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            var move = Object.FindFirstObjectByType<BoardTransition>(FindObjectsInactive.Include);
            if (move == null || move.shop == null) { Debug.LogError("No BoardTransition / ShopFloor in the open scene."); return; }
            BuildFloor(move.shop);
            var hud = GameObject.Find("HUD");
            if (hud != null)
            {
                var panel = hud.transform.Find("ShopPanel");
                if (panel != null) panel.gameObject.SetActive(false);
                var view = hud.GetComponentInChildren<ShopView>(true);
                if (view == null) view = Object.FindFirstObjectByType<ShopView>(FindObjectsInactive.Include);
                if (view != null) view.enabled = false;
                BuildGameOver(hud.transform);
            }
            EditorSceneManager.MarkSceneDirty(move.shop.gameObject.scene);
            Debug.Log("Poker TD: 3D shop built. Save the scene to keep it.");
        }

        /// <summary>Game over screen: dark overlay, GAME OVER title, info line and a RESTART button
        /// (a copy of the Start Wave button, so it has the same style and hover).</summary>
        static void BuildGameOver(Transform hud)
        {
            var go = hud.Find("GameOver");
            if (go == null) return;
            if (go.Find("Info") != null && go.Find("Restart") != null) return;   // already made: keep Wong's layout
            var rt = (RectTransform)go;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<UnityEngine.UI.Image>();
            if (img != null) { img.sprite = null; img.color = new Color(0.06f, 0.04f, 0.035f, 0.93f); img.raycastTarget = true; }
            var title = go.Find("Text");
            var tmp = title != null ? title.GetComponent<TextMeshProUGUI>() : null;
            if (tmp != null)
            {
                tmp.font = Title();
                var outline = AssetDatabase.LoadAssetAtPath<Material>(Res + "Fonts/Action Comics Black SDF Outline.mat");
                if (outline != null) tmp.fontSharedMaterial = outline;
                tmp.fontSize = 150f;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.enableAutoSizing = true; tmp.fontSizeMin = 60f; tmp.fontSizeMax = 150f;   // Action Comics draws big
                tmp.color = new Color(0.95f, 0.35f, 0.25f);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.text = "GAME OVER";
                var trt = (RectTransform)title;
                trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
                trt.sizeDelta = new Vector2(1400f, 220f);
                trt.anchoredPosition = new Vector2(0f, 130f);
            }
            var info = go.Find("Info");
            if (info == null)
            {
                info = new GameObject("Info", typeof(RectTransform)).transform;
                info.SetParent(go, false);
            }
            var it = info.GetComponent<TextMeshProUGUI>();
            if (it == null) it = info.gameObject.AddComponent<TextMeshProUGUI>();
            it.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Res + "Fonts/Super Chips SDF.asset");
            var o2 = AssetDatabase.LoadAssetAtPath<Material>(Res + "Fonts/Super Chips SDF Outline.mat");
            if (o2 != null) it.fontSharedMaterial = o2;
            it.fontSize = 44f;
            it.color = new Color(0.98f, 0.93f, 0.82f);
            it.alignment = TextAlignmentOptions.Center;
            it.text = "YOU REACHED LEVEL 1  ·  WAVE 1";
            it.raycastTarget = false;
            var irt = (RectTransform)info;
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = new Vector2(1400f, 140f);
            irt.anchoredPosition = new Vector2(0f, -10f);

            var restart = go.Find("Restart");
            var start = hud.Find("StartWave");
            if (restart == null && start != null)
            {
                restart = Object.Instantiate(start.gameObject, go).transform;
                restart.name = "Restart";
            }
            if (restart != null)
            {
                var rrt = (RectTransform)restart;
                rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(0.5f, 0.5f);
                rrt.anchoredPosition = new Vector2(0f, -160f);
                rrt.localScale = Vector3.one * 1.3f;
                var btn = restart.GetComponent<UnityEngine.UI.Button>();
                if (btn != null) { btn.onClick = new UnityEngine.UI.Button.ButtonClickedEvent(); btn.interactable = true; }
                foreach (var l in restart.GetComponentsInChildren<TMP_Text>(true)) l.text = "RESTART";
            }
            go.gameObject.SetActive(false);
        }

        static GameObject M(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Kit + name + ".fbx");
        static Vector3 Cell(int c, int r) => new Vector3((c - (Cols - 1) / 2f) * Tile, 0f, (r - (Rows - 1) / 2f) * Tile);

        static Transform Put(GameObject model, Transform parent, Vector3 localPos, float yaw, float scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            return go.transform;
        }

        /// <summary>Find a child by name, or null.</summary>
        static Transform Child(Transform parent, string name) => parent != null ? parent.Find(name) : null;

        /// <summary>
        /// Builds what is MISSING of the 3D shop and links it to Shop3D. Anything that already exists
        /// (tiles, zones, mats, titles, slots, pedestals, buttons, texts) is kept as it is, with Wong's
        /// own positions and text settings (Wong, 2026-11-24). To get a fresh default part, delete it first.
        /// </summary>
        public static void BuildFloor(Transform shop)
        {
            for (int i = shop.childCount - 1; i >= 0; i--)
            {
                var c = shop.GetChild(i);
                if (c.name == "Stage" || c.name.StartsWith("Pedestal")) Object.DestroyImmediate(c.gameObject);   // old graybox
            }
            var root = Child(shop, "KenneyArt");
            if (root == null) { root = new GameObject("KenneyArt").transform; root.SetParent(shop, false); }

            // floor: dirt yard, grass and trees on the edge (only when there is no floor yet)
            if (Child(root, "Tiles") == null)
            {
                var tiles = new GameObject("Tiles").transform;
                tiles.SetParent(root, false);
                var rng = new System.Random(8);
                for (int c = 0; c < Cols; c++)
                    for (int r = 0; r < Rows; r++)
                    {
                        bool edge = c == 0 || r == 0 || c == Cols - 1 || r == Rows - 1;
                        string model = edge ? (rng.NextDouble() < 0.35 ? "tile-tree" : "tile") : "tile-dirt";
                        Put(M(model), tiles, Cell(c, r) + Vector3.down * 0.2f, rng.Next(4) * 90f, Tile).name = "Shop_" + c + "_" + r;
                    }
            }

            var shop3d = shop.GetComponent<Shop3D>();
            if (shop3d == null) shop3d = shop.gameObject.AddComponent<Shop3D>();
            Undo.RecordObject(shop3d, "3D shop");

            shop3d.machineSlots = Zone(root, "Zone_Machines", "MACHINES", new Vector3(-6.3f, 0.2f, 3.6f), true);
            shop3d.itemSlots = Zone(root, "Zone_Items", "ITEMS", new Vector3(6.3f, 0.2f, 3.6f), false);
            shop3d.upgradeSlots = Zone(root, "Zone_Upgrades", "UPGRADES", new Vector3(-6.3f, 0.2f, -3.9f), false);

            var buttons = Child(root, "Zone_Buttons");
            if (buttons == null)
            {
                buttons = new GameObject("Zone_Buttons").transform;
                buttons.SetParent(root, false);
                buttons.localPosition = new Vector3(6.3f, 0.2f, -3.9f);
            }
            if (Child(buttons, "Title") == null)
                Text(buttons, "Title", "SHOP", Title(), 14f, new Color(1f, 0.78f, 0.28f), new Vector3(0f, 0.02f, 1.6f), new Vector2(10f, 2.5f)).alignment = TextAlignmentOptions.Center;
            shop3d.rerollButton = Button(buttons, "Reroll", "REROLL  $3", new Color(0.25f, 0.5f, 0.9f), new Vector3(0f, 0.02f, -0.7f), out TextMeshPro rerollLabel);
            shop3d.rerollLabel = rerollLabel;
            shop3d.continueButton = Button(buttons, "Continue", "CONTINUE", new Color(0.96f, 0.55f, 0.16f), new Vector3(0f, 0.02f, -2.7f), out _);
            EditorUtility.SetDirty(shop3d);
        }

        /// <summary>A mat with a title and 3 slots (machine zones: square pedestals). Existing parts are kept.</summary>
        static Transform[] Zone(Transform parent, string name, string title, Vector3 pos, bool pedestals)
        {
            var z = Child(parent, name);
            if (z == null)
            {
                z = new GameObject(name).transform;
                z.SetParent(parent, false);
                z.localPosition = pos;
            }
            if (Child(z, "Mat") == null)
            {
                var mat = new GameObject("Mat").AddComponent<SpriteRenderer>();
                mat.transform.SetParent(z, false);
                mat.transform.localPosition = new Vector3(0f, 0.01f, 0f);
                mat.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Res + "UI/ShopZone.png");
                mat.sprite = sprite;
                if (sprite != null) mat.transform.localScale = Vector3.one * (ZoneWidth / sprite.bounds.size.x);
            }
            float depth = ZoneWidth * 400f / 768f;
            if (Child(z, "Title") == null)
                Text(z, "Title", title, Title(), 4.2f, new Color(1f, 0.78f, 0.28f), new Vector3(0.3f, 0.02f, depth * 0.5f + 0.5f), new Vector2(ZoneWidth, 1f)).alignment = TextAlignmentOptions.Left;
            var slots = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var s = Child(z, "Slot" + i);
                if (s == null)
                {
                    float x = ZoneWidth * ((148f + i * 236f) / 768f - 0.5f);
                    s = new GameObject("Slot" + i).transform;
                    s.SetParent(z, false);
                    s.localPosition = new Vector3(x, 0.02f, pedestals ? 0.45f : 0.25f);
                }
                if (pedestals && Child(s, "Pedestal") == null) Put(M("tower-square-bottom-a"), s, Vector3.zero, 0f, 1.6f).name = "Pedestal";
                slots[i] = s;
            }
            return slots;
        }

        /// <summary>A 3D button (face sprite, collider, label). An existing button is kept; only its label is looked up.</summary>
        static Transform Button(Transform parent, string name, string label, Color color, Vector3 pos, out TextMeshPro text)
        {
            var b = Child(parent, name);
            if (b != null)
            {
                text = b.GetComponentInChildren<TextMeshPro>(true);
                if (b.GetComponent<Collider>() == null) b.gameObject.AddComponent<BoxCollider>().size = new Vector3(6f, 0.3f, 1.8f);
                return b;
            }
            b = new GameObject(name).transform;
            b.SetParent(parent, false);
            b.localPosition = pos;
            var sr = new GameObject("Face").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(b, false);
            sr.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Res + "UI/Button3D.png");
            sr.sprite = sprite;
            sr.color = color;
            if (sprite != null) sr.transform.localScale = Vector3.one * (6f / sprite.bounds.size.x);
            var box = b.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(6f, 0.3f, 1.8f);
            text = Text(b, "Label", label, Title(), 5.2f, new Color(0.98f, 0.95f, 0.88f), new Vector3(0f, 0.03f, 0.12f), new Vector2(5.6f, 1.4f));
            text.alignment = TextAlignmentOptions.Center;
            text.sortingOrder = 3;
            return b;
        }

        static TMP_FontAsset Title() => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Res + "Fonts/Action Comics Black SDF.asset");

        static TextMeshPro Text(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color, Vector3 pos, Vector2 box)
        {
            var t = new GameObject(name).AddComponent<TextMeshPro>();
            t.transform.SetParent(parent, false);
            t.transform.localPosition = pos;
            t.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            t.font = font;
            var outline = AssetDatabase.LoadAssetAtPath<Material>(Res + "Fonts/" + (font != null ? font.name : "") + " Outline.mat");
            if (outline != null) t.fontSharedMaterial = outline;
            t.fontSize = size;
            t.color = color;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = box;
            t.text = text;
            t.sortingOrder = 2;
            return t;
        }
    }
}
