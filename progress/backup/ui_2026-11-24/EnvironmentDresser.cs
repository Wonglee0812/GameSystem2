using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>Places map pieces made of normal bricks: trees, bushes, rocks and huts.
    /// They are not locked: the player can move them for free or sell them.</summary>
    public class EnvironmentDresser : MonoBehaviour
    {
        [Header("Bricks")]
        public ShapeDef brick2x2;
        public ShapeDef brick2x4;
        public ShapeDef plate4x4;
        public ShapeDef round1x1;
        public MaterialDef wood, stone, plastic;

        [Header("Colours")]
        [Tooltip("Leaf colours (one is picked per tree or bush). Keep them darker than the grass so trees stand out.")]
        public Color[] leafColors = { new Color(0.1f, 0.42f, 0.2f), new Color(0.2f, 0.5f, 0.12f), new Color(0.08f, 0.36f, 0.26f), new Color(0.85f, 0.55f, 0.15f) };
        [Tooltip("Roof colour of the huts.")]
        public Color roofColor = new Color(0.85f, 0.27f, 0.2f);
        [Tooltip("Rock colours (one is picked per rock).")]
        public Color[] rockColors = { new Color(0.55f, 0.55f, 0.57f), new Color(0.48f, 0.47f, 0.47f), new Color(0.62f, 0.6f, 0.58f) };

        [Header("Density (chance per cell)")]
        [Tooltip("Chance of a tree on a grass cell. Default 0.05.")]
        [Range(0f, 0.2f)] public float treeChance = 0.05f;
        [Tooltip("Chance of a bush on a grass cell. Default 0.035.")]
        [Range(0f, 0.2f)] public float bushChance = 0.035f;
        [Tooltip("Chance of a rock on a rock cell (a fifth of that on sand). Default 0.05.")]
        [Range(0f, 0.2f)] public float rockChance = 0.05f;
        [Tooltip("Chance of a hut on a grass cell. Default 0.006.")]
        [Range(0f, 0.05f)] public float hutChance = 0.006f;
        [Tooltip("Cells from the coast with no map pieces. Default 2.")]
        public int coastMargin = 2;

        Transform root;
        bool[,] used;
        MapGenerator map;

        public void Dress(MapGenerator m, System.Random rng)
        {
            map = m;
            if (root != null) Destroy(root.gameObject);
            root = new GameObject("MapPieces").transform;
            used = new bool[map.size, map.size];
            int count = 0;
            for (int i = 0; i < map.size; i++)
                for (int j = 0; j < map.size; j++)
                {
                    if (!Free(i, j, 1)) continue;
                    var z = map.Zones[i, j];
                    double roll = rng.NextDouble();
                    if (z == MapGenerator.ZoneType.Grass)
                    {
                        if (roll < hutChance && Free(i, j, 2)) { Hut(i, j); count++; }
                        else if (roll < hutChance + treeChance && Free(i, j, 1)) { Tree(i, j, rng); count++; }
                        else if (roll < hutChance + treeChance + bushChance) { Bush(i, j, rng); count++; }
                    }
                    else if (z == MapGenerator.ZoneType.Rock && roll < rockChance && Free(i, j, 1)) { Rock(i, j, rng); count++; }
                    else if (z == MapGenerator.ZoneType.Sand && roll < rockChance * 0.2f && Free(i, j, 1)) { Rock(i, j, rng); count++; }
                }
            Debug.Log($"[BrickPanic] map pieces: {count}");
        }

        /// <summary>True when all cells within r are flat (same height), on the island, not near the coast and not used.</summary>
        bool Free(int i, int j, int r)
        {
            if (!map.Inside(i, j)) return false;
            int h = map.Layers[i, j];
            for (int a = i - r - coastMargin; a <= i + r + coastMargin; a++)
                for (int b = j - r - coastMargin; b <= j + r + coastMargin; b++)
                    if (!map.Inside(a, b)) return false;
            for (int a = i - r; a <= i + r; a++)
                for (int b = j - r; b <= j + r; b++)
                {
                    if (used[a, b] || map.Layers[a, b] != h) return false;
                    var z = map.Zones[a, b];
                    if (z == MapGenerator.ZoneType.Volcano || z == MapGenerator.ZoneType.Crater) return false;
                }
            return true;
        }

        void Mark(int i, int j, int r)
        {
            for (int a = i - r; a <= i + r; a++)
                for (int b = j - r; b <= j + r; b++)
                    used[a, b] = true;
        }

        Vector3 Base(int i, int j) => map.transform.TransformPoint(map.CellCentre(i, j)) + Vector3.up * (map.TopY(i, j) - map.transform.position.y);

        Block Put(ShapeDef s, MaterialDef m, Vector3 p, int turns, Color? tint = null) => BlockFactory.Create(s, m, p, turns, root, tint);

        void Tree(int i, int j, System.Random rng)
        {
            Mark(i, j, 1);
            Vector3 p = Base(i, j);
            int trunk = 2 + rng.Next(0, 3);
            float h = BrickGrid.BrickHeight;
            for (int k = 0; k < trunk; k++) Put(brick2x2, wood, p + Vector3.up * (k * h), 0);
            var leaf = leafColors[rng.Next(leafColors.Length)];
            float y = trunk * h;
            Put(plate4x4, plastic, p + Vector3.up * y, 0, leaf);
            y += BrickGrid.PlateHeight;
            Put(brick2x4, plastic, p + Vector3.up * y, rng.Next(0, 2), leaf);
            y += h;
            Put(brick2x2, plastic, p + Vector3.up * y, 0, leaf);
        }

        void Bush(int i, int j, System.Random rng)
        {
            Mark(i, j, 0);
            Vector3 p = Base(i, j);
            var leaf = leafColors[rng.Next(leafColors.Length)];
            Put(brick2x2, plastic, p, 0, leaf);
            Put(round1x1, plastic, p + new Vector3(0.25f, BrickGrid.BrickHeight, 0.25f), 0, leaf);
        }

        void Rock(int i, int j, System.Random rng)
        {
            Mark(i, j, 1);
            Vector3 p = Base(i, j);
            var c = rockColors[rng.Next(rockColors.Length)];
            int turns = rng.Next(0, 2);
            Vector3 off = turns == 0 ? new Vector3(0.5f, 0f, 0f) : new Vector3(0f, 0f, 0.5f);
            Put(brick2x4, stone, p + off, turns, c);
            if (rng.NextDouble() < 0.6) Put(brick2x2, stone, p + Vector3.up * BrickGrid.BrickHeight, 0, c);
        }

        void Hut(int i, int j)
        {
            Mark(i, j, 2);
            // 2 x 2 m block house centred on the corner between four cells
            Vector3 p = Base(i, j) + new Vector3(0.5f, 0f, 0.5f);
            float h = BrickGrid.BrickHeight;
            for (int row = 0; row < 3; row++)
            {
                bool turned = row % 2 == 1;
                Vector3 a = turned ? new Vector3(-0.5f, 0f, 0f) : new Vector3(0f, 0f, -0.5f);
                Put(brick2x4, wood, p + a + Vector3.up * (row * h), turned ? 1 : 0);
                Put(brick2x4, wood, p - a + Vector3.up * (row * h), turned ? 1 : 0);
            }
            Put(plate4x4, plastic, p + Vector3.up * (3 * h), 0, roofColor);
            Put(brick2x2, plastic, p + Vector3.up * (3 * h + BrickGrid.PlateHeight), 0, roofColor);
        }
    }
}
