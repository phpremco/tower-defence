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

        // The signatures that run (all six since round 4c).
        private static readonly BossSignature[] Fightable =
        {
            BossSignature.Burrow, BossSignature.HuntTheCold, BossSignature.GateBreaker, BossSignature.Overgrowth, BossSignature.Splitting, BossSignature.Recognition,
        };

        // No Crystal Plains level has lava, ice or bog yet, so the bosses that need them are fought on a copy of a level whose
        // pools are turned into their liquid; the Shattered gets a copy of Fault Line (two roads, two flight paths) with a
        // sealed ruin gate as its boss entrance (arena only; the campaign never sees these copies).
        private static readonly (string Boss, string Source, string Id, string Name, TerrainKind[] From, TerrainKind To, bool Ruins)[] ArenaLevels =
        {
            ("CinderMatriarch", "Level_C1_06", "Arena_Lava", "Crysium Vein, molten", new[] { TerrainKind.Water }, TerrainKind.Lava, false),
            ("FrostWarden", "Level_C1_05", "Arena_Ice", "Quartz Flats, frozen", new[] { TerrainKind.Water }, TerrainKind.Ice, false),
            ("Rotbloom", "Level_C1_02", "Arena_Bog", "Glass Meadows, rotting", new[] { TerrainKind.Water }, TerrainKind.Bog, false),
            ("Shattered", "Level_C1_04", "Arena_Ruins", "Fault Line, the old ruins", new TerrainKind[0], TerrainKind.Ground, true),
        };

        // Rotbloom's roots spread over the waves before it: in the arena three short escort waves come first.
        private static readonly Dictionary<string, int> ArenaLeadWaves = new Dictionary<string, int> { { "Rotbloom", 3 } };

        private const string ArenaLevelFolder = DataRoot + "/Levels/Arena";

        // Where the sealed ruin gate stands: this many cells off the road, near this share of the main road.
        private const int RuinCellsOffRoad = 3;
        private const float RuinRoadShare = 0.12f;
        private const int RuinEdgeCells = 2;
        private const int RuinRoadMargin = 1;

        private sealed class BossSpec
        {
            public string Id;
            public Creature Creature;
            public BossPhase[] Phases;
            public BossSignature Signature;
            public BossArrival Arrival;
            public PoolKinds Pools;
            public bool ClimbsGates = true;
            public Color WakeGlow = new Color(1f, 0.35f, 0.6f, 0.8f);
            public string ArmourPart = string.Empty;

            // Half its body width in world units, legs included (the model's width at the titans' scale of 0.8).
            public float BodyHalfWidth = 1.1f;

            // Towers within this many world units of each other count as one cluster for a spore volley. Cells are 1 m and a
            // defence usually stands 2.7 to 3.2 m apart (a tower and its neighbours, diagonals included): at 2.6 nearly every
            // tower was a cluster of one and a volley hit one pad. At 3.5 the fullest cluster of a normal defence holds 3 to 5
            // towers, so a volley draws its 3 pods; the quarter cap (BossDefinition.MaxFieldShare) still limits small defences.
            public float ClusterRadius = 3.5f;

            // The victory panel waits for its death clip (the Sealed One's white Release) when true.
            public bool HoldResultForDeath;
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
                Arrival = BossArrival.Pool,
                Pools = PoolKinds.Lava,
                WakeGlow = new Color(1f, 0.45f, 0.1f, 0.9f),
                BodyHalfWidth = 1.8f,
                SignatureText = "She leaves the road to hunt your cold towers (every tower that slows), the nearest first, and smashes it into a wreck you can rebuild: one tower each time she sets out, never more than about a quarter of your towers. She walks between the pads round your towers and climbs the rock ridges, and a lava pool nearer her prey is her shortcut. Without cold towers she walks the road. Her trail of lava burns the towers it touches, and walkers less. Below half her health she lays eggs in the lava that hatch into fire larvae after ten seconds.",
                TelegraphText = "She rises out of the largest lava pool. When she sets out she rears up, a thin line of heat runs from her to the tower she hunts, and that tower starts to glow warm.",
                CounterText = "Use a cheap cold tower far from the road as bait so she walks a long way past your firepower, or build no cold at all and let her take the long road. Keep towers off the banks of the lava pools, and shoot her eggs before they hatch.",
                Voice = new VoiceSpec { Enemy = "CinderMatriarch", Move = "Bosses/Ant/ant_walk_loop_boss", Screech = "Bosses/Ant/ant_screech_boss", Growl = "Bosses/Ant/ant_screech_boss" },
            };
            yield return new BossSpec
            {
                Id = "FrostWarden",
                // No armour layer on its health: its ice armour is the mechanic (a share of each hit lands until it breaks).
                Creature = Titan("FrostWarden", "A guardian of ice that freezes your defence in place.", 4500f, 0f, 0.5f, new Color(0.55f, 0.75f, 0.9f), new Color(0.6f, 0.9f, 1f)),
                Phases = new[] { new BossPhase("Armoured", 1f), new BossPhase("Exposed", 0.4f) },
                Signature = BossSignature.GateBreaker,
                Arrival = BossArrival.Pool,
                Pools = PoolKinds.Ice | PoolKinds.Water,
                WakeGlow = new Color(0.55f, 0.85f, 1f, 0.8f),
                BodyHalfWidth = 1.25f,
                ArmourPart = "FrostWarden_IceArmour",
                SignatureText = "Every gate it reaches it breathes into an ice block: the gate keeps standing but holds its fire for eight seconds. It climbs onto the block, and the creatures behind it follow over, then it breaks down through it and the gate is a wreck you can rebuild. Each gate it breaks makes it angrier and faster. Its ice armour lets only half of every hit through; below 40% of its health the armour bursts off and it runs.",
                TelegraphText = "It breaks out of a frozen lake. Frost creeps over the ground in front of it, the gate it heads for gets a blue haze and the ice cracks, and icicles grow on its back as its rage rises.",
                CounterText = "Do not lean on gates: spread towers along the long road. A lava ball cracks its ice armour, and for five seconds every hit lands double. The hero's repair thaws a frozen gate, and the Warden slips off without breaking it.",
                Voice = new VoiceSpec { Enemy = "FrostWarden", Move = "Bosses/IceBeetle/ice_beetle_walk_loop_boss", Screech = "Bosses/IceBeetle/ice_beetle_screech_boss", Growl = "Bosses/IceBeetle/ice_beetle_screech_boss" },
            };
            yield return new BossSpec
            {
                Id = "Rotbloom",
                Creature = Titan("Rotbloom", "A walking flower of rot that makes the map itself hostile.", 4000f, 0f, 0.3f, new Color(0.35f, 0.45f, 0.15f), new Color(0.75f, 1f, 0.3f)),
                Phases = new[] { new BossPhase("Rooted", 1f), new BossPhase("Uprooted", 0.5f) },
                Signature = BossSignature.Overgrowth,
                Arrival = BossArrival.Pool,
                Pools = PoolKinds.Bog,
                WakeGlow = new Color(0.55f, 1f, 0.3f, 0.8f),
                BodyHalfWidth = 1.6f,
                SignatureText = "Every twelve seconds it fires spore pods at the fullest cluster of towers within its reach: three pads at most, and never more than about a quarter of your towers overgrown at once. An overgrown pad cannot be built on, and the tower on it fires at 70% while vines grow over it, until the growth is burned away. Rooted, it creeps along the road on growing roots; below half its health it tears itself out, walks faster and leaves rot behind it. It sinks into the bog and comes up at another bank.",
                TelegraphText = "Roots spread at the bank of a bog over the three waves before it comes, and it grows out of that bog. Before a volley the flower opens its petals with a sweet rising sound, and the target pads glow green two seconds before the pods land.",
                CounterText = "Splash damage (cannon, mortar) that lands on a pad burns it clean, and the hero's repair burns the vines off the tower it guards. The pods fly: air towers can shoot them down before they land. Do not stack all your towers in one cluster.",
                Voice = new VoiceSpec { Enemy = "Rotbloom", Move = "Bosses/Leech/leech_slime_loop_boss", Screech = "Bosses/Leech/leech_screech_boss", Growl = "Bosses/Leech/leech_screech_boss" },
            };
            yield return new BossSpec
            {
                Id = "Shattered",
                Creature = Titan("Shattered", "A crystal giant that splits apart when it takes damage.", 4800f, 0f, 0.5f, new Color(0.6f, 0.7f, 0.85f), new Color(0.8f, 0.95f, 1f)),
                Phases = new[] { new BossPhase("Whole", 1f), new BossPhase("Split", 0.66f), new BossPhase("Rejoined", 0.33f) },
                Signature = BossSignature.Splitting,
                Arrival = BossArrival.Ruins,
                Pools = PoolKinds.Water,
                WakeGlow = new Color(0.7f, 0.88f, 1f, 0.8f),
                BodyHalfWidth = 1.4f,
                SignatureText = "At two thirds of its health it bursts into three shards that each take a different route: one runs its own road, one flies the air route and one takes the second road, each with a third of what it had left. At a third the surviving shards pull back together where the one nearest your base stands. Each shard you killed takes 30% off the giant that forms.",
                TelegraphText = "It breaks out of a sealed gate in the old ruins. Near its split point cracks of light pulse on its body, and when it bursts each shard's route lights up briefly.",
                CounterText = "Cover every route: air defence and the second front matter. Kill one shard fast and the rejoined giant is weaker; let them run and it forms closer to your base.",
                Voice = new VoiceSpec { Enemy = "Shattered", Steps = "Bosses/DinoSoldier/dino_soldier_step_boss", StepSpacing = 1f, Screech = "Bosses/DinoSoldier/dino_soldier_death_boss" },
            };
            yield return new BossSpec
            {
                Id = "SealedOne",
                Creature = Titan("SealedOne", "The secret under the planet: its immune system, not an animal.", 9000f, 0f, 0.25f, new Color(0.85f, 0.85f, 0.95f), Color.white),
                Phases = new[] { new BossPhase("First seal", 1f), new BossPhase("Second seal", 0.75f), new BossPhase("Third seal", 0.5f), new BossPhase("Release", 0.25f) },
                Signature = BossSignature.Recognition,
                Arrival = BossArrival.Rift,
                Pools = PoolKinds.Water | PoolKinds.Quicksand,
                WakeGlow = new Color(0.7f, 0.4f, 1f, 0.9f),
                BodyHalfWidth = 2.4f,
                ClimbsGates = false,
                SignatureText = "It does not come to you: the ground at the centre of the map cracks open, a new road rises from there to your base, and it hauls itself along it and onto the base. It learns: the tower type that hurt it most in one phase does half damage in the next. Each phase drains one gate's gun (the gate still blocks) until the phase ends.",
                TelegraphText = "The ground rumbles and violet cracks open at the centre before it rises. At each phase change ORA names the dampened weapon, the boss bar shows its icon greyed out, and the sealed crystals at the rift turn that weapon's colour. A violet haze hangs over the drained gate.",
                CounterText = "Build a mix of three or four tower types; a single kind loses half its punch every phase. Do not lean on one gate's gun. Its last breath is a white release.",
                HoldResultForDeath = true,
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
            (EnemyDefinition egg, EnemyDefinition larva) = CreateBrood();
            var lavaBall = AssetDatabase.LoadAssetAtPath<Game.PathObjects.PathObjectDefinition>($"{DataRoot}/PathObjects/LavaBall.asset");
            if (lavaBall != null)
            {
                MarkBurning(lavaBall);
            }

            EnemyDefinition pod = CreateSporePod();
            ShardSpec[] shards = CreateShards();
            var made = new List<EnemyDefinition>();
            foreach (BossSpec spec in ContinentBosses())
            {
                BossDefinition boss = CreateBossData(spec, skitter, egg, larva, pod, shards);

                EnemyDefinition enemy = CreateEnemy(spec.Creature);
                var so = new SerializedObject(enemy);
                so.FindProperty("_boss").objectReferenceValue = boss;
                so.FindProperty("_art").objectReferenceValue = Art(spec.Id);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(enemy);
                CreateVoice(spec.Voice);
                made.Add(enemy);
            }

            AssetDatabase.SaveAssets();
            return made;
        }

        // The Cinder Matriarch's brood (Docs/BOSSES.md): the egg she lays in the lava (it lies still, shootable, and hatches
        // unless shot) and the fire larva that crawls out of it, both wearing their round-28 Blender art.
        private static (EnemyDefinition Egg, EnemyDefinition Larva) CreateBrood()
        {
            EnemyDefinition larva = CreateEnemy(new Creature
            {
                Id = "FireLarva", Description = "A fire bug hatched from the Cinder Matriarch's eggs: small, quick and hot.", Layer = TargetLayers.Ground,
                Health = 24f, Speed = 1.3f, BaseDamage = 1, Element = Element.Fire, Attack = AttackStyle.Bite, BlockadeDamage = 4f, AttackRange = 2.5f, AttackInterval = 1f,
                Color = new Color(1f, 0.4f, 0.1f), Eyes = new Color(1f, 0.85f, 0.4f), Accent = new Color(1f, 0.55f, 0.15f), Blood = new Color(1f, 0.6f, 0.2f),
                Scale = 0.45f, Shape = EnemyShape.Skitter, Death = DeathStyle.Shatter,
            });
            EnemyDefinition egg = CreateEnemy(new Creature
            {
                Id = "CinderEgg", Description = "An egg of the Cinder Matriarch in the lava. Shoot it before it hatches.", Layer = TargetLayers.Ground,
                Health = 70f, Speed = 0.1f, BaseDamage = 0, Element = Element.Fire, Attack = AttackStyle.Bite, AttackRange = 0.5f, AttackInterval = 1f,
                Color = new Color(0.5f, 0.15f, 0.05f), Eyes = new Color(1f, 0.55f, 0.15f), Accent = new Color(1f, 0.5f, 0.1f), Blood = new Color(1f, 0.55f, 0.15f),
                Scale = 1f, Shape = EnemyShape.None, Death = DeathStyle.Shatter,
            });
            CreateVoice(new VoiceSpec { Enemy = "FireLarva", Move = "Monsters/Ant/ant_walk_loop", Screech = "Monsters/Ant/ant_screech", Pitch = 1.35f });
            // The egg never walks: its "move" is the lava bubbling round it.
            CreateVoice(new VoiceSpec { Enemy = "CinderEgg", Move = EnvironmentFolder + "lava_bubbling_loop" });
            foreach (EnemyDefinition brood in new[] { larva, egg })
            {
                var so = new SerializedObject(brood);
                so.FindProperty("_art").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{BlenderArtBuilder.ArtDataFolder}/{brood.name}.asset");
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(brood);
            }

            return (egg, larva);
        }

        private static ModelArtDefinition Art(string id) => AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{BlenderArtBuilder.ArtDataFolder}/{id}.asset");

        // The length of the art's death clip as the corpse plays it, or zero without one.
        private static float DeathSeconds(string id)
        {
            ModelArtDefinition art = Art(id);
            int death = art != null ? art.IndexOf(ModelClipRole.Death) : -1;
            return death >= 0 ? art.Clips[death].Length : 0f;
        }

        // Rotbloom's spore pod (round 28): a small flyer, so the air towers can shoot it down before it lands on a pad.
        private static EnemyDefinition CreateSporePod()
        {
            EnemyDefinition pod = CreateEnemy(new Creature
            {
                Id = "RotSporePod", Description = "A spore pod Rotbloom fires at your pads. Shoot it down in the air and the pad stays clean.",
                Layer = TargetLayers.Air, Flies = true, Health = 20f, Speed = 1f, BaseDamage = 0, Attack = AttackStyle.Bite, AttackRange = 0.5f, AttackInterval = 1f,
                Color = new Color(0.45f, 0.6f, 0.2f), Eyes = new Color(0.75f, 1f, 0.35f), Accent = new Color(0.6f, 1f, 0.3f), Blood = new Color(0.6f, 1f, 0.35f),
                Scale = 1.5f, Shape = EnemyShape.None, Death = DeathStyle.Shatter,
            });
            CreateVoice(new VoiceSpec { Enemy = "RotSporePod", Move = "Monsters/Flying/wings_flapping_loop", Pitch = 1.6f });
            Wear(pod, "RotSporePod");
            return pod;
        }

        // The Shattered's three shards (round 28): the crawler keeps its road, the runner takes the second road, the flyer the air.
        // Their health is set at the split (a third of what the giant had left); the data value only prices them.
        private static ShardSpec[] CreateShards()
        {
            var crystal = new Color(0.6f, 0.75f, 0.95f);
            var glow = new Color(0.8f, 0.95f, 1f);
            EnemyDefinition crawler = CreateEnemy(new Creature
            {
                Id = "ShardCrawler", Description = "A shard of the Shattered on six legs: it keeps to the giant's road.", Layer = TargetLayers.Ground,
                Health = 300f, Speed = 0.6f, BaseDamage = 8, Attack = AttackStyle.Slam, BlockadeDamage = 40f, AttackRange = 2f, AttackInterval = 1.2f,
                Color = crystal, Eyes = glow, Accent = glow, Blood = glow, Scale = 0.8f, Shape = EnemyShape.None, Death = DeathStyle.Shatter,
            });
            EnemyDefinition runner = CreateEnemy(new Creature
            {
                Id = "ShardRunner", Description = "A shard of the Shattered on two long legs: it runs the second road.", Layer = TargetLayers.Ground,
                Health = 300f, Speed = 0.9f, BaseDamage = 8, Attack = AttackStyle.Slam, BlockadeDamage = 40f, AttackRange = 2f, AttackInterval = 1.2f,
                Color = crystal, Eyes = glow, Accent = glow, Blood = glow, Scale = 0.8f, Shape = EnemyShape.None, Death = DeathStyle.Shatter,
            });
            EnemyDefinition flyer = CreateEnemy(new Creature
            {
                Id = "ShardFlyer", Description = "A shard of the Shattered on crystal wings: it takes the air route.", Layer = TargetLayers.Air, Flies = true,
                Health = 300f, Speed = 0.8f, BaseDamage = 8, Attack = AttackStyle.Bite, AttackRange = 1f, AttackInterval = 1.2f,
                Color = crystal, Eyes = glow, Accent = glow, Blood = glow, Scale = 0.8f, Shape = EnemyShape.None, Death = DeathStyle.Shatter,
            });
            CreateVoice(new VoiceSpec { Enemy = "ShardCrawler", Move = "Monsters/Ant/ant_walk_loop", Screech = "Monsters/DinoSoldier/dino_soldier_death", Pitch = 0.8f });
            CreateVoice(new VoiceSpec { Enemy = "ShardRunner", Steps = "Monsters/DinoSoldier/dino_soldier_step", StepSpacing = 1.2f, Screech = "Monsters/DinoSoldier/dino_soldier_death" });
            CreateVoice(new VoiceSpec { Enemy = "ShardFlyer", Move = "Monsters/Flying/large_wings_flapping_loop", Screech = "Monsters/DinoSoldier/dino_soldier_death", Pitch = 1.2f });
            foreach (EnemyDefinition shard in new[] { crawler, runner, flyer })
            {
                Wear(shard, shard.name);
            }

            // Offsets and spires from the Shattered report's round-28 Split and Rejoin blocks.
            return new[]
            {
                new ShardSpec(crawler, ShardRoute.OwnRoad, new Vector3(-1.6f, 0f, -1.8f), "Core_Land"),
                new ShardSpec(runner, ShardRoute.OtherRoad, new Vector3(1.6f, 0f, 2.2f), "Core_Road"),
                new ShardSpec(flyer, ShardRoute.Air, new Vector3(0f, 3.2f, -0.4f), "Core_Air"),
            };
        }

        private static void Wear(EnemyDefinition enemy, string art)
        {
            var so = new SerializedObject(enemy);
            so.FindProperty("_art").objectReferenceValue = Art(art);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
        }

        private static void WriteOvergrowth(SerializedObject so, EnemyDefinition pod)
        {
            so.FindProperty("_overgrowth._pod").objectReferenceValue = pod;
            so.FindProperty("_overgrowth._vines").objectReferenceValue = pod != null ? Art("RotOvergrowth") : null;
            so.FindProperty("_overgrowth._roots").objectReferenceValue = pod != null ? Art("RotbloomRoots") : null;
        }

        private static void WriteShards(SerializedObject so, ShardSpec[] shards)
        {
            SerializedProperty array = so.FindProperty("_split._shards");
            array.arraySize = shards.Length;
            for (int i = 0; i < shards.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_enemy").objectReferenceValue = shards[i].Enemy;
                element.FindPropertyRelative("_route").enumValueIndex = (int)shards[i].Route;
                element.FindPropertyRelative("_offset").vector3Value = shards[i].Offset;
                element.FindPropertyRelative("_spire").stringValue = shards[i].Spire;
            }
        }

        private static BossDefinition CreateBossData(BossSpec spec, EnemyDefinition skitter, EnemyDefinition egg, EnemyDefinition larva, EnemyDefinition pod, ShardSpec[] shards)
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
            so.FindProperty("_dive._wakeGlow").colorValue = spec.WakeGlow;
            so.FindProperty("_bodyHalfWidth").floatValue = spec.BodyHalfWidth;
            bool hunts = spec.Signature == BossSignature.HuntTheCold;
            so.FindProperty("_hunt._egg").objectReferenceValue = hunts ? egg : null;
            so.FindProperty("_hunt._hatchling").objectReferenceValue = hunts ? larva : null;
            so.FindProperty("_hunt._clutches").intValue = hunts ? 2 : 0;
            so.FindProperty("_gateBreaker._armourPart").stringValue = spec.ArmourPart;
            so.FindProperty("_signatureText").stringValue = spec.SignatureText;
            so.FindProperty("_telegraphText").stringValue = spec.TelegraphText;
            so.FindProperty("_counterText").stringValue = spec.CounterText;
            so.FindProperty("_overgrowth._clusterRadius").floatValue = spec.ClusterRadius;
            so.FindProperty("_resultHoldSeconds").floatValue = spec.HoldResultForDeath ? DeathSeconds(spec.Id) : 0f;
            // Written with the rest, so the boss validates whole.
            WriteOvergrowth(so, spec.Signature == BossSignature.Overgrowth ? pod : null);
            WriteShards(so, spec.Signature == BossSignature.Splitting ? shards : new ShardSpec[0]);
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

        // The level a boss is fought in: a copy with its pools turned into the liquid the boss needs, or none (the arena's own).
        private static LevelDefinition ArenaLevelFor(string boss)
        {
            foreach (var arena in ArenaLevels)
            {
                if (arena.Boss == boss)
                {
                    return CreateArenaLevel(arena.Source, arena.Id, arena.Name, arena.From, arena.To, arena.Ruins);
                }
            }

            return null;
        }

        // A copy of a campaign level (same asset GUID on every run, so the arena keeps pointing at it) with some pool
        // kinds turned into another and, for a boss out of the ruins, a sealed ruin gate as its boss entrance; everything
        // else, art included, stays the source's.
        private static LevelDefinition CreateArenaLevel(string source, string id, string displayName, TerrainKind[] from, TerrainKind to, bool ruins)
        {
            var original = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{DataRoot}/Levels/{source}.asset");
            if (original == null)
            {
                return null;
            }

            EnsureFolder(ArenaLevelFolder);
            string path = $"{ArenaLevelFolder}/{id}.asset";
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (level == null)
            {
                level = Object.Instantiate(original);
                AssetDatabase.CreateAsset(level, path);
            }
            else
            {
                EditorUtility.CopySerialized(original, level);
            }

            level.name = id;
            var so = new SerializedObject(level);
            so.FindProperty("_displayName").stringValue = displayName;
            SerializedProperty terrain = so.FindProperty("_terrain");
            var turned = new HashSet<Vector2Int>();
            for (int i = 0; i < terrain.arraySize; i++)
            {
                SerializedProperty patch = terrain.GetArrayElementAtIndex(i);
                SerializedProperty kind = patch.FindPropertyRelative("Kind");
                if (System.Array.IndexOf(from, (TerrainKind)kind.intValue) >= 0)
                {
                    kind.intValue = (int)to;
                    turned.Add(patch.FindPropertyRelative("Cell").vector2IntValue);
                }
            }

            // A nest of the old liquid's element no longer has its home: the arena's own wave uses no nests, so it goes.
            SerializedProperty nests = so.FindProperty("_nests");
            for (int i = nests.arraySize - 1; i >= 0; i--)
            {
                if (turned.Contains(nests.GetArrayElementAtIndex(i).FindPropertyRelative("Cell").vector2IntValue))
                {
                    nests.DeleteArrayElementAtIndex(i);
                }
            }

            if (ruins)
            {
                SerializedProperty entrance = so.FindProperty("_bossEntrance");
                entrance.FindPropertyRelative("Kind").intValue = (int)EntranceKind.Ground;
                entrance.FindPropertyRelative("Cell").vector2IntValue = RuinGateCell(original);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(level);
            return level;
        }

        // The sealed ruin gate: open ground a few cells off the main road near its start, clear of the road, pools and the
        // field's edge, so the giant breaks out in view and has the whole road ahead of it.
        private static Vector2Int RuinGateCell(LevelDefinition level)
        {
            Vector2Int[] road = level.MainRoad;
            Vector2Int near = road[Mathf.Clamp(Mathf.RoundToInt(road.Length * RuinRoadShare), 0, road.Length - 1)];
            var roadCells = new HashSet<Vector2Int>(level.Road.Cells);
            var blocked = new HashSet<Vector2Int>(level.BlockedCells);
            Vector2Int best = near;
            float bestScore = float.PositiveInfinity;
            for (int x = RuinEdgeCells; x < level.GridSize.x - RuinEdgeCells; x++)
            {
                for (int y = RuinEdgeCells; y < level.GridSize.y - RuinEdgeCells; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (roadCells.Contains(cell) || blocked.Contains(cell) || level.Terrain.IsLiquid(cell) || NearAny(roadCells, cell, RuinRoadMargin))
                    {
                        continue;
                    }

                    float score = Mathf.Abs(Vector2Int.Distance(cell, near) - RuinCellsOffRoad);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = cell;
                    }
                }
            }

            return best;
        }

        private static bool NearAny(HashSet<Vector2Int> cells, Vector2Int cell, int reach)
        {
            for (int x = -reach; x <= reach; x++)
            {
                for (int y = -reach; y <= reach; y++)
                {
                    if (cells.Contains(cell + new Vector2Int(x, y)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // The developer boss arena: Shard Ridge on Medium, a few Skitters, and every continent boss that can be fought yet.
        internal static void CreateBossArena(List<EnemyDefinition> bosses)
        {
            var arena = LoadOrCreate<BossArenaDefinition>(ArenaPath);
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>($"{DataRoot}/Levels/CampaignCatalog.asset");
            var so = new SerializedObject(arena);
            so.FindProperty("_level").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{DataRoot}/Levels/{ArenaLevel}.asset");
            so.FindProperty("_difficulty").objectReferenceValue = catalog != null && catalog.Difficulties.Count > ArenaDifficulty ? catalog.Difficulties[ArenaDifficulty] : null;
            var playable = new List<BossArenaEntry>();
            foreach (EnemyDefinition boss in bosses)
            {
                // A boss is fightable once its signature runs; the others are Codex data until their round.
                if (boss.Boss != null && System.Array.IndexOf(Fightable, boss.Boss.Signature) >= 0)
                {
                    ArenaLeadWaves.TryGetValue(boss.name, out int lead);
                    playable.Add(new BossArenaEntry(boss, ArenaLevelFor(boss.name), lead));
                }
            }

            SerializedProperty entries = so.FindProperty("_entries");
            entries.arraySize = playable.Count;
            for (int i = 0; i < playable.Count; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_boss").objectReferenceValue = playable[i].Boss;
                entry.FindPropertyRelative("_level").objectReferenceValue = playable[i].Level;
                entry.FindPropertyRelative("_leadWaves").intValue = playable[i].LeadWaves;
            }

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
