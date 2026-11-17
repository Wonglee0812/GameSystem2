using UnityEngine;

namespace DamRush
{
    /// <summary>A place where builders can take materials: a storage or a pile on the ground.</summary>
    public interface ISupply
    {
        HexCell Hex { get; }
        int Stock(Item item);
        Vector3 Door(Vector3 from);
    }

    /// <summary>
    /// Materials lying on the ground. When water wears a dam down, or the leader removes an
    /// order, half of the materials in it are not lost: they fall next to the hex as a pile.
    /// Builders take from a pile when it is closer than a storage, and gatherers carry piles
    /// of their own material back to a storage.
    /// </summary>
    public class MaterialPile : ISupply
    {
        public readonly HexCell hex;
        public readonly Item item;
        public int count;
        public readonly Vector3 pos;
        public GameObject visual;

        public MaterialPile(HexCell hex, Item item, int count, Vector3 pos)
        {
            this.hex = hex; this.item = item; this.count = count; this.pos = pos;
        }

        public HexCell Hex => hex;
        public int Stock(Item i) => i == item ? count : 0;
        public Vector3 Door(Vector3 from) => pos + (from - pos).normalized * 0.4f;
    }
}
