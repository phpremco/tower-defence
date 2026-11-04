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

        private static void GenerateCoreTowers(GeneratorContext context)
        {
            context.Pulse = CreateTower("PulseRifle", "Gun Turret", TargetLayers.Ground | TargetLayers.Air, 14f, 0f, new Color(0.3f, 0.7f, 1f),
                new[] { Lvl(50, 5f, 3.5f, 0.35f), Lvl(60, 9f, 3.8f, 0.3f), Lvl(90, 15f, 4.1f, 0.25f) }, 150f);
            context.Cannon = CreateTower("Cannon", "Cannon", TargetLayers.Ground, 9f, 1.2f, new Color(1f, 0.8f, 0.3f),
                new[] { Lvl(100, 25f, 4f, 1.4f), Lvl(120, 48f, 4.3f, 1.2f), Lvl(180, 85f, 4.6f, 1f) }, 220f);
            // The Flak (the old Rocket, id unchanged): air only, fast proximity-fused shells that burst among packed flyers.
            context.Rocket = CreateTower("Rocket", "Flak Turret", TargetLayers.Air, FlakShellSpeed, FlakSplash, new Color(0.9f, 0.9f, 1f),
                new[] { Lvl(120, 14f, 5.5f, 0.55f), Lvl(140, 22f, 5.9f, 0.5f), Lvl(200, 34f, 6.3f, 0.45f) }, 130f);
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
