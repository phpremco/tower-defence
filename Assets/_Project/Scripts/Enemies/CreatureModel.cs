using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// One imported creature model in the scene: the near and far LOD under a LOD group, both wearing one material of
    /// their own (so a hit flash, a frost or a fading glow touches this creature only, and the SRP batcher still batches
    /// it with its kin), plus a copy of the second slot's material for a two-material boss, and the clip player that
    /// drives both rigs. Built once per pooled owner and reused.
    /// </summary>
    public sealed class CreatureModel
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int OverlayColorId = Shader.PropertyToID("_OverlayColor");

        // The far LOD stays drawn however small the creature gets: a crowd far off must not wink out.
        private const float FarLodScreenHeight = 0.0005f;

        private readonly Color[] _restEmissions;
        private Color _overlay = Color.clear;
        private float _glow = 1f;

        private CreatureModel(ModelArtDefinition art, Transform root, Transform near, Transform far, Material[] materials, ModelClipPlayer player, Renderer[] renderers)
        {
            Art = art;
            Root = root;
            Near = near;
            Far = far;
            Materials = materials;
            Player = player;
            Renderers = renderers;
            _restEmissions = new Color[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                _restEmissions[i] = materials[i].GetColor(EmissionColorId);
            }
        }

        public ModelArtDefinition Art { get; }

        /// <summary>The model's root: scaled to the creature, placed by the owner.</summary>
        public Transform Root { get; }

        /// <summary>The near LOD's instance, where the sockets are looked up.</summary>
        public Transform Near { get; }

        /// <summary>The far LOD's instance with its own lighter rig, or null.</summary>
        public Transform Far { get; }

        /// <summary>This creature's own copy of the art material.</summary>
        public Material Material => Materials[0];

        /// <summary>This creature's copies of every slot's material (one, or two for a boss with crystals or ice).</summary>
        public Material[] Materials { get; }

        public ModelClipPlayer Player { get; }

        public Renderer[] Renderers { get; }

        /// <summary>The emission at rest, for effects that pulse or fade the glow.</summary>
        public Color RestEmission => _restEmissions[0];

        /// <param name="scale">World scale of the model (the owner's scale times the art's factor).</param>
        /// <param name="lowDetail">Wear the Low (Switch) material.</param>
        public static CreatureModel Create(Transform parent, ModelArtDefinition art, float scale, bool lowDetail)
        {
            var root = new GameObject($"{art.name}_model").transform;
            root.SetParent(parent, false);
            // The exports face -Z once imported; the art's yaw turns the head onto the owner's +Z, the way it walks.
            root.localRotation = Quaternion.Euler(0f, art.Yaw, 0f);
            root.localScale = Vector3.one * scale;
            Material[] materials = CopyMaterials(art, lowDetail);

            GameObject near = Object.Instantiate(art.Model, root, false);
            near.name = art.Model.name;
            Renderer[] nearRenderers = Dress(near, materials);
            Animator[] animators;
            Renderer[] all;
            GameObject far = null;
            if (art.Lod1 != null)
            {
                far = Object.Instantiate(art.Lod1, root, false);
                far.name = art.Lod1.name;
                Renderer[] farRenderers = Dress(far, materials);
                var group = root.gameObject.AddComponent<LODGroup>();
                group.SetLODs(new[] { new LOD(art.LodScreenHeight, nearRenderers), new LOD(FarLodScreenHeight, farRenderers) });
                group.RecalculateBounds();
                animators = new[] { Rig(near), Rig(far) };
                all = Concat(nearRenderers, farRenderers);
            }
            else
            {
                animators = new[] { Rig(near) };
                all = nearRenderers;
            }

            var player = new ModelClipPlayer(art, animators);
            return new CreatureModel(art, root, near.transform, far != null ? far.transform : null, materials, player, all);
        }

        /// <summary>The bone of that name in the near LOD, or the root when the model has none.</summary>
        public Transform Socket(string bone)
        {
            if (string.IsNullOrEmpty(bone))
            {
                return Root;
            }

            Transform found = Find(Near, bone);
            return found != null ? found : Root;
        }

        /// <summary>The bone of that name in each LOD's rig (near first), for a view that scales it over the clips.</summary>
        public Transform[] Bones(string bone)
        {
            var found = new List<Transform>(2);
            Transform near = Find(Near, bone);
            if (near != null)
            {
                found.Add(near);
            }

            Transform far = Far != null ? Find(Far, bone) : null;
            if (far != null)
            {
                found.Add(far);
            }

            return found.ToArray();
        }

        /// <summary>Leans the whole surface towards a colour (a hit flash, a frost); amount zero clears it.</summary>
        public void SetOverlay(Color color, float amount)
        {
            var overlay = new Color(color.r, color.g, color.b, amount);
            if (overlay == _overlay)
            {
                return;
            }

            _overlay = overlay;
            foreach (Material material in Materials)
            {
                material.SetColor(OverlayColorId, overlay);
            }
        }

        /// <summary>Scales the glow against its rest value (1 at rest, 0 dark).</summary>
        public void SetGlow(float factor)
        {
            if (Mathf.Approximately(factor, _glow))
            {
                return;
            }

            _glow = factor;
            for (int i = 0; i < Materials.Length; i++)
            {
                Materials[i].SetColor(EmissionColorId, _restEmissions[i] * factor);
            }
        }

        public void Dispose()
        {
            Player.Dispose();
            foreach (Material material in Materials)
            {
                Object.Destroy(material);
            }
        }

        private static Material[] CopyMaterials(ModelArtDefinition art, bool lowDetail)
        {
            var first = new Material(art.MaterialFor(lowDetail)) { name = art.name };
            if (!art.HasSecondMaterial)
            {
                return new[] { first };
            }

            return new[] { first, new Material(art.SecondMaterialFor(lowDetail)) { name = art.name + "_Second" } };
        }

        // A mesh with two sub-meshes (body and crystals) wears both slots, in the export's order; every other mesh the first.
        private static Renderer[] Dress(GameObject instance, Material[] materials)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (materials.Length > 1 && SubMeshes(renderer) > 1)
                {
                    renderer.sharedMaterials = materials;
                }
                else
                {
                    renderer.sharedMaterial = materials[0];
                }

                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    skinned.updateWhenOffscreen = false;
                }
            }

            return renderers;
        }

        private static int SubMeshes(Renderer renderer)
        {
            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer skinned)
            {
                mesh = skinned.sharedMesh;
            }
            else if (renderer.TryGetComponent(out MeshFilter filter))
            {
                mesh = filter.sharedMesh;
            }

            return mesh != null ? mesh.subMeshCount : 1;
        }

        private static Animator Rig(GameObject instance)
        {
            Animator animator = instance.GetComponent<Animator>();
            return animator != null ? animator : instance.AddComponent<Animator>();
        }

        private static Renderer[] Concat(Renderer[] a, Renderer[] b)
        {
            var all = new Renderer[a.Length + b.Length];
            a.CopyTo(all, 0);
            b.CopyTo(all, a.Length);
            return all;
        }

        private static Transform Find(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = Find(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
