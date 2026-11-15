using Game.Core;
using Game.Towers;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Generator part for the Gun Turret, the Cannon and the Flak Turret, the blockade and the base's own gun, and their looks.</summary>
    public static partial class ProjectGenerator
    {
        private const float FlakShellSpeed = 14f;
        private const float FlakSplash = 1.1f;
        private const float FlakFuse = 0.6f;

        // The Gun Turret aims up too, but what its bullets land there is the combat rules' (a fifth, round 61: a row of them
        // shot every raid down and the Flak was never needed); nothing about the sky is typed on a tower any more (round 67).

        // The Gun Turret's bullets do not turn (Remco 2026-11-06), so they fly fast enough to be near instant: few die with their
        // creature in flight (was 14 cells a second).
        private const float BulletSpeed = 30f;
        private const float CannonSplash = 1.2f;

        // Round 22, the swarm role in the data: a shell hurts at most three creatures (a pack, a queue), a flak burst four packed
        // flyers, so against a dense ant swarm the rate-of-fire guns deal more per coin and the Cannon keeps the groups and plates.
        private const int CannonSplashTargets = 3;
        private const int FlakSplashTargets = 4;
        // Round 21a3: the shell is fired at the spot and flies 0.6 to 0.9 s (3 to 4.5 cells at 5 a second), so an ant at full run
        // (1.6 a second) is about at the blast's edge when it lands, a queue or a pack is not; it used to home in and never miss.
        private const float CannonShellSpeed = 5f;
        // Round 21a3: half the Gun Turret's and the Laser's seconds passed with nothing in reach (Tools/PlayTest/tower_efficiency.txt),
        // so both reach as far as the Cannon; the ledger now counts what they really deal (TowerDefinition.LedgerEfficiency).
        internal const float LightGunReach = 4f;

        // Round 21a2, the swarm role in play: the probes measured the Gun Turret below the Cannon per coin even against loose
        // ants (9.2 against 11.4 DPS per 100 coins): the Cannon reloads slower now. A longer-reaching, faster gun (4.0, 0.30 s)
        // only made the ledger, which already rates it far above its in-play share, solve harsher waves (the greedy probe lost
        // Quartz Flats and Crysium Vein), and a smaller first shell and blast lost the creative bot more levels than the Cannon
        // row; both were dropped. Every step still beats more towers of the same type (TowerRolesTests).

        private static void GenerateCoreTowers(GeneratorContext context)
        {
            // Round 67: every step improves one named thing, and the damage per second of each level is what it was (the Gun
            // Turret 14.3 / 30 / 60, the Cannon 15.6 / 35.6 / 73.9, the Flak 25.5 / 50 / 88.9), so the prices and the upgrade
            // rule stand. The reach no longer grows with the stars (it grew 0.3 a level): no gun's step is its reach. The Cannon's
            // two steps are both its shell: a faster Cannon would be a better swarm gun, which is not its job (a shell takes three
            // creatures at most, so a flood of small ones is the light guns' work at every level).
            context.Pulse = CreateTower("PulseRifle", "Gun Turret", TargetLayers.Ground | TargetLayers.Air, BulletSpeed, 0f, new Color(0.3f, 0.7f, 1f),
                Levels(Lvl(50, 5f, LightGunReach, 0.35f), Faster("Faster cycling", 9f / 0.3f), Harder("Heavier rounds", 15f / 0.25f)), 150f,
                roleLine: "Light fire, cheap: streams of small creatures, runners and soft bodies. Only chips armour, and lands a fifth in the air.", roles: new[] { TowerRole.LightFire });
            context.Cannon = CreateTower("Cannon", "Cannon", TargetLayers.Ground, CannonShellSpeed, CannonSplash, new Color(1f, 0.8f, 0.3f),
                Levels(Lvl(100, 25f, 4f, 1.6f), Harder("Heavier shells", 48f / 1.35f), Harder("High-explosive filler", 85f / 1.15f)), 220f, homing: false, splashTargets: CannonSplashTargets,
                roleLine: "Blast: breaks bunched groups and strips armour. Cannot aim up, and its shell lands where a runner was.", roles: new[] { TowerRole.Splash, TowerRole.AntiArmour });
            // The Flak (the old Rocket, id unchanged): air only, fast proximity-fused shells that burst among packed flyers.
            context.Rocket = CreateTower("Rocket", "Flak Turret", TargetLayers.Air, FlakShellSpeed, FlakSplash, new Color(0.9f, 0.9f, 1f),
                Levels(Lvl(120, 14f, 5.5f, 0.55f), Harder("Heavier bursts", 25f / 0.5f), Faster("Faster loaders", 40f / 0.45f)), 130f, retargetRadius: MissileRetargetRadius, splashTargets: FlakSplashTargets,
                roleLine: "Air defence: the one gun that lands in full on flyers. Cannot hit the ground.", roles: new[] { TowerRole.AntiAir });
            SetFuse(context.Rocket, FlakFuse);

            // Level 0 cost is the price to rebuild a wrecked blockade. The gun arrives with the first upgrade. A gate is a
            // structure, not a tower: its steps are refits (health and gun together).
            context.Blockade = CreateTower("Blockade", "Blockade", TargetLayers.Ground, 12f, 0f, new Color(0.55f, 0.65f, 0.8f),
                Levels(Lvl(60, 0f, 3.7f, 0.5f), Refit("Armed gate", Lvl(90, 6f, 3.9f, 0.45f, 300f)), Refit("Fortified gate", Lvl(150, 11f, 4.2f, 0.35f, 500f))), 500f, blocksGround: true,
                roleLine: "Holds the road. Its gun helps; it replaces no tower.", roles: new[] { TowerRole.Helper });

            // The base's own gun: unarmed at base weapon level zero, one or two of these beside the road once bought.
            context.BaseTurret = CreateTower("BaseTurret", "Base Turret", TargetLayers.Ground | TargetLayers.Air, 14f, 0f, new Color(0.45f, 0.85f, 1f),
                Levels(Lvl(0, 7f, 4.2f, 0.4f), Refit("Turret Mk II", Lvl(70, 12f, 4.5f, 0.35f)), Refit("Turret Mk III", Lvl(110, 19f, 4.8f, 0.3f))), 220f, baseDefence: true,
                roleLine: "The base's last line. It helps; it replaces no tower.", roles: new[] { TowerRole.Helper });
            SetShape(context.BaseTurret, TowerShape.PulseRifle);
        }

        private static void SetFuse(TowerDefinition tower, float fuse)
        {
            var so = new UnityEditor.SerializedObject(tower);
            so.FindProperty("_fuseRadius").floatValue = fuse;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Every tower wears a procedural shape until the Blender tower art replaces it.
        private static void GenerateTowerLooks(GeneratorContext context)
        {
            SetShapes(context.Roster, context.Cannon, context.Rocket, context.Blockade);
        }
    }
}
