using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 72: shaders the game finds by name at runtime survive a player build only through the keeper materials on
    /// the Boot scene. The IL2CPP smoke run found the Boot scene holding an older, shorter list (no Game/ExhaustPlume),
    /// so every level threw while it was built. Every keeper material must be on the Boot scene, and the shaders found
    /// by name must each have one.
    /// </summary>
    public sealed class ShaderKeeperTests
    {
        private const string KeeperFolder = "Assets/_Project/Art/Materials/ShaderKeepers";
        private const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";

        private static readonly string[] ShadersFoundByName =
        {
            Game.Core.PlumeKit.ShaderName,
            Game.UI.SpriteBatch.ShaderName,
            Game.UI.ExhaustPlume.ShimmerShader,
            Game.UI.BossCutIn.ReflectionShader,
            Game.Core.SurfaceMaterials.UnitShaderName,
            Game.UI.PlanetMaterials.SurfaceShader,
            Game.UI.PlanetMaterials.CloudShader,
            Game.UI.PlanetMaterials.AtmosphereShader,
            Game.UI.SpaceBackdrop.SkyShader,
            "Game/ImpactRipple",
            "Game/TiltShift",
        };

        [Test]
        public void EveryKeeperMaterial_IsOnTheBootScene()
        {
            string scene = File.ReadAllText(BootScenePath);
            var missing = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { KeeperFolder }))
            {
                if (!scene.Contains("guid: " + guid))
                {
                    missing.Add(AssetDatabase.GUIDToAssetPath(guid));
                }
            }

            CollectionAssert.IsEmpty(missing, "keepers not on the Boot scene (run Tools/Generate/Shader Keepers)");
        }

        [Test]
        public void EveryShaderFoundByName_HasAKeeper()
        {
            var kept = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { KeeperFolder }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material != null && material.shader != null)
                {
                    kept.Add(material.shader.name);
                }
            }

            var missing = new List<string>();
            foreach (string shader in ShadersFoundByName)
            {
                if (!kept.Contains(shader))
                {
                    missing.Add(shader);
                }
            }

            CollectionAssert.IsEmpty(missing, "shaders found by name without a keeper material");
        }
    }
}
