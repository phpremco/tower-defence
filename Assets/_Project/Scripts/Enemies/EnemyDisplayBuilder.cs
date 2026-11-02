using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// A creature standing still for a menu or the codex: the same procedural body as on the field, every moving
    /// part at its modelled rest, without the enemy logic, health bar or effects behind it.
    /// </summary>
    public static class EnemyDisplayBuilder
    {
        private const float BossGlow = 1.4f;

        public static Transform Build(Transform parent, EnemyDefinition definition)
        {
            var root = new GameObject($"{definition.name}_display").transform;
            root.SetParent(parent, false);
            if (definition.Shape == EnemyShape.None)
            {
                if (definition.Model != null)
                {
                    Object.Instantiate(definition.Model, root).transform.localScale = Vector3.one * definition.ModelScale;
                }

                return root;
            }

            EnemyModelFactory.Parts parts = EnemyModelFactory.Create(definition.Shape);
            float scale = definition.Scale * parts.ScaleFactor;
            root.localScale = Vector3.one * scale;
            root.localPosition = Vector3.up * (parts.HoverHeight * scale);
            Material[] materials = EnemyModelFactory.CreateMaterials(definition.Color, definition.EyeColor, definition.AccentColor, definition.IsBoss ? BossGlow : 1f, parts.Skin);
            Spawn("Shell", root, parts.Body, materials, Vector3.zero, false);
            foreach (CreaturePart spec in parts.Moving)
            {
                Transform part = Spawn(spec.Role.ToString(), root, spec.Mesh, materials, spec.Pivot, spec.Mirrored);
                if (spec.Lower != null)
                {
                    Spawn("Knee", part, spec.Lower, materials, spec.Knee, false);
                }
            }

            return root;
        }

        private static Transform Spawn(string name, Transform parent, Mesh mesh, Material[] materials, Vector3 position, bool mirrored)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(mirrored ? -1f : 1f, 1f, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go.transform;
        }
    }
}
