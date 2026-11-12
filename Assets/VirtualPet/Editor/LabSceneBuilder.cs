using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using WindPet.EditorTools;

namespace WindPet.EditorTools
{
    /// <summary>
    /// Imports the lab exported from Blender (Assets/VirtualPet/Art/Lab) and sets it up:
    /// - URP materials: baked colour + normal + emission, transparent glass, ink outline
    /// - static parts (desk, wall, shelf frame, window, wall decor, item machine):
    ///   mesh colliders on the "Environment" layer, used as surfaces to carry props on
    /// - every prop in the *Items / Props files: Rigidbody + box collider on the
    ///   "Props" layer, mass from its size; the lantern hangs on a hinge
    /// - an ItemGrabber so props can be hovered (white outline) and carried
    /// Menu: Wind Pet > Build Lab In Scene
    /// </summary>
    public static class LabSceneBuilder
    {
        const string ArtDir = "Assets/VirtualPet/Art/Lab";
        const string TexDir = ArtDir + "/Textures";
        const string MatDir = ArtDir + "/Materials";
        static readonly HashSet<string> MovableFiles = new HashSet<string> { "Lab_ShelfItems", "Lab_DeskItems", "Lab_Props" };
        const float Density = 300f;   // kg per m3, light cartoon props

        [MenuItem("Wind Pet/Build Lab In Scene")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder(ArtDir, "Materials");
            SetupTextures();
            int propsLayer = LayerMask.NameToLayer("Props");
            int envLayer = LayerMask.NameToLayer("Environment");

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var glass = GlassMaterial(lit);
            var ink = Shader.Find("WindPet/InkOutline");
            var outline = OutlineMaterial(ink, "M_InkOutline", new Color(0.17f, 0.1f, 0.06f), 0.012f);
            var hover = OutlineMaterial(ink, "M_InkOutlineHover", Color.white, 0.025f);

            var old = GameObject.Find("Lab");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("Lab");
            // Blender (x, y, z) arrives as Unity (-x, z, -y); turning the root
            // 180 degrees around Y gives (x, z, y), the same layout as in Blender.
            root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ArtDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                instance.name = file;
                var main = BakedMaterial(lit, file);

                foreach (var r in instance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    bool isOutline = r.name.EndsWith("_Outline");
                    r.sharedMaterial = isOutline ? outline : r.name.EndsWith("_Glass") ? glass : main;
                    r.shadowCastingMode = isOutline ? ShadowCastingMode.Off : ShadowCastingMode.On;
                }

                if (MovableFiles.Contains(file))
                {
                    var items = new List<Transform>();
                    foreach (Transform child in instance.transform) items.Add(child);
                    foreach (var item in items) SetupProp(item.gameObject, propsLayer);
                }
                else if (file == "Lab_Work")
                {
                    // the work desk: stamp and ink pad are props the player picks up,
                    // the turbine and the printer stand still, the rotor only spins
                    var items = new List<Transform>();
                    foreach (Transform child in instance.transform) items.Add(child);
                    foreach (var item in items)
                    {
                        if (item.name == "Item_Stamp" || item.name == "Item_InkPad") SetupProp(item.gameObject, propsLayer);
                        else if (item.name != "Item_TurbineRotor") AddStaticColliders(item.gameObject, envLayer);
                    }
                }
                else AddStaticColliders(instance, envLayer);
            }

            ArrangeDesk(root.transform);
            FixShelfBoards(root.transform, envLayer);
            AddDeskSlab(root.transform, envLayer);
            TidyDesk(root.transform);
            FitShelfItems(root.transform, envLayer);
            SetupCauldronFX(root.transform);
            SetupViewWalls(propsLayer);
            SetupGrabber(hover, propsLayer, envLayer);
            WorkSetup.Setup(root.transform);                  // turbine, printer, pachinko slot, power
            WindPet.EditorTools.ItemUIBuilder.SetupTools();   // fan, dryer and stamp are tools
            AssetDatabase.SaveAssets();
            Debug.Log("Lab built in scene.");
        }

