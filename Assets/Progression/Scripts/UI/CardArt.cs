using System.Collections.Generic;
using UnityEngine;

namespace PokerTD.UI
{
    /// <summary>
    /// Card sprites from Resources/Cards: "Hearts_K", "Spades_10", ... and "Card_Back".
    /// The faces were cut from one sheet with a script (52 cards, transparent corners).
    /// </summary>
    public static class CardArt
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Face(Card card) => Load($"Cards/{card.suit}_{card.RankText}");
        public static Sprite Back => Load("Cards/Card_Back");

        static Sprite Load(string path)
        {
            if (!cache.TryGetValue(path, out Sprite s) || s == null)
            {
                s = Resources.Load<Sprite>(path);
                if (s == null) Debug.LogWarning($"[PokerTD] Missing card sprite: Resources/{path}");
                cache[path] = s;
            }
            return s;
        }
    }
}
