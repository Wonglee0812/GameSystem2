using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Any prop the player can take into the item slot (single click). A plain
    /// holdable can only be thrown. A ToolItem (fan, hair dryer) can also be used.
    /// </summary>
    public class HoldableItem : MonoBehaviour
    {
        public string displayName = "Item";
        public Sprite icon;

        /// <summary>
        /// Left click while this item is in the slot. Return true when the click was used;
        /// a plain item returns false, so the slot throws it instead.
        /// </summary>
        public virtual bool Use(ToolBelt belt, Vector2 screen) => false;
    }
}
