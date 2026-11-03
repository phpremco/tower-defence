using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// A generated map before it becomes a level asset: the main road (ending at the base), an optional second road that
    /// ends on a junction waypoint of the main road, gates, plaza cells, the two flight routes, terrain and nests.
    /// </summary>
    public sealed class RoadLayout
    {
        public string Key = string.Empty;
        public int Attempt;
        public int Width;
        public int Height;
        public int RoadWidth = 2;

        /// <summary>Corner cells of the main road, from the spawn on the west edge to the base.</summary>
        public Cell[] Road = new Cell[0];

        /// <summary>Corner cells of the second road up to the junction, or null; its last cell is a waypoint of <see cref="Road"/>.</summary>
        public Cell[] SecondRoad;

        public Cell[] NorthAir = new Cell[0];
        public Cell[] SouthAir = new Cell[0];
        public readonly List<Cell> Gates = new List<Cell>();
        public readonly List<Cell> PlazaCells = new List<Cell>();
        public readonly List<LayoutPatch> Terrain = new List<LayoutPatch>();
        public readonly List<LayoutNest> Nests = new List<LayoutNest>();

        /// <summary>Why the generator could not lay out this attempt at all (the spec asks for more than the grid holds), or null.</summary>
        public string Problem;

        public Cell Base => Road.Length > 0 ? Road[Road.Length - 1] : new Cell(0, 0);

        /// <summary>The second road walked on to the base along the main road, as the creatures walk it; null without one.</summary>
        public Cell[] SecondRoadToBase
        {
            get
            {
                if (SecondRoad == null || SecondRoad.Length == 0)
                {
                    return null;
                }

                Cell junction = SecondRoad[SecondRoad.Length - 1];
                var cells = new List<Cell>(SecondRoad);
                int index = LayoutGeometry.IndexOf(Road, junction);
                for (int i = index + 1; index >= 0 && i < Road.Length; i++)
                {
                    cells.Add(Road[i]);
                }

                return cells.ToArray();
            }
        }
    }
}
