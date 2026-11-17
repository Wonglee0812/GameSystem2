using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DamRush.EditorTools
{
    /// <summary>
    /// Builds the Dam Rush prototype scene: valley, water, village, camera, game and HUD.
    /// Data assets (config, materials, shapes) are only created when missing, so tuned
    /// numbers survive a rebuild. Menu: Dam Rush/Build Sandbox Scene.
    /// </summary>
    public static class DamRushBuilder
    {
        const string Root = "Assets/Sandbox";
        const string ScenePath = Root + "/Scenes/DamRush.unity";

        [MenuItem("Dam Rush/Build Sandbox Scene")]
        public static void Build()
        {
            Folder(Root, "Data"); Folder(Root, "Materials"); Folder(Root, "Scenes");
            var config = Asset<SandboxConfig>(Root + "/Data/SandboxConfig.asset", null);

            var wood = MaterialAsset("Wood", new Color(0.62f, 0.42f, 0.22f), 600f, 0.6f, 50000f, 7f, 4f, false, false);
            var stone = MaterialAsset("Stone", new Color(0.56f, 0.57f, 0.6f), 2500f, 0.7f, 30000f, 12f, 6f, false, false);
            var mud = MaterialAsset("Mud", new Color(0.36f, 0.27f, 0.18f), 1700f, 0.9f, 80000f, 4f, 2f, true, true);
            var block = ShapeAsset("Block", ShapeKind.Block, Vector3.one);
            var log = ShapeAsset("Log", ShapeKind.Log, new Vector3(2.5f, 0.5f, 0.5f));
            var ramp = ShapeAsset("Ramp", ShapeKind.Ramp, Vector3.one);

            var groundMat = GroundMaterial();
            var waterMat = Transparent("M_DR_Water", new Color(0.18f, 0.45f, 0.75f, 0.72f), 0.85f);
            var ghostMat = Transparent("M_DR_Ghost", new Color(0.3f, 1f, 0.4f, 0.45f), 0.2f);
            var hutMat = Lit("M_DR_Hut", new Color(0.78f, 0.6f, 0.38f));
            var roofMat = Lit("M_DR_Roof", new Color(0.45f, 0.3f, 0.2f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // light
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.25f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.65f);

            // valley
            var terrainGo = new GameObject("Valley", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider), typeof(ValleyTerrain));
            var terrain = terrainGo.GetComponent<ValleyTerrain>();
            terrain.config = config;
            terrain.Generate();
            terrainGo.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // water
            var waterGo = new GameObject("Water", typeof(MeshFilter), typeof(MeshRenderer), typeof(WaterGrid));
            var water = waterGo.GetComponent<WaterGrid>();
            water.config = config; water.terrain = terrain;
            var wr = waterGo.GetComponent<MeshRenderer>();
            wr.sharedMaterial = waterMat;
            wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var parts = new GameObject("Parts").transform;
            var debris = new GameObject("Debris").transform;
            BuildVillage(terrain, hutMat, roofMat);

            // camera
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 45f; cam.nearClipPlane = 0.2f; cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.75f, 0.85f);
            var rig = camGo.AddComponent<CameraRig>();
            rig.cam = cam; rig.terrain = terrain;
            rig.focus = new Vector3(terrain.LengthX * 0.45f, 0f, terrain.WidthZ * 0.5f);

            // game
            var gameGo = new GameObject("Game");
            var game = gameGo.AddComponent<DamGame>();
            game.config = config; game.water = water; game.terrain = terrain;
            game.partsRoot = parts; game.debrisRoot = debris;
            var tool = gameGo.AddComponent<BuildTool>();
            tool.game = game; tool.cam = cam; tool.terrain = terrain;
            tool.shapes = new[] { block, log, ramp };
            tool.materials = new[] { wood, stone, mud };
            tool.ghostMaterial = ghostMat;

            BuildHud(game, tool, config);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Debug.Log("Dam Rush scene built: " + ScenePath);
        }

        // ------------------------------------------------------------------ world

        static void BuildVillage(ValleyTerrain terrain, Material hut, Material roof)
        {
            var village = new GameObject("Village").transform;
            Rect r = terrain.VillageRect;
            var spots = new[] { new Vector2(0.15f, 0.3f), new Vector2(0.4f, 0.65f), new Vector2(0.62f, 0.28f), new Vector2(0.85f, 0.6f), new Vector2(0.3f, 0.85f) };
            int n = 0;
            foreach (var s in spots)
            {
                float x = r.xMin + s.x * r.width, z = r.yMin + s.y * r.height;
                float y = terrain.GroundAt(x, z);
                var house = new GameObject("Lodge_" + n++).transform;
                house.SetParent(village);
                house.position = new Vector3(x, y, z);
                house.rotation = Quaternion.Euler(0f, n * 37f, 0f);
                Piece(house, hut, new Vector3(0f, 0.35f, 0f), new Vector3(0.9f, 0.7f, 0.8f), Quaternion.identity);
                Piece(house, roof, new Vector3(0f, 0.78f, 0f), new Vector3(0.7f, 0.7f, 0.85f), Quaternion.Euler(0f, 0f, 45f));
            }
            // a flag pole marks the village from far away
            var pole = Piece(village, roof, Vector3.zero, new Vector3(0.08f, 2.2f, 0.08f), Quaternion.identity);
            float px = r.center.x, pz = r.yMax - 0.5f;
            pole.position = new Vector3(px, terrain.GroundAt(px, pz) + 1.1f, pz);
            var flag = Piece(village, hut, Vector3.zero, new Vector3(0.6f, 0.4f, 0.03f), Quaternion.identity);
            flag.position = pole.position + new Vector3(0.32f, 0.85f, 0f);
        }

        static Transform Piece(Transform parent, Material mat, Vector3 localPos, Vector3 scale, Quaternion rot)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<Collider>()); // decoration: no physics, no water blocking
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go.transform;
        }

        // ------------------------------------------------------------------ HUD

        static void BuildHud(DamGame game, BuildTool tool, SandboxConfig config)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetAsLastSibling();

            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var c = canvasGo.transform;
            var hud = canvasGo.AddComponent<DamHud>();
            hud.game = game; hud.tool = tool; hud.config = config;

            // top left
            var tl = Panel(c, "TopLeft", new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(560, 170));
            hud.phaseText = Text(tl, "Phase", new Vector2(16, -10), new Vector2(530, 90), 34, TextAlignmentOptions.TopLeft);
            hud.budgetText = Text(tl, "Budget", new Vector2(16, -100), new Vector2(530, 36), 28, TextAlignmentOptions.TopLeft);
            hud.statsText = Text(tl, "Stats", new Vector2(16, -136), new Vector2(530, 30), 20, TextAlignmentOptions.TopLeft);

            // top right: village health
            var tr = Panel(c, "Village", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(460, 90));
            hud.villageText = Text(tr, "VillageText", new Vector2(16, -8), new Vector2(430, 36), 24, TextAlignmentOptions.TopLeft);
            var barBg = Box(tr, "BarBg", new Vector2(16, -50), new Vector2(428, 24), new Color(0.15f, 0.15f, 0.15f, 1f));
            var fill = Box(barBg, "Fill", Vector2.zero, new Vector2(428, 24), Color.green);
            hud.villageFill = fill.GetComponent<Image>();

            // flood button
            hud.floodButton = Button(c, "FloodButton", new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(280, 70), "FLOOD!  (Space)", 30);
            hud.floodButton.image.color = new Color(0.35f, 0.65f, 1f);

            // bottom palette
            var bottom = Panel(c, "Palette", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(1300, 190));
            hud.infoText = Text(bottom, "Info", new Vector2(20, -10), new Vector2(1260, 70), 24, TextAlignmentOptions.TopLeft);
            hud.shapeButtons = new Button[3];
            hud.materialButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                hud.shapeButtons[i] = Button(bottom, "Shape" + i, new Vector2(0, 1), new Vector2(20 + i * 205, -84), new Vector2(195, 56), "Shape", 24, true);
                hud.materialButtons[i] = Button(bottom, "Material" + i, new Vector2(0, 1), new Vector2(680 + i * 205, -84), new Vector2(195, 56), "Material", 24, true);
            }
            hud.hintText = Text(bottom, "Hint", new Vector2(20, -148), new Vector2(1260, 32), 19, TextAlignmentOptions.TopLeft);
            hud.hintText.color = new Color(0.85f, 0.85f, 0.85f);

            // game over
            var over = Panel(c, "GameOver", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 320));
            over.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            hud.gameOverPanel = over.gameObject;
            hud.gameOverText = Text(over, "Text", new Vector2(30, -30), new Vector2(640, 180), 30, TextAlignmentOptions.Top);
            hud.restartButton = Button(over, "Restart", new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(240, 64), "Restart", 28);
        }

        static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.12f, 0.72f);
            return rt;
        }

        static RectTransform Box(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().raycastTarget = false;
            return rt;
        }

        static TMP_Text Text(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var t = go.GetComponent<TextMeshProUGUI>();
            t.fontSize = fontSize; t.alignment = align; t.color = Color.white; t.raycastTarget = false;
            t.text = name;
            return t;
        }

        static Button Button(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, string label, float fontSize, bool topLeft = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = topLeft ? new Vector2(0, 1) : new Vector2(0.5f, anchor.y);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var t = Text(go.transform, "Label", Vector2.zero, size, fontSize, TextAlignmentOptions.Center);
            t.text = label; t.color = new Color(0.1f, 0.1f, 0.1f);
            return go.GetComponent<Button>();
        }

        // ------------------------------------------------------------------ assets

        static void Folder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }

        static T Asset<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            init?.Invoke(a);
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static MaterialDef MaterialAsset(string name, Color color, float density, float friction, float joint, float tough, float cost, bool erodes, bool glue)
        {
            var mat = Lit("M_DR_" + name, color);
            var def = Asset<MaterialDef>(Root + "/Data/Mat_" + name + ".asset", d =>
            {
                d.displayName = name; d.color = color; d.density = density; d.friction = friction;
                d.jointStrength = joint; d.toughness = tough; d.costPerCubicMetre = cost;
                d.erodes = erodes; d.gluesToGround = glue;
            });
            def.renderMaterial = mat;
            EditorUtility.SetDirty(def);
            return def;
        }

        static ShapeDef ShapeAsset(string name, ShapeKind kind, Vector3 size) =>
            Asset<ShapeDef>(Root + "/Data/Shape_" + name + ".asset", d => { d.displayName = name; d.kind = kind; d.size = size; });

        static Material Lit(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Transparent(string name, Color color, float smoothness)
        {
            var m = Lit(name, color);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Smoothness", smoothness);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>The valley mesh has vertex colours (river bed brown, banks green).</summary>
        static Material GroundMaterial()
        {
            string path = Root + "/Materials/M_DR_Ground.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Simple Lit") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_ReceiveShadows", 1f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void AddToBuildSettings(string path)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (list.Exists(s => s.path == path)) return;
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
