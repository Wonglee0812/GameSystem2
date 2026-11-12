using System.Collections.Generic;
using System.Linq;

namespace PokerTD
{
    /// <summary>
    /// Finds the best poker hand in 1-5 cards. Flush and straight need
    /// config.flushStraightCards cards (4 or 5): with 4 towers an enemy can never hold 5 different cards.
    /// A straight may use the ace low (A-2-3-4-5). Based on the grouping idea of the
    /// earlier EnemyCardHandler prototype, with the ace-low straight fixed.
    /// </summary>
    public static class HandEvaluator
    {
        public static HandType Evaluate(IReadOnlyList<Card> cards)
        {
            if (cards == null || cards.Count == 0) return HandType.HighCard;
            var groups = cards.GroupBy(c => c.rank).Select(g => g.Count()).OrderByDescending(n => n).ToList();
            int need = TDGame.Instance != null ? TDGame.Instance.config.flushStraightCards : 5;
            bool enough = cards.Count >= need;
            // flush: 'need' cards of one suit; straight: 'need' different ranks in a row
            bool flush = enough && cards.GroupBy(c => c.suit).Any(g => g.Count() >= need);
            bool straight = enough && HasRun(cards.Select(c => c.rank).Distinct().ToList(), need);

            if (straight && flush) return HandType.StraightFlush;
            if (groups[0] == 4) return HandType.FourOfAKind;
            if (groups[0] == 3 && groups.Count > 1 && groups[1] == 2) return HandType.FullHouse;
            if (flush) return HandType.Flush;
            if (straight) return HandType.Straight;
            if (groups[0] == 3) return HandType.ThreeOfAKind;
            if (groups[0] == 2 && groups.Count > 1 && groups[1] == 2) return HandType.TwoPair;
            if (groups[0] == 2) return HandType.Pair;
            return HandType.HighCard;
        }

        /// <summary>True when 'need' ranks follow each other. The ace counts high (14) and low (1).</summary>
        static bool HasRun(List<int> ranks, int need)
        {
            var set = new HashSet<int>(ranks);
            if (set.Contains(14)) set.Add(1);
            foreach (int start in set)
            {
                int n = 1;
                while (set.Contains(start + n)) n++;
                if (n >= need) return true;
            }
            return false;
        }

        public static string Name(HandType hand)
        {
            switch (hand)
            {
                case HandType.HighCard: return "High Card";
                case HandType.TwoPair: return "Two Pair";
                case HandType.ThreeOfAKind: return "Three of a Kind";
                case HandType.FullHouse: return "Full House";
                case HandType.FourOfAKind: return "Four of a Kind";
                case HandType.StraightFlush: return "Straight Flush";
                default: return hand.ToString();
            }
        }
    }
}
