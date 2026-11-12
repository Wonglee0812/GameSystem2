using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// Damage of one burst = chips x mult (sheet Burst in PokerTD_Model.xlsx).
    /// Chips = base chips x (1 + chips per level x (level - 1)); mult = base mult.
    /// Then the buff machines that cover the last tower are applied one by one, in
    /// priority order (added in step 4). Locked hands count as High Card.
    /// </summary>
    public static class DamageCalc
    {
        public struct Result { public HandType hand; public float chips; public float mult; public float damage; }

        public static Result Burst(HandType hand, Tower lastTower)
        {
            var game = TDGame.Instance;
            if (!game.IsUnlocked(hand)) hand = HandType.HighCard;
            var s = game.config.Stats(hand);
            float chips = s.chips * (1f + s.chipsPerLevel * (game.Level(hand) - 1));
            float mult = s.mult;
            // step 4: foreach buff on lastTower, in slot order -> chips / mult
            return new Result { hand = hand, chips = chips, mult = mult, damage = chips * mult };
        }
    }
}
