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

        // A corrupted row's extra parts keep the export's own maps (the baked skins have none for them): their albedo and
        // glow lean towards the skins' violet (the corrupted bakes average about 0.2/0.1/0.3 albedo and 0.3/0.15/0.45 glow).
        private static readonly Color CorruptedPartTint = new Color(0.72f, 0.6f, 0.95f);
        private static readonly Color CorruptedPartGlow = new Color(0.8f, 0.45f, 1f);

        // Round 45 (boss close-up detail pass): the boss-family faces and claws, and the continent bosses' eyes, teeth and
        // claws, each an object of its own with its own maps (report key round45_detail or r45_detail).
        private static readonly string[] FaceClaws = { "Face", "Claws" };
        private static readonly string[] EyesTeethClaws = { "Eyes", "Teeth", "Claws" };

        // Round 46 (small-enemy close-up detail pass, report key r46_detail): wet eyes and mouthparts as one Face object,
        // claws, talons and spurs as one Claws object. The slugs (Oozer, Mireleech) have no limbs, so only a face.
        private static readonly string[] FaceOnly = { "Face" };

        // The element bats' baked skins cover the body only; the export's element-neutral parts lean towards each skin's glow.
        // Tide and Frost glow warm like the ember bat (Skins_warm_report.json), so their parts keep the export's own colour.
        private static readonly Color MossBatPartGlow = new Color(0.75f, 1f, 0.6f);
        private static readonly Color DustBatPartGlow = new Color(1f, 0.95f, 0.8f);

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
            // Mercer's tense hold after the Startled flinch (README round 35).
            "Startled_Hold",
            // Spencer pass 4: the jetpack flight, in place; the NTI flag's cloth.
            "Jet_Fly", "Wave",
            // Spencer's acting pass (round 40): the nervous aim held while the brood glares.
            "Aim_Hold_Nervous_Loop",
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
            // Round 36: every species lunges with a strike of its own (anticipation, stretch, follow-through; hit on frame 10).
            var strike = new BlenderClipSpec("Strike", ModelClipRole.Attack, 10);
            var bat = new[] { new BlenderClipSpec("Glide", ModelClipRole.Idle), strike };
            var rows = new List<BlenderArtEntry>
            {
                // Look numbers below come from play-size screenshots (imp1_*): the small and the dark-red species read too dark
                // against the night ground, so their albedo is lifted and their glow pushed.
                new BlenderArtEntry("Skitter", "Skitter") { ExtraParts = FaceClaws, MoveDistance = 0.36f, Brightness = 1.6f, Emission = 3.5f, Rim = 0.9f, Clips = new[] { strike } },
                new BlenderArtEntry("Roller", "Roller") { ExtraParts = FaceClaws, Clips = new[] { strike } },
                new BlenderArtEntry("Cragback", "Cragback") { ExtraParts = FaceClaws, DeathFadeFrom = 8, DeathFadeTo = 40, Clips = new[] { strike } },
                new BlenderArtEntry("Prowler", "Prowler") { ExtraParts = FaceClaws, Brightness = 1.45f, Emission = 3.5f, Rim = 0.9f, Clips = new[] { strike } },
                new BlenderArtEntry("Skyray", "Skyray")
                {
                    ExtraParts = FaceClaws,
                    Emission = 3.2f,
                    BodyCentrePivot = true,
                    Clips = new[] { new BlenderClipSpec("Dive", ModelClipRole.Attack, 10) },
                },
                new BlenderArtEntry("Shalebat", "Shalebat") { ExtraParts = FaceClaws, BodyCentrePivot = true, Clips = bat },
                // Round 36: the wisp strikes floating or settled, whichever stance it is in.
                new BlenderArtEntry("Wisp", "Wisp")
                {
                    ExtraParts = FaceClaws,
                    Bleeds = false,
                    Clips = new[] { new BlenderClipSpec("Strike_Float", ModelClipRole.Attack, 10), new BlenderClipSpec("Strike_Ground", ModelClipRole.GroundedAttack, 10) },
                },
                new BlenderArtEntry("Stinger", "Stinger") { ExtraParts = FaceClaws, Clips = new[] { new BlenderClipSpec("Strike", ModelClipRole.Attack, 10) } },
                new BlenderArtEntry("Bulwark", "Bulwark") { ExtraParts = FaceClaws, Clips = new[] { new BlenderClipSpec("Block", ModelClipRole.Block, 7), strike } },
                // Round 31: the slug strike stretches the body itself (impact frame 10).
                new BlenderArtEntry("Oozer", "Oozer") { ExtraParts = FaceOnly, Clips = new[] { new BlenderClipSpec("Strike", ModelClipRole.Attack, 10) } },
                new BlenderArtEntry("Bat", "Bat") { ExtraParts = FaceClaws, BodyCentrePivot = true, Clips = bat },
                new BlenderArtEntry("Bat_Moss", "Bat", "Moss") { ExtraParts = FaceClaws, PartGlowTint = MossBatPartGlow, BodyCentrePivot = true, Clips = bat },
                new BlenderArtEntry("Bat_Dust", "Bat", "Dust") { ExtraParts = FaceClaws, PartGlowTint = DustBatPartGlow, BodyCentrePivot = true, Clips = bat },
                new BlenderArtEntry("Bat_Tide", "Bat", "Tide") { ExtraParts = FaceClaws, BodyCentrePivot = true, Clips = bat },
                new BlenderArtEntry("Bat_Frost", "Bat", "Frost") { ExtraParts = FaceClaws, BodyCentrePivot = true, Clips = bat },
                new BlenderArtEntry("Queen", "Queen")
                {
                    ExtraParts = FaceClaws,
                    // Round 2 grew her far past the old queen (2.8 high in model units): at the boss factor she covered a
                    // quarter of the screen, so she gets her own (imp1_d_1).
                    Scale = 0.33f,
                    MoveDistance = 0.9f,
                    Clips = new[] { new BlenderClipSpec("Scream", ModelClipRole.Power, 25, "Pulse") },
                },
                new BlenderArtEntry("BroodMother", "BroodMother")
                {
                    ExtraParts = FaceClaws,
                    Scale = BossFactor,
                    Clips = new[] { new BlenderClipSpec("Spawn", ModelClipRole.Power, 40, "Spawn", "Vent") },
                },
                new BlenderArtEntry("PackAlpha", "PackAlpha")
                {
                    ExtraParts = FaceClaws,
                    Scale = BossFactor,
                    Clips = new[] { new BlenderClipSpec("Howl", ModelClipRole.Power, 22, "Rally") },
                },
                new BlenderArtEntry("CoreWisp", "CoreWisp")
                {
                    ExtraParts = new[] { "Eyes", "Barbs", "Claws" },
                    Scale = BossFactor,
                    Bleeds = false,
                    Clips = new[] { new BlenderClipSpec("Pulse", ModelClipRole.Power, 14, "Pulse") },
                },
                new BlenderArtEntry("AcidEmpress", "AcidEmpress")
                {
                    ExtraParts = FaceClaws,
                    Scale = BossFactor,
                    Clips = new[]
                    {
                        new BlenderClipSpec("Strike", ModelClipRole.Attack, 10),
                        new BlenderClipSpec("Spray", ModelClipRole.Power, 26, "Volley", "Gland"),
                    },
                },
                new BlenderArtEntry("CrystalWarlord", "CrystalWarlord")
                {
                    ExtraParts = EyesTeethClaws,
                    Scale = BossFactor,
                    Clips = new[]
                    {
                        new BlenderClipSpec("Block", ModelClipRole.Block, 7),
                        new BlenderClipSpec("Sweep", ModelClipRole.Power, 20, "Pulse"),
                    },
                },
                new BlenderArtEntry("SlimeMonarch", "SlimeMonarch")
                {
                    ExtraParts = new[] { "Face" },
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
                    ExtraParts = FaceClaws,
                    Scale = BossFactor,
                    Clips = new[] { new BlenderClipSpec("Quake", ModelClipRole.Power, 16, "Pulse") },
                },
                // The bat boss (exported as SkySovereign) on the Storm Matriarch, a bat boss of scale 1.2 in the data:
                // 0.7 keeps the model at the size it was built for (0.84). Its tail lens throws her element storm.
                new BlenderArtEntry("BatBoss", "SkySovereign")
                {
                    ExtraParts = FaceClaws,
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
                    ExtraParts = FaceClaws,
                    Scale = 0.514f,
                    BodyCentrePivot = true,
                },

                // Round 15, P3 species (import stage 4b). The Shardback sheds its six prism clusters with its armour; the
                // Mireleech's latch bites on frame 13.
                // The amber sockets glowed through the ice at 2.5 (cre_close): lower, so the cold prisms lead.
                new BlenderArtEntry("Shardback", "Shardback")
                {
                    ExtraParts = FaceClaws,
                    Emission = 2f,
                    ShedBonesKey = "shard_bones_in_shed_order",
                    Clips = new[] { new BlenderClipSpec("Shatter", ModelClipRole.Shatter, 3), strike },
                },
                new BlenderArtEntry("Mireleech", "Mireleech") { ExtraParts = FaceOnly, Clips = new[] { new BlenderClipSpec("Latch", ModelClipRole.Attack, 13) } },

                // Art ready for species without game data yet ("art klaar, wacht op data"): an EnemyDefinition that points
                // at one of these wears it with no further code. Their special moments are recorded as clips for later views.
                new BlenderArtEntry("Knitback", "Knitback") { ExtraParts = FaceClaws, Clips = new[] { new BlenderClipSpec("Regrow", ModelClipRole.Power, 24, "Regrow", "Pod_1") } },
                // Burning trail and heat vents: it sheds embers, and its vents glow harder (README: emission 3.0).
                new BlenderArtEntry("Slagstrider", "Slagstrider") { ExtraParts = FaceClaws, Embers = true, Emission = 3f },
                new BlenderArtEntry("Siftjaw", "Siftjaw")
                {
                    ExtraParts = FaceClaws,
                    Clips = new[] { new BlenderClipSpec("Burrow", ModelClipRole.Burrow, 24), new BlenderClipSpec("Emerge", ModelClipRole.Emerge, 8) },
                },
                new BlenderArtEntry("Surgefin", "Surgefin") { ExtraParts = FaceClaws, Clips = new[] { new BlenderClipSpec("Emerge", ModelClipRole.Emerge, 10) } },
                new BlenderArtEntry("FireLarva", "FireLarva") { Embers = true },

                // Round 14 and 16 continent bosses: two materials (body and crystals, ice, bloom or seal), built at their
                // game size, so the art's factor is 1 on the boss's own scale.
                new BlenderArtEntry("Gravemaw", "Gravemaw")
                {
                    Scale = TitanFactor,
                    SecondMaterial = "Crystal",
                    // Round 38: the worm gullet with its rows of teeth is an object of its own (Gravemaw_Maw and _LOD1)
                    // with its own maps, so the throat does not wear the body's skin. Round 45 adds the sensor crown's
                    // eyes and the talons and spurs.
                    ExtraParts = new[] { "Maw", "Eyes", "Claws" },
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
                        // Round 33: its own gate crossing (weights and the per-frame gate burden from the round28 block).
                        new BlenderClipSpec("Gate_Mount", ModelClipRole.GateMount),
                        new BlenderClipSpec("Gate_Straddle_Loop", ModelClipRole.GateStraddle),
                        new BlenderClipSpec("Gate_Dismount", ModelClipRole.GateDismount),
                    },
                },
                new BlenderArtEntry("CinderMatriarch", "CinderMatriarch")
                {
                    ExtraParts = EyesTeethClaws,
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
                        // Round 33: she strides over a gate on her stilts and settles her thorax on the beam.
                        new BlenderClipSpec("Gate_Mount", ModelClipRole.GateMount),
                        new BlenderClipSpec("Gate_Straddle_Loop", ModelClipRole.GateStraddle),
                        new BlenderClipSpec("Gate_Dismount", ModelClipRole.GateDismount),
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
                    // Round 15: its pale stone and ice washed out white on the codex pedestal.
                    DisplayExposure = 0.6f,
                    SecondMaterial = "Ice",
                    // Round 30: the baked ice maps are cold now, so the round-5 flat tint is dropped (baked albedo and glow
                    // kept); the cold rim and a faint glow (emission x 2, README round 30) stay.
                    Ice = new IceLook { FlatColour = false, Tint = Color.white, Glow = Color.white, GlowStrength = 2f },
                    // Round 28: the ice armour is an object of its own with its own maps (FrostWarden_IceArmour_*).
                    ExtraParts = new[] { "IceArmour", "Eyes", "Teeth", "Claws" },
                    IceParts = new[] { "IceArmour" },
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
                    ExtraParts = EyesTeethClaws,
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
                    ExtraParts = EyesTeethClaws,
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
                    ExtraParts = EyesTeethClaws,
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
                    BlenderArtEntry corrupted = row.WithSkin(row.Id + CorruptedSuffix, CorruptedSkin);
                    corrupted.PartTint = CorruptedPartTint;
                    corrupted.PartGlowTint = CorruptedPartGlow;
                    rows.Add(corrupted);
                }
            }
        }
    }
}
