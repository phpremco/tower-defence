using Game.Balance;

namespace Game.Editor
{
    /// <summary>
    /// The wave script of every level: roster, rhythm, air raids and bosses. Pure data on creature ids, so the generator
    /// and the BalanceLab tool read the same scripts; the size of each wave comes from <see cref="WaveSolver"/>.
    /// Bosses per difficulty: every level keeps its bosses on all difficulties, Hard adds one boss wave and Psycho
    /// another (GAME_DESIGN: harder difficulties bring extra boss waves). Bosses of the first world always come through
    /// the main gate (Docs/LEVEL_GUIDE.md); the frozen shelf and the marsh use their own entrance, as before.
    /// </summary>
    public static class WaveScripts
    {
        // Spawn sources as the game stores them (Game.Levels.SpawnSource).
        private const int Nest = 4;
        private const int Hard = 2;
        private const int Psycho = 3;

        private static readonly BossRoute[] Gate = { BossRoute.MainGate };
        private static readonly BossRoute[] OwnEntrance = { BossRoute.Entrance };

        public static WaveScriptSpec[] All => new[] { Landing, Forest, Lava, Ruins, Nest_, Ice, Swamp };

        public static WaveScriptSpec ByKey(string key)
        {
            foreach (WaveScriptSpec script in All)
            {
                if (script.Key == key)
                {
                    return script;
                }
            }

            return null;
        }

        private static TroopSpec Join(string enemy, int firstWave, float weight, float interval, int source = 0, float delay = 0f, int minDifficulty = 0, int pack = 0) =>
            new TroopSpec { Enemy = enemy, FirstWave = firstWave, Weight = weight, Interval = interval, Source = source, Delay = delay, MinDifficulty = minDifficulty, Pack = pack };

        private static BossSpec Boss(int wave, string enemy, int minDifficulty = 0, BossRoute[] routes = null) =>
            new BossSpec { Wave = wave, Enemy = enemy, MinDifficulty = minDifficulty, Routes = routes ?? Gate };

        // The landing zone teaches the game: ant crowds first, then the pack hunters, scorpions from wave 6, and no sky
        // at all on easy; from medium on tide bats rise out of the lake halfway through.
        public static WaveScriptSpec Landing => new WaveScriptSpec
        {
            Key = "Landing",
            Waves = 24,
            Budget = 7f, FinalBudget = 170f,
            Troops = new[] { Join("Skitter", 1, 3f, 0.45f, pack: 6), Join("Prowler", 3, 2.2f, 1.2f, pack: 3), Join("Stinger", 6, 1.2f, 3f), Join("Tidebat", 8, 1.4f, 1.6f, Nest, 6f, 1) },
            Flyer = "Skyray",
            AirWaves = new[] { 9, 15, 20, 23 },
            AirWavesPerDifficulty = new[] { 0, 1, 2, 3 },
            AirSprinkleFrom = 3,
            Rhythm = "PPPRPPHPMPPRHPMPHPRPMRHP",
            Bosses = new[] { Boss(12, "BroodMother"), Boss(18, "BroodMother", Hard), Boss(6, "PackAlpha", Psycho), Boss(24, "PackAlpha") },
        };

        // The woods are about speed and the sky: rollers rush the roads, packs hunt, moss and tide bats pour out of
        // the trees and ponds, and the slugs grease the way for everyone else.
        public static WaveScriptSpec Forest => new WaveScriptSpec
        {
            Key = "Forest",
            Waves = 30,
            Budget = 9f, FinalBudget = 230f,
            Troops = new[]
            {
                Join("Skitter", 1, 2f, 0.45f, pack: 6), Join("Prowler", 1, 2.5f, 1.1f, pack: 3), Join("Roller", 2, 2.5f, 1.6f),
                Join("Mossbat", 3, 2f, 1.5f, Nest, 8f), Join("Tidebat", 5, 1.2f, 1.5f, Nest, 10f),
                Join("Oozer", 7, 1.5f, 2.4f), Join("Stinger", 10, 1.2f, 3f),
            },
            Flyer = "Skyray",
            AirWaves = new[] { 7, 13, 18, 23, 27 },
            AirWavesPerDifficulty = new[] { 1, 2, 3, 5 },
            AirSprinkleFrom = 2,
            Rhythm = "PRPMRPMRPPRMRPMRHRMPRMHRPMRHPR",
            Bosses = new[] { Boss(15, "RollerKing"), Boss(22, "RollerKing", Hard), Boss(8, "RollerKing", Psycho), Boss(30, "StormMatriarch") },
        };

