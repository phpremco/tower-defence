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

        // The Gun Turret aims up too but its pulses do half damage there, so the sky asks for the Flak (round 21a, Docs/BALANCE.md).
        private const float GunAirShare = 0.5f;

        // The Gun Turret's bullets do not turn (Remco 2026-11-06), so they fly fast enough to be near instant: few die with their
        // creature in flight (was 14 cells a second).
        private const float BulletSpeed = 30f;
        private const float CannonSplash = 1.2f;
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
            context.Pulse = CreateTower("PulseRifle", "Gun Turret", TargetLayers.Ground | TargetLayers.Air, BulletSpeed, 0f, new Color(0.3f, 0.7f, 1f),
                new[] { Lvl(50, 5f, LightGunReach, 0.35f), Lvl(50, 9f, LightGunReach + 0.3f, 0.3f), Lvl(90, 15f, LightGunReach + 0.6f, 0.25f) }, 150f, airDamageFactor: GunAirShare, softTargetShare: LightGunSoftShare);
            context.Cannon = CreateTower("Cannon", "Cannon", TargetLayers.Ground, CannonShellSpeed, CannonSplash, new Color(1f, 0.8f, 0.3f),
                new[] { Lvl(100, 25f, 4f, 1.6f), Lvl(110, 48f, 4.3f, 1.35f), Lvl(180, 85f, 4.6f, 1.15f) }, 220f, homing: false);
            // The Flak (the old Rocket, id unchanged): air only, fast proximity-fused shells that burst among packed flyers.
            context.Rocket = CreateTower("Rocket", "Flak Turret", TargetLayers.Air, FlakShellSpeed, FlakSplash, new Color(0.9f, 0.9f, 1f),
                new[] { Lvl(120, 14f, 5.5f, 0.55f), Lvl(100, 25f, 5.9f, 0.5f), Lvl(150, 40f, 6.3f, 0.45f) }, 130f, retargetRadius: MissileRetargetRadius);
            SetFuse(context.Rocket, FlakFuse);

            // Level 0 cost is the price to rebuild a wrecked blockade. The gun arrives with the first upgrade.
            context.Blockade = CreateTower("Blockade", "Blockade", TargetLayers.Ground, 12f, 0f, new Color(0.55f, 0.65f, 0.8f),
                new[] { Lvl(60, 0f, 3.7f, 0.5f), Lvl(90, 6f, 3.9f, 0.45f, 300f), Lvl(150, 11f, 4.2f, 0.35f, 500f) }, 500f, blocksGround: true);

            // The base's own gun: unarmed at base weapon level zero, one or two of these beside the road once bought.
            context.BaseTurret = CreateTower("BaseTurret", "Base Turret", TargetLayers.Ground | TargetLayers.Air, 14f, 0f, new Color(0.45f, 0.85f, 1f),
                new[] { Lvl(0, 7f, 4.2f, 0.4f), Lvl(70, 12f, 4.5f, 0.35f), Lvl(110, 19f, 4.8f, 0.3f) }, 220f, baseDefence: true);
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
