using System;

namespace PokerTD
{
    public enum Suit { Spades, Hearts, Diamonds, Clubs }

    public enum HandType { HighCard, Pair, TwoPair, ThreeOfAKind, Straight, Flush, FullHouse, FourOfAKind, StraightFlush }

    /// <summary>One playing card. Rank 2-14 (11 J, 12 Q, 13 K, 14 A).</summary>
    [Serializable]
    public struct Card : IEquatable<Card>
    {
        public int rank;
        public Suit suit;

        public Card(int rank, Suit suit) { this.rank = rank; this.suit = suit; }

        /// <summary>Damage of one hit with this card: number cards their number, J/Q/K 10, A 11 (Balatro card chips).</summary>
        public int Value => rank <= 10 ? rank : rank == 14 ? 11 : 10;

        public bool IsRed => suit == Suit.Hearts || suit == Suit.Diamonds;

        public string RankText => rank <= 10 ? rank.ToString() : rank == 11 ? "J" : rank == 12 ? "Q" : rank == 13 ? "K" : "A";
        public string SuitText => suit == Suit.Spades ? "♠" : suit == Suit.Hearts ? "♥" : suit == Suit.Diamonds ? "♦" : "♣";

        /// <summary>"7♥" with the suit colour as TextMeshPro rich text.</summary>
        public string Rich => $"<mark=#FFF6E4E6 padding=\"6,6,2,2\"><color={(IsRed ? "#D8392E" : "#2B1A10")}>{RankText}{SuitText}</color></mark>";
        public override string ToString() => RankText + SuitText;

        public bool Equals(Card other) => rank == other.rank && suit == other.suit;
        public override bool Equals(object obj) => obj is Card c && Equals(c);
        public override int GetHashCode() => rank * 4 + (int)suit;
    }
}
