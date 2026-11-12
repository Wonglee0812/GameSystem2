using System.Collections.Generic;
using UnityEngine;

namespace PokerTD
{
    /// <summary>A normal 52-card deck. It is shuffled again at the start of every battle turn.</summary>
    public class Deck
    {
        readonly List<Card> pile = new List<Card>();

        public int Count => pile.Count;

        public void Reset()
        {
            pile.Clear();
            for (int s = 0; s < 4; s++)
                for (int r = 2; r <= 14; r++) pile.Add(new Card(r, (Suit)s));
            for (int i = pile.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pile[i], pile[j]) = (pile[j], pile[i]);
            }
        }

        public bool TryDraw(out Card card)
        {
            card = default;
            if (pile.Count == 0) return false;
            card = pile[pile.Count - 1];
            pile.RemoveAt(pile.Count - 1);
            return true;
        }
    }
}
