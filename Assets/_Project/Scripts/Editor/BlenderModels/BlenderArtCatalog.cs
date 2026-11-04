using System.Collections.Generic;
using Game.Core;

namespace Game.Editor
{
    /// <summary>
    /// Which Blender exports the game imports, how each sits in the game, and which enemy wears which. The numbers come
    /// from <c>ArtSource/README.md</c> (scales, pivots, power frames); the look numbers are tuned on in-game screenshots.
    /// Later import stages (hero, towers, structures, scenery) add their own rows here.
    /// </summary>
    public static class BlenderArtCatalog
    {
        // A boss model is built at its species' size times this (README, "Unity notes for the bosses").
        private const float BossFactor = 0.6f;

        // Cycles that loop; every other take plays once.
        private static readonly HashSet<string> LoopClips = new HashSet<string>
        {
            "Walk", "Run", "Crawl", "Fly", "Glide", "Float", "Grounded", "Roll", "Idle",
            // The towers' cycles (README rounds 7 and 11).
            "IdleSweep", "Spin", "Beam", "FireAlternate", "Spray", "Slosh",
        };

        private static List<BlenderArtEntry> _creatures;

        /// <summary>The creature art rows, in roster order.</summary>
        public static IReadOnlyList<BlenderArtEntry> Creatures => _creatures ??= CreateCreatures();

        /// <summary>Which art each enemy definition wears, by enemy asset name. Enemies not listed keep their procedural body.</summary>
        public static readonly IReadOnlyDictionary<string, string> EnemyArt = new Dictionary<string, string>
        {
            { "Skitter", "Skitter" },
            { "Roller", "Roller" },
            { "Cragback", "Cragback" },
            { "Prowler", "Prowler" },
            { "Skyray", "Skyray" },
            { "Shalebat", "Shalebat" },
            { "Wisp", "Wisp" },
            { "Stinger", "Stinger" },
            { "Bulwark", "Bulwark" },
            { "Oozer", "Oozer" },
            { "Emberbat", "Bat" },
            { "Mossbat", "Bat_Moss" },
            { "Dustbat", "Bat_Dust" },
            { "Tidebat", "Bat_Tide" },
            { "Frostbat", "Bat_Frost" },
            // No bog skin is baked yet: the marsh bat wears the moss one, the closest in colour.
            { "Mirebat", "Bat_Moss" },
            { "HiveQueen", "Queen" },
            { "BroodMother", "BroodMother" },
            { "PackAlpha", "PackAlpha" },
            { "CoreWisp", "CoreWisp" },
            { "AcidEmpress", "AcidEmpress" },
            { "CrystalWarlord", "CrystalWarlord" },
            { "SlimeMonarch", "SlimeMonarch" },
            { "RollerKing", "RollerKing" },
            // Curator decision 2026-11-04: each boss wears the model of its own family. In the data the Storm Matriarch
            // is the bat boss and the Sky Sovereign the ray boss; the exports are named the other way round.
            { "StormMatriarch", "BatBoss" },
            { "SkySovereign", "RayBoss" },
        };

        public static bool IsLoop(string clip) => LoopClips.Contains(clip);

        /// <summary>Every export folder the creature rows read.</summary>
        public static IEnumerable<string> SyncedAssets()
        {
            var seen = new HashSet<string>();
            foreach (BlenderArtEntry entry in Creatures)
            {
                if (seen.Add(entry.Asset))
                {
                    yield return entry.Asset;
                }
            }
        }

