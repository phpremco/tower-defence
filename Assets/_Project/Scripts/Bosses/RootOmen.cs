using System.Collections.Generic;
using Game.Enemies;
using Game.Levels;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The roots that announce a boss before it comes (Docs/BOSSES.md, Rotbloom: "roots spread over three waves before it
    /// shows up, as decor that grows a little each wave"): from the waves the level will send, it knows which wave brings
    /// a boss with roots, and at the start of each of the waves before it the clump at the bank of the pool it will rise
    /// from grows a stage. No countdown: the field itself tells the player something is coming.
    /// </summary>
    public sealed class RootOmen
    {
        private readonly BossField _field;
        private readonly WaveDefinition[] _waves;
        private readonly Dictionary<EnemyDefinition, RootsView> _roots = new Dictionary<EnemyDefinition, RootsView>();

        public RootOmen(BossField field, WaveDefinition[] waves)
        {
            _field = field;
            _waves = waves ?? new WaveDefinition[0];
        }

        /// <summary>The roots of a boss, for the tests and probes (null when it has none yet).</summary>
        public RootsView RootsOf(EnemyDefinition boss) => _roots.TryGetValue(boss, out RootsView roots) ? roots : null;

        /// <summary>True when one of the waves brings a boss whose roots announce it.</summary>
        public static bool Needed(WaveDefinition[] waves)
        {
            foreach (WaveDefinition wave in waves ?? new WaveDefinition[0])
            {
                foreach (SpawnGroup group in wave != null ? wave.Groups : new SpawnGroup[0])
                {
                    BossDefinition boss = group.Enemy != null ? group.Enemy.Boss : null;
                    if (boss != null && boss.Signature == BossSignature.Overgrowth && boss.Overgrowth.Roots != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>A wave has started (its index from zero): every rooted boss still to come grows its roots a stage.</summary>
        public void OnWaveStarted(int started)
        {
            for (int w = started; w < _waves.Length; w++)
            {
                if (_waves[w] == null)
                {
                    continue;
                }

                foreach (SpawnGroup group in _waves[w].Groups)
                {
                    BossDefinition boss = group.Enemy != null ? group.Enemy.Boss : null;
                    if (boss == null || boss.Signature != BossSignature.Overgrowth || boss.Overgrowth.Roots == null)
                    {
                        continue;
                    }

                    int stage = boss.Overgrowth.StageFor(w - started);
                    if (stage >= 0)
                    {
                        Grow(group.Enemy, boss, stage);
                    }
                }
            }
        }

        // At the bank of the pool it will rise from, the side nearest the road, facing the road.
        private void Grow(EnemyDefinition enemy, BossDefinition boss, int stage)
        {
            if (!ContinentBossBrain.TryEntrancePool(_field, boss.Dive.Pools, out _, out IReadOnlyList<Vector2Int> cells, out _))
            {
                return;
            }

            Vector3 middle = ContinentBossBrain.Middle(_field, cells);
            Vector3 road = OffRoadRoute.NearestRoadCellWorld(_field.Level, middle);
            Vector3 bank = BossGeometry.Nearest(_field.Grid, cells, road, out _);
            if (!_roots.TryGetValue(enemy, out RootsView roots))
            {
                roots = RootsView.Create(_field.Effects, boss.Overgrowth.Roots, boss.Overgrowth.RootStages, "RootOmen");
                _roots[enemy] = roots;
            }

            roots.Show(stage, new Vector3(bank.x, _field.GroundAt(bank), bank.z), BossGeometry.Flat(road - bank));
        }
    }
}
