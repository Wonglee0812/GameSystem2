using UnityEngine;

namespace PokerTD
{
    public enum OfferKind { Machine, HandLevel, TowerSlot, MachineSlot, Item, Unlock }

    public enum ItemKind { Slow, ExtraDiscard, BurstMarked, Freeze, HandLevel, TowerSlot, MachineSlot }

    /// <summary>
    /// One thing for sale in the shop turn. The shop rolls a mixed row of these by weight
    /// (TDConfig, header Shop): buff machines, hand upgrades (like Balatro's Planet cards),
    /// extra tower or machine slots, one-use items and, rarely, the unlock of a locked hand.
    /// </summary>
    public class ShopOffer
    {
        public OfferKind kind;
        public TDConfig.BuffType machine;      // Machine
        public HandType hand;                   // HandLevel, Unlock
        public TDConfig.ItemType item;          // Item

        /// <summary>Price right now. Slot prices go up after each slot bought.</summary>
        public int Cost(TDGame game)
        {
            var c = game.config;
            switch (kind)
            {
                case OfferKind.Machine: return machine.cost;
                case OfferKind.HandLevel: return c.handLevelCost;
                case OfferKind.TowerSlot: return c.towerSlotCost + c.slotCostStep * game.TowerSlotsBought;
                case OfferKind.MachineSlot: return c.machineSlotCost + c.slotCostStep * game.MachineSlotsBought;
                case OfferKind.Item: return item.cost;
                default: return c.unlockCost;
            }
        }

        public string Title
        {
            get
            {
                switch (kind)
                {
                    case OfferKind.Machine: return machine.Short;
                    case OfferKind.HandLevel: return $"{HandEvaluator.Name(hand)} +1 level";
                    case OfferKind.TowerSlot: return "+1 tower slot";
                    case OfferKind.MachineSlot: return "+1 machine slot";
                    case OfferKind.Item: return item.name;
                    default: return $"Unlock {HandEvaluator.Name(hand)}";
                }
            }
        }

        public string Label
        {
            get
            {
                switch (kind)
                {
                    case OfferKind.Machine: return "buff machine";
                    case OfferKind.HandLevel: return "hand upgrade";
                    case OfferKind.TowerSlot:
                    case OfferKind.MachineSlot: return "slot · uncommon";
                    case OfferKind.Item: return "item · " + item.description;
                    default: return "unlock · rare";
                }
            }
        }

        public Color Color
        {
            get
            {
                switch (kind)
                {
                    case OfferKind.Machine: return machine.color;
                    case OfferKind.HandLevel: return new Color(0.45f, 0.75f, 1f);
                    case OfferKind.TowerSlot:
                    case OfferKind.MachineSlot: return new Color(0.5f, 0.9f, 0.5f);
                    case OfferKind.Item: return new Color(1f, 0.85f, 0.4f);
                    default: return new Color(1f, 0.45f, 0.85f);
                }
            }
        }
    }
}
