using PokerTD.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PokerTD.EditorTools
{
    /// <summary>
    /// Menu "Poker TD/Build New HUD In Scene": builds the new HUD (2026-11-24) as real scene
    /// objects, so it can be seen and edited in Edit mode:
    /// - WorldHUD (root object): the 3D texts on the table, only if it does not exist yet
    ///   (Wong places them by hand; "Rebuild World HUD" resets them and the camera);
    /// - on the HUD canvas: BuildFade + BuildCards (tower cards), ItemCards (empty slots), DeckPile in Hand,
    ///   Sort and Burst buttons, and every button styled (Visual child + PolishButton);
    /// - the old Status line and tower buttons are switched off (not deleted).
    /// Running it again rebuilds these parts (hand-made changes to them are lost).
    /// Run it again after "Poker TD/Build Prototype Scene", which rebuilds the whole scene.
    /// </summary>
    public static class PokerTDHudBuilder
    {
        const string FontFolder = "Assets/Progression/Art/Resources/Fonts/";

        [MenuItem("Poker TD/Rebuild World HUD (resets the 3D texts)")]
        public static void RebuildWorld()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            if (!EditorUtility.DisplayDialog("Rebuild World HUD", "This puts the 3D texts and the camera back to the default layout. Hand-made changes to WorldHUD are lost.", "Rebuild", "Cancel")) return;
            var world = Object.FindFirstObjectByType<WorldHud>(FindObjectsInactive.Include);
            if (world == null) world = new GameObject("WorldHUD").AddComponent<WorldHud>();
            world.Build();
            EditorUtility.SetDirty(world);
            EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
        }

        [MenuItem("Poker TD/Build New HUD In Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            var hud = Object.FindFirstObjectByType<TDHud>(FindObjectsInactive.Include);
            var hand = hud != null ? hud.GetComponent<HandView>() : null;
            var config = AssetDatabase.LoadAssetAtPath<TDConfig>("Assets/Progression/Data/TDConfig.asset");
            if (hud == null || hand == null || config == null) { Debug.LogError("[PokerTD] HUD, HandView or TDConfig not found."); return; }

            MakeOutlineMaterial("Action Comics Black SDF", 0.22f);
            MakeOutlineMaterial("Action Comics SDF", 0.22f);
            MakeOutlineMaterial("Super Chips SDF", 0.18f);

            // old parts off
            if (hud.status != null) hud.status.gameObject.SetActive(false);
            foreach (var b in hud.towerButtons) if (b != null) b.gameObject.SetActive(false);

            // buttons
            HudLayout.EnsureButtons(hud.transform, hand.discardButton, hud.startWave);
            HudLayout.Apply(hud.transform);

            // deck pile in the hand area
            var oldPile = hand.area.Find("DeckPile");
            if (oldPile != null) Object.DestroyImmediate(oldPile.gameObject);
            hand.BuildDeckPile();
            HudLayout.Apply(hud.transform);   // again, to style the new deck counter

            // card bars
            var build = hud.GetComponent<BuildCardBar>();
            if (build == null) build = hud.gameObject.AddComponent<BuildCardBar>();
            build.Build(config);
            var items = hud.GetComponent<ItemCardBar>();
            if (items == null) items = hud.gameObject.AddComponent<ItemCardBar>();
            items.Build(config.itemSlots);
            var layout = hud.GetComponent<HudLayout>();
            if (layout == null) layout = hud.gameObject.AddComponent<HudLayout>();
            layout.builtInScene = true;

            // 3D texts: kept if they exist (Wong places them by hand); "Rebuild World HUD" resets them
            var world = Object.FindFirstObjectByType<WorldHud>(FindObjectsInactive.Include);
            if (world == null)
            {
                world = new GameObject("WorldHUD").AddComponent<WorldHud>();
                world.Build();
            }

            foreach (var o in new Object[] { hud, hand, build, items, layout, world })
                EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            EditorSceneManager.SaveScene(hud.gameObject.scene);
            Debug.Log("[PokerTD] New HUD built into the scene and saved.");
        }

        /// <summary>One shared outline material per font (dark outline), used by every label with that font.</summary>
        static void MakeOutlineMaterial(string fontName, float width)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontFolder + fontName + ".asset");
            if (font == null) return;
            string path = FontFolder + fontName + " Outline.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(font.material);
                AssetDatabase.CreateAsset(m, path);
            }
            m.CopyPropertiesFromMaterial(font.material);
            m.EnableKeyword("OUTLINE_ON");
            m.SetFloat("_OutlineWidth", width);
            m.SetColor("_OutlineColor", new Color32(40, 28, 20, 255));
            m.SetFloat("_FaceDilate", 0.1f);
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
        }
    }
}
