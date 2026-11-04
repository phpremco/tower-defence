using Game.Balance;
using static Game.Editor.WaveScripts;

namespace Game.Editor
{
    /// <summary>
    /// Continent 1, Crystal Plains (Docs/STORY.md, S4 spec): levels 1 to 6 of twenty. The script's unlock rule puts each
    /// weapon in the reward of the level before its debut: the Laser debuts in level 2, the Flak (the Rocket) in level 3,
    /// Quick Reload in level 5, the Land Mine and Chrono Field in level 6. No flyers before late in level 3; the fast
    /// Roller is level 5's teaser. Stone (rifts, the Cragback, the Shalebat) is heavy and slow but has no armour yet:
    /// the armour story starts in level 11. Levels 1 and 2 are short (10 to 12 waves) for newcomers; 3 to 6 run 14 to 18.
    /// </summary>
    public static partial class LevelSpecs
    {
        private const int CrystalMoneyStep = 20;

        // Air raids of levels 3 and 4 come packed, so the Flak's splash is clearly the answer (curator decision 8).
        private const float PackedFlyers = 0.45f;
        private const float LooseFlyers = 0.8f;

        // From Medium on level 3's raids are bigger, so a gun-only defence feels the missing Flak; Easy stays forgiving
        // (curator decision after balance, 2026-11-04). 1.3, the low end of the curator's range: at 1.6 the gun-only probe
        // bot lost every Medium run, at 1.3 it lost two of four (one Skyray through is enough) while the Flak bot held
        // all four untouched. Level 4 keeps its raids: its road joins late, the guns stand where the flight routes meet,
        // and a gun-only bot downed every flyer even at 1.6 and 1.8 times the raid.
        private static readonly float[] BigRaidsFromMedium = { 1f, 1.3f };

        /// <summary>Gun (PulseRifle), Cannon, Laser, Flak (Rocket), Tesla and Railgun; the Acid Sprayer joins in S5.</summary>
        public static readonly string[] CrystalRoster = { "PulseRifle", "Cannon", "Laser", "Rocket", "Tesla", "Railgun" };

