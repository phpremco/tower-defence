using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Hero
{
    /// <summary>
    /// Puts the armour plates of the hero's armour tier on his body: each plate rides on its bone, appears at the tier
    /// its data names and grows a little with every tier after it. Used by the hero in the level and by the figure the
    /// menus show, so the upgrade screen shows the armour he will wear.
    /// </summary>
    public static class HeroArmourPlates
    {
        private const float PlateSmoothness = 0.55f;
        private const float PlateMetallic = 0.6f;
        private const float TrimSmoothness = 0.3f;

        private static readonly Dictionary<HeroGearArtDefinition, Material[]> Materials = new Dictionary<HeroGearArtDefinition, Material[]>();

        /// <summary>
        /// Builds the plates the gear's armour tier shows; <paramref name="findBone"/> looks a bone up by name on the body
        /// they are for. Returns how many plates were placed.
        /// </summary>
        public static int Build(Func<string, Transform> findBone, HeroGearArtDefinition art, HeroGear gear)
        {
            if (art == null || gear.ArmourTier <= 0)
            {
                return 0;
            }

            Material[] materials = MaterialsFor(art);
            int placed = 0;
            foreach (ArmourPlateSpec plate in art.Plates)
            {
                Transform bone = plate.FromTier <= gear.ArmourTier ? findBone(plate.Bone) : null;
                if (bone == null)
                {
                    continue;
                }

                var go = new GameObject("ArmourPlate");
                go.transform.SetParent(bone, false);
                go.transform.localPosition = plate.LocalPosition;
                go.transform.localRotation = Quaternion.Euler(plate.LocalEuler);
                // The bone may carry the import's scale; the plate's size is in metres of the figure.
                Vector3 boneScale = bone.lossyScale;
                float growth = Growth(plate.FromTier, gear, art.PlateGrowth);
                go.transform.localScale = new Vector3(growth / Mathf.Max(0.0001f, boneScale.x), growth / Mathf.Max(0.0001f, boneScale.y), growth / Mathf.Max(0.0001f, boneScale.z));
                go.AddComponent<MeshFilter>().sharedMesh = ArmourPlateMesh.For(plate.Size, art.PlateThickness);
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                placed++;
            }

            return placed;
        }

        /// <summary>A plate's size at the gear's armour tier: one at the tier that adds it, the full growth at the last tier.</summary>
        public static float Growth(int fromTier, HeroGear gear, float fullGrowth)
        {
            int span = Mathf.Max(1, gear.ArmourTiers - fromTier);
            return Mathf.Lerp(1f, fullGrowth, Mathf.Clamp01((gear.ArmourTier - fromTier) / (float)span));
        }

        private static Material[] MaterialsFor(HeroGearArtDefinition art)
        {
            if (Materials.TryGetValue(art, out Material[] cached) && cached[0] != null && cached[1] != null && cached[2] != null)
            {
                return cached;
            }

            Material shell = SurfaceMaterials.Create(art.PlateColor, 0f, PlateSmoothness, PlateMetallic);
            shell.name = "HeroArmour_Shell";
            Material trim = SurfaceMaterials.Create(art.PlateTrimColor, art.PlateTrimGlow, TrimSmoothness);
            trim.name = "HeroArmour_Trim";
            Material frame = SurfaceMaterials.Create(art.PlateFrameColor, 0f, PlateSmoothness, PlateMetallic);
            frame.name = "HeroArmour_Frame";
            var materials = new[] { shell, trim, frame };
            Materials[art] = materials;
            return materials;
        }
    }
}
