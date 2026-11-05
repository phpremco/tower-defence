using System.Collections.Generic;
using Game.Core;
using UnityEngine;

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

        // The continent bosses (README rounds 14 and 16) are built at their game size: no factor on the boss's own scale.
        private const float TitanFactor = 1f;

        public const string CorruptedSkin = "Corrupted";
        public const string CorruptedSuffix = "_Corrupted";

        // Exports with a baked corrupted skin under Skins/Corrupted (ArtSource README round 15, Corrupted_skins_report.json).
        private static readonly HashSet<string> CorruptedSkins = new HashSet<string>
        {
            "Skitter", "Roller", "Cragback", "Prowler", "Skyray", "Shalebat", "Wisp", "Stinger", "Bat", "Bulwark", "Oozer",
            "Shardback", "Mireleech", "Knitback", "Slagstrider", "Siftjaw", "Surgefin", "Queen", "BroodMother", "PackAlpha",
            "CoreWisp", "AcidEmpress", "CrystalWarlord", "SlimeMonarch", "RollerKing", "SkySovereign", "StormMatriarch",
        };

        // Cycles that loop; every other take plays once.
        private static readonly HashSet<string> LoopClips = new HashSet<string>
        {
            "Walk", "Run", "Crawl", "Fly", "Glide", "Float", "Grounded", "Roll", "Idle", "Swim", "WalkEnraged",
            // The towers' cycles (README rounds 7 and 11).
            "IdleSweep", "Spin", "Beam", "FireAlternate", "Spray", "Slosh",
            // The living spawn origins (README round 23): the marker breath.
            "Marked",
            // The continent bosses' mechanic cycles (README round 28): in place, the owner moves the body.
            "Climb_Loop", "Submerged_Loop", "Burrow_Travel", "Hunt_Stride", "Rooted_Idle",
            // Gravemaw's gate crossing (2026-11-05, being made in Blender).
            "Gate_Straddle_Loop",
        };

        // A shard's burst out of the giant and its flight back into it (README round 28).
        private static readonly BlenderClipSpec[] Shard =
        {
            new BlenderClipSpec("Spawn", ModelClipRole.Spawn),
            new BlenderClipSpec("Rejoin", ModelClipRole.Rejoin),
            // Round 29: the runner and the crawler climb a 1 m ruin ledge (the flyer has no climb clips; a missing clip is skipped).
            new BlenderClipSpec("Climb_Up", ModelClipRole.ClimbUp),
            new BlenderClipSpec("Climb_Loop", ModelClipRole.ClimbLoop),
            new BlenderClipSpec("Climb_Down", ModelClipRole.ClimbDown),
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
            { "Shardback", "Shardback" },
            { "Mireleech", "Mireleech" },
            // The continent bosses (Docs/BOSSES.md); only Gravemaw is placed so far, the others are Codex data.
            { "Gravemaw", "Gravemaw" },
            { "CinderMatriarch", "CinderMatriarch" },
            { "FrostWarden", "FrostWarden" },
            { "Rotbloom", "Rotbloom" },
            // The Cinder Matriarch's brood (round 28): the egg she lays in the lava and the fire larva that hatches from it.
            { "CinderEgg", "CinderEgg" },
            { "FireLarva", "FireLarva" },
            { "Shattered", "Shattered" },
            { "SealedOne", "SealedOne" },
            // Round 28: Rotbloom's spore pod (a flyer the air towers shoot down) and the Shattered's three shards.
            { "RotSporePod", "RotSporePod" },
            { "ShardRunner", "ShardRunner" },
            { "ShardFlyer", "ShardFlyer" },
            { "ShardCrawler", "ShardCrawler" },
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
                // Round 31: the slug strike stretches the body itself (impact frame 10).
                new BlenderArtEntry("Oozer", "Oozer") { Clips = new[] { new BlenderClipSpec("Strike", ModelClipRole.Attack, 10) } },
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
                        // Round 31: the stretching strike (impact frame 13).
                        new BlenderClipSpec("Strike", ModelClipRole.Attack, 13),
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

                // Round 15, P3 species (import stage 4b). The Shardback sheds its six prism clusters with its armour; the
                // Mireleech's latch bites on frame 13.
                // The amber sockets glowed through the ice at 2.5 (cre_close): lower, so the cold prisms lead.
                new BlenderArtEntry("Shardback", "Shardback")
                {
                    Emission = 2f,
                    ShedBonesKey = "shard_bones_in_shed_order",
                    Clips = new[] { new BlenderClipSpec("Shatter", ModelClipRole.Shatter, 3) },
                },
                new BlenderArtEntry("Mireleech", "Mireleech") { Clips = new[] { new BlenderClipSpec("Latch", ModelClipRole.Attack, 13) } },

                // Art ready for species without game data yet ("art klaar, wacht op data"): an EnemyDefinition that points
                // at one of these wears it with no further code. Their special moments are recorded as clips for later views.
                new BlenderArtEntry("Knitback", "Knitback") { Clips = new[] { new BlenderClipSpec("Regrow", ModelClipRole.Power, 24, "Regrow", "Pod_1") } },
                // Burning trail and heat vents: it sheds embers, and its vents glow harder (README: emission 3.0).
                new BlenderArtEntry("Slagstrider", "Slagstrider") { Embers = true, Emission = 3f },
                new BlenderArtEntry("Siftjaw", "Siftjaw")
                {
                    Clips = new[] { new BlenderClipSpec("Burrow", ModelClipRole.Burrow, 24), new BlenderClipSpec("Emerge", ModelClipRole.Emerge, 8) },
                },
                new BlenderArtEntry("Surgefin", "Surgefin") { Clips = new[] { new BlenderClipSpec("Emerge", ModelClipRole.Emerge, 10) } },
                new BlenderArtEntry("FireLarva", "FireLarva") { Embers = true },

                // Round 14 and 16 continent bosses: two materials (body and crystals, ice, bloom or seal), built at their
                // game size, so the art's factor is 1 on the boss's own scale.
                new BlenderArtEntry("Gravemaw", "Gravemaw")
                {
                    Scale = TitanFactor,
                    SecondMaterial = "Crystal",
                    Clips = new[]
                    {
                        new BlenderClipSpec("Roar", ModelClipRole.Roar, 12),
                        new BlenderClipSpec("Dive", ModelClipRole.Burrow, 31),
                        new BlenderClipSpec("Emerge", ModelClipRole.Emerge, 20),
                        // Round 28: the mechanic clips; their offsets, holes and banks come from the report's round28 block.
                        new BlenderClipSpec("Climb_Up", ModelClipRole.ClimbUp),
                        new BlenderClipSpec("Climb_Loop", ModelClipRole.ClimbLoop),
                        new BlenderClipSpec("Climb_Down", ModelClipRole.ClimbDown),
                        new BlenderClipSpec("Dive_Into_Liquid", ModelClipRole.DiveIn, 42),
                        new BlenderClipSpec("Submerged_Loop", ModelClipRole.Submerged),
                        new BlenderClipSpec("Surface_Burst", ModelClipRole.SurfaceBurst, 24),
                        new BlenderClipSpec("Burrow_Down", ModelClipRole.BurrowDown, 31),
                        new BlenderClipSpec("Burrow_Travel", ModelClipRole.BurrowTravel),
                        new BlenderClipSpec("Surface_Throw", ModelClipRole.SurfaceThrow, 30),
                        // Its own gate crossing (2026-11-05, in the making): picked up when the export has them; until then the
                        // brain falls back to the rock-climb clips (ContinentBossBrain.ClimbStructure).
                        new BlenderClipSpec("Gate_Mount", ModelClipRole.GateMount),
                        new BlenderClipSpec("Gate_Straddle_Loop", ModelClipRole.GateStraddle),
                        new BlenderClipSpec("Gate_Dismount", ModelClipRole.GateDismount),
                    },
                },
                new BlenderArtEntry("CinderMatriarch", "CinderMatriarch")
                {
                    Scale = TitanFactor,
                    SecondMaterial = "Crystal",
                    Embers = true,
                    Clips = new[]
                    {
                        new BlenderClipSpec("Spawn", ModelClipRole.Power, 30, "Spawn", "Vent_1"),
                        new BlenderClipSpec("Roar", ModelClipRole.Roar, 20),
                        // Round 28: Hatred of the Cold; moments, offsets and the strike reach come from the round28 block.
                        new BlenderClipSpec("Climb_Up", ModelClipRole.ClimbUp),
                        new BlenderClipSpec("Climb_Loop", ModelClipRole.ClimbLoop),
                        new BlenderClipSpec("Climb_Down", ModelClipRole.ClimbDown),
                        new BlenderClipSpec("Dive_Into_Liquid", ModelClipRole.DiveIn, 28),
                        new BlenderClipSpec("Submerged_Loop", ModelClipRole.Submerged),
                        new BlenderClipSpec("Surface_Burst", ModelClipRole.SurfaceBurst, 16),
                        new BlenderClipSpec("Hunt_Stride", ModelClipRole.HuntMove),
                        new BlenderClipSpec("Smash", ModelClipRole.Smash),
                        new BlenderClipSpec("Lay_Eggs", ModelClipRole.LayEggs, 0, string.Empty, "Drip"),
                    },
                },
                // Her egg (round 28): it rests, then hatches (the larva leaves at frame 32) or bursts when it is shot.
                new BlenderArtEntry("CinderEgg", "CinderEgg")
                {
                    Bleeds = false,
                    Embers = true,
                    Clips = new[]
                    {
                        new BlenderClipSpec("Hatch", ModelClipRole.Hatch, 32),
                        new BlenderClipSpec("Burst", ModelClipRole.Death),
                    },
                },
                new BlenderArtEntry("FrostWarden", "FrostWarden")
                {
                    Scale = TitanFactor,
                    SecondMaterial = "Ice",
                    // Round 30: the baked ice maps are cold now, so the round-5 flat tint is dropped (baked albedo and glow
                    // kept); the cold rim and a faint glow (emission x 2, README round 30) stay.
                    Ice = new IceLook { FlatColour = false, Tint = Color.white, Glow = Color.white, GlowStrength = 2f },
                    // Round 28: the ice armour is an object of its own with its own maps (FrostWarden_IceArmour_*).
                    ExtraParts = new[] { "IceArmour" },
                    Clips = new[]
                    {
                        new BlenderClipSpec("FreezePulse", ModelClipRole.Power, 30, "Pulse"),
                        new BlenderClipSpec("Enrage", ModelClipRole.Enrage),
                        // Round 28: Gate Breaker; moments, offsets and the gate reach come from the round28 block.
                        new BlenderClipSpec("Climb_Up", ModelClipRole.ClimbUp),
                        new BlenderClipSpec("Climb_Loop", ModelClipRole.ClimbLoop),
                        new BlenderClipSpec("Climb_Down", ModelClipRole.ClimbDown),
                        new BlenderClipSpec("Dive_Into_Liquid", ModelClipRole.DiveIn, 30),
                        new BlenderClipSpec("Submerged_Loop", ModelClipRole.Submerged),
                        new BlenderClipSpec("Surface_Burst", ModelClipRole.SurfaceBurst, 16),
                        new BlenderClipSpec("Freeze_Gate", ModelClipRole.FreezeGate, 0, string.Empty, "Socket_Breath"),
                        new BlenderClipSpec("Shatter_Through", ModelClipRole.ShatterThrough),
                        new BlenderClipSpec("Armour_Break", ModelClipRole.ArmourBreak),
                    },
                },
                new BlenderArtEntry("Rotbloom", "Rotbloom")
                {
                    Scale = TitanFactor,
                    SecondMaterial = "Bloom",
                    Clips = new[]
                    {
                        new BlenderClipSpec("Regrow", ModelClipRole.Power, 28, "Regrow", "Pod_R1"),
                        new BlenderClipSpec("Bloom", ModelClipRole.Power, 34, "Pulse", "Bloom"),
                        // Round 28: Overgrowth; shots, offsets and banks come from the round28 block.
                        new BlenderClipSpec("Rooted_Idle", ModelClipRole.Rooted),
                        new BlenderClipSpec("Uproot", ModelClipRole.Uproot, 36),
                        new BlenderClipSpec("Spore_Volley", ModelClipRole.SporeVolley, 0, string.Empty, "Socket_Spore"),
                        new BlenderClipSpec("Roar", ModelClipRole.Roar, 16),
                        new BlenderClipSpec("Climb_Up", ModelClipRole.ClimbUp),
                        new BlenderClipSpec("Climb_Loop", ModelClipRole.ClimbLoop),
                        new BlenderClipSpec("Climb_Down", ModelClipRole.ClimbDown),
                        new BlenderClipSpec("Dive_Into_Liquid", ModelClipRole.DiveIn, 30),
                        new BlenderClipSpec("Submerged_Loop", ModelClipRole.Submerged),
                        new BlenderClipSpec("Surface_Burst", ModelClipRole.SurfaceBurst, 18),
                    },
                },
                // Its props (round 28): the spore pod flies (Fly) and pops (Burst); the vines and the roots are still models.
                new BlenderArtEntry("RotSporePod", "RotSporePod")
                {
                    Scale = TitanFactor,
                    BodyCentrePivot = true,
                    Bleeds = false,
                    Emission = 3.2f,
                    Clips = new[] { new BlenderClipSpec("Burst", ModelClipRole.Death, 6) },
                },
                new BlenderArtEntry("RotOvergrowth", "RotOvergrowth") { Scale = TitanFactor },
                new BlenderArtEntry("RotbloomRoots", "RotbloomRoots") { Scale = TitanFactor },
                new BlenderArtEntry("Shattered", "Shattered")
                {
                    Scale = TitanFactor,
                    SecondMaterial = "Crystal",
                    // The crystals burst at frame 46 and are gone by 54: the glow dies with them.
                    DeathFadeFrom = 46,
                    DeathFadeTo = 54,
                    Clips = new[]
                    {
                        new BlenderClipSpec("PhaseBreak1", ModelClipRole.PhaseBreak, 12, "2"),
                        new BlenderClipSpec("PhaseBreak2", ModelClipRole.PhaseBreak, 14, "3"),
                        // Round 28: Splitting (burst at 28, formed at 30) and the shared body's climbs and dives.
                        new BlenderClipSpec("Split", ModelClipRole.Split, 28),
                        new BlenderClipSpec("Rejoin", ModelClipRole.Rejoin, 30),
                        new BlenderClipSpec("Roar", ModelClipRole.Roar, 18),
                        new BlenderClipSpec("Climb_Up", ModelClipRole.ClimbUp),
                        new BlenderClipSpec("Climb_Loop", ModelClipRole.ClimbLoop),
                        new BlenderClipSpec("Climb_Down", ModelClipRole.ClimbDown),
                        new BlenderClipSpec("Dive_Into_Liquid", ModelClipRole.DiveIn, 28),
                        new BlenderClipSpec("Submerged_Loop", ModelClipRole.Submerged),
                        new BlenderClipSpec("Surface_Burst", ModelClipRole.SurfaceBurst, 16),
                    },
                },
                // Its shards (round 28): each bursts out of the giant (Spawn) and flies back into it (Rejoin).
                new BlenderArtEntry("ShardRunner", "ShardRunner") { Scale = TitanFactor, Clips = Shard },
                new BlenderArtEntry("ShardFlyer", "ShardFlyer") { Scale = TitanFactor, BodyCentrePivot = true, Clips = Shard },
                new BlenderArtEntry("ShardCrawler", "ShardCrawler") { Scale = TitanFactor, Clips = Shard },
                new BlenderArtEntry("SealedOne", "SealedOne")
                {
                    Scale = TitanFactor,
                    SecondMaterial = "Seal",
                    // Round 28: its glow per phase and the white release, swapped at the Release clip's white frame.
                    EmissionSets = new[] { "Phase1", "Phase2", "Phase3", "Phase4", "Release" },
                    DeathEmissionSet = "Release",
                    Clips = new[]
                    {
                        new BlenderClipSpec("Rise", ModelClipRole.Emerge, 22),
                        new BlenderClipSpec("Summon", ModelClipRole.Power, 44, "Spawn", "Head"),
                        // Its end is the release, not a death; the old Death (the seal shutting) is kept as a burrow.
                        new BlenderClipSpec("Release", ModelClipRole.Death, 64),
                        new BlenderClipSpec("Death", ModelClipRole.Burrow, 124),
                        new BlenderClipSpec("Roar", ModelClipRole.Roar, 30),
                        new BlenderClipSpec("Climb_Up", ModelClipRole.ClimbUp, 30),
                        new BlenderClipSpec("Climb_Loop", ModelClipRole.ClimbLoop),
                        new BlenderClipSpec("Climb_Down", ModelClipRole.ClimbDown, 30),
                        new BlenderClipSpec("Dive_Into_Liquid", ModelClipRole.DiveIn, 45),
                        new BlenderClipSpec("Submerged_Loop", ModelClipRole.Submerged),
                        new BlenderClipSpec("Surface_Burst", ModelClipRole.SurfaceBurst, 24),
                        new BlenderClipSpec("Rise_OntoBase", ModelClipRole.RiseOntoBase, 96),
                        new BlenderClipSpec("Adapt", ModelClipRole.Adapt, 30),
                        new BlenderClipSpec("OnBase_Loop", ModelClipRole.OnBaseLoop),
                    },
                },
            };

            AddCorruptedSkins(rows);
            return rows;
        }

        // Every one-material export with a baked corrupted skin gets a second row wearing it (<Id>_Corrupted): the same
        // mesh, rig and clips with the violet-veined maps, so a corrupted enemy is data only.
        private static void AddCorruptedSkins(List<BlenderArtEntry> rows)
        {
            int count = rows.Count;
            for (int i = 0; i < count; i++)
            {
                BlenderArtEntry row = rows[i];
                if (row.Skin == null && row.SecondMaterial == null && CorruptedSkins.Contains(row.Asset))
                {
                    rows.Add(row.WithSkin(row.Id + CorruptedSuffix, CorruptedSkin));
                }
            }
        }
    }
}
