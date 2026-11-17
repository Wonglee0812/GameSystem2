using UnityEngine;

namespace DamRush
{
    public enum ShapeKind { Block, Log, Ramp }

    /// <summary>A building part shape. Size is in metres before rotation.</summary>
    [CreateAssetMenu(menuName = "Dam Rush/Shape")]
    public class ShapeDef : ScriptableObject
    {
        public string displayName = "Block";
        public ShapeKind kind = ShapeKind.Block;
        public Vector3 size = Vector3.one;

        /// <summary>Real volume of the shape (m3), used for mass, buoyancy and cost.</summary>
        public float Volume
        {
            get
            {
                switch (kind)
                {
                    case ShapeKind.Log: return Mathf.PI * (size.y * 0.5f) * (size.z * 0.5f) * size.x;
                    case ShapeKind.Ramp: return size.x * size.y * size.z * 0.5f;
                    default: return size.x * size.y * size.z;
                }
            }
        }
    }
}
