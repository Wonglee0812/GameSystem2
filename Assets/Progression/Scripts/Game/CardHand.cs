using System;
using System.Collections.Generic;

namespace PokerTD
{
    /// <summary>
    /// The player's cards in the battle turn. At the start you get config.HandSize cards
    /// (8, more when the tower limit goes above 8). Selected cards can be discarded and replaced from the deck, a few
    /// times per turn. Cards are then dragged onto towers; a tower keeps its card for
    /// the whole wave. A card dropped on a tower that already has one swaps with it.
    /// Cards still in hand when the wave starts are lost.
    /// </summary>
    public class CardHand
    {
        public readonly List<Card> Cards = new List<Card>();
        public readonly HashSet<int> Selected = new HashSet<int>();
        public int DiscardsLeft { get; private set; }
        public event Action Changed;

        readonly Deck deck = new Deck();

        public void Deal(int count, int discards)
        {
            deck.Reset();
            Cards.Clear();
            Selected.Clear();
            DiscardsLeft = discards;
            foreach (var t in Tower.All) t.ClearCard();
            Draw(count);
        }

        /// <summary>One more card, for example when a tower is built during the turn.</summary>
        public void Draw(int count = 1)
        {
            for (int i = 0; i < count; i++)
                if (deck.TryDraw(out Card c)) Cards.Add(c);
            Changed?.Invoke();
        }

        public void Toggle(int index)
        {
            if (!Selected.Remove(index)) Selected.Add(index);
            Changed?.Invoke();
        }

        /// <summary>Throws the selected cards away and draws the same number. Uses one discard.</summary>
        public bool Discard()
        {
            if (DiscardsLeft <= 0 || Selected.Count == 0) return false;
            var keep = new List<Card>();
            for (int i = 0; i < Cards.Count; i++) if (!Selected.Contains(i)) keep.Add(Cards[i]);
            int n = Cards.Count - keep.Count;
            Cards.Clear();
            Cards.AddRange(keep);
            Selected.Clear();
            DiscardsLeft--;
            Draw(n);
            return true;
        }

        /// <summary>Puts the card at 'index' on the tower. Its old card comes back to the hand.</summary>
        public void Assign(int index, Tower tower)
        {
            if (index < 0 || index >= Cards.Count || tower == null) return;
            var card = Cards[index];
            Cards.RemoveAt(index);
            if (tower.HasCard) Cards.Insert(index, tower.Card);
            tower.SetCard(card);
            Selected.Clear();
            Changed?.Invoke();
        }

        /// <summary>Sorts the hand: by rank (A high first) or by suit (then rank).</summary>
        public void Sort(bool bySuit)
        {
            if (bySuit) Cards.Sort((a, b) => a.suit != b.suit ? a.suit.CompareTo(b.suit) : b.rank.CompareTo(a.rank));
            else Cards.Sort((a, b) => a.rank != b.rank ? b.rank.CompareTo(a.rank) : a.suit.CompareTo(b.suit));
            Selected.Clear();
            Changed?.Invoke();
        }

        /// <summary>Takes a tower's card back into the hand.</summary>
        public void TakeBack(Tower tower)
        {
            if (tower == null || !tower.HasCard) return;
            Cards.Add(tower.Card);
            tower.ClearCard();
            Changed?.Invoke();
        }
    }
}