        // The lava fields erupt: ember bats burst from the vents from the first wave, wisps drift down the roads,
        // scorpions and shield bearers walk the heat.
        public static WaveScriptSpec Lava => new WaveScriptSpec
        {
            Key = "Lava",
            Waves = 36,
            Budget = 7f, FinalBudget = 300f,
            Troops = new[]
            {
                Join("Prowler", 1, 3f, 1.1f, pack: 3), Join("Emberbat", 1, 2.5f, 1.5f, Nest, 6f), Join("Stinger", 4, 1.5f, 2.8f),
                Join("Skitter", 3, 1.5f, 0.45f, pack: 6), Join("Wisp", 4, 2f, 1.3f), Join("Roller", 5, 2f, 1.6f), Join("Bulwark", 10, 1.2f, 3.2f),
            },
            Flyer = "Skyray",
            AirWaves = new[] { 5, 9, 14, 19, 26, 32 },
            AirWavesPerDifficulty = new[] { 2, 3, 5, 6 },
            AirSprinkleFrom = 1,
            Rhythm = "PMPHMPMHPPMHMPMHMPHPMHPMHMPHMPMHPMHP",
            Bosses = new[] { Boss(12, "CoreWisp"), Boss(24, "AcidEmpress"), Boss(30, "AcidEmpress", Hard), Boss(18, "CoreWisp", Psycho), Boss(36, "CrystalWarlord") },
        };

        // The ruins are a siege: shield bearers in convoys, slugs that grease the roads, dust bats out of the sand
        // and tide bats from the cistern, and rays over the walls.
        public static WaveScriptSpec Ruins => new WaveScriptSpec
        {
            Key = "Ruins",
            Waves = 40,
            Budget = 10f, FinalBudget = 350f,
            Troops = new[]
            {
                Join("Bulwark", 9, 2f, 2.8f), Join("Dustbat", 1, 2.5f, 1.4f, Nest, 6f), Join("Oozer", 1, 1.5f, 2.2f),
                Join("Stinger", 6, 1.5f, 2.6f), Join("Skitter", 4, 1.5f, 0.45f, pack: 6), Join("Prowler", 5, 2f, 1.1f, pack: 3),
                Join("Tidebat", 8, 1.2f, 1.4f, Nest, 9f), Join("Wisp", 9, 1.5f, 1.3f),
            },
            Flyer = "Skyray",
            AirWaves = new[] { 6, 10, 16, 21, 26, 31, 37 },
            AirWavesPerDifficulty = new[] { 2, 3, 5, 7 },
            AirSprinkleFrom = 2,
            Rhythm = "PPMHPHMHPHMPHMHPHMHPHPMHPHMHPHMPHMHPPMHP",
            Bosses = new[] { Boss(14, "SlimeMonarch"), Boss(27, "SkySovereign"), Boss(34, "SlimeMonarch", Hard), Boss(20, "SlimeMonarch", Psycho), Boss(40, "CrystalWarlord") },
        };

