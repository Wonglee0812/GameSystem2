using System;
using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// Every tuning number of the Poker TD prototype, in one asset. The starting values
    /// come from PokerTD_Model.xlsx (02_Progression/2_Design/Model). Change them there
    /// first, then copy them here.
    /// </summary>
    [CreateAssetMenu(menuName = "Poker TD/Config", fileName = "TDConfig")]
    public class TDConfig : ScriptableObject
    {
        [Serializable]
        public class HandStats
        {
            public HandType hand;
            [Tooltip("Base chips at level 1 (Balatro chips x5).")]
            public float chips;
            [Tooltip("Base mult (Balatro).")]
            public float mult;
            [Tooltip("Each level adds this part of the base chips (0.5 = +50%).")]
            public float chipsPerLevel;
            [Tooltip("Must be unlocked in the shop before it counts.")]
            public bool startsLocked;
        }

        [Serializable]
        public class EnemyType
        {
            public string name;
            public float healthMultiplier = 1f;
            public float speed = 2f;
            public int gold = 1;
            [Tooltip("Lives lost when it reaches the base.")]
            public int damage = 1;
            public Color color = Color.white;
            public float size = 0.8f;
            [Tooltip("Boom enemies stun towers in this radius when they die (0 = none).")]
            public float stunRadius;
            public float stunSeconds;
            [Tooltip("First ante this enemy can appear.")]
            public int fromAnte = 1;
            [Tooltip("Share of a wave once it can appear.")]
            [Range(0f, 1f)] public float share = 0.25f;
        }

        public enum TowerKind { Cannon, Gatling, Trap }

        public enum BuffKind { AddChips, PercentChips, AddMult, XMult }

        [Serializable]
        public class BuffType
        {
            public BuffKind kind;
            public string name;
            [Tooltip("+Chips: flat chips. +%Chips: 0.25 = +25% of the chips so far. +Mult: added to mult. xMult: mult is multiplied by this.")]
            public float value;
            public int cost = 6;
            public Color color = Color.white;

            /// <summary>"+50 chips", "x1.5 mult" ...</summary>
            public string Short =>
                kind == BuffKind.AddChips ? $"+{value:0.#} chips" :
                kind == BuffKind.PercentChips ? $"+{value * 100f:0}% chips" :
                kind == BuffKind.AddMult ? $"+{value:0.#} mult" : $"x{value:0.##} mult";
        }

        [Serializable]
        public class TowerType
        {
            public TowerKind kind;
            public string name;
            public int cost = 5;
            public float range = 4f;
            [Tooltip("Shots per second (trap: mark pulses per second).")]
            public float fireRate = 1f;
            public float projectileSpeed = 12f;
            [Tooltip("Cannon blast radius / trap zone radius.")]
            public float areaRadius;
            public Color color = Color.white;
            [Tooltip("Traps are built on the path, the others next to it.")]
            public bool onPath;
        }

        [Header("Hands (PokerTD_Model.xlsx, sheet Hands)")]
        public HandStats[] hands =
        {
            new HandStats { hand = HandType.HighCard,      chips = 25,  mult = 1, chipsPerLevel = 0.60f },
            new HandStats { hand = HandType.Pair,          chips = 50,  mult = 2, chipsPerLevel = 0.50f },
            new HandStats { hand = HandType.TwoPair,       chips = 100, mult = 2, chipsPerLevel = 0.45f },
            new HandStats { hand = HandType.ThreeOfAKind,  chips = 150, mult = 3, chipsPerLevel = 0.40f },
            new HandStats { hand = HandType.Straight,      chips = 150, mult = 4, chipsPerLevel = 0.35f },
            new HandStats { hand = HandType.Flush,         chips = 175, mult = 4, chipsPerLevel = 0.30f },
            new HandStats { hand = HandType.FullHouse,     chips = 200, mult = 4, chipsPerLevel = 0.30f },
            new HandStats { hand = HandType.FourOfAKind,   chips = 300, mult = 7, chipsPerLevel = 0.25f, startsLocked = true },
            new HandStats { hand = HandType.StraightFlush, chips = 500, mult = 8, chipsPerLevel = 0.20f, startsLocked = true },
        };
        [Tooltip("Locked hands burst as High Card until they are unlocked.")]
        public bool useLocks = true;

        [Header("Hands")]
        [Tooltip("Cards needed for a Flush or a Straight. Each tower holds one card and the same card marks only once, so an enemy can carry at most (towers) different cards. 4 = like Balatro's Four Fingers joker.")]
        [Range(4, 5)] public int flushStraightCards = 4;

        [Header("Marks")]
        [Tooltip("Seconds to wait for the next mark, by number of marks already on the enemy (1, 2, 3, 4). 5 marks burst at once.")]
        public float[] markWait = { 2f, 1.6f, 1.2f, 0.8f };

        [Header("Enemies and waves (sheet Inputs)")]
        public float baseHealth = 250f;
        public float healthGrowthPerAnte = 2f;
        public int wavesPerAnte = 3;
        [Tooltip("Health multiplier of wave 1, 2, 3 inside an ante (normal, elite, boss).")]
        public float[] waveHealth = { 1f, 1.25f, 1.5f };
        public int enemiesPerWave = 6;
        public int extraEnemiesPerAnte = 2;
        public float spawnInterval = 0.9f;
        public EnemyType[] enemies =
        {
            new EnemyType { name = "Normal", healthMultiplier = 1f,   speed = 2.0f, gold = 1, damage = 1, color = new Color(0.85f, 0.85f, 0.85f), size = 0.8f, fromAnte = 1, share = 1f },
            new EnemyType { name = "Speed",  healthMultiplier = 0.6f, speed = 3.6f, gold = 1, damage = 1, color = new Color(0.4f, 0.8f, 1f),    size = 0.6f, fromAnte = 2, share = 0.25f },
            new EnemyType { name = "Tank",   healthMultiplier = 3f,   speed = 1.2f, gold = 2, damage = 2, color = new Color(0.55f, 0.45f, 0.4f), size = 1.1f, fromAnte = 3, share = 0.2f },
            new EnemyType { name = "Boom",   healthMultiplier = 1.2f, speed = 1.8f, gold = 2, damage = 1, color = new Color(1f, 0.45f, 0.2f),   size = 0.8f, fromAnte = 4, share = 0.15f, stunRadius = 3f, stunSeconds = 2.5f },
        };

        [Header("Towers")]
        public TowerType[] towers =
        {
            new TowerType { kind = TowerKind.Cannon,  name = "Cannon",  cost = 6, range = 5f,   fireRate = 0.5f, projectileSpeed = 9f,  areaRadius = 2.6f, color = new Color(0.35f, 0.35f, 0.4f) },
            new TowerType { kind = TowerKind.Gatling, name = "Gatling", cost = 5, range = 4f,   fireRate = 3f,   projectileSpeed = 18f, color = new Color(0.8f, 0.65f, 0.25f) },
            new TowerType { kind = TowerKind.Trap,    name = "Trap",    cost = 4, range = 1.2f, fireRate = 1f,   areaRadius = 1.2f, color = new Color(0.6f, 0.25f, 0.6f), onPath = true },
        };

        [Header("Buff machines (sheet Burst)")]
        [Tooltip("Placeholder machines until the final list and rarity tiers are chosen. Values from sheet Burst.")]
        public BuffType[] buffs =
        {
            new BuffType { kind = BuffKind.AddChips,     name = "+Chips",  value = 50f,   cost = 6, color = new Color(0.3f, 0.55f, 1f) },
            new BuffType { kind = BuffKind.PercentChips, name = "+%Chips", value = 0.25f, cost = 7, color = new Color(0.3f, 0.85f, 0.9f) },
            new BuffType { kind = BuffKind.AddMult,      name = "+Mult",   value = 4f,    cost = 6, color = new Color(1f, 0.35f, 0.3f) },
            new BuffType { kind = BuffKind.XMult,        name = "xMult",   value = 1.5f,  cost = 9, color = new Color(0.95f, 0.3f, 0.75f) },
        };
        [Tooltip("Width of one ring around a buff machine, in world units (one tile = 2). Number of rings = tower limit.")]
        public float ringWidth = 2.5f;
        [Tooltip("On: ring 1 (applied first) is the inner ring, closest to the machine. Off: ring 1 is the outer ring.")]
        public bool ringOneIsInner = true;
        [Tooltip("How many buff machines the player may own (placed + waiting), like joker slots.")]
        public int startMachineLimit = 5;
        [Tooltip("Random machines offered in the shop after each wave.")]
        public int shopMachineOffers = 3;
        [Tooltip("Selling a placed machine gives back this part of its cost (Balatro: half).")]
        [Range(0f, 1f)] public float sellBack = 0.5f;

        [Header("Turns")]
        [Tooltip("Cards dealt each battle turn (Balatro hand size: 8).")]
        public int baseHandSize = 8;
        [Tooltip("Extra cards for each tower slot above the base hand size.")]
        public int cardsPerExtraTower = 2;
        [Tooltip("Off: a card that is already on the enemy does not restart its burst timer.")]
        public bool sameCardResetsTimer = false;
        [Tooltip("Towers aim first at enemies that do not carry their card yet.")]
        public bool aimAtUnmarked = true;
        [Tooltip("Discards per battle turn (Balatro starts with 3).")]
        public int discardsPerTurn = 3;
        [Tooltip("How many towers may stand on the map at the start. More can be bought later.")]
        public int startTowerLimit = 4;
        [Tooltip("Seconds for the board to sink into the shop and rise again.")]
        public float boardMoveSeconds = 0.9f;

        [Header("Player")]
        public int startLives = 20;
        public int startGold = 15;
        [Tooltip("Gold after each wave: base + per ante (sheet Inputs).")]
        public int waveGold = 4;
        public int waveGoldPerAnte = 1;

        public HandStats Stats(HandType hand)
        {
            foreach (var h in hands) if (h.hand == hand) return h;
            return hands[0];
        }

        /// <summary>8 cards; above 8 tower slots, 2 more per slot.</summary>
        public int HandSize(int towerLimit) => baseHandSize + cardsPerExtraTower * Mathf.Max(0, towerLimit - baseHandSize);

        public float WaitAfter(int marks) => marks <= 0 ? markWait[0] : markWait[Mathf.Min(marks, markWait.Length) - 1];
    }
}
