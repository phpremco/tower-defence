using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Towers
{
    /// <summary>
    /// One tier of an imported tower model in the scene: the skinned mesh and its far LOD (both in the tier's FBX, on one
    /// rig) under a LOD group, the rig's named bones (the turning <c>Yaw</c>, the tilting <c>Pitch</c>, the shot origins,
    /// the <c>Smoke</c> socket and the three rank stars) with their rest poses, and the clip player when it animates.
    /// The root stays unrotated so the rank plate keeps facing the camera; aiming turns the bones.
    /// </summary>
    public sealed class TowerModel
    {
        public const string YawBone = "Yaw";
        public const string PitchBone = "Pitch";
        public const string SmokeBone = "Smoke";
        public const string StarBonePrefix = "Star_";
        private const string FarSuffix = "_Far";

        // The far LOD stays drawn however small the tower gets.
        private const float FarLodScreenHeight = 0.0005f;

        // A barrel this close to vertical has no usable forward; the muzzle offset gives it instead.
        private const float MinFlatLength = 0.05f;

        private readonly Transform[] _stars;
        private readonly Quaternion _yawRest;
        private readonly Quaternion _pitchRest;

        private TowerModel(ModelArtDefinition art, Transform root, Transform[] muzzles, Transform yaw, Transform pitch, Transform smoke, Transform[] stars, Material material, ModelClipPlayer player)
        {
            Art = art;
            Root = root;
            Muzzles = muzzles;
            Yaw = yaw;
            Pitch = pitch;
            Smoke = smoke;
            _stars = stars;
            Material = material;
            Player = player;
            _yawRest = yaw != null ? yaw.localRotation : Quaternion.identity;
            _pitchRest = pitch != null ? pitch.localRotation : Quaternion.identity;
            RestForward = FlatForward(root, muzzles.Length > 0 ? muzzles[0] : null, out float elevation);
            RestElevation = elevation;
        }

        public ModelArtDefinition Art { get; }

        /// <summary>The tier's root under the tower's mount.</summary>
        public Transform Root { get; }

        /// <summary>The shot origins in bone order (Muzzle_1..n, Arc_1..n, Tube_1..n); never empty for a gun.</summary>
        public Transform[] Muzzles { get; }

        public Transform Yaw { get; }
        public Transform Pitch { get; }

        /// <summary>The damage smoke socket, or null.</summary>
        public Transform Smoke { get; }

        /// <summary>This tower's own copy of the material (for a charge glow), or the shared one for a still.</summary>
        public Material Material { get; }

        /// <summary>The clip player, or null for a still or a tier without clips.</summary>
        public ModelClipPlayer Player { get; }

        /// <summary>The way the barrels point at rest, flat on the ground, in the root's space.</summary>
        public Vector3 RestForward { get; }

        /// <summary>The barrels' modelled elevation in degrees (Flak and Artillery rest raised).</summary>
        public float RestElevation { get; }

        /// <summary>A live tier: its own material copy and, with clips, a clip player.</summary>
        public static TowerModel Create(Transform parent, ModelArtDefinition art, string muzzlePrefix, bool lowDetail)
        {
            var material = new Material(art.MaterialFor(lowDetail)) { name = art.name };
            return Build(parent, art, muzzlePrefix, material, animate: true);
        }

        /// <summary>A still for menus and icons: the near LOD only, the shared material, the rest pose.</summary>
        public static TowerModel CreateStill(Transform parent, ModelArtDefinition art, bool lowDetail)
        {
            return Build(parent, art, TowerArtDefinition.DefaultMuzzlePrefix, art.MaterialFor(lowDetail), animate: false);
        }

        private static TowerModel Build(Transform parent, ModelArtDefinition art, string muzzlePrefix, Material material, bool animate)
        {
            GameObject instance = Object.Instantiate(art.Model, parent, false);
            instance.name = art.name;
            Transform root = instance.transform;
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one * art.Scale;
            Dress(instance, art, material, animate);

            ModelClipPlayer player = null;
            if (animate && art.Clips.Length > 0)
            {
                Animator animator = instance.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = instance.AddComponent<Animator>();
                }

                player = new ModelClipPlayer(art, new[] { animator });
            }

            var stars = new Transform[TowerDefinition.LevelCount];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = Find(root, StarBonePrefix + (i + 1));
            }

            return new TowerModel(art, root, Numbered(root, muzzlePrefix), Find(root, YawBone), Find(root, PitchBone), Find(root, SmokeBone), stars, material, player);
        }

        /// <summary>Shows the first <paramref name="count"/> rank stars and hides the rest by scaling their bones to zero.</summary>
        public void ShowStars(int count)
        {
            for (int i = 0; i < _stars.Length; i++)
            {
                if (_stars[i] != null)
                {
                    _stars[i].localScale = i < count ? Vector3.one : Vector3.zero;
                }
            }
        }

        /// <summary>
        /// Turns the head by <paramref name="yawDegrees"/> about the tower's up axis and, with a pitch, tilts the barrels
        /// by <paramref name="pitchDegrees"/> (up is positive) about the turned head's right axis, both on top of the rest
        /// pose, so whatever a clip did to these two bones this frame is replaced.
        /// </summary>
        public void Aim(float yawDegrees, float pitchDegrees, bool tilt)
        {
            if (Yaw == null)
            {
                return;
            }

            Vector3 up = Root.up;
            Quaternion turn = Quaternion.AngleAxis(yawDegrees, up);
            Yaw.rotation = turn * (Yaw.parent.rotation * _yawRest);
            if (!tilt || Pitch == null)
            {
                return;
            }

            Vector3 forward = turn * Root.TransformDirection(RestForward);
            Vector3 right = Vector3.Cross(up, forward).normalized;
            Pitch.rotation = Quaternion.AngleAxis(-pitchDegrees, right) * (Pitch.parent.rotation * _pitchRest);
        }

        public void Dispose(bool ownMaterial)
        {
            Player?.Dispose();
            if (ownMaterial)
            {
                Object.Destroy(Material);
            }

            Object.Destroy(Root.gameObject);
        }

        /// <summary>The bone of that name under the tier, or null.</summary>
        public Transform Socket(string bone) => Find(Root, bone);

        // The near mesh, and the far one renamed from "_LOD1" at import, share the rig: a LOD group picks one.
        private static void Dress(GameObject instance, ModelArtDefinition art, Material material, bool withFar)
        {
            var near = new List<Renderer>(2);
            var far = new List<Renderer>(2);
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
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

        // The muzzle's +Y points out of the barrel (README round 7); flattened, that is where the head faces at rest.
        private static Vector3 FlatForward(Transform root, Transform muzzle, out float elevation)
        {
            elevation = 0f;
            if (muzzle == null)
            {
                return Vector3.forward;
            }

            Vector3 barrel = root.InverseTransformDirection(muzzle.up);
            var flat = new Vector3(barrel.x, 0f, barrel.z);
            if (flat.magnitude < MinFlatLength)
            {
                Vector3 offset = root.InverseTransformPoint(muzzle.position);
                flat = new Vector3(offset.x, 0f, offset.z);
                if (flat.magnitude < MinFlatLength)
                {
                    return Vector3.forward;
                }

                return flat.normalized;
            }

            elevation = Mathf.Atan2(barrel.y, flat.magnitude) * Mathf.Rad2Deg;
            return flat.normalized;
        }

        private static Transform[] Numbered(Transform root, string prefix)
        {
            var found = new List<Transform>(4);
            for (int n = 1; ; n++)
            {
                Transform bone = Find(root, $"{prefix}_{n}");
                if (bone == null)
                {
                    break;
                }

                found.Add(bone);
            }

            if (found.Count == 0 && prefix != TowerArtDefinition.DefaultMuzzlePrefix)
            {
                return Numbered(root, TowerArtDefinition.DefaultMuzzlePrefix);
            }

            return found.ToArray();
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
