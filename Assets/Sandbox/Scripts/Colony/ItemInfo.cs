using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Names, colours and groups of the items. Raw materials come from the map, processed
    /// materials come from machines (Phase C), food feeds the family.
    /// </summary>
    public static class ItemInfo
    {
        /// <summary>Every real item, in the order the HUD shows them.</summary>
        public static readonly Item[] All =
        {
            Item.Food, Item.Wood, Item.Mud, Item.Stone,
            Item.Branches, Item.PressedWood, Item.Gravel, Item.Sand, Item.Cement,
        };

        /// <summary>Items a storage can be set to (branches count as food, so they have no storage of their own).</summary>
        public static readonly Item[] StorageTypes = { Item.Food, Item.Wood, Item.Mud, Item.Stone, Item.PressedWood, Item.Gravel, Item.Sand, Item.Cement };

        public static string Name(Item i)
        {
            switch (i)
            {
                case Item.PressedWood: return "Pressed wood";
                case Item.None: return "-";
                default: return i.ToString();
            }
        }

        public static string Lower(Item i) => Name(i).ToLower();

        public static Color Colour(Item i)
        {
            switch (i)
            {
                case Item.Food: return new Color(0.55f, 0.82f, 0.4f);
                case Item.Wood: return new Color(0.66f, 0.45f, 0.26f);
                case Item.Mud: return new Color(0.47f, 0.35f, 0.24f);
                case Item.Stone: return new Color(0.66f, 0.68f, 0.72f);
                case Item.Branches: return new Color(0.75f, 0.58f, 0.36f);
                case Item.PressedWood: return new Color(0.82f, 0.62f, 0.38f);
                case Item.Gravel: return new Color(0.55f, 0.55f, 0.55f);
                case Item.Sand: return new Color(0.88f, 0.8f, 0.58f);
                case Item.Cement: return new Color(0.78f, 0.78f, 0.75f);
                default: return Color.white;
            }
        }
    }
}
