using System.Collections.Generic;
using System.Text;
using Game.Core;
using Game.Drones;
using Game.Economy;
using Game.Hero;
using Game.Towers;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Round 66: the hero's permanent gear (the companion repair drone, the armour plates, the blade at its top tier) as
    /// look data, and the targeted steps that write the ten upgrade tiers and that gear without the full generator.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string HeroGearArtPath = BlenderStoryBuilder.StoryArtFolder + "/" + BlenderStoryCatalog.HeroAsset + "GearArt.asset";
        private const string HeroArtPath = BlenderStoryBuilder.StoryArtFolder + "/" + BlenderStoryCatalog.HeroAsset + "Art.asset";
        private const string RepairDronePath = DataRoot + "/Drones/RepairDrone.asset";

        // Vertices within this distance of a plate's outward line count as the body under it.
        private const float PlateProbeRadius = 0.045f;
        private const float PlateGap = 0.003f;
        private const float PlateFallbackDepth = 0.06f;

        /// <summary>
        /// One plate as the designer places it: on a bone, so many metres along it, facing a direction of the standing
        /// figure (he looks along +Z, his left is -X). The seat on the suit's surface is measured from the model.
        /// </summary>
        private readonly struct PlateSeat
        {
            public readonly string Bone;
            public readonly float Along;
            public readonly Vector3 Outward;
            public readonly Vector3 Size;
            public readonly int FromTier;

            public PlateSeat(string bone, float along, Vector3 outward, Vector3 size, int fromTier)
            {
                Bone = bone;
                Along = along;
                Outward = outward;
                Size = size;
                FromTier = fromTier;
            }
        }

        // The armour grows piece by piece: the upper arms first (under the suit's own shoulder pieces, whose shapes no
        // plate sits on cleanly: caps seated on them floated beside the helmet), then the chest, the forearms, the thighs
        // and the shins; from the sixth tier on the plates only grow (HeroGearArtDefinition.PlateGrowth). Sizes are
        // width, length and curl in metres.
        private static readonly PlateSeat[] HeroPlates =
        {
            new PlateSeat("UpperArm_L", 0.15f, new Vector3(-1f, 0f, 0.15f), new Vector3(0.15f, 0.18f, 0.055f), 1),
            new PlateSeat("UpperArm_R", 0.15f, new Vector3(1f, 0f, 0.15f), new Vector3(0.15f, 0.18f, 0.055f), 1),
            new PlateSeat("Spine2", 0.1f, new Vector3(0f, 0.1f, 1f), new Vector3(0.26f, 0.17f, 0.045f), 2),
            new PlateSeat("Forearm_L", 0.1f, new Vector3(-1f, 0f, 0.2f), new Vector3(0.12f, 0.19f, 0.045f), 3),
            new PlateSeat("Forearm_R", 0.1f, new Vector3(1f, 0f, 0.2f), new Vector3(0.12f, 0.19f, 0.045f), 3),
            new PlateSeat("Thigh_L", 0.15f, new Vector3(-0.6f, 0f, 1f), new Vector3(0.16f, 0.24f, 0.055f), 4),
            new PlateSeat("Thigh_R", 0.15f, new Vector3(0.6f, 0f, 1f), new Vector3(0.16f, 0.24f, 0.055f), 4),
            new PlateSeat("Shin_L", 0.15f, new Vector3(0f, 0f, 1f), new Vector3(0.13f, 0.24f, 0.05f), 5),
            new PlateSeat("Shin_R", 0.15f, new Vector3(0f, 0f, 1f), new Vector3(0.13f, 0.24f, 0.05f), 5),
        };

        /// <summary>
        /// Writes the hero's gear art and hangs it on his art: the repair drone's model for the companion, and every armour
        /// plate seated on the suit's surface as it stands in the idle pose. Safe to run again.
        /// </summary>
        public static string ApplyHeroGearArt()
        {
            var heroArt = AssetDatabase.LoadAssetAtPath<HeroArtDefinition>(HeroArtPath);
            if (heroArt == null || heroArt.Body == null)
            {
                return "No hero art at " + HeroArtPath + ": the gear art was not written.\n";
            }

            var gear = BlenderArtBuilder.LoadOrCreate<HeroGearArtDefinition>(HeroGearArtPath);
            var so = new SerializedObject(gear);
            var drone = AssetDatabase.LoadAssetAtPath<DroneDefinition>(RepairDronePath);
            so.FindProperty("_droneBody").objectReferenceValue = drone != null ? drone.Art : null;
            var report = new StringBuilder();
            WritePlates(so.FindProperty("_plates"), heroArt, report);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gear);

            var heroSo = new SerializedObject(heroArt);
            heroSo.FindProperty("_gear").objectReferenceValue = gear;
            heroSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(heroArt);
            AssetDatabase.SaveAssets();
            return report.Insert(0, "Hero gear art: companion " + (drone != null && drone.Art != null ? drone.Art.name : "none") + ", " + HeroPlates.Length + " plates\n").ToString();
        }

        // Seats every plate: its inner face on the suit's surface where the outward line from the bone leaves the body.
        private static void WritePlates(SerializedProperty plates, HeroArtDefinition heroArt, StringBuilder report)
        {
            var root = new GameObject("HeroGearSeating");
            try
            {
                ArtModel body = ArtModel.CreateStill(root.transform, heroArt.Body);
                body.Sample(ModelClipRole.Idle, 0f, loop: true);
                List<Vector3> surface = BodySurface(body);
                plates.arraySize = HeroPlates.Length;
                for (int i = 0; i < HeroPlates.Length; i++)
                {
                    PlateSeat seat = HeroPlates[i];
                    Transform bone = body.Find(seat.Bone);
                    SerializedProperty element = plates.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("Bone").stringValue = seat.Bone;
                    element.FindPropertyRelative("Size").vector3Value = seat.Size;
                    element.FindPropertyRelative("FromTier").intValue = seat.FromTier;
                    if (bone == null)
                    {
                        report.Append("  ").Append(seat.Bone).Append(": bone not found\n");
                        continue;
                    }

                    Vector3 axis = bone.up;
                    Vector3 origin = bone.position + axis * seat.Along;
                    Vector3 outward = Vector3.ProjectOnPlane(seat.Outward, axis).normalized;
                    float depth = SurfaceDepth(surface, origin, outward);
                    Vector3 position = origin + outward * (depth + PlateGap);
                    Quaternion rotation = Quaternion.LookRotation(outward, axis);
                    element.FindPropertyRelative("LocalPosition").vector3Value = bone.InverseTransformPoint(position);
                    element.FindPropertyRelative("LocalEuler").vector3Value = (Quaternion.Inverse(bone.rotation) * rotation).eulerAngles;
                    report.Append("  ").Append(seat.Bone).Append(" tier ").Append(seat.FromTier).Append(": surface ").Append(depth.ToString("0.000")).Append(" m from the bone\n");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // The posed vertices of the body's near mesh, in the figure's space (the seating root stands at the origin).
        private static List<Vector3> BodySurface(ArtModel body)
        {
            var points = new List<Vector3>();
            var baked = new Mesh();
            var vertices = new List<Vector3>();
            foreach (Renderer renderer in body.Renderers)
            {
                // The far LOD and the face are not what a plate lies on.
                if (!(renderer is SkinnedMeshRenderer skinned) || renderer.name != body.Art.name)
                {
                    continue;
                }

                skinned.BakeMesh(baked, false);
                baked.GetVertices(vertices);
                Matrix4x4 toWorld = skinned.transform.localToWorldMatrix;
                foreach (Vector3 vertex in vertices)
                {
                    points.Add(toWorld.MultiplyPoint3x4(vertex));
                }
            }

            Object.DestroyImmediate(baked);
            return points;
        }

        private static float SurfaceDepth(List<Vector3> surface, Vector3 origin, Vector3 outward)
        {
            float depth = float.MinValue;
            foreach (Vector3 point in surface)
            {
                Vector3 delta = point - origin;
                float along = Vector3.Dot(delta, outward);
                if (along > 0f && (delta - outward * along).sqrMagnitude <= PlateProbeRadius * PlateProbeRadius)
                {
                    depth = Mathf.Max(depth, along);
                }
            }

            return depth > 0f ? depth : PlateFallbackDepth;
        }

        /// <summary>
        /// Rewrites the permanent upgrades as the generator's "Upgrades" step does (ten tiers, the hero's gear, the tier caps
        /// per continent), from the towers and drones the catalogue already lists. Safe to run again.
        /// </summary>
        public static string ApplyUpgradeTiers()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(BalanceStages.UpgradesPath);
            var hybridIds = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:CombineRecipe"))
            {
                var recipe = AssetDatabase.LoadAssetAtPath<CombineRecipe>(AssetDatabase.GUIDToAssetPath(guid));
                if (recipe != null && recipe.Result != null)
                {
                    hybridIds.Add(recipe.Result.Id);
                }
            }

            var towersById = new Dictionary<string, TowerDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:TowerDefinition"))
            {
                var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (tower != null)
                {
                    towersById[tower.Id] = tower;
                }
            }

            var standard = new List<TowerDefinition>();
            var hybrids = new List<TowerDefinition>();
            var drones = new List<DroneDefinition>();
            var before = new List<string>();
            foreach (UpgradeDefinition upgrade in catalog.Upgrades)
            {
                before.Add(upgrade.Id);
                if (!string.IsNullOrEmpty(upgrade.TowerId) && towersById.TryGetValue(upgrade.TowerId, out TowerDefinition tower))
                {
                    List<TowerDefinition> list = hybridIds.Contains(tower.Id) ? hybrids : standard;
                    if (!list.Contains(tower))
                    {
                        list.Add(tower);
                    }
                }
                else if (!string.IsNullOrEmpty(upgrade.DroneId))
                {
                    var drone = AssetDatabase.LoadAssetAtPath<DroneDefinition>($"{DataRoot}/Drones/{upgrade.DroneId}.asset");
                    if (drone != null && !drones.Contains(drone))
                    {
                        drones.Add(drone);
                    }
                }
            }

            UpgradeCatalog written = CreateUpgrades(standard.ToArray(), hybrids.ToArray(), drones.ToArray());
            EditorUtility.SetDirty(written);
            AssetDatabase.SaveAssets();

            var report = new StringBuilder();
            report.Append("Upgrades: ").Append(before.Count).Append(" -> ").Append(written.Upgrades.Count).Append(" (").Append(standard.Count).Append(" towers, ").Append(hybrids.Count).Append(" hybrids, ").Append(drones.Count).Append(" drones); tier caps ").Append(string.Join("/", written.TiersPerContinent)).Append('\n');
            foreach (UpgradeDefinition upgrade in written.Upgrades)
            {
                if (!before.Contains(upgrade.Id))
                {
                    report.Append("  new: ").Append(upgrade.Id).Append('\n');
                }
            }

            foreach (string id in before)
            {
                bool kept = false;
                foreach (UpgradeDefinition upgrade in written.Upgrades)
                {
                    kept |= upgrade.Id == id;
                }

                if (!kept)
                {
                    report.Append("  DROPPED: ").Append(id).Append('\n');
                }
            }

            return report.ToString();
        }
    }
}
