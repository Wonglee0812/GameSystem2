using System.Collections.Generic;
using System.Text;

namespace PokerTD
{
    /// <summary>
    /// Damage of one burst = chips x mult (sheet Burst in PokerTD_Model.xlsx).
    /// Chips = base chips x (1 + chips per level x (level - 1)); mult = base mult.
    /// Then every buff machine that reaches the last tower is applied one by one, in
    /// ring order (ring 1 first). Locked hands count as High Card.
    /// </summary>
    public static class DamageCalc
    {
        public struct Result { public HandType hand; public float chips; public float mult; public float damage; }

        /// <summary>One buff on a tower: which machine, and in which of its rings the tower stands.</summary>
        public struct Buff { public BuffMachine machine; public int ring; }

        public static Result Burst(HandType hand, Tower lastTower)
        {
            hand = Counted(hand);
            Start(hand, out float chips, out float mult);
            foreach (var b in BuffsOn(lastTower)) Apply(b.machine.Type, ref chips, ref mult);
            return new Result { hand = hand, chips = chips, mult = mult, damage = chips * mult };
        }

        /// <summary>The buffs that reach a tower, sorted in the order they are applied.</summary>
        public static List<Buff> BuffsOn(Tower tower)
        {
            var list = new List<Buff>();
            if (tower == null) return list;
            foreach (var m in BuffMachine.All)
            {
                int ring = m.RingOf(tower.transform.position);
                if (ring > 0) list.Add(new Buff { machine = m, ring = ring });
            }
            // ring 1 first; same ring: the machine built first goes first
            list.Sort((a, b) => a.ring != b.ring ? a.ring.CompareTo(b.ring) : a.machine.BuildOrder.CompareTo(b.machine.BuildOrder));
            return list;
        }

        public static void Apply(TDConfig.BuffType buff, ref float chips, ref float mult)
        {
            switch (buff.kind)
            {
                case TDConfig.BuffKind.AddChips:     chips += buff.value; break;
                case TDConfig.BuffKind.PercentChips: chips *= 1f + buff.value; break;
                case TDConfig.BuffKind.AddMult:      mult += buff.value; break;
                case TDConfig.BuffKind.XMult:        mult *= buff.value; break;
            }
        }

        /// <summary>
        /// Text for the hover panel: the burst of an example hand on this tower, step by step.
        /// </summary>
        public static string Explain(Tower tower, HandType example)
        {
            var hand = Counted(example);
            Start(hand, out float chips, out float mult);
            var game = TDGame.Instance;
            var sb = new StringBuilder();
            sb.Append($"<b>{tower.Type.name}</b>   {(tower.HasCard ? tower.Card.ToString() : "no card")}\n");
            sb.Append($"<size=80%><color=#BBBBBB>Example burst: {HandEvaluator.Name(hand)} (level {game.Level(hand)})</color></size>\n");
            sb.Append($"Start<pos=8em>{chips:0.#} chips x {mult:0.##} mult\n");
            var buffs = BuffsOn(tower);
            if (buffs.Count == 0) sb.Append("<color=#BBBBBB>No buff machine reaches this tower.</color>\n");
            int step = 1;
            foreach (var b in buffs)
            {
                Apply(b.machine.Type, ref chips, ref mult);
                string what = b.machine.Type.Short;
                sb.Append($"{step++}. {what}<pos=8em>-> {chips:0.#} x {mult:0.##}   <color=#BBBBBB>ring {b.ring}</color>\n");
            }
            sb.Append($"<b>Damage = {chips * mult:#,0}</b>");
            return sb.ToString();
        }

        static HandType Counted(HandType hand) => TDGame.Instance.IsUnlocked(hand) ? hand : HandType.HighCard;

        static void Start(HandType hand, out float chips, out float mult)
        {
            var game = TDGame.Instance;
            var s = game.config.Stats(hand);
            chips = s.chips * (1f + s.chipsPerLevel * (game.Level(hand) - 1));
            mult = s.mult;
        }
    }
}
