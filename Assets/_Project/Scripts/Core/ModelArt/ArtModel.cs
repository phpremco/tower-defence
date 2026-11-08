using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Core
{
    /// <summary>
    /// One imported (Blender) model placed in the scene for a structure, a drone or a placed object: the instance turned
    /// and scaled by its art, its near and far meshes under a LOD group, one material for all of them (the owner's own
    /// copy when it changes the glow), and a clip player when the model has clips. The views of the gate, the base, the
    /// drones, the path objects and the hero build on it; the creatures and towers keep their own (stage 1 and 2) wrappers.
    /// The art's extra parts (<see cref="ModelPart"/>: the hero's face and visor) wear their own materials.
    /// </summary>
    public sealed class ArtModel
    {
        private const string FarSuffix = "_Far";
        // A far twin that kept its export name (the far LOD from a file of its own).
        private const string SourceFarSuffix = "_LOD1";

        // The far LOD stays drawn however small the model gets.
        private const float FarLodScreenHeight = 0.0005f;

        private readonly bool _ownMaterial;

        // The far mesh from its own file (the art's Lod1) on its own rig, posed alongside the near one; null without.
        private readonly Transform _farRig;

        private ArtModel(ModelArtDefinition art, Transform root, Material material, bool ownMaterial, ModelClipPlayer player, Renderer[] renderers, Transform farRig)
        {
            _farRig = farRig;
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

        /// <summary>The material every mesh of the model wears, apart from the art's extra parts.</summary>
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
            if (_farRig != null)
            {
                clip.FarClip.SampleAnimation(_farRig.gameObject, Mathf.Min(time, clip.FarClip.length));
            }

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
                if (renderer.name.EndsWith(suffix) || renderer.name.EndsWith(suffix + FarSuffix) || renderer.name.EndsWith(suffix + SourceFarSuffix))
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
            // A one-mesh export carries the FBX axis turn on its root itself (the base's wreck and damage overlay, the gate's
            // wreck): replacing it with the yaw stood those models on their edge, so the yaw is put in front of it instead.
            root.localRotation = Quaternion.Euler(0f, art.Yaw, 0f) * art.Model.transform.localRotation;
            root.localScale = Vector3.one * art.Scale;
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Transform farRig = null;
            Renderer[] farRenderers = new Renderer[0];
            Animator farAnimator = null;
            if (withFar && art.Lod1 != null)
            {
                // Under the near instance, so it follows every turn and move the owner gives the model.
                GameObject far = Object.Instantiate(art.Lod1, root, false);
                far.name = art.Lod1.name;
                farRig = far.transform;
                farRenderers = far.GetComponentsInChildren<Renderer>(true);
                farAnimator = far.GetComponent<Animator>();
            }

            Dress(instance, renderers, farRenderers, art, material, withFar);

            ModelClipPlayer player = null;
            Animator animator = instance.GetComponent<Animator>();
            if (animate && art.Clips.Length > 0)
            {
                if (animator == null)
                {
                    animator = instance.AddComponent<Animator>();
                }

                if (farRig != null && farAnimator == null)
                {
                    farAnimator = farRig.gameObject.AddComponent<Animator>();
                }

                player = new ModelClipPlayer(art, farAnimator != null ? new[] { animator, farAnimator } : new[] { animator });
            }
            else
            {
                // A static model, a still or one posed by Sample: nothing to evaluate.
                Disable(animator);
                Disable(farAnimator);
            }

            return new ArtModel(art, root, material, ownMaterial, player, Concat(renderers, farRenderers), farRig);
        }

        // The near meshes and the far ones (renamed from "_LOD1" at import) share the rig: a LOD group picks one.
        // A far mesh from its own file (<paramref name="farRenderers"/>) counts as far whatever its name. Every mesh wears
        // the given material, except the art's extra parts (a face, a visor's glass), which wear their own.
        private static void Dress(GameObject instance, Renderer[] renderers, Renderer[] farRenderers, ModelArtDefinition art, Material material, bool withFar)
        {
            var near = new List<Renderer>(renderers.Length);
            var far = new List<Renderer>(renderers.Length + farRenderers.Length);
            bool low = ModelQuality.IsLow;
            foreach (Renderer renderer in Concat(renderers, farRenderers))
            {
                ModelPart part = art.PartFor(renderer.name);
                Material worn = part != null ? part.MaterialFor(low) : null;
                renderer.sharedMaterial = worn != null ? worn : material;
                renderer.shadowCastingMode = part == null || part.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = true;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    skinned.updateWhenOffscreen = false;
                }

                (renderer.name.EndsWith(FarSuffix) || System.Array.IndexOf(farRenderers, renderer) >= 0 ? far : near).Add(renderer);
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

        private static void Disable(Animator animator)
        {
            if (animator != null)
            {
                animator.enabled = false;
            }
        }

        private static Renderer[] Concat(Renderer[] a, Renderer[] b)
        {
            if (b.Length == 0)
            {
                return a;
            }

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
