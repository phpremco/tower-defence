using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Shows where a tower can go, only while one is being placed: at rest the buildable ground is plain ground (no tiles,
    /// no grid). While placing, the ground shader lights the free cells softly around the placement cursor, fading out a
    /// few cells away, so the player sees the spots near where they look without the whole map turning into a board; the
    /// hovered cell keeps its own valid or invalid marker. Pure view: it reads <see cref="LevelContext.Mode"/>, the
    /// selection and the cursor cell, and feeds the environment shaders a mask of the free cells (rebuilt when placing
    /// starts and whenever a structure is added, so cells with towers stay dark), the cursor and the highlight level.
    /// </summary>
    public sealed class BuildPadHighlight : MonoBehaviour
    {
        private const float FadeSpeed = 6f;
        private const float StructureCellTolerance = 0.3f;

        // The glow follows the cursor smoothly instead of jumping a cell at a time (per second, exponential).
        private const float CursorFollow = 18f;

        private static readonly int MaskId = Shader.PropertyToID("_EnvPadMask");
        private static readonly int RectId = Shader.PropertyToID("_EnvPadRect");
        private static readonly int GridId = Shader.PropertyToID("_EnvPadGrid");
        private static readonly int HighlightId = Shader.PropertyToID("_EnvPadHighlight");
        private static readonly int CursorId = Shader.PropertyToID("_EnvPadCursor");
        private static readonly int ColourId = Shader.PropertyToID("_EnvPadColour");

        private LevelDefinition _level;
        private LevelContext _context;
        private Texture2D _mask;
        private Color32[] _pixels;
        private float _highlight;
        private bool _placing;
        private int _structures = -1;
        private Vector3 _cursor;
        private float _radius;

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
            EnvironmentSet set = level.Theme.Environment;
            _radius = set.PlacingRadiusCells * level.CellSize;
            Color colour = set.PlacingColour * set.PlacingGlow;
            Shader.SetGlobalTexture(MaskId, _mask);
            Shader.SetGlobalVector(RectId, new Vector4(-world.x * 0.5f, -world.y * 0.5f, 1f / world.x, 1f / world.y));
            Shader.SetGlobalVector(GridId, new Vector4(size.x, size.y, 0f, 0f));
            Shader.SetGlobalColor(ColourId, new Color(colour.r, colour.g, colour.b, 1f));
            Shader.SetGlobalFloat(HighlightId, 0f);
            RebuildMask();
        }

        private void Update()
        {
            if (_context == null && !Services.TryGet(out _context))
            {
                return;
            }

            // Only a tower goes on a build spot; a path object is placed on the road.
            bool placing = _context.Mode == PlacementMode.Placing && _context.SelectedTower != null && _context.SelectedPathObject == null;
            int structures = _context.Structures.Count;
            if (placing && (!_placing || structures != _structures))
            {
                RebuildMask();
                _structures = structures;
            }

            Vector3 target = _level.Grid.CellToWorld(_context.CursorCell);
            _cursor = placing && !_placing ? target : Vector3.Lerp(_cursor, target, 1f - Mathf.Exp(-CursorFollow * Time.unscaledDeltaTime));
            _placing = placing;
            _highlight = Mathf.MoveTowards(_highlight, placing ? 1f : 0f, FadeSpeed * Time.unscaledDeltaTime);
            Shader.SetGlobalFloat(HighlightId, _highlight);
            if (_highlight > 0f)
            {
                Shader.SetGlobalVector(CursorId, new Vector4(_cursor.x, _cursor.z, _radius, 0f));
            }
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
