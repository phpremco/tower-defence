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
        private const float SpotAlpha = 0.16f;
        private const float SpotMargin = 0.1f;
        private const float SpotEmission = 0.35f;
        private static readonly Color SpotColor = new Color(0.78f, 0.8f, 0.86f);

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
            RoadCorridor road = context.Level.Road;
            var posts = new List<HeroPost>(blockades.Count + 1);
            var names = new string[blockades.Count + 1];
            var cells = new Vector2Int[blockades.Count + 1];

            // The blockade towers were spawned in the order of the level data, so the spot at the same index is the
            // cell the gate stands on; the hero holds the rung of the corridor one cell before it.
            BlockadeSpot[] spots = context.Level.Blockades;
            for (int i = 0; i < blockades.Count; i++)
            {
                Vector3 along = road.DirectionAt(spots[i].Cell);
                Vector3 stand = blockades[i].Position - along * grid.CellSize;
                posts.Add(new HeroPost(blockades[i], stand, -along));
                cells[i] = StandCell(spots[i].Cell, along);
                names[i] = $"Blockade {i + 1}";
                CreateSpotMarker(stand, along, road.Width, grid.CellSize, parent);
            }

            Vector2Int[] main = context.Level.MainRoad;
            Vector2Int baseCell = main[main.Length - 1];
            Vector3 basePosition = context.Base.Position;
            Vector3 baseAlong = road.DirectionAt(baseCell);
            Vector3 baseStand = basePosition - baseAlong * grid.CellSize;
            posts.Add(new HeroPost(context.Base, baseStand, -baseAlong));
            cells[blockades.Count] = StandCell(baseCell, baseAlong);
            names[blockades.Count] = BaseName;
            CreateSpotMarker(baseStand, baseAlong, road.Width, grid.CellSize, parent);

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

        // The camera follows the hero to its post, so the player watches the fight they just ordered.
        private void SendHero(int postIndex)
        {
            _hero.MoveTo(postIndex);
            _context.CameraFocusRequested.Raise(_hero.StandAt(_hero.PostIndex));
        }

        private static Vector2Int StandCell(Vector2Int cell, Vector3 along) =>
            cell - new Vector2Int(Mathf.RoundToInt(along.x), Mathf.RoundToInt(along.z));

        // A glowing strip across the road shows the rung that is kept free for the hero, and where to click to move it there.
        private static void CreateSpotMarker(Vector3 position, Vector3 along, int widthCells, float cellSize, Transform parent)
        {
            var size = new Vector3(cellSize * (widthCells - SpotMargin), 0.02f, cellSize * (1f - SpotMargin));
            GameObject marker = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, SpotColor, parent, position + Vector3.up * SpotHeight, size);
            marker.transform.rotation = Quaternion.LookRotation(along);
            marker.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(SpotColor, SpotAlpha, SpotEmission);
            marker.name = "HeroSpot";
        }

        private bool CanAct => _context.Outcome == LevelOutcome.Running && !_context.Paused;

        private void OnNextPost()
        {
            if (CanAct)
            {
                SendHero((_hero.PostIndex + 1) % _hero.PostCount);
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
                    SendHero(i);
                    _context.Changed.Raise();
                    return;
                }
            }
        }
    }
}
