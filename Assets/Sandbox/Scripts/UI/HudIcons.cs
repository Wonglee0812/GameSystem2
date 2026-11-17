using System;
using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Simple flat icons for the HUD, painted in code from distance functions (circles, boxes,
    /// capsules, triangles, hexagons), so the HUD has icons before real art exists. Every shape
    /// gets a dark ink outline and a little light from the top, close to the hand-drawn icon
    /// style of Sandcastle (Bubblebird Studio). Icons are made once and cached.
    /// To use drawn art later, put a texture with the same name in Resources/HudIcons/
    /// (for example Resources/HudIcons/Dam.png): it is used instead of the painted one.
    /// </summary>
    public static class HudIcons
    {
        const int S = 96;          // pixels
        const float Outline = 0.07f;
        static readonly Color Ink = new Color(0.2f, 0.15f, 0.1f);
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        // colours
        static readonly Color Wood = new Color(0.66f, 0.45f, 0.26f), WoodLight = new Color(0.86f, 0.68f, 0.45f);
        static readonly Color Mud = new Color(0.5f, 0.37f, 0.25f), Stone = new Color(0.66f, 0.68f, 0.72f), StoneDark = new Color(0.5f, 0.52f, 0.56f);
        static readonly Color Water = new Color(0.32f, 0.72f, 0.9f), Leaf = new Color(0.45f, 0.72f, 0.3f), LeafDark = new Color(0.32f, 0.56f, 0.22f);
        static readonly Color Sand = new Color(0.93f, 0.8f, 0.55f), Gold = new Color(0.96f, 0.76f, 0.3f), Red = new Color(0.88f, 0.32f, 0.25f);
        static readonly Color Pink = new Color(0.93f, 0.5f, 0.6f), Cream = new Color(0.98f, 0.95f, 0.88f), Glass = new Color(0.75f, 0.9f, 0.95f);
        static readonly Color Cement = new Color(0.62f, 0.66f, 0.72f), Blue = new Color(0.3f, 0.5f, 0.85f);

        struct Layer { public Func<Vector2, float> d; public Color c; public bool outline; }
        static Layer L(Func<Vector2, float> d, Color c, bool outline = true) => new Layer { d = d, c = c, outline = outline };

        public static Texture2D Get(string name)
        {
            if (cache.TryGetValue(name, out var t) && t != null) return t;
            t = Resources.Load<Texture2D>("HudIcons/" + name);
            if (t == null) t = Paint(Shapes(name));
            cache[name] = t;
            return t;
        }

        public static Texture2D ForItem(Item i) => Get(i.ToString());

        // ------------------------------------------------------------------ shapes

        static Layer[] Shapes(string n)
        {
            switch (n)
            {
                // ---- materials
                case "Food": return new[] {
                    L(p => Seg(p, new Vector2(0f, -0.85f), new Vector2(0f, -0.1f), 0.06f), LeafDark),
                    L(p => Lens(Rot(p, new Vector2(-0.35f, 0.05f), 35f), 0.75f, 0.32f), Leaf),
                    L(p => Lens(Rot(p, new Vector2(0.35f, 0.15f), -35f), 0.7f, 0.3f), Leaf),
                    L(p => Lens(Rot(p, new Vector2(0f, 0.45f), 0f), 0.55f, 0.24f), LeafDark) };
                case "Wood": return new[] {
                    L(p => Seg(p, new Vector2(-0.6f, -0.3f), new Vector2(0.45f, 0.25f), 0.33f), Wood),
                    L(p => Circle(p, new Vector2(0.5f, 0.28f), 0.33f), WoodLight),
                    L(p => Circle(p, new Vector2(0.5f, 0.28f), 0.12f), Wood, false) };
                case "Mud": return new[] {
                    L(p => Min(Circle(p, new Vector2(-0.38f, -0.25f), 0.42f), Circle(p, new Vector2(0.35f, -0.28f), 0.4f), Circle(p, new Vector2(0f, 0.15f), 0.45f)), Mud),
                    L(p => Circle(p, new Vector2(-0.1f, 0.3f), 0.1f), new Color(0.62f, 0.48f, 0.34f), false) };
                case "Stone": return new[] {
                    L(p => Hex(Rot(p, new Vector2(0f, -0.05f), 12f), 0.68f) - 0.08f, Stone),
                    L(p => Box(Rot(p, new Vector2(-0.2f, 0.25f), 30f), new Vector2(0.22f, 0.07f), 0.05f), Cream, false) };
                case "PressedWood": return new[] {
                    L(p => Box(p - new Vector2(0f, -0.45f), new Vector2(0.8f, 0.17f), 0.05f), Wood),
                    L(p => Box(p - new Vector2(0f, -0.05f), new Vector2(0.8f, 0.17f), 0.05f), WoodLight),
                    L(p => Box(p - new Vector2(0f, 0.35f), new Vector2(0.8f, 0.17f), 0.05f), Wood) };
                case "Gravel": return new[] {
                    L(p => Circle(p, new Vector2(-0.45f, -0.4f), 0.28f), Stone), L(p => Circle(p, new Vector2(0.2f, -0.45f), 0.3f), StoneDark),
                    L(p => Circle(p, new Vector2(0.55f, 0.05f), 0.25f), Stone), L(p => Circle(p, new Vector2(-0.15f, 0.15f), 0.3f), Stone),
                    L(p => Circle(p, new Vector2(0.15f, 0.6f), 0.22f), StoneDark) };
                case "Sand": return new[] {
                    L(p => Ellipse(p - new Vector2(0f, -0.35f), new Vector2(0.88f, 0.5f)), Sand),
                    L(p => Circle(p, new Vector2(-0.3f, -0.2f), 0.06f), Mud, false), L(p => Circle(p, new Vector2(0.2f, -0.05f), 0.06f), Mud, false),
                    L(p => Circle(p, new Vector2(0.35f, -0.4f), 0.06f), Mud, false) };
                case "Cement": return new[] {
                    L(p => Box(p - new Vector2(0f, -0.12f), new Vector2(0.55f, 0.68f), 0.2f), Cement),
                    L(p => Seg(p, new Vector2(-0.3f, 0.62f), new Vector2(0.3f, 0.62f), 0.1f), StoneDark),
                    L(p => Box(p - new Vector2(0f, -0.15f), new Vector2(0.32f, 0.16f), 0.05f), Cream, false) };

                // ---- tools
                case "Dam": return new[] {
                    L(p => Box(p - new Vector2(0f, -0.72f), new Vector2(0.95f, 0.2f), 0.1f), Water),
                    L(p => Seg(p, new Vector2(-0.5f, -0.5f), new Vector2(-0.5f, 0.55f), 0.21f), Wood),
                    L(p => Seg(p, new Vector2(0f, -0.55f), new Vector2(0f, 0.7f), 0.21f), WoodLight),
                    L(p => Seg(p, new Vector2(0.5f, -0.5f), new Vector2(0.5f, 0.5f), 0.21f), Wood) };
                case "DamMk2": return new[] {
                    L(p => Box(p - new Vector2(0f, -0.62f), new Vector2(0.9f, 0.25f), 0.08f), Stone),
                    L(p => Seg(p, new Vector2(-0.5f, -0.25f), new Vector2(-0.5f, 0.65f), 0.2f), Wood),
                    L(p => Seg(p, new Vector2(0f, -0.25f), new Vector2(0f, 0.75f), 0.2f), WoodLight),
                    L(p => Seg(p, new Vector2(0.5f, -0.25f), new Vector2(0.5f, 0.6f), 0.2f), Wood) };
                case "Spillway": return new[] {
                    L(p => Box(p - new Vector2(-0.5f, 0f), new Vector2(0.3f, 0.7f), 0.05f), Stone),
                    L(p => Box(p - new Vector2(0.05f, -0.25f), new Vector2(0.28f, 0.45f), 0.05f), StoneDark),
                    L(p => Box(p - new Vector2(0.55f, -0.5f), new Vector2(0.28f, 0.2f), 0.05f), Stone),
                    L(p => Seg(p, new Vector2(-0.5f, 0.78f), new Vector2(0.8f, -0.25f), 0.08f), Water) };
                case "Dig": return new[] {
                    L(p => Seg(p, new Vector2(-0.7f, 0.75f), new Vector2(0.15f, -0.1f), 0.09f), Wood),
                    L(p => Seg(p, new Vector2(-0.85f, 0.6f), new Vector2(-0.55f, 0.9f), 0.1f), Wood),
                    L(p => Box(Rot(p, new Vector2(0.38f, -0.38f), 45f), new Vector2(0.24f, 0.36f), 0.18f), Stone) };
                case "Levee": return new[] {
                    L(p => Box(p - new Vector2(-0.6f, -0.5f), new Vector2(0.35f, 0.3f), 0.05f), Water),
                    L(p => Max(Ellipse(p - new Vector2(0.1f, -0.75f), new Vector2(0.85f, 0.75f)), -(p.y + 0.75f)), Mud),
                    L(p => Seg(p, new Vector2(-0.3f, 0f), new Vector2(0.4f, 0f), 0.05f), new Color(0.62f, 0.48f, 0.34f), false) };
                case "Floodgate": return new[] {
                    L(p => Box(p - new Vector2(0f, -0.75f), new Vector2(0.95f, 0.15f), 0.08f), Water),
                    L(p => Box(p - new Vector2(-0.6f, 0f), new Vector2(0.14f, 0.85f), 0.05f), Stone),
                    L(p => Box(p - new Vector2(0.6f, 0f), new Vector2(0.14f, 0.85f), 0.05f), Stone),
                    L(p => Box(p - new Vector2(0f, -0.25f), new Vector2(0.45f, 0.14f), 0.04f), Wood),
                    L(p => Box(p - new Vector2(0f, 0.08f), new Vector2(0.45f, 0.14f), 0.04f), WoodLight),
                    L(p => Seg(p, new Vector2(0f, 0.25f), new Vector2(0f, 0.75f), 0.05f), StoneDark) };
                case "Sandbag": return new[] {
                    L(p => Ellipse(p - new Vector2(-0.42f, -0.45f), new Vector2(0.42f, 0.25f)), Sand),
                    L(p => Ellipse(p - new Vector2(0.42f, -0.45f), new Vector2(0.42f, 0.25f)), Sand),
                    L(p => Ellipse(p - new Vector2(0f, 0f), new Vector2(0.42f, 0.25f)), new Color(0.88f, 0.74f, 0.5f)),
                    L(p => Ellipse(p - new Vector2(0f, 0.45f), new Vector2(0.38f, 0.22f)), Sand) };
                case "Base": return new[] {
                    L(p => Hex(p - new Vector2(0f, -0.1f), 0.78f), Wood),
                    L(p => Hex(p - new Vector2(0f, -0.1f), 0.48f), WoodLight),
                    L(p => Seg(p, new Vector2(-0.3f, -0.1f), new Vector2(0.3f, -0.1f), 0.05f), Wood, false) };
                case "PumpBase": return new[] {
                    L(p => Hex(p - new Vector2(-0.15f, -0.2f), 0.68f), Wood),
                    L(p => Min(Circle(p, new Vector2(0.45f, 0.1f), 0.3f), Tri(p, new Vector2(0.2f, 0.25f), new Vector2(0.7f, 0.25f), new Vector2(0.45f, 0.85f))), Water) };
                case "Nursery": return new[] {
                    L(p => Hex(p - new Vector2(0f, -0.2f), 0.72f), Wood),
                    L(p => Min(Circle(p, new Vector2(-0.17f, 0.12f), 0.22f), Circle(p, new Vector2(0.17f, 0.12f), 0.22f), Tri(p, new Vector2(-0.37f, 0.04f), new Vector2(0.37f, 0.04f), new Vector2(0f, -0.42f))), Pink) };
                case "Storage": return new[] {
                    L(p => Box(p - new Vector2(0f, -0.08f), new Vector2(0.7f, 0.62f), 0.06f), WoodLight),
                    L(p => Seg(p, new Vector2(-0.58f, -0.58f), new Vector2(0.58f, 0.42f), 0.07f), Wood, false),
                    L(p => Seg(p, new Vector2(-0.58f, 0.42f), new Vector2(0.58f, -0.58f), 0.07f), Wood, false),
                    L(p => Seg(p, new Vector2(-0.7f, 0.54f), new Vector2(0.7f, 0.54f), 0.06f), Wood, false) };
                case "FoodFarm": return new[] {
                    L(p => Box(p - new Vector2(0f, -0.62f), new Vector2(0.95f, 0.28f), 0.1f), Water),
                    L(p => Lens(Rot(p, new Vector2(-0.35f, 0f), 25f), 0.65f, 0.28f), Leaf),
                    L(p => Lens(Rot(p, new Vector2(0.3f, 0.1f), -25f), 0.7f, 0.3f), LeafDark),
                    L(p => Lens(Rot(p, new Vector2(0f, 0.3f), 0f), 0.5f, 0.22f), Leaf) };
                case "MaterialLab": return new[] {
                    L(p => Box(p - new Vector2(0f, 0.45f), new Vector2(0.17f, 0.35f), 0.04f), Glass),
                    L(p => Circle(p, new Vector2(0f, -0.3f), 0.55f), Glass),
                    L(p => Max(Circle(p, new Vector2(0f, -0.3f), 0.45f), p.y + 0.25f), Leaf, false),
                    L(p => Circle(p, new Vector2(0.15f, -0.45f), 0.08f), Cream, false) };
                case "BuildingLab": return new[] {
                    L(p => Box(p, new Vector2(0.75f, 0.6f), 0.08f), Blue),
                    L(p => Hex(p - new Vector2(-0.15f, 0f), 0.35f), Cream, false),
                    L(p => Hex(p - new Vector2(-0.15f, 0f), 0.25f), Blue, false),
                    L(p => Seg(p, new Vector2(0.3f, -0.4f), new Vector2(0.75f, 0.45f), 0.11f), Gold) };
                case "WaterWheel": return new[] {
                    L(p => Box(p - new Vector2(0f, -0.78f), new Vector2(0.95f, 0.16f), 0.08f), Water),
                    L(p => Mathf.Abs(Circle(p, Vector2.zero, 0.62f)) - 0.1f, Wood),
                    L(p => Min(Seg(p, new Vector2(-0.62f, 0f), new Vector2(0.62f, 0f), 0.06f), Seg(p, new Vector2(0f, -0.62f), new Vector2(0f, 0.62f), 0.06f),
                        Seg(p, new Vector2(-0.44f, -0.44f), new Vector2(0.44f, 0.44f), 0.06f), Seg(p, new Vector2(-0.44f, 0.44f), new Vector2(0.44f, -0.44f), 0.06f)), Wood),
                    L(p => Circle(p, Vector2.zero, 0.14f), WoodLight) };
                case "Machine": case "Industry": return new[] {
                    L(p => Gear(p, 0.55f, 0.2f, 8), Stone),
                    L(p => Circle(p, Vector2.zero, 0.2f), StoneDark) };
                case "AlarmBell": return new[] {
                    L(p => Min(Circle(p, new Vector2(0f, 0.15f), 0.48f), Box(p - new Vector2(0f, -0.2f), new Vector2(0.48f, 0.3f), 0.02f), Box(p - new Vector2(0f, -0.5f), new Vector2(0.68f, 0.1f), 0.08f)), Gold),
                    L(p => Circle(p, new Vector2(0f, -0.72f), 0.13f), Gold),
                    L(p => Seg(p, new Vector2(0f, 0.62f), new Vector2(0f, 0.8f), 0.08f), StoneDark) };
                case "Fell": return new[] {
                    L(p => Seg(p, new Vector2(-0.55f, -0.75f), new Vector2(0.3f, 0.6f), 0.09f), Wood),
                    L(p => Box(Rot(p, new Vector2(0.25f, 0.45f), -32f), new Vector2(0.38f, 0.2f), 0.06f), Stone) };
                case "Priority": return new[] {
                    L(p => Seg(p, new Vector2(-0.45f, -0.85f), new Vector2(-0.45f, 0.8f), 0.07f), Wood),
                    L(p => Tri(p, new Vector2(-0.4f, 0.8f), new Vector2(-0.4f, 0.05f), new Vector2(0.7f, 0.42f)), Red) };
                case "Repair": case "Tools": return new[] {
                    L(p => Seg(p, new Vector2(-0.6f, -0.7f), new Vector2(0.2f, 0.3f), 0.1f), Wood),
                    L(p => Box(Rot(p, new Vector2(0.28f, 0.42f), -38f), new Vector2(0.45f, 0.18f), 0.06f), Stone) };

                // ---- categories and menu buttons
                case "Water": return new[] {
                    L(p => Min(Circle(p, new Vector2(0f, -0.25f), 0.5f), Tri(p, new Vector2(-0.45f, -0.05f), new Vector2(0.45f, -0.05f), new Vector2(0f, 0.85f))), Water),
                    L(p => Circle(p, new Vector2(-0.18f, -0.35f), 0.1f), Cream, false) };
                case "Buildings": return new[] {
                    L(p => Box(p - new Vector2(0f, -0.35f), new Vector2(0.55f, 0.45f), 0.05f), WoodLight),
                    L(p => Tri(p, new Vector2(-0.8f, 0.05f), new Vector2(0.8f, 0.05f), new Vector2(0f, 0.8f)), Red),
                    L(p => Box(p - new Vector2(0f, -0.5f), new Vector2(0.16f, 0.3f), 0.04f), Wood) };
                case "Work": return new[] {
                    L(p => Ellipse(p - new Vector2(-0.45f, -0.6f), new Vector2(0.35f, 0.32f)), Wood), L(p => Circle(p, new Vector2(-0.45f, -0.05f), 0.24f), WoodLight),
                    L(p => Ellipse(p - new Vector2(0.45f, -0.6f), new Vector2(0.35f, 0.32f)), Wood), L(p => Circle(p, new Vector2(0.45f, -0.05f), 0.24f), WoodLight),
                    L(p => Ellipse(p - new Vector2(0f, -0.35f), new Vector2(0.38f, 0.35f)), Mud), L(p => Circle(p, new Vector2(0f, 0.3f), 0.27f), WoodLight) };
                case "Family": return new[] {
                    L(p => Min(Circle(p, new Vector2(-0.3f, 0.2f), 0.38f), Circle(p, new Vector2(0.3f, 0.2f), 0.38f), Tri(p, new Vector2(-0.66f, 0.05f), new Vector2(0.66f, 0.05f), new Vector2(0f, -0.75f))), Pink) };
                case "Events": return new[] {
                    L(p => Box(p, new Vector2(0.6f, 0.75f), 0.1f), Cream),
                    L(p => Min(Seg(p, new Vector2(-0.35f, 0.4f), new Vector2(0.35f, 0.4f), 0.05f), Seg(p, new Vector2(-0.35f, 0.1f), new Vector2(0.35f, 0.1f), 0.05f),
                        Seg(p, new Vector2(-0.35f, -0.2f), new Vector2(0.35f, -0.2f), 0.05f), Seg(p, new Vector2(-0.35f, -0.5f), new Vector2(0.1f, -0.5f), 0.05f)), StoneDark, false) };
                case "Orders": return new[] {
                    L(p => Box(p - new Vector2(0f, -0.08f), new Vector2(0.58f, 0.72f), 0.08f), WoodLight),
                    L(p => Box(p - new Vector2(0f, -0.12f), new Vector2(0.45f, 0.55f), 0.04f), Cream, false),
                    L(p => Box(p - new Vector2(0f, 0.68f), new Vector2(0.25f, 0.12f), 0.05f), Stone),
                    L(p => Min(Seg(p, new Vector2(-0.28f, 0.2f), new Vector2(0.28f, 0.2f), 0.05f), Seg(p, new Vector2(-0.28f, -0.1f), new Vector2(0.28f, -0.1f), 0.05f), Seg(p, new Vector2(-0.28f, -0.4f), new Vector2(0.1f, -0.4f), 0.05f)), StoneDark, false) };
                case "Beaver": return new[] {
                    L(p => Circle(p, new Vector2(-0.5f, 0.5f), 0.2f), Wood), L(p => Circle(p, new Vector2(0.5f, 0.5f), 0.2f), Wood),
                    L(p => Ellipse(p - new Vector2(0f, 0f), new Vector2(0.7f, 0.65f)), Wood),
                    L(p => Ellipse(p - new Vector2(0f, -0.25f), new Vector2(0.35f, 0.25f)), WoodLight, false),
                    L(p => Circle(p, new Vector2(-0.25f, 0.15f), 0.08f), Ink, false), L(p => Circle(p, new Vector2(0.25f, 0.15f), 0.08f), Ink, false),
                    L(p => Box(p - new Vector2(0f, -0.42f), new Vector2(0.1f, 0.1f), 0.02f), Cream, false) };
                case "Sun": return new[] { L(p => Circle(p, Vector2.zero, 0.5f), Gold) };
                case "Moon": return new[] { L(p => Max(Circle(p, Vector2.zero, 0.55f), -Circle(p, new Vector2(0.3f, 0.2f), 0.45f)), new Color(0.8f, 0.85f, 1f)) };
                case "Help": return new[] { L(p => Circle(p, Vector2.zero, 0.75f), Blue) };
                default: return new[] { L(p => Circle(p, Vector2.zero, 0.6f), Stone) };
            }
        }

        // ------------------------------------------------------------------ painter

        static Texture2D Paint(Layer[] layers)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[S * S];
            float pixel = 2f / S; // size of one pixel in shape units
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    var p = new Vector2((x + 0.5f) / S * 2f - 1f, (y + 0.5f) / S * 2f - 1f);
                    Color c = new Color(0, 0, 0, 0);
                    foreach (var l in layers)
                    {
                        float d = l.d(p);
                        if (l.outline)
                        {
                            float ao = Mathf.Clamp01(0.5f - (d - Outline) / pixel);
                            c = Over(c, Ink, ao);
                        }
                        float a = Mathf.Clamp01(0.5f - d / pixel);
                        if (a <= 0f) continue;
                        // a little light from the top, darker at the bottom
                        Color f = l.c * (0.92f + 0.14f * p.y); f.a = 1f;
                        c = Over(c, f, a);
                    }
                    px[y * S + x] = c;
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        static Color Over(Color dst, Color src, float a)
        {
            float outA = a + dst.a * (1f - a);
            if (outA <= 0f) return new Color(0, 0, 0, 0);
            Color c = (src * a + dst * dst.a * (1f - a)) / outA;
            c.a = outA;
            return c;
        }

        // ------------------------------------------------------------------ distance functions (Quilez, n.d.)

        static float Min(params float[] v) { float m = float.MaxValue; foreach (var x in v) m = Mathf.Min(m, x); return m; }
        static float Max(float a, float b) => Mathf.Max(a, b);
        static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
        static float Ellipse(Vector2 p, Vector2 half)
        {
            float k = Mathf.Min(half.x, half.y);
            return (new Vector2(p.x / half.x, p.y / half.y).magnitude - 1f) * k;
        }
        static float Box(Vector2 p, Vector2 half, float round)
        {
            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + new Vector2(round, round);
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - round;
        }
        static float Seg(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }
        /// <summary>Leaf shape: the overlap of two circles, length along y.</summary>
        static float Lens(Vector2 p, float length, float width)
        {
            float r = (length * length + width * width) / (2f * width);
            return Mathf.Max(Circle(p, new Vector2(width - r, 0f), r), Circle(p, new Vector2(r - width, 0f), r)) + 0f * length;
        }
        static Vector2 Rot(Vector2 p, Vector2 centre, float deg)
        {
            float a = -deg * Mathf.Deg2Rad;
            var q = p - centre;
            return new Vector2(q.x * Mathf.Cos(a) - q.y * Mathf.Sin(a), q.x * Mathf.Sin(a) + q.y * Mathf.Cos(a));
        }
        static float Hex(Vector2 p, float r)
        {
            var k = new Vector3(-0.866025404f, 0.5f, 0.577350269f);
            p = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y));
            var kxy = new Vector2(k.x, k.y);
            p -= 2f * Mathf.Min(Vector2.Dot(kxy, p), 0f) * kxy;
            p -= new Vector2(Mathf.Clamp(p.x, -k.z * r, k.z * r), r);
            return p.magnitude * Mathf.Sign(p.y);
        }
        static float Tri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 e0 = b - a, e1 = c - b, e2 = a - c;
            Vector2 v0 = p - a, v1 = p - b, v2 = p - c;
            Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
            float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            Vector2 d = Vector2.Min(Vector2.Min(new Vector2(pq0.sqrMagnitude, s * (v0.x * e0.y - v0.y * e0.x)),
                                                new Vector2(pq1.sqrMagnitude, s * (v1.x * e1.y - v1.y * e1.x))),
                                                new Vector2(pq2.sqrMagnitude, s * (v2.x * e2.y - v2.y * e2.x)));
            return -Mathf.Sqrt(d.x) * Mathf.Sign(d.y);
        }
        static float Gear(Vector2 p, float r, float tooth, int teeth)
        {
            float d = Circle(p, Vector2.zero, r);
            for (int i = 0; i < teeth; i++)
            {
                float a = i * 360f / teeth;
                var dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                d = Mathf.Min(d, Box(Rot(p, dir * r, a), new Vector2(tooth, 0.12f), 0.04f));
            }
            return d;
        }
    }
}
