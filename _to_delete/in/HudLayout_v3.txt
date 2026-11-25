using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PokerTD.UI
{
    /// <summary>
    /// New HUD layout (2026-11-24): 3D boards (WorldHud), build cards (bottom left), item cards
    /// (top left) and every button at the bottom right next to the deck, styled with UiKit and
    /// Wong's button animation. Normally all of it is built into the scene by the menu
    /// "Poker TD/Build New HUD In Scene" (builtInScene = true), so it can be edited by hand and
    /// is left alone at Play. Without that, the same parts are made at Play (fallback).
    /// </summary>
    public class HudLayout : MonoBehaviour
    {
        public bool builtInScene;

        IEnumerator Start()
        {
            if (builtInScene) yield break;
            if (FindFirstObjectByType<WorldHud>() == null) new GameObject("WorldHUD").AddComponent<WorldHud>();
            if (GetComponent<BuildCardBar>() == null) gameObject.AddComponent<BuildCardBar>();
            if (GetComponent<ItemCardBar>() == null) gameObject.AddComponent<ItemCardBar>();
            yield return null;   // the other HUD scripts make the Sort and Burst buttons in their Start
            Apply(transform);
        }

        /// <summary>Makes the Sort and Burst buttons if they are missing (Edit mode), as copies of Discard and Start Wave.</summary>
        public static void EnsureButtons(Transform hud, Button discard, Button startWave)
        {
            Copy(hud, discard, "Sort: Rank", "SORT: RANK");
            Copy(hud, discard, "Sort: Suit", "SORT: SUIT");
            Copy(hud, startWave, "EarlyBurstToggle", "BURST AT BEST HAND: ON");
        }

        static void Copy(Transform hud, Button template, string name, string label)
        {
            if (hud.Find(name) != null) return;
            var b = Object.Instantiate(template, hud);
            b.name = name;
            var t = b.GetComponentInChildren<TMP_Text>(true);
            if (t != null) t.text = label;
        }

        /// <summary>Places and styles the buttons at the bottom right (Edit mode or Play).</summary>
        public static void Apply(Transform hud)
        {
            Place(hud, "StartWave", new Vector2(-232f, 28f), new Vector2(250f, 86f), UiKit.Orange, 34f, 1.08f);
            Place(hud, "Discard", new Vector2(-232f, 126f), new Vector2(250f, 70f), UiKit.Red, 28f, 1.1f);
            Place(hud, "Sort: Suit", new Vector2(-232f, 208f), new Vector2(122f, 54f), UiKit.Blue, 18f, 1.2f);
            Place(hud, "Sort: Rank", new Vector2(-360f, 208f), new Vector2(122f, 54f), UiKit.Blue, 18f, 1.2f);
            Place(hud, "EarlyBurstToggle", new Vector2(-232f, 274f), new Vector2(250f, 50f), UiKit.Wood, 16f, 1.1f);

            var continueButton = hud.Find("ShopPanel/Continue")?.GetComponent<Button>();
            if (continueButton != null) UiKit.StyleButton(continueButton, UiKit.Orange, 30f, 1.1f);

            var deckCount = hud.Find("Hand/DeckPile/Count")?.GetComponent<TMP_Text>();
            if (deckCount != null) { deckCount.font = UiKit.Number; deckCount.color = UiKit.Cream; }
        }

        static void Place(Transform hud, string name, Vector2 pos, Vector2 size, Color color, float fontSize, float hoverScale)
        {
            var t = hud.Find(name);
            if (t == null) { Debug.LogWarning($"[PokerTD] HudLayout: no button '{name}'"); return; }
            var rt = (RectTransform)t;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var b = t.GetComponent<Button>();
            if (b != null) UiKit.StyleButton(b, color, fontSize, hoverScale);
        }
    }
}
