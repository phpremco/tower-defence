using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// Builds a display copy of a tower's procedural model (pedestal, head and barrel) for places that show a tower
    /// without playing it: the tower cards, the reward screen and the upgrade screen. <see cref="TowerView"/> builds
    /// the live model the same way.
    /// </summary>
    public static class TowerModelBuilder
    {
        public static bool HasModel(TowerDefinition definition) => definition.Shape != TowerShape.None;

        /// <summary>The model at the given level, standing on y = 0 under <paramref name="parent"/>, facing +z.</summary>
        public static GameObject Build(Transform parent, TowerDefinition definition, int level)
        {
            var root = new GameObject(definition.DisplayName);
            root.transform.SetParent(parent, false);
            Material[] materials = TowerModelFactory.CreateMaterials(definition.Color, level);
            TowerModelFactory.Parts parts = TowerModelFactory.Create(definition.Shape, level);

            float headHeight = 0f;
            if (!definition.BlocksGroundEnemies)
            {
                Part("Pedestal", root.transform, TowerModelFactory.Pedestal(level), materials, Vector3.zero).transform.localScale = Vector3.one * TowerModelFactory.PedestalScale;
                headHeight = TowerModelFactory.PedestalHeight;
            }

            Transform head = new GameObject("Head").transform;
            head.SetParent(root.transform, false);
            head.localPosition = new Vector3(0f, headHeight, 0f);
            head.localScale = Vector3.one * parts.Scale;
            Part("ShapeHead", head, parts.Head, materials, Vector3.zero);
            Part("ShapeBarrel", head, parts.Barrel, materials, parts.BarrelPivot);
            return root;
        }

        private static GameObject Part(string name, Transform parent, Mesh mesh, Material[] materials, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go;
        }
    }
}
