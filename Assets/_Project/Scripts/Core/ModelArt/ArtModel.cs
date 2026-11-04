using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Core
{
    /// <summary>
    /// One imported (Blender) model placed in the scene for a structure, a drone or a placed object: the instance turned
    /// and scaled by its art, its near and far meshes under a LOD group, one material for all of them (the owner's own
    /// copy when it changes the glow), and a clip player when the model has clips. The views of the gate, the base, the
    /// drones and the path objects build on it; the creatures and towers keep their own (stage 1 and 2) wrappers.
    /// </summary>
    public sealed class ArtModel
    {
        private const string FarSuffix = "_Far";

        // The far LOD stays drawn however small the model gets.
        private const float FarLodScreenHeight = 0.0005f;

        private readonly bool _ownMaterial;

        private ArtModel(ModelArtDefinition art, Transform root, Material material, bool ownMaterial, ModelClipPlayer player, Renderer[] renderers)
        {
            Art = art;
            Root = root;
            Material = material;
            _ownMaterial = ownMaterial;
            Player = player;
            Renderers = renderers;
        }

        public ModelArtDefinition Art { get; }

        /// <summary>The model instance: turned by the art's yaw and scaled by its scale under the parent.</summary>
        public Transform Root { get; }

        /// <summary>The material every mesh of the model wears.</summary>
        public Material Material { get; }

        /// <summary>The clip player, or null for a still or a model without clips.</summary>
        public ModelClipPlayer Player { get; }

        /// <summary>Every mesh of the model, near and far.</summary>
        public Renderer[] Renderers { get; }

        /// <summary>A live model: with clips it animates, and with <paramref name="ownMaterial"/> it wears its own copy of the material.</summary>
        public static ArtModel Create(Transform parent, ModelArtDefinition art, bool ownMaterial)
        {
            Material shared = art.MaterialFor(ModelQuality.IsLow);
            Material material = ownMaterial ? new Material(shared) { name = art.name } : shared;
            return Build(parent, art, material, ownMaterial, animate: true, withFar: true);
        }

        /// <summary>A live model that wears a material its owner already has (a damage overlay on the owner's copy).</summary>
        public static ArtModel Create(Transform parent, ModelArtDefinition art, Material material)
        {
            return Build(parent, art, material, ownMaterial: false, animate: true, withFar: true);
        }

        /// <summary>A still for menus and icons: the near meshes only, the shared material, the bind pose.</summary>
        public static ArtModel CreateStill(Transform parent, ModelArtDefinition art)
        {
            return Build(parent, art, art.MaterialFor(ModelQuality.IsLow), ownMaterial: false, animate: false, withFar: false);
        }

        /// <summary>A still that wears a material the caller owns (a portrait lit in its speaker's colour).</summary>
        public static ArtModel CreateStill(Transform parent, ModelArtDefinition art, Material material)
        {
            return Build(parent, art, material, ownMaterial: false, animate: false, withFar: false);
        }

        /// <summary>
        /// A model posed by <see cref="Sample"/> instead of a clip player, near and far meshes under a LOD group: for a
        /// cutscene, whose every moment must follow from the shot time alone so a sought frame matches a played one.
        /// </summary>
        public static ArtModel CreateSampled(Transform parent, ModelArtDefinition art, bool ownMaterial)
        {
            Material shared = art.MaterialFor(ModelQuality.IsLow);
            Material material = ownMaterial ? new Material(shared) { name = art.name } : shared;
            return Build(parent, art, material, ownMaterial, animate: false, withFar: true);
        }

        /// <summary>Length of the clip of a role in seconds, or zero when the model has none.</summary>
        public float LengthOf(ModelClipRole role)
        {
            int index = Art.IndexOf(role);
            return index >= 0 ? Art.Clips[index].Length : 0f;
        }

        /// <summary>
        /// Poses the model at a moment of the clip of a role (wrapped for a cycle, clamped for a clip played once). False
        /// when the model has no such clip, so the caller can keep the bind pose or another look.
        /// </summary>
        public bool Sample(ModelClipRole role, float seconds, bool loop)
        {
            int index = Art.IndexOf(role);
            if (index < 0)
            {
                return false;
            }

            ModelClip clip = Art.Clips[index];
            float length = Mathf.Max(0.0001f, clip.Length);
            float time = loop ? Mathf.Repeat(seconds, length) : Mathf.Clamp(seconds, 0f, length);
            clip.Clip.SampleAnimation(Root.gameObject, time);
            return true;
        }

        /// <summary>The node of that name under the model (a bone, a socket or a mesh), or null.</summary>
        public Transform Find(string name) => Find(Root, name);

        /// <summary>The numbered nodes <c>prefix_1..n</c> in order, stopping at the first gap.</summary>
        public Transform[] Numbered(string prefix)
        {
            var found = new List<Transform>(4);
            for (int n = 1; ; n++)
            {
                Transform node = Find(Root, $"{prefix}_{n}");
                if (node == null)
                {
                    return found.ToArray();
                }

                found.Add(node);
            }
        }

        /// <summary>The meshes whose name ends with the suffix (a module's near and far mesh), possibly none.</summary>
        public Renderer[] RenderersEndingWith(string suffix)
        {
            var found = new List<Renderer>(2);
            foreach (Renderer renderer in Renderers)
            {
                if (renderer.name.EndsWith(suffix) || renderer.name.EndsWith(suffix + FarSuffix))
                {
                    found.Add(renderer);
                }
            }

            return found.ToArray();
        }

        public void SetActive(bool active) => Root.gameObject.SetActive(active);

        public void Dispose()
        {
            Player?.Dispose();
            if (_ownMaterial)
            {
                Object.Destroy(Material);
            }

            Object.Destroy(Root.gameObject);
        }

        private static ArtModel Build(Transform parent, ModelArtDefinition art, Material material, bool ownMaterial, bool animate, bool withFar)
        {
            GameObject instance = Object.Instantiate(art.Model, parent, false);
            instance.name = art.name;
            Transform root = instance.transform;
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.Euler(0f, art.Yaw, 0f);
            root.localScale = Vector3.one * art.Scale;
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Dress(instance, renderers, art, material, withFar);

            ModelClipPlayer player = null;
            Animator animator = instance.GetComponent<Animator>();
            if (animate && art.Clips.Length > 0)
            {
                if (animator == null)
                {
                    animator = instance.AddComponent<Animator>();
                }

                player = new ModelClipPlayer(art, new[] { animator });
            }
            else if (animator != null)
            {
                // A static model or a still: nothing to evaluate.
                animator.enabled = false;
            }

            return new ArtModel(art, root, material, ownMaterial, player, renderers);
        }

        // The near meshes and the far ones (renamed from "_LOD1" at import) share the rig: a LOD group picks one.
        private static void Dress(GameObject instance, Renderer[] renderers, ModelArtDefinition art, Material material, bool withFar)
        {
            var near = new List<Renderer>(renderers.Length);
            var far = new List<Renderer>(renderers.Length);
            foreach (Renderer renderer in renderers)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    skinned.updateWhenOffscreen = false;
                }

                (renderer.name.EndsWith(FarSuffix) ? far : near).Add(renderer);
            }

            if (far.Count == 0)
            {
                return;
            }

            if (!withFar)
            {
                foreach (Renderer renderer in far)
                {
                    renderer.enabled = false;
                }

                return;
            }

            var group = instance.AddComponent<LODGroup>();
            group.SetLODs(new[] { new LOD(art.LodScreenHeight, near.ToArray()), new LOD(FarLodScreenHeight, far.ToArray()) });
            group.RecalculateBounds();
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
