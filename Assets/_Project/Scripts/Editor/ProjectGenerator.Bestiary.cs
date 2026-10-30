using Game.Core;
using Game.Enemies;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Generator part for the creatures: every species with its own skill and colour, the boss of each species
    /// with its powers, and the nests the element creatures rise from. Stats are starting points for balancing.
    /// </summary>
    public static partial class ProjectGenerator
    {
        /// <summary>Every enemy kind the levels can draw on.</summary>
        private sealed class Bestiary
        {
            public EnemyDefinition Skitter;
            public EnemyDefinition Stinger;
            public EnemyDefinition Emberbat;
            public EnemyDefinition Tidebat;
            public EnemyDefinition Mossbat;
            public EnemyDefinition Dustbat;
            public EnemyDefinition Bulwark;
            public EnemyDefinition Prowler;
            public EnemyDefinition Wisp;
            public EnemyDefinition Skyray;
            public EnemyDefinition Oozer;
            public EnemyDefinition Roller;
            public EnemyDefinition BroodMother;
            public EnemyDefinition AcidEmpress;
            public EnemyDefinition StormMatriarch;
            public EnemyDefinition CrystalWarlord;
            public EnemyDefinition PackAlpha;
            public EnemyDefinition CoreWisp;
            public EnemyDefinition SkySovereign;
            public EnemyDefinition SlimeMonarch;
            public EnemyDefinition RollerKing;
            public EnemyDefinition Queen;
        }

        /// <summary>The stats of one species or boss, so the creation reads as a table.</summary>
        private struct Creature
        {
            public string Id;
            public TargetLayers Layer;
            public bool Flies;
            public float Health;
            public float Armor;
            public float ArmorReduction;
            public float Speed;
            public int Reward;
            public int BaseDamage;
            public Element Element;
            public AttackStyle Attack;
            public float AttackDamage;
            public float BlockadeDamage;
            public float AttackRange;
            public float AttackInterval;
            public int GateStrikes;
            public float WalkSeconds;
            public float RollSeconds;
            public float RollSpeed;
            public float TrailSeconds;
            public float TrailHaste;
            public float TrailDamage;
            public float RageSeconds;
            public float RageSpeed;
            public float RageRadius;
            public bool Boss;
            public Color Color;
            public Color Eyes;
            public Color Accent;
            public Color Blood;
            public float Scale;
            public EnemyShape Shape;
            public DeathStyle Death;
            public BossAbilityDefinition[] Abilities;
        }

        private static readonly Color VenomGreen = new Color(0.62f, 1f, 0.2f);
        private static readonly Color GooGreen = new Color(0.35f, 1f, 0.25f);
        private static readonly Color RedBlood = new Color(0.9f, 0.2f, 0.2f);
        private static readonly Color WispLight = new Color(0.8f, 0.9f, 1f);
        private static readonly Color RayBlue = new Color(0.3f, 0.8f, 1f);
        private static readonly Color SlimeGreen = new Color(0.45f, 1f, 0.15f);
        private static readonly Color RollerOchre = new Color(0.8f, 0.6f, 0.2f);

        // The whole roster. Each species has one skill that asks for a different answer: crowds for area damage,
        // plates for heavy single shots, flyers for air defence, a trail that must be crossed, a pack that runs
        // when one falls, a glass cannon that both gun kinds can hit.
        private static Bestiary CreateBestiary()
        {
            var b = new Bestiary();

            b.Skitter = CreateEnemy(new Creature
            {
                Id = "Skitter", Layer = TargetLayers.Ground, Health = 16f, Speed = 1.6f, Reward = 2, BaseDamage = 1,
                Attack = AttackStyle.Bite, BlockadeDamage = 3f, AttackRange = 2.5f, AttackInterval = 1.1f,
                Color = new Color(1f, 0.62f, 0.12f), Eyes = new Color(1f, 0.95f, 0.5f), Accent = new Color(0.75f, 1f, 0.25f), Blood = VenomGreen,
                Scale = 0.4f, Shape = EnemyShape.Skitter, Death = DeathStyle.Shatter,
            });
            b.Stinger = CreateEnemy(new Creature
            {
                Id = "Stinger", Layer = TargetLayers.Ground, Health = 120f, Armor = 80f, ArmorReduction = 0.65f, Speed = 0.9f, Reward = 12, BaseDamage = 3,
                Attack = AttackStyle.Sting, AttackDamage = 5f, BlockadeDamage = 10f, AttackRange = 2.6f, AttackInterval = 1.5f,
                Color = new Color(0.6f, 0.2f, 0.85f), Eyes = new Color(1f, 0.5f, 1f), Accent = new Color(1f, 0.35f, 0.9f), Blood = new Color(0.55f, 1f, 0.35f),
                Scale = 0.8f, Shape = EnemyShape.Stinger, Death = DeathStyle.Shatter,
            });
            b.Emberbat = CreateBat(b, "Emberbat", Element.Fire, new Color(1f, 0.4f, 0.12f), new Color(1f, 0.9f, 0.5f), new Color(1f, 0.6f, 0.2f));
            b.Tidebat = CreateBat(b, "Tidebat", Element.Water, new Color(0.25f, 0.65f, 1f), new Color(0.8f, 1f, 1f), new Color(0.4f, 0.9f, 1f));
            b.Mossbat = CreateBat(b, "Mossbat", Element.Wood, new Color(0.35f, 0.85f, 0.3f), new Color(0.9f, 1f, 0.6f), new Color(0.6f, 1f, 0.4f));
            b.Dustbat = CreateBat(b, "Dustbat", Element.Sand, new Color(0.9f, 0.75f, 0.4f), new Color(1f, 0.95f, 0.7f), new Color(1f, 0.9f, 0.5f));
            b.Bulwark = CreateEnemy(new Creature
            {
                Id = "Bulwark", Layer = TargetLayers.Ground, Health = 220f, Armor = 160f, ArmorReduction = 0.7f, Speed = 0.7f, Reward = 18, BaseDamage = 5,
                Attack = AttackStyle.Slash, AttackDamage = 9f, BlockadeDamage = 24f, AttackRange = 1.6f, AttackInterval = 1.6f,
                Color = new Color(0.3f, 0.6f, 0.35f), Eyes = new Color(1f, 0.9f, 0.4f), Accent = new Color(0.4f, 1f, 0.95f), Blood = GooGreen,
                Scale = 0.9f, Shape = EnemyShape.Bulwark, Death = DeathStyle.Collapse,
            });
            b.Prowler = CreateEnemy(new Creature
            {
                Id = "Prowler", Layer = TargetLayers.Ground, Health = 55f, Speed = 1.5f, Reward = 6, BaseDamage = 2,
                Attack = AttackStyle.Bite, BlockadeDamage = 7f, AttackRange = 2.5f, AttackInterval = 1f,
                RageSeconds = 3f, RageSpeed = 1.6f, RageRadius = 3f,
                Color = new Color(0.8f, 0.28f, 0.15f), Eyes = new Color(1f, 0.35f, 0.2f), Accent = new Color(1f, 0.6f, 0.2f), Blood = RedBlood,
                Scale = 0.62f, Shape = EnemyShape.Prowler, Death = DeathStyle.Shatter,
            });
            b.Wisp = CreateEnemy(new Creature
            {
                Id = "Wisp", Layer = TargetLayers.Ground | TargetLayers.Air, Health = 30f, Speed = 1.2f, Reward = 6, BaseDamage = 4,
                Attack = AttackStyle.Slam, BlockadeDamage = 14f, AttackRange = 2.5f, AttackInterval = 1.2f,
                Color = new Color(0.7f, 0.85f, 1f), Eyes = Color.white, Accent = new Color(0.75f, 0.9f, 1f), Blood = WispLight,
                Scale = 0.5f, Shape = EnemyShape.Wisp, Death = DeathStyle.Burst,
            });
            b.Skyray = CreateEnemy(new Creature
            {
                Id = "Skyray", Layer = TargetLayers.Air, Flies = true, Health = 70f, Speed = 1.3f, Reward = 9, BaseDamage = 3,
                Attack = AttackStyle.Whip, AttackDamage = 8f, BlockadeDamage = 8f, AttackRange = 2.2f, AttackInterval = 1.4f, GateStrikes = 3,
                Color = new Color(0.25f, 0.35f, 0.65f), Eyes = new Color(0.5f, 1f, 1f), Accent = new Color(0.4f, 0.95f, 1f), Blood = RayBlue,
                Scale = 0.7f, Shape = EnemyShape.Skyray, Death = DeathStyle.Crash,
            });
            b.Oozer = CreateEnemy(new Creature
            {
                Id = "Oozer", Layer = TargetLayers.Ground, Health = 90f, Speed = 0.75f, Reward = 8, BaseDamage = 2,
                Attack = AttackStyle.Slam, BlockadeDamage = 9f, AttackRange = 2.5f, AttackInterval = 1.5f,
                TrailSeconds = 9f, TrailHaste = 1.45f, TrailDamage = 2.5f,
                Color = new Color(0.55f, 0.8f, 0.15f), Eyes = new Color(1f, 1f, 0.5f), Accent = new Color(0.5f, 1f, 0.2f), Blood = SlimeGreen,
                Scale = 0.7f, Shape = EnemyShape.Oozer, Death = DeathStyle.Shatter,
            });
            b.Roller = CreateEnemy(new Creature
            {
                Id = "Roller", Layer = TargetLayers.Ground, Health = 110f, Speed = 1f, Reward = 7, BaseDamage = 2,
                Attack = AttackStyle.Bite, BlockadeDamage = 8f, AttackRange = 2.5f, AttackInterval = 1.2f,
                WalkSeconds = 3f, RollSeconds = 2.2f, RollSpeed = 2.6f,
                Color = new Color(0.8f, 0.5f, 0.2f), Eyes = new Color(1f, 0.8f, 0.3f), Accent = new Color(1f, 0.7f, 0.3f), Blood = RollerOchre,
                Scale = 0.7f, Shape = EnemyShape.Roller, Death = DeathStyle.Shatter,
            });

            CreateBosses(b);
            return b;
        }

        // One bat per element: the same creature in a different light, born in a different nest.
        private static EnemyDefinition CreateBat(Bestiary b, string id, Element element, Color color, Color eyes, Color accent)
        {
            return CreateEnemy(new Creature
            {
                Id = id, Layer = TargetLayers.Air, Flies = true, Health = 28f, Speed = 1.5f, Reward = 5, BaseDamage = 2, Element = element,
                Attack = AttackStyle.Bolt, AttackDamage = 4f, BlockadeDamage = 4f, AttackRange = 2.8f, AttackInterval = 1.3f, GateStrikes = 2,
                Color = color, Eyes = eyes, Accent = accent, Blood = accent,
                Scale = 0.5f, Shape = EnemyShape.Bat, Death = DeathStyle.Crash,
            });
        }

        // The queen or king of every species: the same silhouette grown huge, with the species' skill turned into a
        // power, and a pace of its own. The brood mother is slow and endless, the pack alpha fast and loud, the
        // core wisp breaks apart as it dies, the sky sovereign burns from above.
        private static void CreateBosses(Bestiary b)
        {
            BossAbilityDefinition brood = CreateAbility("Brood", BossAbilityKind.Spawn, 10f, 6f, 0f, 1f, 1f, 1f, b.Skitter, 5, new Color(1f, 0.7f, 0.2f));
            BossAbilityDefinition venomBurst = CreateAbility("VenomBurst", BossAbilityKind.Pulse, 7f, 4f, 12f, 2.4f, 1f, 1f, null, 1, new Color(0.75f, 1f, 0.25f));
            BossAbilityDefinition acidRain = CreateAbility("AcidRain", BossAbilityKind.Volley, 9f, 5f, 18f, 1f, 1f, 6f, null, 1, new Color(1f, 0.35f, 0.9f), targets: 4);
            BossAbilityDefinition plateRegrowth = CreateAbility("PlateRegrowth", BossAbilityKind.Regrow, 12f, 10f, 150f, 1f, 1f, 1f, null, 1, new Color(1f, 0.5f, 1f));
            BossAbilityDefinition elementStorm = CreateAbility("ElementStorm", BossAbilityKind.Volley, 7f, 4f, 12f, 1f, 1f, 7f, null, 1, new Color(0.6f, 1f, 0.4f), targets: 5);
            BossAbilityDefinition batSwarm = CreateAbility("BatSwarm", BossAbilityKind.Spawn, 14f, 8f, 0f, 1f, 1f, 1f, b.Mossbat, 3, new Color(0.6f, 1f, 0.4f));
            BossAbilityDefinition swordSweep = CreateAbility("SwordSweep", BossAbilityKind.Pulse, 8f, 5f, 40f, 2.8f, 1f, 1f, null, 1, new Color(0.5f, 1f, 1f));
            BossAbilityDefinition crystalRegrowth = CreateAbility("CrystalRegrowth", BossAbilityKind.Regrow, 10f, 8f, 200f, 1f, 1f, 1f, null, 1, new Color(0.5f, 1f, 1f));
            BossAbilityDefinition howl = CreateAbility("Howl", BossAbilityKind.Rally, 12f, 5f, 0f, 1f, 5f, 1f, null, 1, new Color(1f, 0.5f, 0.15f), speedFactor: 1.6f);
            BossAbilityDefinition corePulse = CreateAbility("CorePulse", BossAbilityKind.Pulse, 7f, 4f, 30f, 3f, 1f, 1f, null, 1, new Color(0.85f, 0.9f, 1f));
            BossAbilityDefinition fission = CreateAbility("Fission", BossAbilityKind.Split, 1f, 0f, 0f, 1f, 1f, 1f, b.Wisp, 3, new Color(0.85f, 0.9f, 1f), trigger: 0.25f);
            BossAbilityDefinition tailLaser = CreateAbility("TailLaser", BossAbilityKind.Laser, 10f, 5f, 16f, 1f, 3.5f, 6f, null, 1, new Color(0.5f, 1f, 1f));
            BossAbilityDefinition slimeFlood = CreateAbility("SlimeFlood", BossAbilityKind.Pulse, 9f, 5f, 15f, 3.5f, 1f, 1f, null, 1, new Color(0.6f, 1f, 0.2f));
            BossAbilityDefinition spawnOozers = CreateAbility("SpawnOozers", BossAbilityKind.Spawn, 15f, 9f, 0f, 1f, 1f, 1f, b.Oozer, 2, new Color(0.6f, 1f, 0.2f));
            BossAbilityDefinition quake = CreateAbility("Quake", BossAbilityKind.Pulse, 8f, 5f, 30f, 2.5f, 1f, 1f, null, 1, new Color(1f, 0.7f, 0.3f));
            BossAbilityDefinition queenPulse = CreateAbility("QueenPulse", BossAbilityKind.Pulse, 14f, 9f, 45f, 3.5f, 1f, 1f, null, 1, new Color(1f, 0.3f, 0.4f));
            BossAbilityDefinition queenBrood = CreateAbility("QueenBrood", BossAbilityKind.Spawn, 12f, 6f, 0f, 1f, 1f, 1f, b.Skitter, 6, new Color(1f, 0.3f, 0.35f));

            b.BroodMother = CreateEnemy(new Creature
            {
                Id = "BroodMother", Layer = TargetLayers.Ground, Health = 900f, Speed = 0.55f, Reward = 150, BaseDamage = 10, Boss = true,
                Attack = AttackStyle.Bite, BlockadeDamage = 45f, AttackRange = 2.5f, AttackInterval = 1.3f,
                Color = new Color(0.95f, 0.45f, 0.08f), Eyes = new Color(1f, 0.95f, 0.5f), Accent = new Color(0.9f, 1f, 0.3f), Blood = VenomGreen,
                Scale = 1.3f, Shape = EnemyShape.BroodMother, Death = DeathStyle.Shatter, Abilities = new[] { brood, venomBurst },
            });
            b.AcidEmpress = CreateEnemy(new Creature
            {
                Id = "AcidEmpress", Layer = TargetLayers.Ground, Health = 1300f, Armor = 700f, ArmorReduction = 0.7f, Speed = 0.6f, Reward = 220, BaseDamage = 12, Boss = true,
                Attack = AttackStyle.Sting, AttackDamage = 14f, BlockadeDamage = 60f, AttackRange = 3.5f, AttackInterval = 1f,
                Color = new Color(0.45f, 0.1f, 0.7f), Eyes = new Color(1f, 0.4f, 1f), Accent = new Color(1f, 0.3f, 0.8f), Blood = new Color(0.6f, 1f, 0.3f),
                Scale = 1.5f, Shape = EnemyShape.AcidEmpress, Death = DeathStyle.Shatter, Abilities = new[] { acidRain, plateRegrowth },
            });
            b.StormMatriarch = CreateEnemy(new Creature
            {
                Id = "StormMatriarch", Layer = TargetLayers.Air, Flies = true, Health = 700f, Speed = 1.1f, Reward = 180, BaseDamage = 10, Element = Element.Wood, Boss = true,
                Attack = AttackStyle.Bolt, AttackDamage = 10f, BlockadeDamage = 10f, AttackRange = 3f, AttackInterval = 0.8f, GateStrikes = 6,
                Color = new Color(0.3f, 0.75f, 0.35f), Eyes = new Color(0.95f, 1f, 0.6f), Accent = new Color(0.6f, 1f, 0.4f), Blood = new Color(0.6f, 1f, 0.4f),
                Scale = 1.2f, Shape = EnemyShape.StormMatriarch, Death = DeathStyle.Crash, Abilities = new[] { elementStorm, batSwarm },
            });
            b.CrystalWarlord = CreateEnemy(new Creature
            {
                Id = "CrystalWarlord", Layer = TargetLayers.Ground, Health = 1800f, Armor = 1200f, ArmorReduction = 0.75f, Speed = 0.5f, Reward = 260, BaseDamage = 15, Boss = true,
                Attack = AttackStyle.Slash, AttackDamage = 30f, BlockadeDamage = 90f, AttackRange = 2f, AttackInterval = 1.4f,
                Color = new Color(0.2f, 0.45f, 0.3f), Eyes = new Color(1f, 0.85f, 0.3f), Accent = new Color(0.5f, 1f, 1f), Blood = GooGreen,
                Scale = 1.6f, Shape = EnemyShape.CrystalWarlord, Death = DeathStyle.Collapse, Abilities = new[] { swordSweep, crystalRegrowth },
            });
            b.PackAlpha = CreateEnemy(new Creature
            {
                Id = "PackAlpha", Layer = TargetLayers.Ground, Health = 800f, Speed = 1.3f, Reward = 160, BaseDamage = 8, Boss = true,
                Attack = AttackStyle.Bite, AttackDamage = 12f, BlockadeDamage = 40f, AttackRange = 2f, AttackInterval = 0.9f,
                WalkSeconds = 4f, RollSeconds = 1.5f, RollSpeed = 2.2f, RageSeconds = 4f, RageSpeed = 1.7f, RageRadius = 5f,
                Color = new Color(0.6f, 0.15f, 0.1f), Eyes = new Color(1f, 0.3f, 0.15f), Accent = new Color(1f, 0.5f, 0.15f), Blood = RedBlood,
                Scale = 1.3f, Shape = EnemyShape.PackAlpha, Death = DeathStyle.Shatter, Abilities = new[] { howl },
            });
            b.CoreWisp = CreateEnemy(new Creature
            {
                Id = "CoreWisp", Layer = TargetLayers.Ground | TargetLayers.Air, Health = 600f, Speed = 0.9f, Reward = 170, BaseDamage = 10, Boss = true,
                Attack = AttackStyle.Slam, BlockadeDamage = 40f, AttackRange = 2.5f, AttackInterval = 1.2f,
                Color = new Color(0.8f, 0.75f, 1f), Eyes = Color.white, Accent = new Color(0.85f, 0.8f, 1f), Blood = new Color(0.85f, 0.8f, 1f),
                Scale = 1.3f, Shape = EnemyShape.CoreWisp, Death = DeathStyle.Burst, Abilities = new[] { corePulse, fission },
            });
            b.SkySovereign = CreateEnemy(new Creature
            {
                Id = "SkySovereign", Layer = TargetLayers.Air, Flies = true, Health = 1000f, Speed = 1f, Reward = 200, BaseDamage = 12, Boss = true,
                Attack = AttackStyle.Whip, AttackDamage = 22f, BlockadeDamage = 22f, AttackRange = 2.5f, AttackInterval = 1.1f, GateStrikes = 8,
                Color = new Color(0.15f, 0.25f, 0.55f), Eyes = new Color(0.6f, 1f, 1f), Accent = new Color(0.5f, 1f, 1f), Blood = RayBlue,
                Scale = 1.4f, Shape = EnemyShape.SkySovereign, Death = DeathStyle.Crash, Abilities = new[] { tailLaser },
            });
            b.SlimeMonarch = CreateEnemy(new Creature
            {
                Id = "SlimeMonarch", Layer = TargetLayers.Ground, Health = 1500f, Speed = 0.45f, Reward = 220, BaseDamage = 12, Boss = true,
                Attack = AttackStyle.Slam, AttackDamage = 20f, BlockadeDamage = 70f, AttackRange = 2f, AttackInterval = 1.6f,
                TrailSeconds = 14f, TrailHaste = 1.6f, TrailDamage = 5f,
                Color = new Color(0.45f, 0.7f, 0.1f), Eyes = new Color(1f, 1f, 0.5f), Accent = new Color(0.6f, 1f, 0.2f), Blood = SlimeGreen,
                Scale = 1.5f, Shape = EnemyShape.SlimeMonarch, Death = DeathStyle.Shatter, Abilities = new[] { spawnOozers, slimeFlood },
            });
            b.RollerKing = CreateEnemy(new Creature
            {
                Id = "RollerKing", Layer = TargetLayers.Ground, Health = 1100f, Armor = 500f, ArmorReduction = 0.6f, Speed = 0.9f, Reward = 190, BaseDamage = 10, Boss = true,
                Attack = AttackStyle.Bite, BlockadeDamage = 60f, AttackRange = 2.5f, AttackInterval = 1.2f,
                WalkSeconds = 3f, RollSeconds = 3f, RollSpeed = 2.8f,
                Color = new Color(0.7f, 0.4f, 0.15f), Eyes = new Color(1f, 0.75f, 0.3f), Accent = new Color(1f, 0.6f, 0.2f), Blood = RollerOchre,
                Scale = 1.3f, Shape = EnemyShape.RollerKing, Death = DeathStyle.Shatter, Abilities = new[] { quake },
            });
            b.Queen = CreateEnemy(new Creature
            {
                Id = "HiveQueen", Layer = TargetLayers.Ground, Health = 3200f, Speed = 0.55f, Reward = 500, BaseDamage = 20, Boss = true,
                Attack = AttackStyle.Bite, BlockadeDamage = 120f, AttackRange = 2.5f, AttackInterval = 1.2f,
                Color = new Color(0.6f, 0.05f, 0.1f), Eyes = new Color(1f, 0.4f, 0.4f), Accent = new Color(1f, 0.3f, 0.4f), Blood = new Color(0.7f, 0.1f, 0.2f),
                Scale = 2.7f, Shape = EnemyShape.Queen, Death = DeathStyle.Shatter, Abilities = new[] { queenBrood, queenPulse },
            });
        }

        private static EnemyDefinition CreateEnemy(Creature c)
        {
            var enemy = LoadOrCreate<EnemyDefinition>($"{DataRoot}/Enemies/{c.Id}.asset");
            var so = new SerializedObject(enemy);
            so.FindProperty("_displayName").stringValue = c.Id;
            so.FindProperty("_layer").intValue = (int)c.Layer;
            so.FindProperty("_flies").boolValue = c.Flies;
            so.FindProperty("_maxHealth").floatValue = c.Health;
            so.FindProperty("_armor").floatValue = c.Armor;
            so.FindProperty("_armorReduction").floatValue = c.Armor > 0f ? c.ArmorReduction : 0.6f;
            so.FindProperty("_speed").floatValue = c.Speed;
            so.FindProperty("_reward").intValue = c.Reward;
            so.FindProperty("_baseDamage").intValue = c.BaseDamage;
            so.FindProperty("_element").enumValueIndex = (int)c.Element;
            so.FindProperty("_attackStyle").enumValueIndex = (int)c.Attack;
            so.FindProperty("_attackDamage").floatValue = c.AttackDamage;
            so.FindProperty("_blockadeDamage").floatValue = c.BlockadeDamage;
            so.FindProperty("_attackRange").floatValue = c.AttackRange;
            so.FindProperty("_attackInterval").floatValue = c.AttackInterval;
            so.FindProperty("_gateStrikes").intValue = c.GateStrikes;
            so.FindProperty("_walkSeconds").floatValue = c.WalkSeconds;
            so.FindProperty("_rollSeconds").floatValue = c.RollSeconds;
            so.FindProperty("_rollSpeedMultiplier").floatValue = c.RollSpeed > 0f ? c.RollSpeed : 2.5f;
            so.FindProperty("_trailSeconds").floatValue = c.TrailSeconds;
            so.FindProperty("_trailHaste").floatValue = c.TrailHaste > 0f ? c.TrailHaste : 1.4f;
            so.FindProperty("_trailDamagePerSecond").floatValue = c.TrailDamage;
            so.FindProperty("_rageSeconds").floatValue = c.RageSeconds;
            so.FindProperty("_rageSpeedMultiplier").floatValue = c.RageSpeed > 0f ? c.RageSpeed : 1.5f;
            so.FindProperty("_rageRadius").floatValue = c.RageRadius > 0f ? c.RageRadius : 3f;
            so.FindProperty("_isBoss").boolValue = c.Boss;
            SetReferences(so.FindProperty("_abilities"), c.Abilities ?? new BossAbilityDefinition[0]);
            so.FindProperty("_color").colorValue = c.Color;
            so.FindProperty("_eyeColor").colorValue = c.Eyes;
            so.FindProperty("_accentColor").colorValue = c.Accent;
            so.FindProperty("_bloodColor").colorValue = c.Blood;
            so.FindProperty("_scale").floatValue = c.Scale;
            so.FindProperty("_shape").intValue = (int)c.Shape;
            so.FindProperty("_deathStyle").enumValueIndex = (int)c.Death;
            so.FindProperty("_model").objectReferenceValue = null;
            so.FindProperty("_modelScale").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        private static BossAbilityDefinition CreateAbility(
            string id,
            BossAbilityKind kind,
            float interval,
            float firstDelay,
            float damage,
            float radius,
            float sweepSeconds,
            float range,
            EnemyDefinition minion,
            int minionCount,
            Color color,
            int targets = 3,
            float speedFactor = 1.5f,
            float trigger = 0.25f)
        {
            var ability = LoadOrCreate<BossAbilityDefinition>($"{DataRoot}/Enemies/Abilities/{id}.asset");
            var so = new SerializedObject(ability);
            so.FindProperty("_displayName").stringValue = id;
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.FindProperty("_interval").floatValue = interval;
            so.FindProperty("_firstDelay").floatValue = firstDelay;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_radius").floatValue = radius;
            so.FindProperty("_sweepSeconds").floatValue = sweepSeconds;
            so.FindProperty("_range").floatValue = range;
            so.FindProperty("_minion").objectReferenceValue = minion;
            so.FindProperty("_minionCount").intValue = minionCount;
            so.FindProperty("_targetCount").intValue = targets;
            so.FindProperty("_speedFactor").floatValue = speedFactor;
            so.FindProperty("_triggerFraction").floatValue = trigger;
            so.FindProperty("_color").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ability);
            return ability;
        }

        private static ElementNest Nest(Element element, int x, int y) => new ElementNest { Element = element, Cell = new Vector2Int(x, y) };

        // Nests of a liquid element snap to the nearest cell of that liquid that survived erosion; the others stay put.
        private static void WriteNests(SerializedProperty array, ElementNest[] nests, TerrainPatch[] kept)
        {
            array.arraySize = nests.Length;
            for (int i = 0; i < nests.Length; i++)
            {
                TerrainKind wanted = LevelValidator.NestTerrain(nests[i].Element);
                Vector2Int cell = TerrainMap.IsLiquid(wanted) ? NearestOfKind(kept, nests[i].Cell, wanted) : nests[i].Cell;
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Cell").vector2IntValue = cell;
                element.FindPropertyRelative("Element").enumValueIndex = (int)nests[i].Element;
            }
        }

        private static Vector2Int NearestOfKind(TerrainPatch[] patches, Vector2Int wanted, TerrainKind kind)
        {
            Vector2Int best = wanted;
            float bestSqr = float.MaxValue;
            foreach (TerrainPatch patch in patches)
            {
                if (patch.Kind != kind || patch.Cell.x < 0 || patch.Cell.y < 0)
                {
                    continue;
                }

                float sqr = (patch.Cell - wanted).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = patch.Cell;
                }
            }

            return best;
        }

        private static Vector2Int[] NestCells(ElementNest[] nests)
        {
            var cells = new Vector2Int[nests.Length];
            for (int i = 0; i < nests.Length; i++)
            {
                cells[i] = nests[i].Cell;
            }

            return cells;
        }
    }
}
