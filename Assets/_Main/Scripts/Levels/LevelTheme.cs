using BlobRunner.Core;
using UnityEngine;

namespace BlobRunner.Levels
{
    /// <summary>Colour scheme of a level (background, floor pattern, hazard bars).</summary>
    public sealed class LevelTheme
    {
        public readonly string Name;
        public readonly Color Background;
        public readonly Color FloorA;
        public readonly Color FloorB;
        public readonly Color Edge;
        public readonly Color BarA;
        public readonly Color BarB;

        public LevelTheme(string name, Color background, Color floorA, Color floorB, Color edge, Color barA, Color barB)
        {
            Name = name;
            Background = background;
            FloorA = floorA;
            FloorB = floorB;
            Edge = edge;
            BarA = barA;
            BarB = barB;
        }
    }

    public static class LevelThemes
    {
        /// <summary>Must contain <see cref="LevelGenerator.ThemeCount"/> entries.</summary>
        public static readonly LevelTheme[] All =
        {
            new LevelTheme("Sky",
                new Color(0.00f, 0.50f, 1.00f), new Color(0.96f, 0.97f, 1.00f), new Color(0.85f, 0.89f, 0.96f),
                new Color(0.27f, 0.52f, 0.95f), new Color(1.00f, 0.32f, 0.28f), Color.white),
            new LevelTheme("Mint",
                new Color(0.18f, 0.77f, 0.71f), new Color(0.95f, 1.00f, 0.97f), new Color(0.83f, 0.95f, 0.90f),
                new Color(1.00f, 0.42f, 0.42f), new Color(1.00f, 0.42f, 0.42f), Color.white),
            new LevelTheme("Sunset",
                new Color(1.00f, 0.55f, 0.38f), new Color(1.00f, 0.95f, 0.90f), new Color(0.97f, 0.85f, 0.77f),
                new Color(0.42f, 0.36f, 0.91f), new Color(0.42f, 0.36f, 0.91f), Color.white),
            new LevelTheme("Grape",
                new Color(0.56f, 0.36f, 0.97f), new Color(0.96f, 0.93f, 1.00f), new Color(0.85f, 0.80f, 0.96f),
                new Color(1.00f, 0.85f, 0.24f), new Color(1.00f, 0.82f, 0.24f), new Color(0.24f, 0.16f, 0.40f)),
            new LevelTheme("Candy",
                new Color(1.00f, 0.44f, 0.71f), new Color(1.00f, 0.95f, 0.98f), new Color(0.97f, 0.84f, 0.91f),
                new Color(0.15f, 0.82f, 0.77f), new Color(0.15f, 0.82f, 0.77f), Color.white),
            new LevelTheme("Lime",
                new Color(0.49f, 0.85f, 0.34f), new Color(0.96f, 1.00f, 0.92f), new Color(0.84f, 0.94f, 0.77f),
                new Color(1.00f, 0.54f, 0.00f), new Color(1.00f, 0.54f, 0.00f), Color.white)
        };

        public static LevelTheme For(int index)
        {
            int i = index % All.Length;
            if (i < 0)
                i += All.Length;
            return All[i];
        }
    }

    /// <summary>Colours of the loot / regrown body parts. Must contain <see cref="LevelGenerator.LootColorCount"/> entries.</summary>
    public static class LootPalette
    {
        public static readonly Color[] Colors =
        {
            new Color(1.00f, 0.30f, 0.30f),
            new Color(0.30f, 1.00f, 0.92f),
            new Color(0.30f, 1.00f, 0.31f),
            new Color(0.58f, 0.30f, 1.00f),
            new Color(1.00f, 0.85f, 0.20f),
            new Color(1.00f, 0.45f, 0.80f),
            new Color(0.30f, 0.60f, 1.00f),
            new Color(1.00f, 0.60f, 0.15f)
        };

        public static Color Get(int index)
        {
            int i = index % Colors.Length;
            if (i < 0)
                i += Colors.Length;
            return Colors[i];
        }
    }
}
