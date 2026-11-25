using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PokerTD.UI
{
    /// <summary>
    /// Top-right panel, like the trait list in Teamfight Tactics: which poker hands the
    /// cards on the towers can make on one enemy and how close each hand is (have / need).
    /// The base burst (chips x mult at the current level) shows in a tooltip when the
    /// mouse is on a row. Made at runtime by TDHud.
    /// </summary>
    public class ComboView : MonoBehaviour
    {
        TextMeshProUGUI text, tipText;
        RectTransform panel, tip;

        void Start()
        {
            var go = new GameObject("ComboPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            panel = (RectTransform)go.transform;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 1f);
            panel.anchoredPosition = new Vector2(-30f, -30f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.08f, 0.06f, 0.05f, 0.8f);
            img.raycastTarget = true;   // blocks board clicks under the panel (needed when picking a hand)
            var tgo = new GameObject("Text", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            var trt = (RectTransform)tgo.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16f, 12f); trt.offsetMax = new Vector2(-16f, -12f);
            text = tgo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 22f;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            BuildTooltip();
        }

        void BuildTooltip()
        {
            var go = new GameObject("ComboTooltip", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            tip = (RectTransform)go.transform;
            tip.anchorMin = tip.anchorMax = new Vector2(0f, 0f);
            tip.pivot = new Vector2(1f, 1f);                      // opens to the lower left of the mouse
            var img = go.GetComponent<Image>();
            img.color = new Color(0.02f, 0.02f, 0.02f, 0.92f);
            img.raycastTarget = false;
            var tgo = new GameObject("Text", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            var trt = (RectTransform)tgo.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f, 8f); trt.offsetMax = new Vector2(-12f, -8f);
            tipText = tgo.AddComponent<TextMeshProUGUI>();
            tipText.fontSize = 20f;
            tipText.alignment = TextAlignmentOptions.TopLeft;
            tipText.textWrappingMode = TextWrappingModes.NoWrap;
            tipText.raycastTarget = false;
            go.SetActive(false);
        }

        /// <summary>Shows the tooltip for the row under the mouse (rows are TMP links named after the hand).</summary>
        void UpdateTooltip()
        {
            var mouse = Mouse.current;
            int link = mouse == null ? -1 : TMP_TextUtilities.FindIntersectingLink(text, mouse.position.ReadValue(), null);
            if (link < 0 || !System.Enum.TryParse(text.textInfo.linkInfo[link].GetLinkID(), out HandType hand))
            {
                if (tip.gameObject.activeSelf) tip.gameObject.SetActive(false);
                return;
            }
            var game = TDGame.Instance;
            var s = game.config.Stats(hand);
            int lvl = game.Level(hand);
            float chips = s.chips * (1f + s.chipsPerLevel * (lvl - 1));
            string locked = game.IsUnlocked(hand) ? "" : "\n<color=#FF8060>Locked: bursts as High Card</color>";
            tipText.text = $"<b>{HandEvaluator.Name(hand)}</b>  <size=80%>lvl {lvl}</size>\n"
                         + $"Base burst: <color=#60B0FF>{chips:0} chips</color> x <color=#FF6060>{s.mult:0.#} mult</color> = <b>{chips * s.mult:0}</b>\n"
                         + "<size=80%><color=#BBBBBB>Buff machines come on top.</color></size>" + locked;
            Vector2 size = tipText.GetPreferredValues(tipText.text);
            tip.sizeDelta = new Vector2(size.x + 24f, size.y + 16f);
            float scale = ((RectTransform)transform).lossyScale.x;
            tip.anchoredPosition = mouse.position.ReadValue() / Mathf.Max(0.0001f, scale) + new Vector2(-12f, -12f);
            if (!tip.gameObject.activeSelf) tip.gameObject.SetActive(true);
            tip.SetAsLastSibling();
        }

        void Update()
        {
            var game = TDGame.Instance;
            bool show = game.Current == TDGame.Phase.Deploy || game.Current == TDGame.Phase.Wave;
            if (panel.gameObject.activeSelf != show) panel.gameObject.SetActive(show);
            if (!show) { if (tip.gameObject.activeSelf) tip.gameObject.SetActive(false); return; }
            var cards = new List<Card>();
            foreach (var t in Tower.All) if (t.HasCard) cards.Add(t.Card);
            text.text = Build(cards);
            Vector2 size = text.GetPreferredValues(text.text);
            panel.sizeDelta = new Vector2(size.x + 32f, size.y + 24f);
            text.ForceMeshUpdate();
            UpdateTooltip();
            if (game.PickingHandForItem >= 0 && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                int link = TMP_TextUtilities.FindIntersectingLink(text, Mouse.current.position.ReadValue(), null);
                if (link >= 0 && System.Enum.TryParse(text.textInfo.linkInfo[link].GetLinkID(), out HandType hand))
                    game.PickHandForItem(hand);
            }
        }

        static string Build(List<Card> cards)
        {
            var game = TDGame.Instance;
            int need = game.config.flushStraightCards;
            var sb = new StringBuilder();
            if (game.PickingHandForItem >= 0) sb.Append("<color=#FFD860><b>Pick a hand to level up</b></color>\n");
            sb.Append($"<b>COMBOS</b>  <size=80%><color=#BBBBBB>{cards.Count} card(s) on towers</color></size>\n");
            if (cards.Count == 0 && game.PickingHandForItem < 0) { sb.Append("<color=#BBBBBB>Drag cards onto towers.</color>"); return sb.ToString(); }

            var rankCounts = cards.GroupBy(c => c.rank).Select(g => g.Count()).OrderByDescending(n => n).ToList();
            int top = rankCounts.Count > 0 ? rankCounts[0] : 0, second = rankCounts.Count > 1 ? rankCounts[1] : 0;
            int pairs = rankCounts.Count(n => n >= 2);
            int suitMax = cards.Count > 0 ? cards.GroupBy(c => c.suit).Max(g => g.Count()) : 0;
            int run = BestRun(cards.Select(c => c.rank), need);
            int runFlush = cards.Count > 0 ? cards.GroupBy(c => c.suit).Max(g => BestRun(g.Select(c => c.rank), need)) : 0;

            var lines = new List<(HandType hand, int have, int needed)>
            {
                (HandType.StraightFlush, runFlush, need),
                (HandType.FourOfAKind, Mathf.Min(top, 4), 4),
                (HandType.FullHouse, Mathf.Min(top, 3) + Mathf.Min(second, 2), 5),
                (HandType.Flush, Mathf.Min(suitMax, need), need),
                (HandType.Straight, run, need),
                (HandType.ThreeOfAKind, Mathf.Min(top, 3), 3),
                (HandType.TwoPair, Mathf.Min(pairs, 2), 2),
                (HandType.Pair, Mathf.Min(top, 2), 2),
                (HandType.HighCard, 1, 1),
            };
            var best = game.BestReachableHand;
            foreach (var (hand, have, needed) in lines)
            {
                bool done = have >= needed;
                bool picking = game.PickingHandForItem >= 0;
                if (!picking && !done && have <= 1 && hand != HandType.HighCard) continue;   // nothing started yet
                if (picking && !game.IsUnlocked(hand)) continue;                               // locked hands can not be levelled
                string locked = game.IsUnlocked(hand) ? "" : "  <color=#FF8060>locked</color>";
                string col = hand == best ? "#FFD860" : done ? "#FFFFFF" : "#999999";
                sb.Append($"<link=\"{hand}\"><color={col}>{(done ? "●" : "○")} {HandEvaluator.Name(hand)}<pos=10em>{have}/{needed}{locked}</color></link>\n");
            }
            sb.Append($"<size=80%><color=#BBBBBB>Best on one enemy: {HandEvaluator.Name(game.BestReachableHand)}{(game.BurstAtBestHand && game.BestReachableHand > HandType.HighCard ? " (bursts at once)" : "")}. Hover a hand for its damage.</color></size>");
            return sb.ToString();
        }

        /// <summary>Most different ranks inside any window of 'need' ranks in a row (ace high and low).</summary>
        static int BestRun(IEnumerable<int> ranks, int need)
        {
            var set = new HashSet<int>(ranks);
            if (set.Contains(14)) set.Add(1);
            int best = 0;
            for (int start = 1; start + need - 1 <= 14; start++)
            {
                int n = 0;
                for (int r = start; r < start + need; r++) if (set.Contains(r)) n++;
                best = Mathf.Max(best, n);
            }
            return best;
        }
    }
}