        private static List<BlenderArtEntry> CreateCreatures()
        {
            var bat = new[] { new BlenderClipSpec("Glide", ModelClipRole.Idle) };
            var rows = new List<BlenderArtEntry>
            {
                // Look numbers below come from play-size screenshots (imp1_*): the small and the dark-red species read too dark
                // against the night ground, so their albedo is lifted and their glow pushed.
                new BlenderArtEntry("Skitter", "Skitter") { MoveDistance = 0.36f, Brightness = 1.6f, Emission = 3.5f, Rim = 0.9f },
                new BlenderArtEntry("Roller", "Roller"),
                new BlenderArtEntry("Cragback", "Cragback") { DeathFadeFrom = 8, DeathFadeTo = 40 },
                new BlenderArtEntry("Prowler", "Prowler") { Brightness = 1.45f, Emission = 3.5f, Rim = 0.9f },
                new BlenderArtEntry("Skyray", "Skyray")
                {
                    Emission = 3.2f,
                    BodyCentrePivot = true,
                    Clips = new[] { new BlenderClipSpec("Dive", ModelClipRole.Attack, 10) },
                },
                new BlenderArtEntry("Shalebat", "Shalebat") { BodyCentrePivot = true, Clips = bat },
                new BlenderArtEntry("Wisp", "Wisp") { Bleeds = false },
                new BlenderArtEntry("Stinger", "Stinger") { Clips = new[] { new BlenderClipSpec("Strike", ModelClipRole.Attack, 10) } },
                new BlenderArtEntry("Bulwark", "Bulwark") { Clips = new[] { new BlenderClipSpec("Block", ModelClipRole.Block, 7) } },
                new BlenderArtEntry("Oozer", "Oozer"),
                new BlenderArtEntry("Bat", "Bat") { BodyCentrePivot = true, Clips = bat },
                new BlenderArtEntry("Bat_Moss", "Bat", "Moss") { BodyCentrePivot = true, Clips = bat },
                new BlenderArtEntry("Bat_Dust", "Bat", "Dust") { BodyCentrePivot = true, Clips = bat },
                new BlenderArtEntry("Bat_Tide", "Bat", "Tide") { BodyCentrePivot = true, Clips = bat },
                new BlenderArtEntry("Bat_Frost", "Bat", "Frost") { BodyCentrePivot = true, Clips = bat },
                new BlenderArtEntry("Queen", "Queen")
                {
                    // Round 2 grew her far past the old queen (2.8 high in model units): at the boss factor she covered a
                    // quarter of the screen, so she gets her own (imp1_d_1).
                    Scale = 0.33f,
                    MoveDistance = 0.9f,
                    Clips = new[] { new BlenderClipSpec("Scream", ModelClipRole.Power, 25, "Pulse") },
                },
                new BlenderArtEntry("BroodMother", "BroodMother")
                {
                    Scale = BossFactor,
                    Clips = new[] { new BlenderClipSpec("Spawn", ModelClipRole.Power, 40, "Spawn", "Vent") },
                },
                new BlenderArtEntry("PackAlpha", "PackAlpha")
                {
                    Scale = BossFactor,
                    Clips = new[] { new BlenderClipSpec("Howl", ModelClipRole.Power, 22, "Rally") },
                },
                new BlenderArtEntry("CoreWisp", "CoreWisp")
                {
                    Scale = BossFactor,
                    Bleeds = false,
                    Clips = new[] { new BlenderClipSpec("Pulse", ModelClipRole.Power, 14, "Pulse") },
                },
                new BlenderArtEntry("AcidEmpress", "AcidEmpress")
                {
                    Scale = BossFactor,
                    Clips = new[]
                    {
                        new BlenderClipSpec("Strike", ModelClipRole.Attack, 10),
                        new BlenderClipSpec("Spray", ModelClipRole.Power, 26, "Volley", "Gland"),
                    },
                },
                new BlenderArtEntry("CrystalWarlord", "CrystalWarlord")
                {
                    Scale = BossFactor,
                    Clips = new[]
                    {
                        new BlenderClipSpec("Block", ModelClipRole.Block, 7),
                        new BlenderClipSpec("Sweep", ModelClipRole.Power, 20, "Pulse"),
                    },
                },
                new BlenderArtEntry("SlimeMonarch", "SlimeMonarch")
                {
                    Scale = BossFactor,
                    Clips = new[]
                    {
                        new BlenderClipSpec("Flood", ModelClipRole.Power, 24, "Pulse"),
                        new BlenderClipSpec("Spawn", ModelClipRole.Power, 30, "Spawn", "Sac_L"),
                    },
                },
                new BlenderArtEntry("RollerKing", "RollerKing")
                {
                    Scale = BossFactor,
                    Clips = new[] { new BlenderClipSpec("Quake", ModelClipRole.Power, 16, "Pulse") },
                },
                // The bat boss (exported as SkySovereign) on the Storm Matriarch, a bat boss of scale 1.2 in the data:
                // 0.7 keeps the model at the size it was built for (0.84). Its tail lens throws her element storm.
                new BlenderArtEntry("BatBoss", "SkySovereign")
                {
                    Scale = 0.7f,
                    BodyCentrePivot = true,
                    Clips = new[]
                    {
                        new BlenderClipSpec("Glide", ModelClipRole.Idle),
                        new BlenderClipSpec("Laser", ModelClipRole.Power, 20, "Volley", "Emitter"),
                    },
                },
                // The ray boss (exported as StormMatriarch) on the Sky Sovereign, a ray boss of scale 1.4 in the data:
                // 0.514 keeps the model at its built size (0.72). Its tail laser has no clip of its own; the beam is VFX.
                new BlenderArtEntry("RayBoss", "StormMatriarch")
                {
                    Scale = 0.514f,
                    BodyCentrePivot = true,
                },
            };
            return rows;
        }
    }
}