        public static CampaignLevelSpec[] Crystal => new[]
        {
            // The first attack: ant crowds up the road and stone crawlers climbing out of the rifts. No sky at all.
            new CampaignLevelSpec
            {
                Key = "C1_01",
                Name = "Landing Site",
                Area = "South-west coast",
                Briefing = "The landing site on the south-west coast. Something is climbing out of the cracks in the plain and coming up the road. Hold the gates.",
                Tint = "Crystal",
                Layout = new LayoutSpec
                {
                    Key = "C1_01", Template = RoadTemplate.Serpentine, Seed = 101, Width = 36, Height = 20, Legs = 3, Gates = 3,
                    Terrain = new[] { new TerrainShare("Stone", 0.06f), new TerrainShare(LayoutTerrain.RaisedName, 0.03f) },
                    Nests = new[] { new NestCount("Stone", 2) },
                },
                Waves = new WaveScriptSpec
                {
                    Key = "C1_01",
                    Waves = 12,
                    Budget = 7f, FinalBudget = 95f,
                    Troops = new[] { Join("Skitter", 1, 3f, 0.45f, pack: 6), Join("Cragback", 5, 1.4f, 2.6f, Nest, 7f) },
                    Rhythm = "PPPRPHPMRPHP",
                    AirSprinkleFrom = 4,
                    Bosses = new[] { Boss(12, "BroodMother"), Boss(8, "BroodMother", Hard), Boss(5, "BroodMother", Psycho) },
                },
                RewardTower = "Laser",
                Source = "HomePlanetIntel",
                Credit = "Rebuilt by Dr. Voss's lab",
                RewardStory = "Dr. Voss rebuilt a mining laser into a turret: it fires fast but light, made for swarms.",
                Story = new[]
                {
                    new StorySpec { Kind = "LevelStart", Dialogue = "Dlg_L01_Start" },
                    new StorySpec { Kind = "WaveStart", Wave = 2, Dialogue = "Dlg_L01_Wave2" },
                    new StorySpec { Kind = "FirstSighting", Enemy = "Cragback", Dialogue = "Dlg_L01_Stone" },
                    new StorySpec { Kind = "BossSpawn", Dialogue = "Dlg_L01_Boss" },
                    new StorySpec { Kind = "LevelWon", Dialogue = "Dlg_L01_Won" },
                },
                Money = 200,
            },

            // Big, weak swarms for the new laser, wisps that both kinds of gun can hit, crawlers behind them.
            new CampaignLevelSpec
            {
                Key = "C1_02",
                Name = "Glass Meadows",
                Area = "Glass meadows",
                Briefing = "Crystal grass to the horizon. The swarms come in numbers now, many and weak: fast guns will count more than heavy ones.",
                Tint = "Wood",
                Layout = new LayoutSpec
                {
                    Key = "C1_02", Template = RoadTemplate.Hook, Seed = 102, Width = 38, Height = 22, Legs = 4, Gates = 4,
                    Terrain = new[] { new TerrainShare("Water", 0.06f), new TerrainShare("Stone", 0.03f), new TerrainShare(LayoutTerrain.RaisedName, 0.03f) },
                    Nests = new[] { new NestCount("Stone", 1) },
                },
                Waves = new WaveScriptSpec
                {
                    Key = "C1_02",
                    Waves = 12,
                    Budget = 8f, FinalBudget = 115f,
                    Troops = new[] { Join("Skitter", 1, 4f, 0.35f, pack: 10), Join("Wisp", 6, 1.5f, 1.3f), Join("Cragback", 8, 1.2f, 2.6f) },
                    Rhythm = "PRPPRHRPMRHP",
                    AirSprinkleFrom = 4,
                    Bosses = new[] { Boss(12, "BroodMother"), Boss(9, "CoreWisp", Hard), Boss(5, "BroodMother", Psycho) },
                },
                RewardTower = "Rocket",
                Source = "HomePlanetIntel",
                Credit = "Built on Dr. Voss's orders",
                RewardStory = "Dr. Voss had anti-air guns built. Just in case, she says.",
                Story = new[]
                {
                    new StorySpec { Kind = "LevelStart", Dialogue = "Dlg_L02_Start" },
                    new StorySpec { Kind = "WaveStart", Wave = 6, Dialogue = "Dlg_L02_Swarm" },
                },
                Money = 200 + CrystalMoneyStep,
            },

            // A second track out of the south; the first flyers come late, packed, over the ridge.
            new CampaignLevelSpec
            {
                Key = "C1_03",
                Name = "Shard Ridge",
                Area = "Shard ridge",
                Briefing = "A ridge of crystal shards, and a second track comes up out of the south. Keep an eye on the sky.",
                Tint = "Sand",
                Layout = new LayoutSpec
                {
                    Key = "C1_03", Template = RoadTemplate.Fork, Seed = 103, Width = 40, Height = 22, Legs = 3, Gates = 4, Plazas = 1,
                    Terrain = new[] { new TerrainShare("Sand", 0.06f), new TerrainShare("Stone", 0.03f), new TerrainShare(LayoutTerrain.RaisedName, 0.03f) },
                    Nests = new[] { new NestCount("Stone", 1) },
                },
                Waves = new WaveScriptSpec
                {
                    Key = "C1_03",
                    Waves = 16,
                    Budget = 9f, FinalBudget = 150f,
                    Troops = new[] { Join("Skitter", 1, 3f, 0.42f, pack: 6), Join("Cragback", 4, 1.3f, 2.5f), Join("Wisp", 8, 1.4f, 1.3f) },
                    Flyer = "Skyray",
                    FlyerInterval = PackedFlyers,
                    AirRaidScale = BigRaidsFromMedium,
                    AirWaves = new[] { 12, 15, 14 },
                    AirWavesPerDifficulty = new[] { 2, 2, 3, 3 },
                    AirSprinkleFrom = 2,
                    Rhythm = "PRPHPRMPHRPMHRPH",
                    Bosses = new[] { Boss(16, "BroodMother"), Boss(10, "CoreWisp", Hard), Boss(6, "BroodMother", Psycho) },
                },
                Story = new[]
                {
                    new StorySpec { Kind = "LevelStart", Dialogue = "Dlg_L03_Start" },
                    new StorySpec { Kind = "FirstSighting", Enemy = "Skyray", Dialogue = "Dlg_L03_Air" },
                    new StorySpec { Kind = "LevelWon", Dialogue = "Dlg_L03_Won" },
                },
                Money = 200 + CrystalMoneyStep * 2,
            },

            // The plain splits open: rifts everywhere, stone bats rising from them, rays overhead. Ground and air mixed.
            new CampaignLevelSpec
            {
                Key = "C1_04",
                Name = "Fault Line",
                Area = "Fault line",
                Briefing = "The plain splits open here. Rifts run beside the road and things climb out of them, on foot and on wings.",
                Tint = "Stone",
                Layout = new LayoutSpec
                {
                    Key = "C1_04", Template = RoadTemplate.LateJoin, Seed = 104, Width = 40, Height = 22, Legs = 3, Gates = 4,
                    Terrain = new[] { new TerrainShare("Stone", 0.07f), new TerrainShare(LayoutTerrain.RaisedName, 0.02f) },
                    Nests = new[] { new NestCount("Stone", 3) },
                },
                Waves = new WaveScriptSpec
                {
                    Key = "C1_04",
                    Waves = 16,
                    // 165 until 2026-11-03: with the honest kill money (paid late, after the walk to the gate) and the slow
                    // cragbacks stacking onto later waves, a crowd that big left the late creatures weaker than the first ones.
                    Budget = 9f, FinalBudget = 100f,
                    Troops = new[]
                    {
                        Join("Skitter", 1, 3f, 0.4f, pack: 6), Join("Shalebat", 5, 1.5f, 1.2f, Nest, 7f), Join("Wisp", 7, 1.4f, 1.3f),
                        Join("Cragback", 9, 1.3f, 2.5f),
                    },
                    Flyer = "Skyray",
                    FlyerInterval = PackedFlyers,
                    AirWaves = new[] { 3, 6, 9, 12, 15, 8, 14 },
                    AirWavesPerDifficulty = new[] { 4, 5, 6, 7 },
                    AirSprinkleFrom = 2,
                    Rhythm = "PRMPHRMPRHPMRPHP",
                    Bosses = new[] { Boss(16, "StormMatriarch"), Boss(10, "BroodMother", Hard), Boss(5, "CoreWisp", Psycho) },
                },
                RewardHeroPerk = "Perk_QuickReload",
                Credit = "Field upgrade from engineering",
                Story = new[]
                {
                    new StorySpec { Kind = "LevelStart", Dialogue = "Dlg_L04_Start" },
                    new StorySpec { Kind = "BossSpawn", Enemy = "StormMatriarch", Dialogue = "Dlg_L04_Boss" },
                },
                Money = 200 + CrystalMoneyStep * 3,
            },

            // Open flats, a long winding road, stone bats out of the cracks, and a glimpse of something very fast.
            new CampaignLevelSpec
            {
                Key = "C1_05",
                Name = "Quartz Flats",
                Area = "Quartz flats",
                Briefing = "The quartz flats, open and wide. The road winds through the dig sites; keep an eye on the cracks.",
                Tint = "Water",
                Layout = new LayoutSpec
                {
                    Key = "C1_05", Template = RoadTemplate.Hook, Seed = 105, Width = 40, Height = 22, Legs = 5, Gates = 4,
                    Terrain = new[] { new TerrainShare("Water", 0.06f), new TerrainShare("Stone", 0.03f), new TerrainShare(LayoutTerrain.RaisedName, 0.03f) },
                    Nests = new[] { new NestCount("Stone", 2) },
                },
                Waves = new WaveScriptSpec
                {
                    Key = "C1_05",
                    Waves = 17,
                    Budget = 10f, FinalBudget = 185f,
                    Troops = new[]
                    {
                        Join("Skitter", 1, 2.6f, 0.42f, pack: 6), Join("Cragback", 3, 1.3f, 2.5f), Join("Shalebat", 6, 1.5f, 1.3f, Nest, 7f),
                        Join("Wisp", 9, 1.4f, 1.3f),
                    },
                    Flyer = "Skyray",
                    FlyerInterval = LooseFlyers,
                    AirWaves = new[] { 4, 8, 13, 16, 6, 10 },
                    AirWavesPerDifficulty = new[] { 3, 4, 5, 6 },
                    AirSprinkleFrom = 2,
                    Rhythm = "PMRPHPRMHPRPMHRPH",
                    Bosses = new[] { Boss(17, "CoreWisp"), Boss(11, "StormMatriarch", Hard), Boss(5, "BroodMother", Psycho) },
                    Teasers = new[] { Teaser("Roller", 12, 2), Teaser("Roller", 15, 3) },
                },
                RewardGadgets = new[] { "Mine", "SlowField" },
                Credit = "Crysium prototypes by Dr. Voss",
                Story = new[]
                {
                    new StorySpec { Kind = "FirstSighting", Enemy = "Roller", Dialogue = "Dlg_L05_Fast" },
                },
                Money = 200 + CrystalMoneyStep * 4,
            },

            // The vein that never runs dry: the fast rollers and the pack hunters in earnest, and the first gadgets.
            new CampaignLevelSpec
            {
                Key = "C1_06",
                Name = "Crysium Vein",
                Area = "Crysium vein",
                Briefing = "Down to the vein. The crystals here never run dry. Voss has something new for the road: a mine, and a field that slows time.",
                Tint = "Vein",
                Layout = new LayoutSpec
                {
                    Key = "C1_06", Template = RoadTemplate.Fork, Seed = 106, Width = 42, Height = 24, Legs = 3, Gates = 5, Plazas = 1,
                    Terrain = new[] { new TerrainShare("Stone", 0.04f), new TerrainShare("Water", 0.03f), new TerrainShare(LayoutTerrain.RaisedName, 0.03f) },
                    Nests = new[] { new NestCount("Stone", 1) },
                },
                Waves = new WaveScriptSpec
                {
                    Key = "C1_06",
                    Waves = 18,
                    Budget = 10f, FinalBudget = 210f,
                    Troops = new[]
                    {
                        Join("Roller", 2, 1.4f, 1.6f), Join("Skitter", 1, 2.4f, 0.42f, pack: 6), Join("Prowler", 6, 2.2f, 1.1f, pack: 3),
                        Join("Shalebat", 5, 1.3f, 1.4f, Nest, 8f), Join("Cragback", 10, 1.2f, 2.5f),
                    },
                    Flyer = "Skyray",
                    FlyerInterval = LooseFlyers,
                    AirWaves = new[] { 8, 11, 14, 17, 5, 15 },
                    AirWavesPerDifficulty = new[] { 3, 4, 5, 6 },
                    AirSprinkleFrom = 1,
                    Rhythm = "RMPHRPMRHPRMPHRPMH",
                    Bosses = new[] { Boss(18, "PackAlpha"), Boss(12, "CoreWisp", Hard), Boss(7, "StormMatriarch", Psycho) },
                },
                Story = new[]
                {
                    new StorySpec { Kind = "LevelStart", Dialogue = "Dlg_L06_Start" },
                    new StorySpec { Kind = "FirstUse", PathObject = "Mine", Dialogue = "Dlg_L06_Mine" },
                    new StorySpec { Kind = "FirstUse", PathObject = "SlowField", Dialogue = "Dlg_L06_Chrono" },
                    new StorySpec { Kind = "FirstSighting", Enemy = "Roller", Dialogue = "Dlg_L06_Fast" },
                    new StorySpec { Kind = "FirstSighting", Enemy = "Prowler", Dialogue = "Dlg_L06_Fast" },
                },
                Money = 200 + CrystalMoneyStep * 5,
            },
        };
    }
}
