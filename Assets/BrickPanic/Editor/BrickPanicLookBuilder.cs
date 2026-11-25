using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrickPanic.EditorTools
{
    /// <summary>Menu "Brick Panic/Apply Look (Palette + Sky)". Wong's choice (2026-11-24): a bright toy palette with more
    /// contrast - plastic red, wood yellow, stone blue, steel dark metal (no green, the island is green) - and the Poker TD
    /// ink background as a fresh blue sky. Sets the brick materials, the island materials, the ink sky, the ambient light
    /// and the sun. Safe to run twice; run "Build HUD In Scene" after it, so the card pictures get the new colours.</summary>
    public static class BrickPanicLookBuilder
    {
        const string MatDir = "Assets/BrickPanic/Data/Materials";

        [MenuItem("Brick Panic/Apply Look (Palette + Sky)")]
        public static void ApplyMenu()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            Apply();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        public static void Apply()
        {
            // prices (Wong, 2026-11-25): a 2x4 costs plastic 0.5, wood 1.5, stone 3, steel 5 (price per stud = that / 8)
            Brick("Plastic", new Color(0.95f, 0.16f, 0.14f), 0.55f, 0f, 0.5f / 8f);
            Brick("Wood", new Color(1f, 0.78f, 0.1f), 0.35f, 0f, 1.5f / 8f);
            Brick("Stone", new Color(0.12f, 0.45f, 0.95f), 0.3f, 0f, 3f / 8f);
            Brick("Steel", new Color(0.3f, 0.32f, 0.38f), 0.7f, 0.65f, 5f / 8f);

            Ground("M_BP_Sand", new Color(1f, 0.84f, 0.48f));
            Ground("M_BP_Grass", new Color(0.3f, 0.72f, 0.26f));
            Ground("M_BP_Rock", new Color(0.6f, 0.58f, 0.6f));
            Ground("M_BP_Volcano", new Color(0.22f, 0.18f, 0.2f));
            Ground("M_BP_Under", new Color(0.55f, 0.32f, 0.18f));

            var mapGen = Object.FindFirstObjectByType<MapGenerator>();
            if (mapGen != null && mapGen.GetComponent<MapBuildIn>() == null) mapGen.gameObject.AddComponent<MapBuildIn>();   // island builds itself brick by brick
            var dresser = Object.FindFirstObjectByType<EnvironmentDresser>();
            if (dresser != null)
            {
                dresser.leafColors = new[] { new Color(0.08f, 0.55f, 0.22f), new Color(0.3f, 0.66f, 0.08f), new Color(0.02f, 0.45f, 0.38f), new Color(1f, 0.55f, 0.08f) };
                dresser.trunkColor = new Color(0.45f, 0.26f, 0.13f);
                dresser.roofColor = new Color(0.95f, 0.24f, 0.36f);
                dresser.rockColors = new[] { new Color(0.6f, 0.6f, 0.64f), new Color(0.5f, 0.5f, 0.54f), new Color(0.7f, 0.68f, 0.66f) };
                EditorUtility.SetDirty(dresser);
            }

            // juice: shake / hit stop / slow motion / sky tint, emote bubbles, wear cracks (on the Systems object)
            var systems = GameObject.Find("Systems");
            if (systems != null)
            {
                if (systems.GetComponent<BPJuice>() == null) systems.AddComponent<BPJuice>();
                var emotes = systems.GetComponent<PersonEmotes>();
                if (emotes == null) emotes = systems.AddComponent<PersonEmotes>();
                const string em = "Assets/BrickPanic/Art/UI/Emotes/";
                foreach (var n in new[] { "panic", "dazed", "dead", "scared", "happy" }) SpriteImport(em + n + ".png");
                emotes.panic = AssetDatabase.LoadAssetAtPath<Sprite>(em + "panic.png");
                emotes.dazed = AssetDatabase.LoadAssetAtPath<Sprite>(em + "dazed.png");
                emotes.dead = AssetDatabase.LoadAssetAtPath<Sprite>(em + "dead.png");
                emotes.scared = AssetDatabase.LoadAssetAtPath<Sprite>(em + "scared.png");
                emotes.happy = AssetDatabase.LoadAssetAtPath<Sprite>(em + "happy.png");
                emotes.emoteMaterial = EmoteMat();
                EditorUtility.SetDirty(emotes);
                var cracks = systems.GetComponent<WearCracks>();
                if (cracks == null) cracks = systems.AddComponent<WearCracks>();
                cracks.crackMaterial = CrackMat();
                EditorUtility.SetDirty(cracks);
                // shelter rule, quake cracks, health bars (Wong, 2026-11-25)
                var shelter = systems.GetComponent<ShelterDebris>();
                if (shelter == null) shelter = systems.AddComponent<ShelterDebris>();
                shelter.debrisMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/BrickPanic/Art/Bricks/bevel-lq-brick-1x2.fbx");
                shelter.rockMaterial = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_BP_Rock.mat");
                shelter.ashMaterial = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_BP_Lava.mat");
                EditorUtility.SetDirty(shelter);
                // quake cracks removed (Wong, 2026-11-25: falling rocks are enough); the script stays for later
                var fiss = systems.GetComponent<QuakeFissures>();
                if (fiss != null) Object.DestroyImmediate(fiss);
                var bars = systems.GetComponent<PersonHealthBars>();
                if (bars == null) bars = systems.AddComponent<PersonHealthBars>();
                bars.barMaterial = EmoteMat();
                EditorUtility.SetDirty(bars);

                // new disasters (Wong, 2026-11-25): tornado + lightning storm, added to the round's disaster pool
                var rounds = systems.GetComponent<RoundManager>();
                var mapG = Object.FindFirstObjectByType<MapGenerator>();
                var act = systems.GetComponent<PhysicsActivator>();
                var fxMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_BP_Fx.mat");
                Mesh Brick(string n) => AssetDatabase.LoadAssetAtPath<Mesh>("Assets/BrickPanic/Art/Bricks/bevel-lq-" + n + ".fbx");
                var tornado = systems.GetComponent<Tornado>();
                if (tornado == null) { tornado = systems.AddComponent<Tornado>(); tornado.displayName = "Tornado"; }
                tornado.map = mapG; tornado.activator = act;
                tornado.funnelMeshes = new[] { Brick("brick-1x1"), Brick("brick-1x2"), Brick("brick-2x2") };
                tornado.funnelMaterial = fxMat;
                EditorUtility.SetDirty(tornado);
                var storm = systems.GetComponent<LightningStorm>();
                if (storm == null) { storm = systems.AddComponent<LightningStorm>(); storm.displayName = "Lightning"; }
                storm.map = mapG; storm.activator = act;
                storm.boltMaterial = VertexColorMat("M_BP_Bolt");
                storm.rainMaterial = VertexColorMat("M_BP_Rain");
                storm.cloudMesh = Brick("brick-2x4");
                storm.cloudMaterial = fxMat;
                storm.scorchMesh = Brick("plate-4x4");
                EditorUtility.SetDirty(storm);
                if (rounds != null) { rounds.tornado = tornado; rounds.lightning = storm; EditorUtility.SetDirty(rounds); }
                ThemeIcons();

                var arrow = systems.GetComponent<TsunamiArrow>();
                if (arrow == null) arrow = systems.AddComponent<TsunamiArrow>();
                arrow.arrowMaterial = ArrowMat();
                EditorUtility.SetDirty(arrow);
            }

            string skyPath = MatDir + "/M_BP_Sky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null) { sky = new Material(Shader.Find("BrickPanic/InkSky")) { name = "M_BP_Sky" }; AssetDatabase.CreateAsset(sky, skyPath); }
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.75f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.6f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.3f, 0.3f);
            if (Camera.main != null) Camera.main.clearFlags = CameraClearFlags.Skybox;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { l.color = new Color(1f, 0.95f, 0.85f); l.intensity = 1.25f; l.shadowStrength = 0.75f; }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[BrickPanic] toy palette and ink sky applied.");
        }

        static void SpriteImport(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            if (imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single && !imp.mipmapEnabled) return;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }

        /// <summary>Unlit material that uses vertex / particle colours (lightning bolt line, rain streaks).</summary>
        static Material VertexColorMat(string name)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            m = new Material(sh != null ? sh : Shader.Find("Sprites/Default")) { name = name };
            if (sh != null)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", name == "M_BP_Bolt" ? 2f : 0f);   // bolt: additive, rain: alpha
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", name == "M_BP_Bolt" ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
                m.SetColor("_BaseColor", Color.white);
            }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>Icons of the new disasters into the UI theme (the HUD itself is not rebuilt: Wong edited it by hand).</summary>
        static void ThemeIcons()
        {
            var theme = AssetDatabase.LoadAssetAtPath<BPTheme>("Assets/BrickPanic/Data/BP_UITheme.asset");
            if (theme == null) return;
            foreach (var n in new[] { "disaster_tornado", "disaster_lightning" })
            {
                string path = "Assets/BrickPanic/Art/UI/Icons/" + n + ".png";
                AssetDatabase.ImportAsset(path);
                SpriteImport(path);
            }
            theme.tornado = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/BrickPanic/Art/UI/Icons/disaster_tornado.png");
            theme.lightning = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/BrickPanic/Art/UI/Icons/disaster_lightning.png");
            EditorUtility.SetDirty(theme);
        }

        /// <summary>Very dark lit material for the quake cracks.</summary>
        static Material DarkMat()
        {
            string path = MatDir + "/M_BP_CrackGround.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_BP_CrackGround" }; AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", new Color(0.1f, 0.07f, 0.09f));
            m.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>See-through arrow for the tsunami forecast (TsunamiArrow sets its colour and alpha).</summary>
        static Material ArrowMat()
        {
            const string texPath = "Assets/BrickPanic/Art/UI/TsunamiArrow.png";
            var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (imp != null && (imp.wrapMode != TextureWrapMode.Clamp || !imp.alphaIsTransparency))
            {
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            var m = TransparentUnlit("M_BP_Arrow");
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            return m;
        }

        /// <summary>URP Unlit, transparent, both sides: the emote bubble quads (texture set per bubble).</summary>
        static Material EmoteMat() => TransparentUnlit("M_BP_Emote");

        static Material TransparentUnlit(string name)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name }; AssetDatabase.CreateAsset(m, path); }
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_Cull", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent + 10;
            m.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>URP Unlit with alpha clipping and the crack texture; WearCracks sets the cutoff per brick.</summary>
        static Material CrackMat()
        {
            const string texPath = "Assets/BrickPanic/Art/UI/Crack.png";
            var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (imp != null && (imp.wrapMode != TextureWrapMode.Clamp || !imp.alphaIsTransparency))
            {
                imp.textureType = TextureImporterType.Default;
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            string path = MatDir + "/M_BP_Crack.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "M_BP_Crack" }; AssetDatabase.CreateAsset(m, path); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Brick(string name, Color c, float smooth, float metal, float pricePerStud)
        {
            var m = AssetDatabase.LoadAssetAtPath<MaterialDef>($"{MatDir}/MAT_{name}.asset");
            if (m == null) return;
            m.color = c; m.smoothness = smooth; m.metallic = metal; m.pricePerStud = pricePerStud;
            EditorUtility.SetDirty(m);
        }

        static void Ground(string name, Color c)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{name}.mat");
            if (m == null) return;
            m.SetColor("_BaseColor", c);
            EditorUtility.SetDirty(m);
        }
    }
}
