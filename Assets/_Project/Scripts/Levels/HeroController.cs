using System.Collections.Generic;
using Game.Combat;
using Game.Hero;
using Game.Platform;
using Game.Towers;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Creates the commander and connects it to the level: the posts it can stand at (every blockade and the base),
    /// the shortcut keys and the "send hero here" request from a blockade's menu.
    /// </summary>
    public sealed class HeroController : MonoBehaviour
    {
        private const string BaseName = "Base";

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

            var posts = new List<IGuardable>(blockades.Count + 1);
            var names = new string[blockades.Count + 1];
            for (int i = 0; i < blockades.Count; i++)
            {
                posts.Add(blockades[i]);
                names[i] = $"Blockade {i + 1}";
            }

            Vector2Int[] road = context.Level.MainRoad;
            Vector3 basePosition = context.Level.Grid.CellToWorld(road[road.Length - 1]);
            posts.Add(new BasePost(basePosition, context.BaseHealth));
            names[blockades.Count] = BaseName;

            var go = new GameObject("Hero");
            go.transform.SetParent(parent, false);
            _hero = go.AddComponent<Commander>();
            _hero.Initialize(definition, context.Targets, context.Projectiles, posts, 0);
            go.AddComponent<HeroView>().Initialize(_hero);

            context.Hero = _hero;
            context.HeroPostNames = names;

            _input.HeroPostPressed += OnNextPost;
            _input.ShockwavePressed += OnShockwave;
            _input.EmergencyRepairPressed += OnRepair;
            _context.HeroSendRequested.Subscribe(OnSendHere);
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

        private void OnRepair()
        {
            if (CanAct)
            {
                _hero.TryEmergencyRepair();
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
