using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PokerTD.UI
{
    /// <summary>
    /// Top-right panel, like the trait list in Teamfight Tactics: which poker hands the
    /// cards on the towers can make on one enemy, how close each hand is (have / need),
    /// and its base burst (chips x mult at the current level). Made at runtime by TDHud.
    /// </summary>
    public class ComboView : MonoBehaviour
    {
        TextMeshProUGUI text;
        RectTransform panel;

        void Start()
        {
            var go = new GameObject("ComboPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            panel = (RectTransform)go.transform;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 1f);
            panel.anchoredPosition = new Vector2(-30f, -30f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.08f, 0.06f, 0.05f, 0.8f);
            img.raycastTarget = false;
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
        }

        void Update()
        {
            var game = TDGame.Instance;
            bool show = game.Current == TDGame.Phase.Deploy || game.Current == TDGame.Phase.Wave;
            if (panel.gameObject.activeSelf != show) panel.gameObject.SetActive(show);
            if (!show) return;
            var cards = new List<Card>();
            foreach (var t in Tower.All) if (t.HasCard) cards.Add(t.Card);
            text.text = Build(cards);
            Vector2 size = text.GetPreferredValues(text.text);
            panel.sizeDelta = new Vector2(size.x + 32f, size.y + 24f);
        }

        static string Build(List<Card> cards)
        {
            var game = TDGame.Instance;
            int need = game.config.flushStraightCards;
            var sb = new StringBuilder();
            sb.Append($"<b>COMBOS</b>  <size=80%><color=#BBBBBB>{cards.Count} card(s) on towers</color></size>\n");
            if (cards.Count == 0) { sb.Append("<color=#BBBBBB>Drag cards onto towers.</color>"); return sb.ToString(); }

            var rankCounts = cards.GroupBy(c => c.rank).Select(g => g.Count()).OrderByDescending(n => n).ToList();
            int top = rankCounts[0], second = rankCounts.Count > 1 ? rankCounts[1] : 0;
            int pairs = rankCounts.Count(n => n >= 2);
            int suitMax = cards.GroupBy(c => c.suit).Max(g => g.Count());
            int run = BestRun(cards.Select(c => c.rank), need);
            int runFlush = cards.GroupBy(c => c.suit).Max(g => BestRun(g.Select(c => c.rank), need));

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
            var best = HandEvaluator.Evaluate(cards);
            foreach (var (hand, have, needed) in lines)
            {
                bool done = have >= needed;
                if (!done && have <= 1 && hand != HandType.HighCard) continue;   // nothing started yet
                var s = game.config.Stats(hand);
                float chips = s.chips * (1f + s.chipsPerLevel * (game.Level(hand) - 1));
                string locked = game.IsUnlocked(hand) ? "" : "  <color=#FF8060>locked</color>";
                string col = hand == best ? "#FFD860" : done ? "#FFFFFF" : "#999999";
                sb.Append($"<color={col}>{(done ? "●" : "○")} {HandEvaluator.Name(hand)}<pos=10em>{have}/{needed}<pos=13em>{chips:0} x {s.mult:0.#}  <size=80%>lvl {game.Level(hand)}</size>{locked}</color>\n");
            }
            sb.Append($"<size=80%><color=#BBBBBB>Best on one enemy: {HandEvaluator.Name(best)}. Buffs come on top.</color></size>");
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
