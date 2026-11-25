using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrickPanic.EditorTools
{
    /// <summary>Menu "Brick Panic/Build Physics Spike Scene". Safe to run twice: existing data assets keep
    /// their values (only missing ones are created), and the spike scene is rebuilt.</summary>
    public static class BrickPanicSpikeBuilder
    {
        const string Root = "Assets/BrickPanic";
        const string BrickArt = Root + "/Art/Bricks";
        const string MatDir = Root + "/Data/Materials";
        const string ShapeDir = Root + "/Data/Shapes";
        const string ScenePath = Root + "/Scenes/BrickPanic_Spike.unity";

        [MenuItem("Brick Panic/Build Physics Spike Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var (plastic, wood, stone, steel, b2x4, b2x2, p2x4, p4x4) = EnsureData();
            var groundMat = TemplateMaterial("M_BP_Ground", new Color(0.36f, 0.66f, 0.33f));

            // scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main;
            cam.transform.position = new Vector3(0f, 26f, -46f);
            cam.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
            cam.farClipPlane = 400f;
            cam.gameObject.AddComponent<FlyCamera>();

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Island";   // floating base: what falls off is lost
            ground.transform.position = new Vector3(1f, -1.5f, 4f);
            ground.transform.localScale = new Vector3(64f, 3f, 48f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            var gpm = new PhysicsMaterial("PM_Ground") { staticFriction = 0.8f, dynamicFriction = 0.8f, frictionCombine = PhysicsMaterialCombine.Average };
            ground.GetComponent<BoxCollider>().sharedMaterial = gpm;
            var grb = ground.AddComponent<Rigidbody>();
            grb.isKinematic = true;
            var quake = ground.AddComponent<Earthquake>();

            var spike = new GameObject("PhysicsSpike");
            var activator = spike.AddComponent<PhysicsActivator>();
            var spawner = spike.AddComponent<SpikeSpawner>();
            spawner.materials = new[] { plastic, wood, stone, steel };
            spawner.brick2x4 = b2x4; spawner.brick2x2 = b2x2; spawner.plate2x4 = p2x4; spawner.plate4x4 = p4x4;
            spawner.quake = quake; spawner.activator = activator;
            spike.AddComponent<KillZone>();
            var stats = spike.AddComponent<SpikeStats>();
            stats.spawner = spawner; stats.quake = quake;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[BrickPanic] spike scene built: " + ScenePath + "  (Play, then Space = quake, R = rebuild)");
        }

        /// <summary>Import scale for the bricks, brick materials (MaterialDef) and shapes (ShapeDef). Safe to run twice.</summary>
        public static (MaterialDef, MaterialDef, MaterialDef, MaterialDef, ShapeDef, ShapeDef, ShapeDef, ShapeDef) EnsureData()
        {
            FixImport();
            var brickTemplate = TemplateMaterial("M_BP_Brick", Color.white);
            var plastic = Mat("Plastic", new Color(0.93f, 0.27f, 0.22f), 0.7f, 0f, 1f, 1f, 250f, 80f, 0.5f, true, 1f, brickTemplate, 10f);
            var wood = Mat("Wood", new Color(0.66f, 0.45f, 0.24f), 0.2f, 0f, 2f, 1.6f, 500f, 180f, 0.7f, true, 4f, brickTemplate, 20f);
            var stone = Mat("Stone", new Color(0.56f, 0.56f, 0.58f), 0.1f, 0f, 4f, 4f, 1200f, 450f, 0.8f, false, 0f, brickTemplate, 35f);
            var steel = Mat("Steel", new Color(0.55f, 0.62f, 0.72f), 0.6f, 0.8f, 7f, 6f, 3000f, 1200f, 0.5f, false, 0f, brickTemplate, 60f);

            ShapeDef b2x4 = null, b2x2 = null, p2x4 = null, p4x4 = null;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { BrickArt }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var s = Shape(path);
                if (path.EndsWith("brick-2x4.fbx")) b2x4 = s;
                if (path.EndsWith("brick-2x2.fbx")) b2x2 = s;
                if (path.EndsWith("plate-2x4.fbx")) p2x4 = s;
                if (path.EndsWith("plate-4x4.fbx")) p4x4 = s;
            }
            AssetDatabase.SaveAssets();
            return (plastic, wood, stone, steel, b2x4, b2x2, p2x4, p4x4);
        }

        /// <summary>Kenney bricks are real size; scale them so 1 stud = 0.5 m. No imported materials.</summary>
        static void FixImport()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { BrickArt }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (ModelImporter)AssetImporter.GetAtPath(path);
                if (Mathf.Approximately(imp.globalScale, BrickGrid.ImportScale) && imp.materialImportMode == ModelImporterMaterialImportMode.None) continue;
                imp.globalScale = BrickGrid.ImportScale;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.SaveAndReimport();
            }
        }

        static Material TemplateMaterial(string name, Color c)
        {
            Directory.CreateDirectory(MatDir);
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var shader = GraphicsSettings.currentRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            m = new Material(shader) { name = name };
            m.SetColor("_BaseColor", c);
            m.SetColor("_Color", c);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static MaterialDef Mat(string name, Color c, float smooth, float metal, float price, float mass, float force, float torque,
                               float friction, bool floats, float heat, Material template, float durability)
        {
            string path = MatDir + "/MAT_" + name + ".asset";
            var m = AssetDatabase.LoadAssetAtPath<MaterialDef>(path);
            if (m != null) { if (m.baseMaterial == null) { m.baseMaterial = template; EditorUtility.SetDirty(m); } return m; }
            m = ScriptableObject.CreateInstance<MaterialDef>();
            m.displayName = name; m.color = c; m.smoothness = smooth; m.metallic = metal; m.baseMaterial = template;
            m.pricePerStud = price; m.massPerStud = mass; m.breakForcePerStud = force; m.breakTorquePerStud = torque;
            m.friction = friction; m.floats = floats; m.heatSeconds = heat; m.durability = durability;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static ShapeDef Shape(string fbxPath)
        {
            Directory.CreateDirectory(ShapeDir);
            string file = Path.GetFileNameWithoutExtension(fbxPath).Replace("bevel-lq-", "");
            string path = ShapeDir + "/SHP_" + file + ".asset";
            var s = AssetDatabase.LoadAssetAtPath<ShapeDef>(path);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(fbxPath);
            if (s != null) { if (s.mesh == null) { s.mesh = mesh; EditorUtility.SetDirty(s); } return s; }
            s = ScriptableObject.CreateInstance<ShapeDef>();
            s.mesh = mesh;
            s.kind = file.Contains("plate") ? ShapeKind.Plate : file.Contains("slope") ? ShapeKind.Slope : file.Contains("round") ? ShapeKind.Round : ShapeKind.Brick;
            var size = mesh.bounds.size;
            s.studs = new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(size.x / BrickGrid.Stud)), Mathf.Max(1, Mathf.RoundToInt(size.z / BrickGrid.Stud)));
            s.displayName = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(file.Replace("-", " "));
            AssetDatabase.CreateAsset(s, path);
            return s;
        }
    }
}
