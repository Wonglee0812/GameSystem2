using TMPro;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>One place for the look of the Brick Panic UI: fonts, sprites, icons and colours.
    /// The HUD builder (menu "Brick Panic/Build HUD In Scene") makes it once and fills it; after that
    /// Wong can change any value here and run the builder again. Fonts and the 9-slice sprites are the
    /// same as in Poker TD, so both games share one hand-drawn comic look.</summary>
    [CreateAssetMenu(menuName = "Brick Panic/UI Theme", fileName = "BP_UITheme")]
    public class BPTheme : ScriptableObject
    {
        [Header("Fonts")]
        [Tooltip("Big titles and buttons (Action Comics Black, capitals only).")]
        public TMP_FontAsset title;
        [Tooltip("Dark outline material for the title font (shared, so labels do not each make a material).")]
        public Material titleOutline;
        [Tooltip("Numbers and small text (Super Chips).")]
        public TMP_FontAsset number;
        [Tooltip("Dark outline material for the number font.")]
        public Material numberOutline;

        [Header("Sprites (9-slice, white, tinted in Unity)")]
        [Tooltip("Rounded panel with a dark outline (cards, pills, windows).")]
        public Sprite panel;
        [Tooltip("Small rounded pill with a dark outline (stats, key badges).")]
        public Sprite pill;
        [Tooltip("Chunky button with a lip at the bottom.")]
        public Sprite button;
        [Tooltip("Soft halo drawn behind the selected card.")]
        public Sprite glow;
        [Tooltip("Plain rounded shape without outline (bar fills).")]
        public Sprite fill;

        [Header("Icons (white, tinted in Unity)")]
        public Sprite money;
        public Sprite person;
        public Sprite goal;
        public Sprite dead;
        public Sprite wear;
        public Sprite people;
        public Sprite rubble;
        public Sprite trophy;
        public Sprite warning;
        public Sprite restart;
        public Sprite quake;
        public Sprite tsunami;
        public Sprite volcano;
        public Sprite tornado;
        public Sprite lightning;

        [Header("Colours")]
        [Tooltip("Card faces and light text. Default warm white.")]
        public Color cream = new Color(1f, 0.97f, 0.9f);
        [Tooltip("Dark text and outlines. Default dark plum.")]
        public Color ink = new Color(0.17f, 0.13f, 0.2f);
        [Tooltip("Titles, money and the selected glow. Default sunny yellow.")]
        public Color yellow = new Color(1f, 0.8f, 0.2f);
        [Tooltip("Main buttons (START, CONTINUE). Default bright orange.")]
        public Color orange = new Color(1f, 0.52f, 0.2f);
        [Tooltip("Danger: dead people, not enough people, game over. Default red.")]
        public Color red = new Color(1f, 0.3f, 0.3f);
        [Tooltip("Good news: survivors, reward. Default mint green.")]
        public Color green = new Color(0.3f, 0.85f, 0.5f);
        [Tooltip("Second buttons and info. Default sky blue.")]
        public Color blue = new Color(0.25f, 0.6f, 1f);
        [Tooltip("Dark see-through zone behind the hotbar. Default plum at 55%.")]
        public Color zone = new Color(0.12f, 0.08f, 0.16f, 0.55f);

        [Header("Disaster colours")]
        public Color quakeColor = new Color(1f, 0.62f, 0.2f);
        public Color tsunamiColor = new Color(0.25f, 0.7f, 1f);
        public Color volcanoColor = new Color(1f, 0.32f, 0.2f);
        public Color tornadoColor = new Color(0.55f, 0.62f, 0.7f);
        public Color lightningColor = new Color(0.62f, 0.45f, 1f);
    }
}