        /// <summary>
        /// Layout changes made after the Blender export (no re-bake needed): the
        /// pachinko machine comes closer to the work desk, and the two props that
        /// would stand inside it move aside. Values are world-space offsets.
        /// </summary>
        static void ArrangeDesk(Transform root)
        {
            Move(root, "Lab_Pachinko", WorkSetup.PachinkoOffset);
            Move(root, "Lab_DeskItems/Item_PottedPlant", new Vector3(-1.5f, 0f, -1.2f));
            Move(root, "Lab_DeskItems/Item_BrassApparatus", new Vector3(-0.5f, 0f, 0f));
        }

        // Small desk props that go up onto the shelves, so the desk has room for the
        // work tools. (x, shelf board, turn 90 degrees to fit the gap)
        // Boards: 0 = bottom (left of the cauldron), 1 = middle, 2 = top.
        static readonly (string name, float x, int board, bool turn)[] ShelfMoves =
        {
            ("Item_TestTubeRack", -7.1f,  0, false),
            ("Item_RoundFlask",   -5.95f, 0, false),
            ("Item_Erlen",        -5.05f, 0, false),
            ("Item_InkBottle",    -4.4f,  0, false),
            ("Item_Tall",         -4.05f, 1, false),
            ("Item_Magnifier",    -5.75f, 1, true),
            ("Item_DeskBook0",    -1.55f, 2, false),
            ("Item_DeskBook1",     1.15f, 2, true),
        };
        static readonly List<Transform> Tidied = new List<Transform>();
        static readonly float[] BoardTops = { 0.08f, 1.16f, 2.23f };
        const float ShelfZ = 4.95f;

        /// <summary>
        /// Moves the props in ShelfMoves from the desk onto the shelves. FitShelfItems
        /// then scales them to fit and stands them on the board.
        /// </summary>
        static void TidyDesk(Transform root)
        {
            var desk = root.Find("Lab_DeskItems");
            var shelf = root.Find("Lab_ShelfItems");
            Tidied.Clear();
            if (desk == null || shelf == null) return;
            foreach (var m in ShelfMoves)
            {
                var item = desk.Find(m.name);
                if (item == null) { Debug.LogWarning("TidyDesk: not found " + m.name); continue; }
                if (m.turn) item.RotateAround(WorldBounds(item).center, Vector3.up, 90f);
                Bounds b = WorldBounds(item);
                item.position += new Vector3(m.x - b.center.x, BoardTops[m.board] + 0.03f - b.min.y, ShelfZ - b.center.z);
                Tidied.Add(item);   // stays in its prefab, but is fitted like a shelf item
            }
        }

        static void Move(Transform root, string path, Vector3 offset)
        {
            var t = root.Find(path);
            if (t != null) t.position += offset;
            else Debug.LogWarning("ArrangeDesk: not found " + path);
        }

