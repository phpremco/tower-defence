using System.Collections.Generic;
using Game.Core;
using Game.Hero;
using Game.Platform;
using Game.Towers;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Creates the commander and connects it to the level: the posts it can hold (in front of every blockade and
    /// at the base), the shortcut keys, the "send hero here" request from a blockade's menu and the money the
    /// emergency repair costs.
    /// </summary>
    public sealed class HeroController : MonoBehaviour
    {
        private const string BaseName = "Base";
        private const float SpotHeight = 0.07f;
        private const float SpotAlpha = 0.28f;
        private const float SpotEmission = 1.2f;
        private static readonly Color SpotColor = new Color(1f, 0.85f, 0.3f);

        private LevelContext _context;
        private IInputProvider _input;
        private IReadOnlyList<Tower> _blockades;
        private Commander _hero;

        public void Initialize(LevelContext context, IInputProvider input, IReadOnlyList<Tower> blockades, Transform parent)
        {
            HeroDefinition definition = context.Level.Hero;
            if (definition == null)
            {
                return;
            }

            _context = context;
            _input = input;
            _blockades = blockades;

            GridMap grid = context.Level.Grid;
            var posts = new List<HeroPost>(blockades.Count + 1);
            var names = new string[blockades.Count + 1];
            var cells = new Vector2Int[blockades.Count + 1];
            for (int i = 0; i < blockades.Count; i++)
            {
                Vector3 position = blockades[i].Position;
                Vector3 along = RoadDirection.At(context.Level, grid.WorldToCell(position));
                posts.Add(new HeroPost(blockades[i], position - along * grid.CellSize, -along));
                names[i] = $"Blockade {i + 1}";
            }

            Vector2Int[] road = context.Level.MainRoad;
            Vector2Int baseCell = road[road.Length - 1];
            Vector3 basePosition = grid.CellToWorld(baseCell);
            Vector3 baseAlong = RoadDirection.At(context.Level, baseCell);
            var basePost = new BasePost(basePosition, context.BaseHealth);
            posts.Add(new HeroPost(basePost, basePosition - baseAlong * grid.CellSize, -baseAlong));
            names[blockades.Count] = BaseName;

            for (int i = 0; i < posts.Count; i++)
            {
                cells[i] = grid.WorldToCell(posts[i].Stand);
                CreateSpotMarker(posts[i].Stand, grid.CellSize, parent);
            }

            var go = new GameObject("Hero");
            go.transform.SetParent(parent, false);
            _hero = go.AddComponent<Commander>();
            _hero.Initialize(definition, context.Targets, posts, 0, context.GlobalModifiers);
            go.AddComponent<HeroView>().Initialize(_hero);

            context.Hero = _hero;
            context.HeroPostNames = names;
            context.HeroPostCells = cells;

            _input.HeroPostPressed += OnNextPost;
            _input.ShockwavePressed += OnShockwave;
            _input.EmergencyRepairPressed += OnRepair;
            _context.HeroSendRequested.Subscribe(OnSendHere);
            _context.HeroRepairRequested.Subscribe(OnRepair);
        }

        private void OnDestroy()
        {
            if (_hero == null)
            {
                return;
            }

            _input.HeroPostPressed -= OnNextPost;
            _input.ShockwavePressed -= OnShockwave;
            _input.EmergencyRepairPressed -= OnRepair;
            _context.HeroSendRequested.Unsubscribe(OnSendHere);
            _context.HeroRepairRequested.Unsubscribe(OnRepair);
        }

        // A glowing tile shows the cell that is kept free for the hero, and where to click to move it there.
        private static void CreateSpotMarker(Vector3 position, float cellSize, Transform parent)
        {
            GameObject marker = PlaceholderVisuals.CreatePrimitive(
                PrimitiveType.Cube, SpotColor, parent, position + Vector3.up * SpotHeight, new Vector3(cellSize * 0.9f, 0.02f, cellSize * 0.9f));
            marker.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(SpotColor, SpotAlpha, SpotEmission);
            marker.name = "HeroSpot";
        }

        private bool CanAct => _context.Outcome == LevelOutcome.Running && !_context.Paused;

        private void OnNextPost()
        {
            if (CanAct)
            {
                _hero.MoveTo((_hero.PostIndex + 1) % _hero.PostCount);
            }
        }

        private void OnShockwave()
        {
            if (CanAct)
            {
                _hero.TryShockwave();
            }
        }

        // Pays only when the repair can actually happen, so a press during the cooldown costs nothing.
        private void OnRepair()
        {
            if (CanAct && _hero.CanEmergencyRepair && _context.Wallet.TrySpend(_hero.Definition.RepairCost))
            {
                _hero.TryEmergencyRepair();
                _context.Changed.Raise();
            }
        }

        private void OnSendHere()
        {
            Tower selected = _context.SelectedPlacedTower;
            for (int i = 0; selected != null && i < _blockades.Count; i++)
            {
                if (_blockades[i] == selected)
                {
                    _hero.MoveTo(i);
                    _context.Changed.Raise();
                    return;
                }
            }
        }
    }
}
