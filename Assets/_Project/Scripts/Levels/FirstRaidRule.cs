using System;
using System.Collections.Generic;
using Game.Core;
using Game.Towers;

namespace Game.Levels
{
    /// <summary>
    /// The first air raid of a level is announced (round 88c; Docs/WAVE_GRAMMAR.md, "The first raid"). A level's first
    /// raid may come late, after a whole level of ground waves and a purse spent on ground guns: the player is warned a
    /// few waves ahead (<see cref="Balance.BalanceRules.FirstRaidWarningWaves"/>). The coins for the air defence are
    /// part of the level's start money since round 88d (<see cref="StartMoneyRule"/>), which reads the sky gun's
    /// price here. Pure arithmetic on the level's data: the level controller warns, the HUD shows it, tests and labs read it.
    /// </summary>
    public static class FirstRaidRule
    {
        public const int None = -1;

        /// <summary>The index of the first wave that is an air raid; <see cref="None"/> for a level without one.</summary>
        public static int FirstRaid(IReadOnlyList<WaveDefinition> waves)
        {
            for (int i = 0; i < waves.Count; i++)
            {
                if (waves[i] != null && waves[i].AirRaid)
                {
                    return i;
                }
            }

            return None;
        }

        /// <summary>The index of the wave at whose start the warning comes: so many waves before the raid, the first wave at the earliest.</summary>
        public static int WarningWave(int firstRaid, int wavesAhead) => firstRaid < 0 ? None : Math.Max(0, firstRaid - Math.Max(0, wavesAhead));

        /// <summary>
        /// The price of the sky gun the level sells: the cheapest gun made for the sky (one that aims up only, as the
        /// balance model's sky gun, BalanceRules.SkyGunForFlyers), or, on a level that sells none, the cheapest gun that
        /// can aim up at all; zero when none can. An all-round gun that also reaches flyers at a fifth of its fire is not
        /// what a raid asks for when a real sky gun is on sale.
        /// </summary>
        public static int SkyGunPrice(IReadOnlyList<TowerDefinition> towers)
        {
            int skyOnly = 0;
            int any = 0;
            for (int i = 0; i < towers.Count; i++)
            {
                TowerDefinition tower = towers[i];
                if (tower == null || tower.IsSupport || tower.BlocksGroundEnemies || (tower.Targets & TargetLayers.Air) == 0)
                {
                    continue;
                }

                int cost = tower.BuildCost;
                if (any == 0 || cost < any)
                {
                    any = cost;
                }

                if ((tower.Targets & TargetLayers.Ground) == 0 && (skyOnly == 0 || cost < skyOnly))
                {
                    skyOnly = cost;
                }
            }

            return skyOnly > 0 ? skyOnly : any;
        }
    }
}
