using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// A raw building material. A part's stats come from material x shape,
    /// so 3 materials and 3 shapes give 9 parts without 9 hand-made prefabs.
    /// </summary>
    [CreateAssetMenu(menuName = "Dam Rush/Material")]
    public class MaterialDef : ScriptableObject
    {
        public string displayName = "Wood";
        public Color color = Color.white;
        [Tooltip("URP material used to draw parts of this material.")]
        public Material renderMaterial;
        [Tooltip("kg per m3. Water is 1000: lower floats, higher sinks.")]
        public float density = 600f;
        public float friction = 0.6f;
        [Tooltip("Newtons a glue joint holds (pull or shear) before it snaps. Only used when glue is involved.")]
        public float jointStrength = 40000f;
        [Tooltip("Speed change (m/s) in one hit that breaks the part into debris.")]
        public float toughness = 6f;
        [Tooltip("Budget cost per m3.")]
        public float costPerCubicMetre = 4f;

        [Header("Specific rules")]
        [Tooltip("Flowing water wears the part away (mud).")]
        public bool erodes;
        public float erosionPerSecond = 0.04f; // share of volume lost per second at 1 m/s flow
        [Tooltip("Glue (mud): joins the parts it touches and sticks to the ground when the flood starts.")]
        public bool gluesToGround;
    }
}
