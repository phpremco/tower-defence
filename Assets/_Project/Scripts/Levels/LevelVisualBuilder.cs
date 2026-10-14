using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>Builds the static look of a level (ground, path tiles, slot pads, base marker) from its definition.</summary>
    public static class LevelVisualBuilder
    {
        private const float GroundThickness = 0.2f;
        private const float TileHeight = 0.04f;
        private const float TileFill = 0.96f;
        private const float BaseSize = 1.2f;

        public static void Build(LevelDefinition level, Transform parent)
        {
            VisualTheme theme = level.Theme;
            Vector2 size = level.WorldSize;
            PlaceholderVisuals.CreatePrimitive(
                PrimitiveType.Cube,
                theme.GroundColor,
                parent,
                new Vector3(0f, -GroundThickness * 0.5f, 0f),
                new Vector3(size.x, GroundThickness, size.y));

            GridMap grid = level.Grid;
            float tile = level.CellSize * TileFill;

            foreach (Vector2Int cell in LevelValidator.LandPathCells(level.LandPath))
            {
                PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, theme.PathColor, parent, grid.CellToWorld(cell) + Vector3.up * TileHeight * 0.5f, new Vector3(tile, TileHeight, tile));
            }

            foreach (Vector2Int cell in level.BuildSlots)
            {
                PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, theme.SlotColor, parent, grid.CellToWorld(cell) + Vector3.up * TileHeight * 0.5f, new Vector3(tile, TileHeight, tile));
            }

            if (level.LandPath.Length > 0)
            {
                Vector2Int end = level.LandPath[level.LandPath.Length - 1];
                float baseSize = level.CellSize * BaseSize;
                PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, theme.BaseColor, parent, grid.CellToWorld(end) + Vector3.up * baseSize * 0.5f, Vector3.one * baseSize);
            }
        }
    }
}
