using System.Collections.Generic;
using Game.Core;
using Game.Drones;
using Game.Economy;
using Game.Hero;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using Game.UI;

namespace Game.Editor
{
    public static partial class ProjectGenerator
    {
        /// <summary>
        /// What the generator steps hand on to the steps after them. A step fills the fields it creates and reads the
        /// ones earlier steps filled, so steps share results by name instead of through long positional parameter lists.
        /// </summary>
        private sealed class GeneratorContext
        {
            public TowerDefinition Pulse { get; set; }
            public TowerDefinition Cannon { get; set; }
            public TowerDefinition Rocket { get; set; }
            public TowerDefinition Blockade { get; set; }
            public TowerDefinition BaseTurret { get; set; }
            public Roster Roster { get; set; }

            public Bestiary Bestiary { get; set; }
            public DifficultyProfile[] Difficulties { get; set; }

            public HeroDefinition Hero { get; set; }

            /// <summary>Every campaign level with the spec it was generated from, in campaign order.</summary>
            public List<SpecLevel> CampaignLevels { get; } = new List<SpecLevel>();

            /// <summary>The campaign levels alone, in campaign order.</summary>
            public LevelDefinition[] Levels
            {
                get
                {
                    var levels = new LevelDefinition[CampaignLevels.Count];
                    for (int i = 0; i < levels.Length; i++)
                    {
                        levels[i] = CampaignLevels[i].Level;
                    }

                    return levels;
                }
            }

            public Dictionary<string, HeroPerkDefinition> HeroPerks { get; } = new Dictionary<string, HeroPerkDefinition>();

            public PathObjectDefinition SlowField { get; set; }
            public PathObjectDefinition Mine { get; set; }
            public PathObjectDefinition LavaBall { get; set; }
            public PathObjectDefinition Barrel { get; set; }
            public PathObjectDefinition GroundDrone { get; set; }
            public PathObjectDefinition AntiAir { get; set; }

            public DroneDefinition RepairDrone { get; set; }
            public DroneDefinition HarvestDrone { get; set; }
            public DroneDefinition AttackDrone { get; set; }

            public CombineRecipe[] Recipes { get; set; }

            public UnlockTable Unlocks { get; set; }
            public PlaytestSettings Playtest { get; set; }
            public UpgradeCatalog Upgrades { get; set; }
            public CodexCatalog Codex { get; set; }
            public UiSkin Skin { get; set; }
            public CampaignCatalog Campaign { get; set; }

            public DroneDefinition[] Drones => new[] { RepairDrone, HarvestDrone, AttackDrone };

            public PathObjectDefinition[] PathObjects => new[] { SlowField, Mine, LavaBall, Barrel, GroundDrone, AntiAir };
        }

        /// <summary>A generated campaign level, the spec it came from and where it sits: continent and index within it.</summary>
        private sealed class SpecLevel
        {
            public SpecLevel(CampaignLevelSpec spec, LevelDefinition level, int continent, int index)
            {
                Spec = spec;
                Level = level;
                Continent = continent;
                Index = index;
            }

            public CampaignLevelSpec Spec { get; }
            public LevelDefinition Level { get; }
            public int Continent { get; }
            public int Index { get; }
        }
    }
}