        static void AddStaticColliders(GameObject go, int envLayer)
        {
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.name.EndsWith("_Outline") || mf.name.EndsWith("_Glass")) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mf.gameObject.layer = envLayer;
            }
        }

        static void SetupProp(GameObject item, int layer)
        {
            foreach (var t in item.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;

            var renderers = item.GetComponentsInChildren<Renderer>();
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);

            var box = item.AddComponent<BoxCollider>();
            box.center = item.transform.InverseTransformPoint(b.center);
            Vector3 s = item.transform.InverseTransformVector(b.size);
            box.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)) * 0.96f;

            var rb = item.AddComponent<Rigidbody>();
            rb.mass = Mathf.Clamp(b.size.x * b.size.y * b.size.z * Density, 0.1f, 15f);
            rb.linearDamping = 0.2f;
            rb.angularDamping = 0.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            item.AddComponent<WindPet.PropBody>();

            if (item.name.StartsWith("Item_Lantern"))
            {
                // hangs from the top of its chain and can swing in the wind
                var hinge = item.AddComponent<HingeJoint>();
                hinge.anchor = Vector3.zero;
                hinge.axis = item.transform.InverseTransformDirection(Vector3.forward);
                hinge.useLimits = true;
                hinge.limits = new JointLimits { min = -40f, max = 40f };
                hinge.useSpring = true;
                hinge.spring = new JointSpring { spring = 2f, damper = 0.2f, targetPosition = 0f };
                rb.mass = 1f;
            }
        }

        /// <summary>
        /// The desk top is one thin mesh collider, so a fast prop can slip through it.
        /// A thick box just under the surface catches anything that gets past.
        /// </summary>
        static void AddDeskSlab(Transform root, int envLayer)
        {
            var desk = root.Find("Lab_Desk");
            if (desk == null) return;
            Bounds b = WorldBounds(desk);
            float top = -1f;
            Physics.SyncTransforms();
            if (Physics.Raycast(new Vector3(0f, 3f, 0f), Vector3.down, out RaycastHit h, 10f, 1 << envLayer)) top = h.point.y;
            var slab = new GameObject("DeskSlab");
            slab.layer = envLayer;
            slab.transform.SetParent(root, true);   // the root has scale 1, the desk model may not
            slab.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var box = slab.AddComponent<BoxCollider>();
            const float thick = 2f;
            box.center = new Vector3(b.center.x, top - 0.015f - thick / 2f, b.center.z);
            box.size = new Vector3(b.size.x, thick, b.size.z);
        }

        /// <summary>
        /// Thin shelf boards inside one big mesh collider let props sink in, so the
        /// boards get simple box colliders instead (measured from the mesh first).
        /// </summary>
        static void FixShelfBoards(Transform root, int envLayer)
        {
            var frame = root.Find("Lab_ShelfFrame");
            if (frame == null) return;
            Physics.SyncTransforms();
            var holder = new GameObject("BoardColliders");
            holder.transform.SetParent(frame, false);
            holder.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            holder.layer = envLayer;
            foreach (var y0 in new[] { 0.6f, 2.0f, 3.2f })
            {
                if (!Physics.Raycast(new Vector3(-3.5f, y0, 4.9f), Vector3.down, out RaycastHit h, 1.5f, 1 << envLayer)) continue;
                var bc = holder.AddComponent<BoxCollider>();
                bc.center = new Vector3(-3.5f, h.point.y - 0.1f, 5.05f);
                bc.size = new Vector3(10.4f, 0.2f, 1.1f);
            }
            foreach (var mc in frame.GetComponentsInChildren<MeshCollider>()) Object.DestroyImmediate(mc);
        }

        /// <summary>
        /// Props taller than the gap between two shelf boards would be pushed out by
        /// physics, so they are scaled down to fit and placed back on their board.
        /// </summary>
        static void FitShelfItems(Transform root, int envLayer)
        {
            var items = root.Find("Lab_ShelfItems");
            if (items == null) return;
            Physics.SyncTransforms();
            int mask = 1 << envLayer;
            var all = new List<Transform>(Tidied);
            foreach (Transform item in items) all.Add(item);
            foreach (var item in all)
            {
                Bounds b = WorldBounds(item);
                Vector3 probe = new Vector3(b.center.x, b.min.y + 0.05f, b.center.z);
                if (!Physics.Raycast(probe, Vector3.down, out RaycastHit floor, 0.5f, mask)) continue;
                if (!Physics.Raycast(probe, Vector3.up, out RaycastHit ceiling, 3f, mask)) continue;
                float space = ceiling.point.y - floor.point.y - 0.06f;
                if (b.size.y > space)
                {
                    float f = space / b.size.y;
                    item.localScale *= f;
                    Physics.SyncTransforms();
                    b = WorldBounds(item);
                }
                // stand exactly on the board
                item.position += Vector3.up * (floor.point.y + 0.005f - b.min.y);
            }
        }

        static Bounds WorldBounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        // ---------------- Cauldron bubbles and steam (particles, WebGL safe) ----------------
        const string FxDir = "Assets/VirtualPet/Art/FX";

        static void SetupCauldronFX(Transform root)
        {
            var pot = root.Find("Lab_DeskItems/Item_Cauldron");
            if (pot == null) return;
            var bubbleMat = FxMaterial("M_Bubble", FxTexture("T_Bubble", BubblePixel), 1.6f);
            var steamMat = FxMaterial("M_Steam", FxTexture("T_SoftBlob", BlobPixel), 1.0f);

            Bounds b = pot.GetComponent<Renderer>().bounds;
            Vector3 surface = new Vector3(b.center.x, b.max.y - 0.09f, b.center.z);   // brew sits just under the rim
            float radius = b.extents.x * 0.55f;
            Color brew = new Color(0.62f, 1f, 0.35f);

            // 1) bubbles that rise out of the pot, wobble and pop
            var rise = FxSystem(pot, "FX_RisingBubbles", surface, bubbleMat);
            var m = rise.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.17f);
            m.startColor = brew;
            m.gravityModifier = -0.04f;            // light, so it floats up a bit faster over time
            m.maxParticles = 40;
            var e = rise.emission; e.rateOverTime = 5f;
            var sh = rise.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radius * 0.8f;
            sh.rotation = new Vector3(-90, 0, 0);
            var n = rise.noise; n.enabled = true; n.strength = 0.25f; n.frequency = 1.2f; n.scrollSpeed = 0.5f;
            SizePop(rise, 0.5f, 1.25f);
            FadeInOut(rise, 1f, 0.05f, 0.92f);

            // 2) bubbles that swell on the surface and burst (the "boiling" look)
            var boil = FxSystem(pot, "FX_SurfaceBubbles", surface + Vector3.up * 0.01f, bubbleMat);
            m = boil.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            m.startSpeed = 0f;
            m.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            m.startColor = brew;
            m.maxParticles = 30;
            e = boil.emission; e.rateOverTime = 9f;
            sh = boil.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radius;
            sh.rotation = new Vector3(-90, 0, 0);
            SizePop(boil, 0.2f, 1.3f);
            FadeInOut(boil, 1f, 0.15f, 0.9f);

            // 3) faint green steam, drawn behind the bubbles
            var steam = FxSystem(pot, "FX_Steam", surface + Vector3.up * 0.05f, steamMat);
            m = steam.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.4f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            m.startColor = new Color(0.75f, 1f, 0.6f);
            m.maxParticles = 20;
            e = steam.emission; e.rateOverTime = 3f;
            sh = steam.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radius * 0.6f;
            sh.rotation = new Vector3(-90, 0, 0);
            n = steam.noise; n.enabled = true; n.strength = 0.12f; n.frequency = 0.5f;
            var rot = steam.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            var sz = steam.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(1, 2.2f)));
            FadeInOut(steam, 0.18f, 0.25f, 0.5f);
            steam.GetComponent<ParticleSystemRenderer>().sortingFudge = 10;   // draw before the bubbles

            var brewFx = pot.GetComponent<WindPet.CauldronBrew>();
            if (brewFx == null) brewFx = pot.gameObject.AddComponent<WindPet.CauldronBrew>();
            brewFx.effects = new[] { rise, boil, steam };
        }

        static ParticleSystem FxSystem(Transform parent, string name, Vector3 worldPos, Material mat)
        {
            var old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.identity;
            var ps = go.AddComponent<ParticleSystem>();
            var m = ps.main;
            m.simulationSpace = ParticleSystemSimulationSpace.World;   // bubbles stay behind if the pot moves
            m.scalingMode = ParticleSystemScalingMode.Local;
            m.prewarm = true;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        // grow from 'start' to full size, then swell to 'popSize' right before vanishing
        static void SizePop(ParticleSystem ps, float start, float popSize)
        {
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, start), new Keyframe(0.25f, 1f), new Keyframe(0.9f, 1.05f), new Keyframe(1f, popSize)));
        }

        static void FadeInOut(ParticleSystem ps, float alpha, float inEnd, float outStart)
        {
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(alpha, inEnd),
                              new GradientAlphaKey(alpha, outStart), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        static Material FxMaterial(string name, Texture2D tex, float boost)
        {
            string path = $"{FxDir}/{name}.mat";
            var shader = Shader.Find("WindPet/SoftParticle");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", tex);
            mat.SetFloat("_Boost", boost);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        delegate Color PixelFn(float x, float y);

        static Texture2D FxTexture(string name, PixelFn fn)
        {
            string path = $"{FxDir}/{name}.png";
            if (!File.Exists(path))
            {
                const int size = 128;
                var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        t.SetPixel(x, y, fn((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f));
                File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.mipmapEnabled = true;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // bubble: see-through middle, bright rim, small white shine at the top left
        static Color BubblePixel(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r > 1f) return Color.clear;
            float fill = 0.18f + 0.25f * r * r;
            float rim = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 0.92f, r)) * (1f - Mathf.InverseLerp(0.94f, 1f, r));
            float sx = x + 0.35f, sy = y - 0.35f;
            float shine = 1f - Mathf.Clamp01(Mathf.Sqrt(sx * sx + sy * sy) / 0.22f);
            // the particle colour tints everything, so the shine is just the brightest, most solid spot
            float a = Mathf.Clamp01(fill + rim * 0.85f + shine);
            return new Color(1f, 1f, 1f, a);
        }

        // soft round puff for the steam
        static Color BlobPixel(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Clamp01(1f - r);
            return new Color(1f, 1f, 1f, a * a * (3f - 2f * a));
        }

        static void SetupViewWalls(int propsLayer)
        {
            var go = GameObject.Find("Interaction");
            if (go == null) go = new GameObject("Interaction");
            foreach (Transform old in go.transform)
                if (old.name.StartsWith("ViewWall")) Object.DestroyImmediate(old.gameObject);
            var keep = go.GetComponent<WindPet.PropViewportKeeper>();
            if (keep == null) keep = go.AddComponent<WindPet.PropViewportKeeper>();
            keep.cam = Camera.main;
            keep.propLayers = 1 << propsLayer;
        }

        static void SetupGrabber(Material hover, int propsLayer, int envLayer)
        {
            var go = GameObject.Find("Interaction");
            if (go == null) go = new GameObject("Interaction");
            var grabber = go.GetComponent<WindPet.ItemGrabber>();
            if (grabber == null) grabber = go.AddComponent<WindPet.ItemGrabber>();
            grabber.cam = Camera.main;
            grabber.itemLayers = 1 << propsLayer;
            grabber.surfaceLayers = 1 << envLayer;
            grabber.hoverOutline = hover;
            var petGo = GameObject.Find("WindPet");
            if (petGo != null) grabber.pet = petGo.GetComponent<WindPet.PetDrag>();
        }

        static void SetupTextures()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                bool normal = path.EndsWith("_Normal.png");
                bool changed = false;
                if (normal && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; changed = true; }
                if (ti.maxTextureSize != 2048) { ti.maxTextureSize = 2048; changed = true; }
                if (changed) ti.SaveAndReimport();
            }
        }

        static Material BakedMaterial(Shader lit, string group)
        {
            string path = $"{MatDir}/M_{group}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); }
            var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/{group}_BaseColor.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/{group}_Normal.png");
            var emit = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/{group}_Emission.png");
            m.SetTexture("_BaseMap", baseTex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.35f);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            if (emit != null)
            {
                m.SetTexture("_EmissionMap", emit);
                m.SetColor("_EmissionColor", Color.white);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material GlassMaterial(Shader lit)
        {
            string path = $"{MatDir}/M_Glass.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); }
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetColor("_BaseColor", new Color(0.88f, 0.97f, 0.93f, 0.28f));
            m.SetFloat("_Smoothness", 0.95f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material OutlineMaterial(Shader shader, string name, Color color, float width)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Width", width);     // extra thickness on top of the hull from Blender
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
