using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The build pads' glow follows what the player is doing: calm while browsing or fighting (dark tiles in the
    /// ground, so the field never reads as a lit board), lit on every free cell while a tower is being placed. Pure view:
    /// it reads <see cref="LevelContext.Mode"/> and the selection, and feeds the environment shader a mask of the free
    /// cells (rebuilt each time placing starts, so cells with towers stay calm) and the highlight level.
    /// </summary>
    public sealed class BuildPadHighlight : MonoBehaviour
    {
        private const float FadeSpeed = 6f;
        private const float StructureCellTolerance = 0.3f;

        private static readonly int MaskId = Shader.PropertyToID("_EnvPadMask");
        private static readonly int RectId = Shader.PropertyToID("_EnvPadRect");
        private static readonly int HighlightId = Shader.PropertyToID("_EnvPadHighlight");

        private LevelDefinition _level;
        private LevelContext _context;
        private Texture2D _mask;
        private Color32[] _pixels;
        private float _highlight;
        private bool _placing;

        public void Initialize(LevelDefinition level)
        {
            _level = level;
            Vector2Int size = level.Grid.Size;
            _mask = new Texture2D(size.x, size.y, TextureFormat.R8, false, true)
            {
                name = "BuildPadMask",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
            };
            _pixels = new Color32[size.x * size.y];
            Vector2 world = level.WorldSize;
            Shader.SetGlobalTexture(MaskId, _mask);
            Shader.SetGlobalVector(RectId, new Vector4(-world.x * 0.5f, -world.y * 0.5f, 1f / world.x, 1f / world.y));
            Shader.SetGlobalFloat(HighlightId, 0f);
            RebuildMask();
        }

        private void Update()
        {
            if (_context == null && !Services.TryGet(out _context))
            {
                return;
            }

            // Only a tower goes on a pad; a path object is placed on the road.
            bool placing = _context.Mode == PlacementMode.Placing && _context.SelectedTower != null && _context.SelectedPathObject == null;
            if (placing && !_placing)
            {
                RebuildMask();
            }

            _placing = placing;
            float target = placing ? 1f : 0f;
            if (Mathf.Approximately(_highlight, target))
            {
                return;
            }

            _highlight = Mathf.MoveTowards(_highlight, target, FadeSpeed * Time.unscaledDeltaTime);
            Shader.SetGlobalFloat(HighlightId, _highlight);
        }

        private void OnDestroy()
        {
            Shader.SetGlobalFloat(HighlightId, 0f);
            if (_mask != null)
            {
                Destroy(_mask);
            }
        }

        // Free build cells are lit; a cell a structure (a tower) stands on is not.
        private void RebuildMask()
        {
            var build = new BuildGrid(_level);
            GridMap grid = _level.Grid;
            Vector2Int size = grid.Size;
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    _pixels[y * size.x + x] = new Color32(build.CanBuild(new Vector2Int(x, y)) ? byte.MaxValue : byte.MinValue, 0, 0, 0);
                }
            }

            if (_context != null)
            {
                StructureRegistry structures = _context.Structures;
                for (int i = 0; i < structures.Count; i++)
                {
                    Vector3 position = structures[i].Position;
                    Vector2Int cell = grid.WorldToCell(position);
                    bool centred = (grid.CellToWorld(cell) - new Vector3(position.x, 0f, position.z)).sqrMagnitude < StructureCellTolerance * StructureCellTolerance;
                    if (grid.Contains(cell) && centred)
                    {
                        _pixels[cell.y * size.x + cell.x] = new Color32(0, 0, 0, 0);
                    }
                }
            }

            _mask.SetPixels32(_pixels);
            _mask.Apply(false, false);
        }
    }
}
