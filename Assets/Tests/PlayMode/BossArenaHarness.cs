using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Bosses;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests
{
    /// <summary>
    /// Shared steps of the sped-up boss arena fights: load a boss's arena level, build towers on free pads by the road,
    /// and check that the boss's body never stands on a tower's or a living gate's footprint except by its mechanic.
    /// </summary>
    public sealed class BossArenaHarness
    {
        public LevelContext Context { get; private set; }
        public BossArenaDefinition Arena { get; private set; }
        public List<string> Overlaps { get; } = new List<string>();

        /// <summary>Loads the arena fight of the boss with that asset name and waits for the level to stand.</summary>
        public IEnumerator Load(string bossName)
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;
            var catalog = Services.Get<CampaignCatalog>();
            Arena = catalog.BossArena;
            EnemyDefinition boss = null;
            foreach (EnemyDefinition candidate in Arena.Bosses)
            {
                if (candidate.name == bossName)
                {
                    boss = candidate;
                }
            }

            Boss = boss;
            Services.Register(new LevelSession(Arena.LevelFor(boss), Arena.Difficulty, false, null, Arena.WavesFor(boss), Arena.StartingMoney));
            SceneManager.LoadScene(SceneNames.Level);
            yield return null;
            yield return null;
            LevelContext context = null;
            while (context == null)
            {
                Services.TryGet(out context);
                yield return null;
            }

            Context = context;
        }

        public EnemyDefinition Boss { get; private set; }

        /// <summary>The boss's pivot inside a standing tower's cell or a living gate's rung while it is not on a structure by design.</summary>
        public void CheckFootprint(Enemy boss, ContinentBossBrain brain)
        {
            if (brain.OnStructure || boss.IsConcealed || Overlaps.Count > 5)
            {
                return;
            }

            GridMap grid = Context.Level.Grid;
            Vector2Int cell = grid.WorldToCell(boss.Position);
            for (int i = 0; i < Context.Structures.Count; i++)
            {
                if (!(Context.Structures[i] is Tower tower))
                {
                    continue;
                }

                Vector2Int at = grid.WorldToCell(tower.Position);
                bool gate = tower.Definition.BlocksGroundEnemies;
                bool hit = gate ? tower.IsAlive && !tower.IsFrozen && new List<Vector2Int>(Context.Level.Road.Rung(at)).Contains(cell) : tower.IsAlive && at == cell;
                if (hit)
                {
                    Overlaps.Add($"{brain.State}: on {tower.Definition.name} at {cell}");
                }
            }
        }

        /// <summary>Builds towers of the given kinds in turn on free pads beside the road, spread out from a share of the road.</summary>
        public List<Tower> Build(IReadOnlyList<TowerDefinition> kinds, int count, float roadShare, float spacing = 2.5f)
        {
            var placement = Object.FindAnyObjectByType<PlacementController>();
            MethodInfo create = typeof(PlacementController).GetMethod("CreateTower", BindingFlags.NonPublic | BindingFlags.Instance);
            LevelDefinition level = Context.Level;
            var grid = new BuildGrid(level);
            var road = new List<Vector2Int>(level.Road.Cells);
            var spots = new List<Vector2Int>();
            for (int x = 0; x < level.GridSize.x; x++)
            {
                for (int y = 0; y < level.GridSize.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (grid.CanBuild(cell) && road.Exists(r => Mathf.Abs(r.x - cell.x) <= 1 && Mathf.Abs(r.y - cell.y) <= 1))
                    {
                        spots.Add(cell);
                    }
                }
            }

            Vector2Int middle = level.MainRoad[Mathf.Clamp(Mathf.RoundToInt(level.MainRoad.Length * roadShare), 0, level.MainRoad.Length - 1)];
            spots.Sort((a, b) => Vector2Int.Distance(a, middle).CompareTo(Vector2Int.Distance(b, middle)));
            var built = new List<Tower>();
            var used = new List<Vector2Int>();
            foreach (Vector2Int cell in spots)
            {
                if (built.Count >= count)
                {
                    break;
                }

                if (used.Exists(u => Vector2Int.Distance(u, cell) < spacing))
                {
                    continue;
                }

                TowerDefinition kind = kinds[built.Count % kinds.Count];
                built.Add((Tower)create.Invoke(placement, new object[] { kind, cell, false }));
                used.Add(cell);
            }

            return built;
        }
    }
}
