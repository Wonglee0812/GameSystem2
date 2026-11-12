using System.Collections.Generic;
using System.Linq;

namespace PokerTD
{
    /// <summary>
    /// Finds the best poker hand in 1-5 cards. Flush and straight need all 5 cards.
    /// A straight may use the ace low (A-2-3-4-5). Based on the grouping idea of the
    /// earlier EnemyCardHandler prototype, with the ace-low straight fixed.
    /// </summary>
    public static class HandEvaluator
    {
        public static HandType Evaluate(IReadOnlyList<Card> cards)
        {
            if (cards == null || cards.Count == 0) return HandType.HighCard;
            var groups = cards.GroupBy(c => c.rank).Select(g => g.Count()).OrderByDescending(n => n).ToList();
            bool five = cards.Count == 5;
            bool flush = five && cards.All(c => c.suit == cards[0].suit);
            bool straight = five && groups.Count == 5 && IsRun(cards.Select(c => c.rank).OrderBy(r => r).ToList());

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

        static bool IsRun(List<int> sorted)
        {
            if (sorted[4] - sorted[0] == 4) return true;
            // ace low: A-2-3-4-5 is sorted as 2,3,4,5,14
            return sorted[4] == 14 && sorted[0] == 2 && sorted[3] == 5;
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