        // The nest throws everything, in a steady rotation of rush, ambush and heavy, and ends with the queen.
        public static WaveScriptSpec Nest_ => new WaveScriptSpec
        {
            Key = "Nest",
            Waves = 48,
            Budget = 10f, FinalBudget = 450f,
            Troops = new[]
            {
                Join("Skitter", 1, 2f, 0.4f, pack: 6), Join("Prowler", 1, 3f, 1f, pack: 3), Join("Roller", 1, 2.5f, 1.5f), Join("Oozer", 2, 1.5f, 2f),
                Join("Tidebat", 2, 2f, 1.3f, Nest, 7f), Join("Dustbat", 3, 1.5f, 1.4f, Nest, 10f),
                Join("Stinger", 3, 1.5f, 2.4f), Join("Wisp", 4, 2f, 1.2f), Join("Bulwark", 7, 1.5f, 2.6f),
            },
            Flyer = "Skyray",
            AirWaves = new[] { 4, 7, 10, 14, 17, 21, 26, 31, 37, 43 },
            AirWavesPerDifficulty = new[] { 3, 5, 7, 10 },
            AirSprinkleFrom = 0,
            Rhythm = "RMHRMHRMHP",
            Bosses = new[] { Boss(12, "PackAlpha"), Boss(24, "AcidEmpress"), Boss(30, "AcidEmpress", Hard), Boss(36, "CoreWisp"), Boss(42, "CoreWisp", Psycho), Boss(48, "HiveQueen") },
        };

        // The frozen shelf is about armour and the cold: shardbacks crawl the roads under their ice, frost bats climb out
        // of the cracked lakes, wisps drift in over the snow and the warlord comes up through the ice.
        public static WaveScriptSpec Ice => new WaveScriptSpec
        {
            Key = "Ice",
            Waves = 34,
            Budget = 10f, FinalBudget = 250f,
            Troops = new[]
            {
                Join("Skitter", 1, 2f, 0.45f, pack: 6), Join("Prowler", 1, 2.4f, 1.1f, pack: 3), Join("Shardback", 5, 1.8f, 2.6f),
                Join("Frostbat", 2, 2f, 1.4f, Nest, 6f), Join("Roller", 4, 2f, 1.6f), Join("Wisp", 6, 1.8f, 1.3f),
                Join("Bulwark", 9, 1.4f, 2.8f), Join("Stinger", 12, 1.2f, 3f),
            },
            Flyer = "Skyray",
            AirWaves = new[] { 6, 11, 16, 21, 26, 31 },
            AirWavesPerDifficulty = new[] { 1, 2, 4, 6 },
            AirSprinkleFrom = 2,
            Rhythm = "PRPHMRPMRH",
            Bosses = new[] { Boss(17, "CrystalWarlord", 0, OwnEntrance), Boss(26, "CrystalWarlord", Hard, OwnEntrance), Boss(9, "CoreWisp", Psycho, OwnEntrance), Boss(34, "CoreWisp", 0, OwnEntrance) },
        };

        // The black marsh is about speed and slime: leeches grease the roads from the first wave, slugs follow, mire bats
        // and tide bats rise from the pools, and the monarch of the slime surfaces from the bog.
        public static WaveScriptSpec Swamp => new WaveScriptSpec
        {
            Key = "Swamp",
            Waves = 36,
            Budget = 10f, FinalBudget = 300f,
            Troops = new[]
            {
                Join("Skitter", 1, 2f, 0.4f, pack: 6), Join("Mireleech", 1, 2.5f, 1.6f), Join("Oozer", 1, 1.8f, 2.2f),
                Join("Mirebat", 2, 2f, 1.4f, Nest, 7f), Join("Prowler", 3, 2f, 1.1f, pack: 3), Join("Tidebat", 4, 1.2f, 1.5f, Nest, 9f),
                Join("Roller", 5, 1.8f, 1.6f), Join("Stinger", 8, 1.3f, 2.8f), Join("Bulwark", 11, 1.4f, 2.8f),
            },
            Flyer = "Skyray",
            AirWaves = new[] { 5, 10, 15, 20, 25, 30, 35 },
            AirWavesPerDifficulty = new[] { 1, 3, 5, 7 },
            AirSprinkleFrom = 1,
            Rhythm = "RMPRHMPRMP",
            Bosses = new[] { Boss(12, "SlimeMonarch", 0, OwnEntrance), Boss(24, "StormMatriarch", 0, OwnEntrance), Boss(30, "SlimeMonarch", Hard, OwnEntrance), Boss(18, "StormMatriarch", Psycho, OwnEntrance), Boss(36, "AcidEmpress", 0, OwnEntrance) },
        };
    }
}
