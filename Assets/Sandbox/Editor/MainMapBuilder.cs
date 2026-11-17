using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DamRush.EditorTools
{
    /// <summary>
    /// Builds the rough main map: forest valley with a main stream and three side streams,
    /// water, a hex grid over the whole map, trees and the 2.5D camera.
    /// Menu: Dam Rush/Build Main Map. The config asset is only created when missing.
    /// </summary>
    public static class MainMapBuilder
    {
        const string Root = "Assets/Sandbox";
        const string ScenePath = Root + "/Scenes/MainMap.unity";

        [MenuItem("Dam Rush/Build Main Map")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Root + "/Data")) AssetDatabase.CreateFolder(Root, "Data");
            var config = AssetDatabase.LoadAssetAtPath<SandboxConfig>(Root + "/Data/MainMapConfig.asset");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<SandboxConfig>();
                config.cellsX = 96; config.cellsZ = 72; config.cellSize = 1f;
                config.substeps = 1; config.tickEvery = 2; config.pipeArea = 1f;
                config.friction = FrictionMode.Manning; config.manningN = 0.035f;
                config.openEndX = true; config.displaceWater = true;
                AssetDatabase.CreateAsset(config, Root + "/Data/MainMapConfig.asset");
            }
            // plan v2: bigger map for the new buildings (128 x 96 m); stream points are scaled with it
            config.cellsX = 128; config.cellsZ = 96;
            EditorUtility.SetDirty(config);

            var groundMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/M_DR_LabGround.mat");
            var waterMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/M_DR_LabWater.mat");
            var lineMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/M_DR_DebugLines.mat");
            var hexMat = HexMaterial();
            if (groundMat == null || waterMat == null || lineMat == null)
            {
                Debug.LogError("Build the Water Lab once first (Dam Rush/Build Water Lab): it creates the shared materials.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.15f; sun.shadows = LightShadows.Soft;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.transform.rotation = Quaternion.Euler(52f, -40f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.58f, 0.6f);

            // ground with streams
            var groundGo = new GameObject("Forest Ground", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider), typeof(ForestTerrain));
            var terrain = groundGo.GetComponent<ForestTerrain>();
            terrain.config = config;
            terrain.streams.Add(Stream("Main stream", 1.5f, 9f, 1f, -1,
                new Vector2(0, 36), new Vector2(12, 40), new Vector2(24, 34), new Vector2(36, 38), new Vector2(48, 32),
                new Vector2(60, 37), new Vector2(72, 33), new Vector2(84, 36), new Vector2(96, 35)));
            terrain.streams.Add(Stream("North stream", 1f, 6f, 0.8f, 0,
                new Vector2(26, 72), new Vector2(29, 62), new Vector2(33, 52), new Vector2(37, 44), new Vector2(40, 37.5f)));
            terrain.streams.Add(Stream("South stream", 1f, 6f, 0.8f, 0,
                new Vector2(50, 0), new Vector2(54, 10), new Vector2(57, 20), new Vector2(60, 30), new Vector2(61, 36)));
            terrain.streams.Add(Stream("East brook", 0.8f, 5f, 0.7f, 0,
                new Vector2(82, 72), new Vector2(80, 62), new Vector2(77, 52), new Vector2(76, 44), new Vector2(74, 34)));
            terrain.Generate();
            groundGo.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // water
            var waterGo = new GameObject("Water", typeof(MeshFilter), typeof(MeshRenderer), typeof(WaterGrid));
            var water = waterGo.GetComponent<WaterGrid>();
            water.config = config; water.terrain = terrain;
            water.plainColor = new Color(0.22f, 0.42f, 0.55f, 0.78f);
            var wr = waterGo.GetComponent<MeshRenderer>();
            wr.sharedMaterial = waterMat;
            wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // stylised look (painted colours, white caustic lines carried by the flow)
            water.stylizedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/M_DR_StylizedWater.mat");
            water.shallowColor = new Color(0.2f, 0.72f, 0.82f, 0.7f);
            water.deepColor = new Color(0.05f, 0.45f, 0.72f, 0.9f);

            var sourcesRoot = new GameObject("Water Sources").transform;
            var sources = new[]
            {
                Source(sourcesRoot, terrain, "Main source", new Vector2(1.5f, 36.5f), 1.5f),
                Source(sourcesRoot, terrain, "North source", new Vector2(26.5f, 70.5f), 0.5f),
                Source(sourcesRoot, terrain, "South source", new Vector2(50.5f, 1.5f), 0.5f),
                Source(sourcesRoot, terrain, "East source", new Vector2(81.8f, 70.5f), 0.3f),
            };

            // hex grid
            var hexGo = new GameObject("Hex Grid", typeof(MeshFilter), typeof(MeshRenderer), typeof(HexGrid));
            var hex = hexGo.GetComponent<HexGrid>();
            hex.ground = terrain; hex.water = water; hex.size = 2f;
            hex.Build();
            var hr = hexGo.GetComponent<MeshRenderer>();
            hr.sharedMaterial = hexMat;
            hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var forest = PlantForest(terrain);

            // camera
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 40f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 400f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.7f, 0.82f);
            var rig = camGo.AddComponent<CameraRig>();
            rig.cam = cam; rig.terrain = terrain;
            rig.focus = new Vector3(terrain.LengthX * 0.5f, 0f, terrain.WidthZ * 0.5f);
            rig.distance = 55f; rig.pitch = 55f; rig.minDistance = 8f; rig.maxDistance = 110f;

            var lines = new GameObject("Debug Lines").AddComponent<DebugLines>();
            lines.material = lineMat;

            var ctrl = new GameObject("Map").AddComponent<MapController>();
            ctrl.water = water; ctrl.hex = hex; ctrl.cam = cam; ctrl.sources = sources;
            // a new random map at the start of every game (seed 0); the streams above are the edit-time preview
            var randomizer = ctrl.gameObject.AddComponent<MapRandomizer>();
            randomizer.terrain = terrain; randomizer.map = ctrl; randomizer.cameraRig = rig;

            // beaver family (spawned at play time around the main lodge)
            var colony = new GameObject("Colony").AddComponent<ColonyManager>();
            colony.grid = hex; colony.water = water; colony.ground = terrain; colony.forestRoot = forest;
            colony.preferredLodgeSpot = new Vector2(46f, 35f) * MapScale;
            colony.fur = Lit("M_DR_BeaverFur", new Color(0.42f, 0.28f, 0.17f));
            colony.tail = Lit("M_DR_BeaverTail", new Color(0.18f, 0.14f, 0.12f));
            colony.teeth = Lit("M_DR_BeaverTeeth", new Color(0.95f, 0.55f, 0.15f));
            colony.dark = Lit("M_DR_BeaverDark", new Color(0.08f, 0.06f, 0.05f));
            colony.lodgeMat = Lit("M_DR_Lodge", new Color(0.36f, 0.27f, 0.18f));
            colony.woodMat = Lit("M_DR_Trunk", new Color(0.33f, 0.24f, 0.16f));
            colony.mudMat = Lit("M_DR_Mud", new Color(0.36f, 0.27f, 0.18f));
            colony.stoneMat = Lit("M_DR_Stone", new Color(0.56f, 0.57f, 0.6f));
            var orders = colony.gameObject.AddComponent<BuildOrders>();
            orders.colony = colony; orders.grid = hex; orders.water = water; orders.cam = cam;
            orders.stoneMat = colony.stoneMat; orders.woodMat = colony.woodMat; orders.mudMat = colony.lodgeMat;
            colony.orders = orders;
            var dayNight = colony.gameObject.AddComponent<DayNight>();
            dayNight.sun = sun; dayNight.cam = cam;
            colony.dayNight = dayNight;
            ctrl.colony = colony;
            randomizer.colony = colony;
            var seasons = colony.gameObject.AddComponent<Seasons>();
            seasons.colony = colony; seasons.map = ctrl;
            colony.seasons = seasons;
            var hud = colony.gameObject.AddComponent<ColonyHud>();
            hud.colony = colony; hud.cam = cam; hud.orders = orders;

            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == ScenePath)) list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"Main map built: {ScenePath}  ({hex.Count} hexes, {hex.Cols} x {hex.Rows})");
        }

        /// <summary>The stream and source points were drawn for a 96 x 72 m map; plan v2 scales them to 128 x 96 m.</summary>
        const float MapScale = 128f / 96f;

        static StreamPath Stream(string name, float halfWidth, float valley, float depth, int joins, params Vector2[] pts)
        {
            for (int i = 0; i < pts.Length; i++) pts[i] *= MapScale;
            return new StreamPath { name = name, points = pts, halfWidth = halfWidth, valleyHalfWidth = valley, sourceDepth = depth, joins = joins };
        }

        static WaterSource Source(Transform parent, ForestTerrain t, string name, Vector2 p, float rate)
        {
            p *= MapScale;
            var s = new GameObject(name).AddComponent<WaterSource>();
            s.transform.SetParent(parent);
            s.transform.position = new Vector3(p.x, t.GroundAt(p.x, p.y) + 0.5f, p.y);
            s.rate = rate; s.radius = 1.5f;
            return s;
        }

        // ------------------------------------------------------------------ forest

        /// <summary>
        /// Scatters trees on a jittered 3 m grid. Broad-leaved trees (willow, birch) grow near the
        /// water, conifers further away. No trees in the stream beds. Decoration only for now.
        /// </summary>
        static Transform PlantForest(ForestTerrain t)
        {
            var root = new GameObject("Forest").transform;
            var trunk = Lit("M_DR_Trunk", new Color(0.33f, 0.24f, 0.16f));
            var birchBark = Lit("M_DR_BirchBark", new Color(0.88f, 0.86f, 0.8f));
            var leaf = Lit("M_DR_LeafWillow", new Color(0.45f, 0.62f, 0.25f));
            var leafBirch = Lit("M_DR_LeafBirch", new Color(0.58f, 0.7f, 0.3f));
            var pine = Lit("M_DR_Pine", new Color(0.16f, 0.32f, 0.2f));

            var rng = new System.Random(4321);
            float R() => (float)rng.NextDouble();
            int count = 0;
            for (float x = 1.5f; x < t.LengthX - 1f; x += 2.5f)
                for (float z = 1.5f; z < t.WidthZ - 1f; z += 2.5f)
                {
                    float px = x + (R() - 0.5f) * 2f, pz = z + (R() - 0.5f) * 2f;
                    float above = t.HeightAboveStream(px, pz, out float dist);
                    if (dist < 4f || above < 0.7f) continue;                       // keep stream banks open
                    float density = Mathf.PerlinNoise(px * 0.06f + 3f, pz * 0.06f + 7f);
                    if (R() > density * 1.6f - 0.1f) continue;                       // clearings where noise is low
                    float y = t.GroundAt(px, pz);
                    bool nearWater = dist < 12f;
                    var tree = new GameObject("Tree").transform;
                    tree.SetParent(root);
                    tree.position = new Vector3(px, y, pz);
                    tree.rotation = Quaternion.Euler(0f, R() * 360f, 0f);
                    float s = 0.8f + R() * 0.5f;
                    if (nearWater && R() < 0.75f)
                    {
                        bool birch = R() < 0.4f;
                        AddPiece(tree, PrimitiveType.Cylinder, birch ? birchBark : trunk, new Vector3(0, 1.2f * s, 0), new Vector3(0.25f, 1.2f * s, 0.25f));
                        AddPiece(tree, PrimitiveType.Sphere, birch ? leafBirch : leaf, new Vector3(0, 2.8f * s, 0), new Vector3(2.4f, 2f, 2.4f) * s);
                        tree.name = birch ? "Birch" : "Willow";
                    }
                    else
                    {
                        AddPiece(tree, PrimitiveType.Cylinder, trunk, new Vector3(0, 0.8f * s, 0), new Vector3(0.3f, 0.8f * s, 0.3f));
                        AddPiece(tree, PrimitiveType.Capsule, pine, new Vector3(0, 3f * s, 0), new Vector3(1.6f, 2.2f, 1.6f) * s);
                        tree.name = "Pine";
                    }
                    count++;
                }
            Debug.Log("Trees planted: " + count);
            return root;
        }

        static void AddPiece(Transform parent, PrimitiveType type, Material m, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
        }

        static Material Lit(string name, Color c)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.1f);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material HexMaterial()
        {
            string path = Root + "/Materials/M_DR_HexLines.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(m, path); }
            m.color = new Color(1f, 1f, 1f, 0.28f);
            m.renderQueue = 3100; // after the water, so lines stay visible in shallow water
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
