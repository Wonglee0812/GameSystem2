using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>One brick material (plastic, wood, stone, steel). Shape x Material makes every brick.</summary>
    [CreateAssetMenu(menuName = "Brick Panic/Material", fileName = "MAT_New")]
    public class MaterialDef : ScriptableObject
    {
        [Tooltip("Name shown to the player. Default: Plastic.")]
        public string displayName = "Plastic";
        [Tooltip("Brick colour. Default: white.")]
        public Color color = Color.white;
        [Tooltip("How shiny the surface is, 0-1. Default 0.5.")]
        [Range(0f, 1f)] public float smoothness = 0.5f;
        [Tooltip("Metal look, 0-1. Default 0.")]
        [Range(0f, 1f)] public float metallic = 0f;
        [Tooltip("Template material (URP Lit). Its colour is replaced by Color. Default: M_BP_Brick.")]
        public Material baseMaterial;

        [Header("Economy")]
        [Tooltip("Price in coins for one stud of a full brick. A plate costs a third. Default 1.")]
        public float pricePerStud = 1f;

        [Header("Physics")]
        [Tooltip("Mass in kg of one stud of a full brick. A plate weighs a third. Default 1.")]
        public float massPerStud = 1f;
        [Tooltip("Pull/push force in newton that one overlapping stud can hold before the joint breaks. Joint force = this x overlapping studs (the weaker material of the two counts). Default 400.")]
        public float breakForcePerStud = 400f;
        [Tooltip("Twist force in newton-metre that one overlapping stud can hold before the joint breaks. Default 150.")]
        public float breakTorquePerStud = 150f;
        [Tooltip("Durability: seconds a joint of this material lasts at full disaster strength (intensity 1) with no load. Load makes it wear faster (up to 3x). Every material breaks in the end; this only buys time. Default 20.")]
        public float durability = 20f;
        [Tooltip("Surface friction, 0-1. Default 0.6.")]
        [Range(0f, 1f)] public float friction = 0.6f;

        [Header("Disasters (used later)")]
        [Tooltip("True = floats in tsunami water. Default true.")]
        public bool floats = true;
        [Tooltip("Seconds this material survives lava heat. 0 = never burns. Default 0.")]
        public float heatSeconds = 0f;

        Material render;
        PhysicsMaterial physics;
        Dictionary<Color, Material> tinted;

        /// <summary>The same material in another colour (used for leaves, roofs and rock shades). Made once per colour.</summary>
        public Material Tinted(Color c)
        {
            if (tinted == null) tinted = new Dictionary<Color, Material>();
            if (tinted.TryGetValue(c, out var m) && m != null) return m;
            m = new Material(RenderMaterial) { name = "M_" + displayName + "_tint" };
            m.SetColor("_BaseColor", c);
            tinted[c] = m;
            return m;
        }

        /// <summary>Shared render material for all bricks of this material (made once).</summary>
        public Material RenderMaterial
        {
            get
            {
                if (render != null) return render;
                render = baseMaterial != null ? new Material(baseMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                render.name = "M_" + displayName;
                render.SetColor("_BaseColor", color);
                render.SetFloat("_Smoothness", smoothness);
                render.SetFloat("_Metallic", metallic);
                return render;
            }
        }

        /// <summary>Shared physics material (friction) for this material.</summary>
        public PhysicsMaterial PhysicsMat
        {
            get
            {
                if (physics != null) return physics;
                physics = new PhysicsMaterial("PM_" + displayName)
                {
                    dynamicFriction = friction,
                    staticFriction = friction,
                    bounciness = 0f,
                    frictionCombine = PhysicsMaterialCombine.Average,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };
                return physics;
            }
        }

        void OnValidate() { render = null; physics = null; tinted = null; }
    }
}
