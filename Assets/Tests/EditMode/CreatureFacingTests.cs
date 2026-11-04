using Game.Core;
using Game.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Creatures walk head first (Remco 2026-11-04: "alles in main menu beweegt achterstevoren"): the imported exports face
    /// -Z, so every creature art turns them half round, and a built creature model has its head bone ahead of its centre
    /// along the owner's forward, the direction the view points the body along its heading.
    /// </summary>
    public sealed class CreatureFacingTests
    {
        private const float MinAhead = 0.9f;

        [Test]
        public void EveryCreatureModel_HasItsHeadAlongTheOwnersForward()
        {
            int checkedModels = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" }))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                ModelArtDefinition art = enemy.Art;
                if (art == null || art.Model == null)
                {
                    continue;
                }

                var owner = new GameObject("Owner");
                try
                {
                    CreatureModel model = CreatureModel.Create(owner.transform, art, 1f, true);
                    Transform head = FindHead(model.Root);
                    if (head == null)
                    {
                        continue;
                    }

                    Vector3 ahead = head.position - owner.transform.position;
                    ahead.y = 0f;
                    Assert.Greater(Vector3.Dot(ahead.normalized, owner.transform.forward), MinAhead, $"{enemy.name} ({art.name}) walks backwards");
                    checkedModels++;
                    model.Player.Dispose();
                    foreach (Material material in model.Materials)
                    {
                        Object.DestroyImmediate(material);
                    }
                }
                finally
                {
                    Object.DestroyImmediate(owner);
                }
            }

            Assert.Greater(checkedModels, 0, "no creature with a Head bone was checked");
        }

        private static Transform FindHead(Transform root)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == "Head")
                {
                    return node;
                }
            }

            return null;
        }
    }
}
