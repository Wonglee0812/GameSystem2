using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using PokerTD.UI;

namespace PokerTD.EditorTools
{
    /// <summary>
    /// Puts the juice into the open Poker TD scene, so every part can be seen and edited:
    /// the Juice object (all switches), a banner text and a red vignette in the HUD,
    /// the ink background quad under the table and the model dissolve material. Menu: Poker TD/Build Juice In Scene. Safe to run twice.
    /// </summary>
    public static class PokerTDJuiceBuilder
    {
        const string Root = "Assets/Progression";

        [MenuItem("Poker TD/Build Juice In Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            var juiceGo = GameObject.Find("Juice");
            if (juiceGo == null) juiceGo = new GameObject("Juice");
            var juice = juiceGo.GetComponent<Juice>();
            if (juice == null) juice = juiceGo.AddComponent<Juice>();
            Undo.RecordObject(juice, "Juice");

            var hud = GameObject.Find("HUD");
            if (hud != null)
            {
                // red vignette over the whole screen (alpha 0 until the castle is hit)
                string vigPath = Root + "/Art/Resources/UI/Vignette.png";
                var imp = AssetImporter.GetAtPath(vigPath) as TextureImporter;
                if (imp != null && imp.textureType != TextureImporterType.Sprite)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                }
                var vig = Child(hud.transform, "JuiceVignette").gameObject;
                var img = vig.GetComponent<Image>();
                if (img == null) img = vig.AddComponent<Image>();
                img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(vigPath);
                img.color = new Color(1f, 0.12f, 0.08f, 0f);
                img.raycastTarget = false;
                var vr = (RectTransform)vig.transform;
                vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one; vr.offsetMin = vr.offsetMax = Vector2.zero;
                vig.transform.SetAsLastSibling();
                juice.vignette = img;

                // banner text in the middle of the screen
                var ban = Child(hud.transform, "JuiceBanner").gameObject;
                var t = ban.GetComponent<TextMeshProUGUI>();
                if (t == null) t = ban.AddComponent<TextMeshProUGUI>();
                t.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Art/Resources/Fonts/Action Comics Black SDF.asset");
                var outline = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Resources/Fonts/Action Comics Black SDF Outline.mat");
                if (outline != null) t.fontSharedMaterial = outline;
                t.text = "WAVE 1";
                t.fontSize = 110f;
                t.alignment = TextAlignmentOptions.Center;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.raycastTarget = false;
                t.color = new Color(1f, 0.85f, 0.3f, 0f);
                var br = (RectTransform)ban.transform;
                br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
                br.sizeDelta = new Vector2(1400f, 220f);
                br.anchoredPosition = new Vector2(0f, 140f);
                ban.transform.SetAsLastSibling();
                juice.banner = t;
            }

            // ink splat background under the table (port of Wong's Splatoon shader graph)
            var old = GameObject.Find("SwirlBackground");
            var sw = GameObject.Find("InkBackground");
            if (sw == null && old != null) { sw = old; sw.name = "InkBackground"; }
            if (sw == null)
            {
                sw = GameObject.CreatePrimitive(PrimitiveType.Quad);
                sw.name = "InkBackground";
                Object.DestroyImmediate(sw.GetComponent<Collider>());
            }
            sw.transform.position = new Vector3(0f, -14f, 6f);
            sw.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            sw.transform.localScale = new Vector3(260f, 260f, 1f);
            string matPath = Root + "/Materials/M_PTD_Ink.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(Shader.Find("PokerTD/InkBackground")); AssetDatabase.CreateAsset(mat, matPath); }
            var sr = sw.GetComponent<MeshRenderer>();
            sr.sharedMaterial = mat;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;
            juice.background = sr;

            // dissolve material for 3D models (Resources, so it is in the build)
            string dPath = Root + "/Art/Resources/M_ModelDissolve.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(dPath) == null)
                AssetDatabase.CreateAsset(new Material(Shader.Find("PokerTD/ModelDissolve")), dPath);

            var castle = GameObject.Find("Castle");
            if (castle != null) juice.castle = castle.transform;
            juice.worldHud = Object.FindFirstObjectByType<WorldHud>();
            EditorUtility.SetDirty(juice);
            EditorSceneManager.MarkSceneDirty(juiceGo.scene);
            Debug.Log("Poker TD: juice built into the scene. Save the scene to keep it.");
        }

        static Transform Child(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t;
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return go.transform;
        }
    }
}
