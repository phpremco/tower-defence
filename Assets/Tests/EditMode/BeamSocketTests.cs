using Game.Core;
using Game.Editor;
using Game.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 50: a boss's beam and bolts leave from a bone its art names (the ray boss's tail, the bat boss's lens, the acid
    /// gland), read every frame, instead of a fixed height over the body that no longer met the redesigned models. The
    /// sockets live in the catalog and the art data, in order of preference, and at least one names a bone of both rigs. The
    /// ray boss aims its tail with its Release clip, timed on the laser.
    /// </summary>
    public sealed class BeamSocketTests
    {
        private const string ArtFolder = "Assets/_Project/Data/Art";
        private const string EnemyFolder = "Assets/_Project/Data/Enemies";
        private const string RayBossRow = "RayBoss";
        private const string LaserTrigger = nameof(BossAbilityKind.Laser);

        [Test]
        public void EveryArtRow_CarriesTheCatalogBeamSockets_AndOneIsABoneOfBothRigs()
        {
            int rows = 0;
            foreach (BlenderArtEntry entry in BlenderArtCatalog.Creatures)
            {
                var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{entry.Id}.asset");
                if (art == null)
                {
                    continue;
                }

                CollectionAssert.AreEqual(entry.BeamSockets, art.BeamSockets, $"{entry.Id}: the art's beam sockets differ from the catalog");
                if (entry.BeamSockets.Length == 0)
                {
                    continue;
                }

                rows++;
                Assert.IsNotNull(FirstBone(art.Model, art.BeamSockets), $"{entry.Id}: none of {string.Join(", ", art.BeamSockets)} in the near rig");
                if (art.Lod1 != null)
                {
                    Assert.IsNotNull(FirstBone(art.Lod1, art.BeamSockets), $"{entry.Id}: none of {string.Join(", ", art.BeamSockets)} in the far rig");
                }
            }

            Assert.Greater(rows, 0, "no art row names a beam socket");
        }

        [Test]
        public void EveryModelledBossWithALaserOrVolley_FiresFromABoneOfItsArt()
        {
            int shooters = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemyFolder }))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (enemy.Art == null || !Shoots(enemy))
                {
                    continue;
                }

                shooters++;
                Assert.IsNotNull(FirstBone(enemy.Art.Model, enemy.Art.BeamSockets), $"{enemy.name} shoots a laser or volley but its art {enemy.Art.name} names no beam socket its rig has");
            }

            Assert.Greater(shooters, 0, "no modelled boss with a laser or volley found");
        }

        [Test]
        public void RayBoss_AimsItsTail_WithTheReleaseClip_OnTheLaser()
        {
            var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{RayBossRow}.asset");
            int index = art.IndexOfTrigger(LaserTrigger);
            Assert.GreaterOrEqual(index, 0, "the ray boss has no clip for its laser");
            ModelClip clip = art.Clips[index];
            Assert.AreEqual(ModelClipRole.Power, clip.Role, "the laser clip should be a power");
            Assert.IsNotNull(clip.Clip, "the laser clip has no animation");
            Assert.Greater(clip.EventSeconds, 0f, "the laser clip needs its release moment");
            Assert.Less(clip.EventSeconds, clip.Length, "the release moment lies inside the clip");
        }

        private static bool Shoots(EnemyDefinition enemy)
        {
            foreach (BossAbilityDefinition ability in enemy.Abilities)
            {
                if (ability != null && (ability.Kind == BossAbilityKind.Laser || ability.Kind == BossAbilityKind.Volley))
                {
                    return true;
                }
            }

            return false;
        }

        // The first of the names, in order, that the model has as a node; null when none.
        private static Transform FirstBone(GameObject model, string[] names)
        {
            Transform[] nodes = model.GetComponentsInChildren<Transform>(true);
            foreach (string name in names)
            {
                foreach (Transform node in nodes)
                {
                    if (node.name == name)
                    {
                        return node;
                    }
                }
            }

            return null;
        }
    }
}
