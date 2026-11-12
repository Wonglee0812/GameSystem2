using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace WindPet.EditorTools
{
    /// <summary>
    /// One-time setup for the item system UI (menu: Wind Pet/...).
    /// "Create Item UI" only adds what is missing, so it never overwrites UI
    /// that was changed by hand afterwards.
    /// "Render Tool Icons" photographs each tool prop into a transparent PNG.
    /// </summary>
    public static class ItemUIBuilder
    {
        const string ShapeDir = "Assets/VirtualPet/UI/Shapes";
        const string IconDir = "Assets/VirtualPet/UI/Icons/Tools";

        [MenuItem("Wind Pet/Create Item UI")]
        public static void CreateItemUI()
        {
            var canvas = GameObject.Find("StatusCanvas");
            if (canvas == null) { Debug.LogError("StatusCanvas not found"); return; }
            Directory.CreateDirectory(ShapeDir);
            var diamond = ShapeSprite("RoundedDiamond", RoundedDiamond);
            var circle = ShapeSprite("SoftCircle", Circle);

            // 1) soft rounded diamond behind the four status icons
            var needs = canvas.transform.Find("NeedsDiamond");
            if (needs != null && needs.Find("Backing") == null)
            {
                var img = NewImage("Backing", needs, diamond, new Color(0f, 0f, 0f, 0.28f));
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(340f, 340f);
                rt.SetAsFirstSibling();
                img.raycastTarget = false;
            }

            // 2) dark round item slot in the bottom-right corner
            var slot = canvas.transform.Find("ItemSlot");
            if (slot == null)
            {
                var img = NewImage("ItemSlot", canvas.transform, circle, new Color(0f, 0f, 0f, 0.45f));
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-40f, 40f);
                rt.sizeDelta = new Vector2(150f, 150f);
                slot = rt;
                var icon = NewImage("Icon", slot, null, Color.white);
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                icon.rectTransform.sizeDelta = new Vector2(110f, 110f);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            // 3) small copy of the tool icon that follows the cursor
            var cursor = canvas.transform.Find("ToolCursor");
            if (cursor == null)
            {
                var img = NewImage("ToolCursor", canvas.transform, null, Color.white);
                img.rectTransform.sizeDelta = new Vector2(72f, 72f);
                img.preserveAspect = true;
                img.raycastTarget = false;
                cursor = img.rectTransform;
            }
            cursor.SetAsLastSibling();

            // the slot is clicked, so the scene needs an EventSystem (new Input System module)
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(es, "EventSystem");
            }

            // wire the ToolBelt
            var belt = slot.GetComponent<ToolBelt>();
            if (belt == null) belt = slot.gameObject.AddComponent<ToolBelt>();
            belt.cam = Camera.main;
            belt.slotIcon = slot.Find("Icon").GetComponent<Image>();
            belt.cursorIcon = (RectTransform)cursor;
            belt.surfaceLayers = 1 << LayerMask.NameToLayer("Environment");
            belt.pet = Object.FindAnyObjectByType<PetDrag>();
            belt.stats = Object.FindAnyObjectByType<PetStats>();
            belt.puffMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/VirtualPet/Art/FX/M_Steam.mat");
            EditorUtility.SetDirty(belt);

            // the pet eats the puffs
            var pet = belt.pet;
            if (pet != null)
            {
                var feed = pet.GetComponent<PetFeeding>();
                if (feed == null) feed = pet.gameObject.AddComponent<PetFeeding>();
                feed.drag = pet;
                feed.wander = pet.GetComponent<PetWander>();
                feed.fsm = Object.FindAnyObjectByType<PetStateMachine>();
                EditorUtility.SetDirty(feed);
            }
            SetupTools();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.scene);
        }

        /// <summary>
        /// Every physics prop can be held (except the lantern, which hangs on a joint).
        /// The fan and the hair dryer are tools. Also called by the lab builder.
        /// </summary>
        public static void SetupTools()
        {
            var work = WorkSetup.Config();
            foreach (var prop in Object.FindObjectsByType<PropBody>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var go = prop.gameObject;
                if (go.GetComponent<Joint>() != null) continue;
                // which kind of holdable: food tools, the stamp, or a plain item (thrown)
                System.Type kind = go.name == "Item_QuatMo" || go.name == "Item_HairDryer" ? typeof(ToolItem)
                                 : go.name == "Item_Stamp" ? typeof(StampTool) : typeof(HoldableItem);
                var item = go.GetComponent<HoldableItem>();
                if (item != null && item.GetType() != kind) { Object.DestroyImmediate(item); item = null; }
                if (item == null) item = (HoldableItem)go.AddComponent(kind);
                item.displayName = Nice(go.name);
                item.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{IconDir}/{go.name}.png");
                if (item is ToolItem tool)
                {
                    bool fan = go.name == "Item_QuatMo";
                    tool.food = fan ? 12f : 24f;              // paid food fills more than free food
                    tool.temperatureChange = fan ? -6f : 6f;
                    tool.powerPerPuff = fan ? 0f : 0.5f;      // the dryer runs on electricity...
                    tool.costPerPuff = fan ? 0f : 3f;         // ...or costs money when there is none
                }
                if (item is StampTool stamp) stamp.config = work;
                if (go.name == "Item_InkPad" && go.GetComponent<InkPad>() == null) go.AddComponent<InkPad>();
                EditorUtility.SetDirty(item);
            }
        }

        static string Nice(string objectName)
        {
            string n = objectName.StartsWith("Item_") ? objectName.Substring(5) : objectName;
            return n == "QuatMo" ? "Palm-leaf fan" : n == "HairDryer" ? "Hair dryer" : n == "InkPad" ? "Ink pad" : n;
        }

        // ---------- Tool icons ----------

        [MenuItem("Wind Pet/Render Item Icons")]
        public static void RenderToolIcons()
        {
            Directory.CreateDirectory(IconDir);
            const int size = 256;
            const int layer = 31;   // unused layer, so only the copy is in the photo
            var camGo = new GameObject("IconCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.cullingMask = 1 << layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = 22f;
            cam.enabled = false;
            // own soft key light from the front, so icons are bright and readable
            var lightGo = new GameObject("IconLight");
            var keyLight = lightGo.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.6f;
            keyLight.cullingMask = 1 << layer;
            lightGo.transform.rotation = Quaternion.Euler(35f, -25f, 0f);
            var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            foreach (var prop in Object.FindObjectsByType<PropBody>(FindObjectsSortMode.None))
            {
                var src = prop.gameObject;
                if (src.GetComponent<Joint>() != null) continue;
                string name = src.name;
                var copy = Object.Instantiate(src);
                // scripts first: some of them need the Rigidbody
                foreach (var c in copy.GetComponentsInChildren<MonoBehaviour>()) Object.DestroyImmediate(c);
                foreach (var c in copy.GetComponentsInChildren<Joint>()) Object.DestroyImmediate(c);
                foreach (var c in copy.GetComponentsInChildren<Rigidbody>()) Object.DestroyImmediate(c);
                foreach (var tr in copy.GetComponentsInChildren<Transform>()) tr.gameObject.layer = layer;
                copy.transform.position = new Vector3(0f, -500f, 0f);
                copy.transform.rotation = Quaternion.Euler(0f, 20f, 0f) * src.transform.rotation;   // a little 3/4 view

                Bounds b = copy.GetComponentInChildren<Renderer>().bounds;
                foreach (var r in copy.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                float dist = b.extents.magnitude / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
                cam.transform.position = b.center + new Vector3(0f, 0.25f, -1f).normalized * dist;
                cam.transform.LookAt(b.center);

                // two photos (black and white background) give a clean alpha channel
                var black = Capture(cam, rt, Color.black);
                var white = Capture(cam, rt, Color.white);
                var outTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pb = black.GetPixels(); var pw = white.GetPixels(); var po = new Color[pb.Length];
                for (int i = 0; i < pb.Length; i++)
                {
                    float a = Mathf.Clamp01(1f - (pw[i].r - pb[i].r + pw[i].g - pb[i].g + pw[i].b - pb[i].b) / 3f);
                    po[i] = a > 0.001f ? new Color(pb[i].r / a, pb[i].g / a, pb[i].b / a, a) : Color.clear;
                }
                outTex.SetPixels(po);
                string path = $"{IconDir}/{name}.png";
                File.WriteAllBytes(path, outTex.EncodeToPNG());
                Object.DestroyImmediate(copy);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
            cam.targetTexture = null;
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(lightGo);
            rt.Release();
            SetupTools();
        }

        static Texture2D Capture(Camera cam, RenderTexture rt, Color background)
        {
            cam.backgroundColor = background;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }

        // ---------- Shapes ----------

        delegate float ShapeFn(float x, float y);   // signed distance, inside < 0

        static Sprite ShapeSprite(string name, ShapeFn fn)
        {
            string path = $"{ShapeDir}/{name}.png";
            if (!File.Exists(path))
            {
                const int size = 256;
                var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float px = 2f / size;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float d = fn((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                        float a = Mathf.Clamp01(0.5f - d / (px * 1.5f));    // soft 1-2 px edge
                        t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static float Circle(float x, float y) => Mathf.Sqrt(x * x + y * y) - 0.97f;

        // square turned 45 degrees with rounded corners
        static float RoundedDiamond(float x, float y)
        {
            float u = (x + y) * 0.70710678f, v = (x - y) * 0.70710678f;
            const float half = 0.62f, radius = 0.12f;
            float qx = Mathf.Abs(u) - (half - radius), qy = Mathf.Abs(v) - (half - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, name);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }
    }
}
