using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Where a level starts: the commander's first post and the view the level camera opens on (the fixed angle at
    /// play zoom, centred on the commander). A cutscene aims a long-lens perspective camera at that view, so its
    /// last shot leads straight into play.
    /// </summary>
    public static class LevelPlayPose
    {
        /// <summary>The commander's first post: in front of the first gate, or of the base when the level has no gate.</summary>
        public static void HeroStart(LevelDefinition level, out Vector3 position, out Vector3 facing)
        {
            RoadCorridor road = level.Road;
            float cell = level.CellSize;
            if (level.Blockades != null && level.Blockades.Length > 0)
            {
                Vector2Int gate = level.Blockades[0].Cell;
                Vector3 along = road.DirectionAt(gate);
                position = GatePosition(level, gate) - along * cell;
                facing = -along;
                return;
            }

            Vector2Int[] main = level.MainRoad;
            Vector2Int baseCell = main[main.Length - 1];
            Vector3 baseAlong = road.DirectionAt(baseCell);
            position = LevelVisualBuilder.BasePosition(level) - baseAlong * cell;
            facing = -baseAlong;
        }

        /// <summary>Where a gate stands: on the centre line of the corridor at its cell, at ground height.</summary>
        public static Vector3 GatePosition(LevelDefinition level, Vector2Int cell)
        {
            Vector3 centre = level.Road.CentreOf(cell);
            return new Vector3(centre.x, level.Terrain.HeightAt(cell), centre.z);
        }

        /// <summary>
        /// A perspective pose that frames what the level camera shows at the start: the level's angle, the
        /// commander in the middle, and a distance at which this field of view covers the play zoom's height.
        /// </summary>
        public static CameraSample Pose(LevelDefinition level, float aspect, float fieldOfView)
        {
            HeroStart(level, out Vector3 hero, out Vector3 _);
            var focus = new Vector3(hero.x, 0f, hero.z);
            float fit = FixedCameraRig.FitSize(level, aspect);
            float size = fit / CameraPanZoom.PlayZoomFor(fit);
            float distance = size / Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            Quaternion rotation = Quaternion.Euler(level.Camera.PitchDegrees, level.Camera.YawDegrees, 0f);
            return new CameraSample(focus - rotation * Vector3.forward * distance, focus, fieldOfView);
        }
    }
}
