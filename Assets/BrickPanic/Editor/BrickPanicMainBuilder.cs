using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrickPanic.EditorTools
{
    /// <summary>Menu "Brick Panic/Build Main Scene". Safe to run twice: existing materials and data keep their
    /// values (only missing ones are made). The scene itself is made new each time.</summary>
    public static class BrickPanicMainBuilder
    {
        const string Root = "Assets/BrickPanic";
        const string MatDir = Root + "/Data/Materials";
        const string ShapeDir = Root + "/Data/Shapes";
        const string ScenePath = Root + "/Scenes/BrickPanic_Main.unity";

        [MenuItem("Brick Panic/Build Main Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            BrickPanicSpikeBuilder.EnsureData();   // import scale, materials, shapes
            MakeReadable(Root + "/Art/Bricks/bevel-lq-brick-2x2.fbx");

            var sand = ColorMat("M_BP_Sand", new Color(0.93f, 0.83f, 0.55f));
            var grass = ColorMat("M_BP_Grass", new Color(0.38f, 0.68f, 0.32f));
            var rock = ColorMat("M_BP_Rock", new Color(0.56f, 0.54f, 0.52f));
            var volcano = ColorMat("M_BP_Volcano", new Color(0.3f, 0.27f, 0.27f));
            var lava = ColorMat("M_BP_Lava", new Color(1f, 0.42f, 0.08f), true);
            var under = ColorMat("M_BP_Under", new Color(0.45f, 0.33f, 0.24f));
            var ghost = GhostMat();
            var water = WaterMat();
            var fxMat = FxMat();
            var person = BrickPanicPeopleBuilder.EnsurePerson();

            MaterialDef M(string n) => AssetDatabase.LoadAssetAtPath<MaterialDef>($"{MatDir}/MAT_{n}.asset");
            ShapeDef S(string n) => AssetDatabase.LoadAssetAtPath<ShapeDef>($"{ShapeDir}/SHP_{n}.asset");
            var mats = new[] { M("Plastic"), M("Wood"), M("Stone"), M("Steel") };
            var shapes = new[] { S("brick-1x1"), S("brick-1x2"), S("brick-2x2"), S("brick-2x4"), S("plate-2x4"), S("plate-4x4"), S("brick-slope-2x2"), S("brick-1x1-round") };

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main;
            cam.transform.position = new Vector3(0f, 42f, -62f);
            cam.transform.rotation = Quaternion.Euler(34f, 0f, 0f);
            cam.farClipPlane = 500f;
            var fly = cam.gameObject.AddComponent<FlyCamera>();

            // floating island
            var island = new GameObject("Island");
            var rb = island.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var quake = island.AddComponent<Earthquake>();
            quake.wearStrength = 0.5f;
            var map = island.AddComponent<MapGenerator>();
            map.topBrick = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Art/Bricks/bevel-lq-brick-2x2.fbx");
            map.sandMat = sand; map.grassMat = grass; map.rockMat = rock; map.volcanoMat = volcano; map.lavaMat = lava; map.underMat = under;
            var dresser = island.AddComponent<EnvironmentDresser>();
            dresser.brick2x2 = S("brick-2x2"); dresser.brick2x4 = S("brick-2x4"); dresser.plate4x4 = S("plate-4x4"); dresser.round1x1 = S("brick-1x1-round");
            dresser.wood = M("Wood"); dresser.stone = M("Stone"); dresser.plastic = M("Plastic");
            map.dresser = dresser;

            // systems
            var systems = new GameObject("Systems");
            var activator = systems.AddComponent<PhysicsActivator>();
            systems.AddComponent<KillZone>();
            var bricks = new GameObject("PlayerBricks");
            var build = systems.AddComponent<BuildController>();
            build.shapes = shapes; build.materials = mats; build.ghostMaterial = ghost;
            build.cam = cam; build.flyCamera = fly; build.blocksRoot = bricks.transform;
            build.personPrefab = person;
            build.peopleRoot = new GameObject("People").transform;
            var tsunami = systems.AddComponent<Tsunami>();
            tsunami.displayName = "Tsunami";
            tsunami.map = map; tsunami.activator = activator; tsunami.waterMaterial = water;
            tsunami.waveMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Art/Bricks/bevel-lq-brick-2x4.fbx");
            var eruption = systems.AddComponent<Eruption>();
            eruption.displayName = "Volcano";
            eruption.map = map; eruption.activator = activator; eruption.lavaMaterial = lava;
            eruption.bombMeshes = new[] {
                AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Art/Bricks/bevel-lq-brick-2x2.fbx"),
                AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Art/Bricks/bevel-lq-brick-1x2.fbx"),
                AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Art/Bricks/bevel-lq-brick-1x1-round.fbx") };
            quake.displayName = "Earthquake";

            var fx = new GameObject("BrickFx").AddComponent<BrickFx>();
            fx.material = fxMat;
            fx.meshes = new[] {
                AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Art/Bricks/bevel-lq-brick-1x1.fbx"),
                AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Art/Bricks/bevel-lq-brick-1x2.fbx"),
                AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Art/Bricks/bevel-lq-brick-2x2.fbx") };

            var tester = systems.AddComponent<RoundTester>();
            tester.build = build; tester.activator = activator; tester.quake = quake; tester.map = map;
            tester.tsunami = tsunami; tester.eruption = eruption;
            tester.enabled = false;   // debug keys only; the RoundManager runs the game

            var rounds = systems.AddComponent<RoundManager>();
            rounds.build = build; rounds.activator = activator; rounds.map = map;
            rounds.quake = quake; rounds.tsunami = tsunami; rounds.eruption = eruption;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            BrickPanicLookBuilder.Apply();   // toy palette + ink sky
            BrickPanicHudBuilder.Build();    // uGUI HUD (needs the scene saved and the systems in it)
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[BrickPanic] main scene built: " + ScenePath);
        }

        static void MakeReadable(string path)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            if (imp == null || imp.isReadable) return;
            imp.isReadable = true;
            imp.SaveAndReimport();
        }

        static Material ColorMat(string name, Color c, bool glow = false)
        {
            Directory.CreateDirectory(MatDir);
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.25f);
            if (glow)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 1.5f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Material FxMat()
        {
            string path = MatDir + "/M_BP_Fx.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_BP_Fx" };
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.3f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.enableInstancing = true;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Material WaterMat()
        {
            string path = MatDir + "/M_BP_Water.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_BP_Water" };
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetColor("_BaseColor", new Color(0.2f, 0.55f, 0.95f, 0.75f));
            m.SetFloat("_Smoothness", 0.85f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Material GhostMat()
        {
            string path = MatDir + "/M_BP_Ghost.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_BP_Ghost" };
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetColor("_BaseColor", new Color(0.3f, 1f, 0.4f, 0.45f));
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
