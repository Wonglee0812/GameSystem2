using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DamRush.EditorTools
{
    /// <summary>
    /// Builds the water lab scene (pool + channel) for testing water, floating, side push and flow.
    /// Data assets are only created when missing, so tuned numbers survive a rebuild.
    /// Menu: Dam Rush/Build Water Lab.
    /// </summary>
    public static class WaterLabBuilder
    {
        const string Root = "Assets/Sandbox";
        const string ScenePath = Root + "/Scenes/WaterLab.unity";

        [MenuItem("Dam Rush/Build Water Lab")]
        public static void Build()
        {
            Folder(Root, "Data"); Folder(Root, "Materials"); Folder(Root, "Scenes");
            var config = Asset<SandboxConfig>(Root + "/Data/WaterLabConfig.asset", c =>
            {
                c.cellsX = 64; c.cellsZ = 40; c.cellSize = 0.5f;
                c.substeps = 2; c.friction = FrictionMode.Manning; c.manningN = 0.035f;
                c.pressureScale = 1f; c.openEndX = true; c.displaceWater = true;
            });

            var wood = MaterialAsset("Wood", new Color(0.62f, 0.42f, 0.22f), 600f, 0.6f, 50000f, 7f, 4f, false);
            var stone = MaterialAsset("Stone", new Color(0.56f, 0.57f, 0.6f), 2500f, 0.7f, 30000f, 12f, 6f, false);
            var mud = MaterialAsset("Mud", new Color(0.36f, 0.27f, 0.18f), 1700f, 0.9f, 80000f, 4f, 2f, true);
            var block = ShapeAsset("Block", ShapeKind.Block, Vector3.one);
            var log = ShapeAsset("Log", ShapeKind.Log, new Vector3(2.5f, 0.5f, 0.5f));
            var ramp = ShapeAsset("Ramp", ShapeKind.Ramp, Vector3.one);

            var groundMat = VertexColour("M_DR_LabGround", false);
            var waterMat = VertexColour("M_DR_LabWater", true);
            var lineMat = LineMaterial();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.2f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.62f, 0.68f);

            var groundGo = new GameObject("Lab Ground", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider), typeof(LabTerrain));
            var terrain = groundGo.GetComponent<LabTerrain>();
            terrain.config = config;
            terrain.Generate();
            groundGo.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            var waterGo = new GameObject("Water", typeof(MeshFilter), typeof(MeshRenderer), typeof(WaterGrid));
            var water = waterGo.GetComponent<WaterGrid>();
            water.config = config; water.terrain = terrain;
            var wr = waterGo.GetComponent<MeshRenderer>();
            wr.sharedMaterial = waterMat;
            wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var src = new GameObject("Water Source").AddComponent<WaterSource>();
            float cz = (terrain.ChannelZ0 + terrain.ChannelZ1) * 0.5f;
            src.transform.position = new Vector3(1.5f, terrain.ChannelBed(1.5f) + 0.5f, cz);
            src.rate = 3f; src.radius = 1.5f;

            var parts = new GameObject("Parts").transform;
            var debris = new GameObject("Debris").transform;

            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 45f; cam.nearClipPlane = 0.2f; cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.75f, 0.85f);
            var rig = camGo.AddComponent<CameraRig>();
            rig.cam = cam; rig.terrain = terrain;
            rig.focus = new Vector3(terrain.LengthX * 0.5f, 0f, terrain.WidthZ * 0.5f);
            rig.distance = 30f; rig.pitch = 55f;

            var lines = new GameObject("Debug Lines").AddComponent<DebugLines>();
            lines.material = lineMat;

            var lab = new GameObject("Lab").AddComponent<LabController>();
            lab.config = config; lab.water = water; lab.terrain = terrain; lab.source = src; lab.cam = cam;
            lab.partsRoot = parts; lab.debrisRoot = debris;
            lab.options = new[]
            {
                Option("Wood block (floats)", block, wood),
                Option("Stone block (sinks)", block, stone),
                Option("Mud block (sinks, sealed bottom)", block, mud),
                Option("Wood log", log, wood),
                Option("Wood log standing", log, wood, standUp: true),
                Option("Stone ramp", ramp, stone),
                Option("Fixed wall block (measures push)", block, stone, wall: true),
            };

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildScenes(ScenePath);
            Debug.Log("Water lab built: " + ScenePath);
        }

        static LabController.SpawnOption Option(string label, ShapeDef s, MaterialDef m, bool wall = false, bool standUp = false) =>
            new LabController.SpawnOption { label = label, shape = s, material = m, fixedWall = wall, standUp = standUp };

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

        static MaterialDef MaterialAsset(string name, Color color, float density, float friction, float joint, float tough, float cost, bool mud)
        {
            var mat = Lit("M_DR_" + name, color);
            var def = Asset<MaterialDef>(Root + "/Data/Mat_" + name + ".asset", d =>
            {
                d.displayName = name; d.color = color; d.density = density; d.friction = friction;
                d.jointStrength = joint; d.toughness = tough; d.costPerCubicMetre = cost;
                d.erodes = mud; d.gluesToGround = mud;
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

        /// <summary>Lit particle shader: it multiplies the mesh vertex colours (depth/speed colours, checker floor).</summary>
        static Material VertexColour(string name, bool transparent)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var shader = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_BaseColor", Color.white);
            if (transparent)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material LineMaterial()
        {
            string path = Root + "/Materials/M_DR_DebugLines.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(m, path); }
            m.renderQueue = 4000; // draw on top of the water
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>The lab replaces the Cycle 1 scene in the build list.</summary>
        static void SetBuildScenes(string path)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path.EndsWith("DamRush.unity"));
            if (!list.Exists(s => s.path == path)) list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
