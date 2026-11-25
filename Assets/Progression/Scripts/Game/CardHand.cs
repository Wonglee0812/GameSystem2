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
        public enum SortMode { None, Rank, Suit }

        public readonly List<Card> Cards = new List<Card>();
        /// <summary>Set by the Sort buttons. Like Balatro: after one click, every new card is sorted in automatically.</summary>
        public SortMode Mode { get; private set; } = SortMode.None;
        public readonly HashSet<int> Selected = new HashSet<int>();
        public int DiscardsLeft { get; private set; }
        public event Action Changed;

        readonly Deck deck = new Deck();

        /// <summary>Cards left in the deck this turn (shown on the deck pile).</summary>
        public int DeckCount => deck.Count;

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
            ApplySort();
            Changed?.Invoke();
        }

        public void Toggle(int index)
        {
            if (!Selected.Remove(index)) Selected.Add(index);
            Changed?.Invoke();
        }

        /// <summary>Extra discards for this turn (Extra discard item).</summary>
        public void AddDiscards(int amount) { DiscardsLeft += amount; Changed?.Invoke(); }

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
            ApplySort();
            Changed?.Invoke();
        }

        /// <summary>Click-to-place: puts the leftmost selected card on the tower. The other selected
        /// cards stay selected, so the player can click the next tower. Returns false without a selection.</summary>
        public bool AssignFirstSelected(Tower tower)
        {
            if (Selected.Count == 0 || tower == null) return false;
            int index = int.MaxValue;
            foreach (int i in Selected) index = Math.Min(index, i);
            if (index < 0 || index >= Cards.Count) return false;
            var keep = new List<Card>();
            foreach (int i in Selected) if (i != index && i < Cards.Count) keep.Add(Cards[i]);
            var card = Cards[index];
            Cards.RemoveAt(index);
            if (tower.HasCard) Cards.Insert(index, tower.Card);
            tower.SetCard(card);
            ApplySort();
            Selected.Clear();
            foreach (var c in keep) { int j = Cards.IndexOf(c); if (j >= 0) Selected.Add(j); }
            Changed?.Invoke();
            return true;
        }

        /// <summary>Sorts the hand by rank (A high first) or by suit (then rank), and keeps
        /// that order for every card drawn later in the run.</summary>
        public void Sort(bool bySuit)
        {
            Mode = bySuit ? SortMode.Suit : SortMode.Rank;
            ApplySort();
            Selected.Clear();
            Changed?.Invoke();
        }

        void ApplySort()
        {
            if (Mode == SortMode.Suit) Cards.Sort((a, b) => a.suit != b.suit ? a.suit.CompareTo(b.suit) : b.rank.CompareTo(a.rank));
            else if (Mode == SortMode.Rank) Cards.Sort((a, b) => a.rank != b.rank ? b.rank.CompareTo(a.rank) : a.suit.CompareTo(b.suit));
        }

        /// <summary>Takes a tower's card back into the hand.</summary>
        public void TakeBack(Tower tower)
        {
            if (tower == null || !tower.HasCard) return;
            Cards.Add(tower.Card);
            tower.ClearCard();
            ApplySort();
            Changed?.Invoke();
        }
    }
}
