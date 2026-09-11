using System;

namespace EpicRPGBot.UI.CardHand
{
    public readonly struct CardId : IEquatable<CardId>, IComparable<CardId>
    {
        public CardId(CardSuit suit, CardRank rank)
        {
            if (rank == CardRank.Joker && suit != CardSuit.None)
            {
                throw new ArgumentException("Joker cannot have a suit.", nameof(suit));
            }

            if (rank != CardRank.Joker && suit == CardSuit.None)
            {
                throw new ArgumentException("Standard cards require a suit.", nameof(suit));
            }

            Suit = suit;
            Rank = rank;
        }

        public CardSuit Suit { get; }
        public CardRank Rank { get; }
        public bool IsJoker => Rank == CardRank.Joker;

        public static CardId Joker => new CardId(CardSuit.None, CardRank.Joker);

        public static bool TryParse(string value, out CardId card)
        {
            card = default;
            var normalized = Normalize(value);
            if (IsJokerLabel(normalized))
            {
                card = Joker;
                return true;
            }

            if (normalized.Length < 2 || !TryParseSuit(normalized[0], out var suit) ||
                !TryParseRank(normalized.Substring(1), out var rank))
            {
                return false;
            }

            card = new CardId(suit, rank);
            return true;
        }

        public override string ToString()
        {
            return IsJoker ? "JOKER" : SuitCode(Suit) + RankCode(Rank);
        }

        public bool Equals(CardId other) => Suit == other.Suit && Rank == other.Rank;
        public override bool Equals(object obj) => obj is CardId other && Equals(other);
        public override int GetHashCode() => ((int)Suit * 397) ^ (int)Rank;
        public int CompareTo(CardId other) => GetSortValue().CompareTo(other.GetSortValue());
        public static bool operator ==(CardId left, CardId right) => left.Equals(right);
        public static bool operator !=(CardId left, CardId right) => !left.Equals(right);

        private int GetSortValue() => IsJoker ? 1000 : ((int)Suit * 20) + (int)Rank;

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().Replace(":", string.Empty).ToUpperInvariant();
        }

        private static bool IsJokerLabel(string value)
        {
            return value == "JOKER" || value == "JKR" || value == "🃏";
        }

        private static bool TryParseSuit(char value, out CardSuit suit)
        {
            switch (value)
            {
                case 'H': suit = CardSuit.Hearts; return true;
                case 'D': suit = CardSuit.Diamonds; return true;
                case 'C': suit = CardSuit.Clubs; return true;
                case 'S': suit = CardSuit.Spades; return true;
                default: suit = CardSuit.None; return false;
            }
        }

        private static bool TryParseRank(string value, out CardRank rank)
        {
            switch (value)
            {
                case "2": rank = CardRank.Two; return true;
                case "3": rank = CardRank.Three; return true;
                case "4": rank = CardRank.Four; return true;
                case "5": rank = CardRank.Five; return true;
                case "6": rank = CardRank.Six; return true;
                case "7": rank = CardRank.Seven; return true;
                case "8": rank = CardRank.Eight; return true;
                case "9": rank = CardRank.Nine; return true;
                case "10": case "T": rank = CardRank.Ten; return true;
                case "J": rank = CardRank.Jack; return true;
                case "Q": rank = CardRank.Queen; return true;
                case "K": rank = CardRank.King; return true;
                case "A": rank = CardRank.Ace; return true;
                default: rank = CardRank.Joker; return false;
            }
        }

        private static string SuitCode(CardSuit suit)
        {
            switch (suit)
            {
                case CardSuit.Hearts: return "H";
                case CardSuit.Diamonds: return "D";
                case CardSuit.Clubs: return "C";
                case CardSuit.Spades: return "S";
                default: throw new ArgumentOutOfRangeException(nameof(suit));
            }
        }

        private static string RankCode(CardRank rank)
        {
            var numericRank = (int)rank;
            if (numericRank >= 2 && numericRank <= 10) return numericRank.ToString();
            if (rank == CardRank.Jack) return "J";
            if (rank == CardRank.Queen) return "Q";
            if (rank == CardRank.King) return "K";
            if (rank == CardRank.Ace) return "A";
            throw new ArgumentOutOfRangeException(nameof(rank));
        }
    }
}
