using System.Collections.Generic;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    public sealed class RoadCorridorTests
    {
        private static readonly Vector2Int[] EastThenNorth = { new Vector2Int(0, 2), new Vector2Int(4, 2), new Vector2Int(4, 8) };

        private LevelDefinition _level;
        private VisualTheme _theme;

        [SetUp]
        public void SetUp()
        {
            _level = ScriptableObject.CreateInstance<LevelDefinition>();
            _theme = ScriptableObject.CreateInstance<VisualTheme>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_level);
            Object.DestroyImmediate(_theme);
        }

        [Test]
        public void ShiftCells_MitresRightAngleCornerToTheRightOfTravel()
        {
            Vector2Int[] shifted = RoadCorridor.ShiftCells(EastThenNorth, 1);

            // Facing east the right hand points south; facing north it points east. The corner gets both.
            CollectionAssert.AreEqual(new[] { new Vector2Int(0, 1), new Vector2Int(5, 1), new Vector2Int(5, 8) }, shifted);
        }

        [Test]
        public void WidthOne_IsExactlyThePathCells()
        {
            var corridor = new RoadCorridor(new GridMap(new Vector2Int(10, 10), 1f), Paths(EastThenNorth), 1, null);

            CollectionAssert.AreEquivalent(new List<Vector2Int>(LevelValidator.LandPathCells(EastThenNorth)), corridor.Cells);
        }

        [Test]
        public void WidthTwo_AddsSecondRowAndFillsOuterCorner()
        {
            var corridor = new RoadCorridor(new GridMap(new Vector2Int(10, 10), 1f), Paths(EastThenNorth), 2, null);

            Assert.IsTrue(corridor.IsRoad(new Vector2Int(2, 1)), "second row beside the east leg");
            Assert.IsTrue(corridor.IsRoad(new Vector2Int(5, 5)), "second row beside the north leg");
            Assert.IsTrue(corridor.IsRoad(new Vector2Int(5, 1)), "outer corner");
            Assert.IsFalse(corridor.IsRoad(new Vector2Int(2, 3)), "left of the east leg stays free");
            Assert.IsFalse(corridor.IsRoad(new Vector2Int(6, 4)), "two cells right of the north leg stays free");
            Assert.AreEqual(0, corridor.RowOf(new Vector2Int(2, 2)));
            Assert.AreEqual(1, corridor.RowOf(new Vector2Int(2, 1)));
            Assert.AreEqual(-1, corridor.RowOf(new Vector2Int(2, 3)));
        }

        [Test]
        public void CentreAndRung_AgreeAcrossTheCorridor()
        {
            var grid = new GridMap(new Vector2Int(10, 10), 1f);
            var corridor = new RoadCorridor(grid, Paths(EastThenNorth), 2, null);
            var first = new Vector2Int(2, 2);
            var second = new Vector2Int(2, 1);

            Vector3 expected = grid.CellToWorld(first) + new Vector3(0f, 0f, -0.5f);
            Assert.AreEqual(expected, corridor.CentreOf(first));
            Assert.AreEqual(expected, corridor.CentreOf(second));
            CollectionAssert.AreEqual(new[] { first, second }, corridor.Rung(first));
            Assert.IsTrue(corridor.RungContains(first, second));
            Assert.IsFalse(corridor.RungContains(first, new Vector2Int(3, 2)));
            Assert.AreEqual(Vector3.right, corridor.DirectionAt(second));
        }

        [Test]
        public void Route_RunsAlongTheCentreLine()
        {
            var grid = new GridMap(new Vector2Int(10, 10), 1f);
            var corridor = new RoadCorridor(grid, Paths(EastThenNorth), 2, null);

            Vector3[] route = corridor.Route(0);

            Assert.AreEqual(grid.CellToWorld(new Vector2Int(0, 2)) + new Vector3(0f, 0f, -0.5f), route[0]);
            Assert.AreEqual(grid.CellToWorld(new Vector2Int(4, 2)) + new Vector3(0.5f, 0f, -0.5f), route[1]);
            Assert.AreEqual(grid.CellToWorld(new Vector2Int(4, 8)) + new Vector3(0.5f, 0f, 0f), route[2]);
        }

        [Test]
        public void TryLocate_FindsTheNextWaypointFromEitherRowAndFromAPlaza()
        {
            var plaza = new[] { new Vector2Int(6, 2), new Vector2Int(6, 1) };
            var corridor = new RoadCorridor(new GridMap(new Vector2Int(10, 10), 1f), Paths(EastThenNorth), 2, plaza);

            Assert.IsTrue(corridor.TryLocate(new Vector2Int(4, 5), out Vector3[] route, out int next));
            Assert.AreEqual(2, next);
            Assert.IsTrue(corridor.TryLocate(new Vector2Int(5, 5), out _, out next));
            Assert.AreEqual(2, next);
            Assert.IsTrue(corridor.TryLocate(new Vector2Int(6, 2), out _, out next), "a plaza cell takes the nearest route");
            Assert.AreEqual(corridor.Route(0), route);
            Assert.IsFalse(corridor.TryLocate(new Vector2Int(0, 9), out _, out _));
        }

        [Test]
        public void Validator_RejectsUnconnectedPlazaAndBlockadeOffTheFirstRow()
        {
            Configure(new Vector2Int(12, 12), 2, EastThenNorth, new[] { new Vector2Int(9, 9) }, new Vector2Int(2, 1));
            var errors = new List<string>();

            LevelValidator.Validate(_level, errors);

            Assert.IsTrue(errors.Exists(e => e.Contains("Plaza cell (9, 9)")), string.Join("\n", errors));
            Assert.IsTrue(errors.Exists(e => e.Contains("Blockade at (2, 1)")), string.Join("\n", errors));
        }

        [Test]
        public void Validator_AcceptsPlazaTouchingTheRoad()
        {
            Configure(new Vector2Int(12, 12), 2, EastThenNorth, new[] { new Vector2Int(6, 4), new Vector2Int(7, 4) }, new Vector2Int(2, 2));
            var errors = new List<string>();

            LevelValidator.Validate(_level, errors);

            Assert.IsFalse(errors.Exists(e => e.Contains("Plaza") || e.Contains("first row") || e.Contains("Road cell")), string.Join("\n", errors));
        }

        [Test]
        public void Validator_RejectsSecondRowOutsideTheGrid()
        {
            // The east leg runs along the bottom edge, so its second row would lie at y = -1.
            Configure(new Vector2Int(12, 12), 2, new[] { new Vector2Int(0, 0), new Vector2Int(5, 0), new Vector2Int(5, 6) }, null, new Vector2Int(2, 0));
            var errors = new List<string>();

            LevelValidator.Validate(_level, errors);

            Assert.IsTrue(errors.Exists(e => e.Contains("Road cell (2, -1)")), string.Join("\n", errors));
        }

        [Test]
        public void RoadShape_TreatsPlazaAsRoadSurface()
        {
            var plaza = new[] { new Vector2Int(7, 2), new Vector2Int(8, 2), new Vector2Int(7, 3), new Vector2Int(8, 3) };
            Configure(new Vector2Int(12, 12), 2, EastThenNorth, plaza, new Vector2Int(2, 2));
            var shape = new RoadShape(_level);
            GridMap grid = _level.Grid;

            Vector3 insidePlaza = grid.CellToWorld(new Vector2Int(8, 3));
            Vector3 onRoad = _level.Road.CentreOf(new Vector2Int(2, 2));
            Vector3 farOff = grid.CellToWorld(new Vector2Int(0, 10));

            Assert.Less(shape.DistanceTo(insidePlaza.x, insidePlaza.z), shape.HalfWidth);
            Assert.Less(shape.DistanceTo(onRoad.x, onRoad.z), shape.HalfWidth);
            Assert.Greater(shape.DistanceTo(farOff.x, farOff.z), shape.HalfWidth);
        }

        private static EnemyPath[] Paths(Vector2Int[] road) => new[] { new EnemyPath { Name = "Road", Flying = false, Waypoints = road } };

        private void Configure(Vector2Int size, int width, Vector2Int[] road, Vector2Int[] plazas, Vector2Int blockade)
        {
            var so = new SerializedObject(_level);
            so.FindProperty("_theme").objectReferenceValue = _theme;
            so.FindProperty("_gridSize").vector2IntValue = size;
            so.FindProperty("_roadWidth").intValue = width;
            SerializedProperty paths = so.FindProperty("_paths");
            paths.arraySize = 1;
            SerializedProperty path = paths.GetArrayElementAtIndex(0);
            path.FindPropertyRelative("Name").stringValue = "Road";
            path.FindPropertyRelative("Flying").boolValue = false;
            WriteCells(path.FindPropertyRelative("Waypoints"), road);
            WriteCells(so.FindProperty("_plazaCells"), plazas ?? new Vector2Int[0]);
            SerializedProperty blockades = so.FindProperty("_blockades");
            blockades.arraySize = 1;
            blockades.GetArrayElementAtIndex(0).FindPropertyRelative("Cell").vector2IntValue = blockade;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteCells(SerializedProperty array, Vector2Int[] cells)
        {
            array.arraySize = cells.Length;
            for (int i = 0; i < cells.Length; i++)
            {
                array.GetArrayElementAtIndex(i).vector2IntValue = cells[i];
            }
        }
    }
}
