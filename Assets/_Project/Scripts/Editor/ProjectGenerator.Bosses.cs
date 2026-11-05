using System.Collections.Generic;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The continent bosses (Docs/BOSSES.md): their boss data (phases, signature, climbing, diving, Codex text), their
    /// creature data with art and voice, their Codex rows, and the developer boss arena. Only Gravemaw is placed (in the
    /// arena, until the Crystal Plains finale exists); the other five are written so the Codex can describe them once
    /// they are met.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string BossFolder = DataRoot + "/Enemies/Bosses";
        private const string ArenaPath = DataRoot + "/Levels/BossArena.asset";

        // The arena level: Shard Ridge has quicksand for the burrow shortcut, solid rock formations to climb and two gates.
        private const string ArenaLevel = "Level_C1_03";
        private const int ArenaDifficulty = 1;

        // The Sealed One's imported model (round 16) has no walk yet; it wears none until its round-28 clips are in.
        private static readonly string[] WithoutArtYet = { "SealedOne" };

        private sealed class BossSpec
        {
            public string Id;
            public Creature Creature;
            public BossPhase[] Phases;
            public BossSignature Signature;
            public BossArrival Arrival;
            public PoolKinds Pools;
            public bool ClimbsGates = true;
            public string SignatureText;
            public string TelegraphText;
            public string CounterText;
            public VoiceSpec Voice;
        }

        private static IEnumerable<BossSpec> ContinentBosses()
        {
            yield return new BossSpec
            {
                Id = "Gravemaw",
                Creature = new Creature
                {
                    Id = "Gravemaw", Description = "The planet's digging maw: it answers the colony's drill with a drill of its own.",
                    Layer = TargetLayers.Ground, Health = 1050f, Speed = 0.55f, BaseDamage = 25, Boss = true,
                    Attack = AttackStyle.Bite, BlockadeDamage = 110f, AttackRange = 2.5f, AttackInterval = 1.4f,
                    Color = new Color(0.55f, 0.2f, 0.4f), Eyes = new Color(1f, 0.45f, 0.75f), Accent = new Color(1f, 0.4f, 0.8f), Blood = new Color(1f, 0.45f, 0.7f),
                    Scale = 0.8f, Shape = EnemyShape.None, Death = DeathStyle.Collapse,
                },
                Phases = new[] { new BossPhase("Surface", 1f), new BossPhase("Burrow", 0.75f), new BossPhase("Deep burrow", 0.5f), new BossPhase("Enraged", 0.25f) },
                Signature = BossSignature.Burrow,
                Arrival = BossArrival.ColonyRig,
                Pools = PoolKinds.Quicksand | PoolKinds.Water | PoolKinds.Bog,
                SignatureText = "At three quarters, half and a quarter of its health it dives under the ground. A crack line runs round your towers to the tower that hurt it most; it breaks out beside it and throws that tower. The tower lands tilted, loses 40% of its health and stays silent for a few seconds, but it is not destroyed. It takes quicksand as a shortcut, climbs over rocks and onto gates, and can hide in a pool for a few seconds.",
                TelegraphText = "The crack glows warm orange and runs ahead of it, the ground rumbles louder as it comes up, and the tower it is after gets a pulsing red ring three seconds before it breaks out.",
                CounterText = "Spread your damage over several towers, so no single tower draws it. Sell or move the ringed tower in time, or put a mine on the crack line: a mine that goes off while it burrows leaves it stunned on the surface. Below a quarter it moves faster and its cracks send up Skitters.",
                Voice = new VoiceSpec { Enemy = "Gravemaw", Move = "Bosses/Scorpion/scorpion_walk_loop_boss", Screech = "Bosses/Scorpion/scorpion_screech_boss", Growl = "Bosses/Scorpion/scorpion_screech_boss", Pulse = EnvironmentFolder + "earthquake" },
            };
            yield return new BossSpec
            {
                Id = "CinderMatriarch",
                Creature = Titan("CinderMatriarch", "The queen of the lava, and she hates the cold.", 5000f, 0f, 0.4f, new Color(0.8f, 0.25f, 0.08f), new Color(1f, 0.6f, 0.2f)),
                Phases = new[] { new BossPhase("Hunting", 1f), new BossPhase("Brood", 0.5f) },
                Signature = BossSignature.HuntTheCold,
                Pools = PoolKinds.Lava,
                SignatureText = "She leaves the road to hunt your cold towers across the map and smashes each into a wreck you can rebuild. Without cold towers she walks the road. Her trail of lava burns towers it touches; below half her health she lays eggs in the lava.",
                TelegraphText = "A thin line of heat runs from her to her first target, and that tower starts to glow warm.",
                CounterText = "Use a cheap cold tower far from the road as bait so she walks past your firepower, or build no cold at all and let her take the long road. Shoot her eggs before they hatch.",
                Voice = new VoiceSpec { Enemy = "CinderMatriarch", Move = "Bosses/Ant/ant_walk_loop_boss", Screech = "Bosses/Ant/ant_screech_boss", Growl = "Bosses/Ant/ant_screech_boss" },
            };
            yield return new BossSpec
            {
                Id = "FrostWarden",
                Creature = Titan("FrostWarden", "A guardian of ice that freezes your defence in place.", 4500f, 1500f, 0.5f, new Color(0.55f, 0.75f, 0.9f), new Color(0.6f, 0.9f, 1f)),
                Phases = new[] { new BossPhase("Armoured", 1f), new BossPhase("Exposed", 0.4f) },
                Signature = BossSignature.GateBreaker,
                Pools = PoolKinds.Ice,
                SignatureText = "Every gate it reaches it freezes solid for a few seconds, then walks straight through and shatters it into a wreck. Each broken gate makes it angrier and faster.",
                TelegraphText = "Frost creeps over the ground in front of it, a gate it targets gets a blue haze and cracks, and icicles grow on its back as its rage rises.",
                CounterText = "Do not lean on gates: spread towers along the long road. Fire breaks its ice armour for double damage, and the hero can thaw a frozen gate with Repair.",
                Voice = new VoiceSpec { Enemy = "FrostWarden", Move = "Bosses/IceBeetle/ice_beetle_walk_loop_boss", Screech = "Bosses/IceBeetle/ice_beetle_screech_boss", Growl = "Bosses/IceBeetle/ice_beetle_screech_boss" },
            };
            yield return new BossSpec
            {
                Id = "Rotbloom",
                Creature = Titan("Rotbloom", "A walking flower of rot that makes the map itself hostile.", 4000f, 0f, 0.3f, new Color(0.35f, 0.45f, 0.15f), new Color(0.75f, 1f, 0.3f)),
                Phases = new[] { new BossPhase("Rooted", 1f), new BossPhase("Uprooted", 0.5f) },
                Signature = BossSignature.Overgrowth,
                Pools = PoolKinds.Bog,
                SignatureText = "Every few seconds it spores a small group of build pads: you cannot build there and a tower on one fires slower until the growth is burned away.",
                TelegraphText = "The flower opens its petals with a sweet rising sound, and the target pads get a green haze two seconds before the spores land.",
                CounterText = "Splash damage burns the pads clean, the hero can clear them, and shooting down the spore pods in the air stops the growth. Do not stack all your towers in one cluster.",
                Voice = new VoiceSpec { Enemy = "Rotbloom", Move = "Bosses/Leech/leech_slime_loop_boss", Screech = "Bosses/Leech/leech_screech_boss", Growl = "Bosses/Leech/leech_screech_boss" },
            };
            yield return new BossSpec
            {
                Id = "Shattered",
                Creature = Titan("Shattered", "A crystal giant that splits apart when it takes damage.", 4800f, 0f, 0.5f, new Color(0.6f, 0.7f, 0.85f), new Color(0.8f, 0.95f, 1f)),
                Phases = new[] { new BossPhase("Whole", 1f), new BossPhase("Split", 0.66f), new BossPhase("Rejoined", 0.33f) },
                Signature = BossSignature.Splitting,
                SignatureText = "At two thirds of its health it splits into three shards that each take a different route: the land, the air and the second road. At a third the survivors pull back together where they stand.",
                TelegraphText = "Cracks of light on the giant show where it will split, and each shard's route lights up briefly.",
                CounterText = "Cover every route, the air and the second front too. Each shard you kill takes a share off the giant that rejoins.",
                Voice = new VoiceSpec { Enemy = "Shattered", Steps = "Bosses/DinoSoldier/dino_soldier_step_boss", StepSpacing = 1f, Screech = "Bosses/DinoSoldier/dino_soldier_death_boss" },
            };
            yield return new BossSpec
            {
                Id = "SealedOne",
                Creature = Titan("SealedOne", "The secret under the planet: its immune system, not an animal.", 9000f, 0f, 0.25f, new Color(0.85f, 0.85f, 0.95f), Color.white),
                Phases = new[] { new BossPhase("First seal", 1f), new BossPhase("Second seal", 0.75f), new BossPhase("Third seal", 0.5f), new BossPhase("Release", 0.25f) },
                Signature = BossSignature.Recognition,
                ClimbsGates = false,
                SignatureText = "It learns. The tower type that hurt it most in one phase does half damage in the next, and each phase drains one gate's gun until the phase ends.",
                TelegraphText = "ORA names the blocked weapon, the boss bar shows its icon dimmed, and the sealed crystals turn that weapon's colour.",
                CounterText = "Build a mix of three or four tower types; a single kind loses its punch every phase.",
                Voice = new VoiceSpec { Enemy = "SealedOne", Move = "Bosses/EnergyBall/energy_ball_static_loop_boss", Screech = "Bosses/EnergyBall/energy_ball_death_boss" },
            };
        }

        private static Creature Titan(string id, string description, float health, float armor, float speed, Color body, Color glow) => new Creature
        {
            Id = id, Description = description, Layer = TargetLayers.Ground, Health = health, Armor = armor, ArmorReduction = 0.6f, Speed = speed, BaseDamage = 25, Boss = true,
            Attack = AttackStyle.Slam, BlockadeDamage = 110f, AttackRange = 2.5f, AttackInterval = 1.4f,
            Color = body, Eyes = glow, Accent = glow, Blood = glow, Scale = 0.8f, Shape = EnemyShape.None, Death = DeathStyle.Collapse,
        };

        /// <summary>The generator step: every continent boss, its Codex row, and the developer boss arena on the campaign.</summary>
        private static void GenerateContinentBosses()
        {
            List<EnemyDefinition> bosses = ApplyContinentBosses();
            AddToCodex(bosses);
            CreateBossArena(bosses);
        }

        /// <summary>Writes the six continent bosses (boss data, creature data, art, voice) and returns their creatures.</summary>
        internal static List<EnemyDefinition> ApplyContinentBosses()
        {
            EnsureFolder(BossFolder);
            var skitter = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{DataRoot}/Enemies/Skitter.asset");
            var made = new List<EnemyDefinition>();
            foreach (BossSpec spec in ContinentBosses())
            {
                BossDefinition boss = CreateBossData(spec, skitter);
                EnemyDefinition enemy = CreateEnemy(spec.Creature);
                var so = new SerializedObject(enemy);
                so.FindProperty("_boss").objectReferenceValue = boss;
                so.FindProperty("_art").objectReferenceValue = System.Array.IndexOf(WithoutArtYet, spec.Id) >= 0 ? null : AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{BlenderArtBuilder.ArtDataFolder}/{spec.Id}.asset");
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(enemy);
                CreateVoice(spec.Voice);
                made.Add(enemy);
            }

            AssetDatabase.SaveAssets();
            return made;
        }

        private static BossDefinition CreateBossData(BossSpec spec, EnemyDefinition skitter)
        {
            var boss = LoadOrCreate<BossDefinition>($"{BossFolder}/{spec.Id}Boss.asset");
            var so = new SerializedObject(boss);
            SerializedProperty phases = so.FindProperty("_phases");
            phases.arraySize = spec.Phases.Length;
            for (int i = 0; i < spec.Phases.Length; i++)
            {
                SerializedProperty phase = phases.GetArrayElementAtIndex(i);
                phase.FindPropertyRelative("Name").stringValue = spec.Phases[i].Name;
                phase.FindPropertyRelative("StartsAt").floatValue = spec.Phases[i].StartsAt;
            }

            so.FindProperty("_arrival").enumValueIndex = (int)spec.Arrival;
            so.FindProperty("_signature").enumValueIndex = (int)spec.Signature;
            so.FindProperty("_burrow._crackMinion").objectReferenceValue = spec.Signature == BossSignature.Burrow ? skitter : null;
            so.FindProperty("_burrow._minionsPerCrack").intValue = spec.Signature == BossSignature.Burrow ? 4 : 0;
            so.FindProperty("_climb._climbsGates").boolValue = spec.ClimbsGates;
            so.FindProperty("_dive._pools").intValue = (int)spec.Pools;
            so.FindProperty("_signatureText").stringValue = spec.SignatureText;
            so.FindProperty("_telegraphText").stringValue = spec.TelegraphText;
            so.FindProperty("_counterText").stringValue = spec.CounterText;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(boss);
            return boss;
        }

        // The Codex lists the continent bosses after the rest; it shows each only once the player has met it.
        private static void AddToCodex(List<EnemyDefinition> bosses)
        {
            var codex = AssetDatabase.LoadAssetAtPath<CodexCatalog>($"{DataRoot}/Levels/CodexCatalog.asset");
            if (codex == null)
            {
                return;
            }

            var so = new SerializedObject(codex);
            SerializedProperty enemies = so.FindProperty("_enemies");
            var listed = new List<Object>();
            for (int i = 0; i < enemies.arraySize; i++)
            {
                listed.Add(enemies.GetArrayElementAtIndex(i).objectReferenceValue);
            }

            foreach (EnemyDefinition boss in bosses)
            {
                if (!listed.Contains(boss))
                {
                    listed.Add(boss);
                }
            }

            SetReferences(enemies, listed.ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(codex);
        }

        // The developer boss arena: Shard Ridge on Medium, a few Skitters, and every continent boss that can be fought yet.
        internal static void CreateBossArena(List<EnemyDefinition> bosses)
        {
            var arena = LoadOrCreate<BossArenaDefinition>(ArenaPath);
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>($"{DataRoot}/Levels/CampaignCatalog.asset");
            var so = new SerializedObject(arena);
            so.FindProperty("_level").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{DataRoot}/Levels/{ArenaLevel}.asset");
            so.FindProperty("_difficulty").objectReferenceValue = catalog != null && catalog.Difficulties.Count > ArenaDifficulty ? catalog.Difficulties[ArenaDifficulty] : null;
            var playable = new List<Object>();
            foreach (EnemyDefinition boss in bosses)
            {
                // A boss is fightable once its signature runs; the others are Codex data until their round.
                if (boss.Boss != null && boss.Boss.Signature == BossSignature.Burrow)
                {
                    playable.Add(boss);
                }
            }

            SetReferences(so.FindProperty("_bosses"), playable.ToArray());
            so.FindProperty("_escort").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{DataRoot}/Enemies/Skitter.asset");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(arena);
            if (catalog != null)
            {
                var catalogSo = new SerializedObject(catalog);
                catalogSo.FindProperty("_bossArena").objectReferenceValue = arena;
                catalogSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalog);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
