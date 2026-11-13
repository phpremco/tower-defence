using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The health bar height of a creature model, in model units. The rest pose alone undersells creatures whose cycles lift
    /// them (wing beats, a curled tail, a rearing slug): their bar sat inside the drawn body for much of the cycle. So the
    /// travel and idle cycles are sampled and the real skinned surface baked; the bar clears the body for
    /// <see cref="CoverShare"/> of those poses, and never sits below the rest-pose measure.
    /// A crown that fans towards and away from the camera (the Shattered's spires, round 53) crosses the bar on screen even
    /// below it in height: the play camera looks down, so a spire leaning away stands higher in the picture than its tip's
    /// height says. The art can name such crest bones; their parts are then measured as the play camera projects them at
    /// four headings, and the bar clears that projection for the same share of poses and headings.
    /// </summary>
    public static class CreatureBarHeight
    {
        // Share of the sampled cycle poses the bar must clear: a wing tip may still flick past it at the top of a beat,
        // but the bar does not float a full wing span above the body.
        public const float CoverShare = 0.75f;

        /// <summary>The play camera's pitch in every level (CameraSettings.PitchDegrees; a test keeps them equal).</summary>
        public const float PlayCameraPitchDegrees = 44f;

        private const int SamplesPerCycle = 16;

        // The headings a creature shows the fixed camera as it walks the roads: towards, across both ways, away.
        private static readonly float[] Headings = { 0f, 90f, 180f, 270f };

        // The camera headings a fight clip's bar is measured at (every 15 degrees): the view interpolates between them.
        public const int ClipHeadingCount = 24;

        private static readonly ModelClipRole[] Cycles = { ModelClipRole.Move, ModelClipRole.Idle };

        /// <summary>The bar height for a creature: the rest-pose top, raised to the cycles' covered top where that is higher.</summary>
        /// <param name="crestBones">Bones whose parts the bar clears as the play camera projects them (see the class notes), or none.</param>
        public static float Measure(GameObject model, ModelArtDefinition art, IReadOnlyCollection<string> crestBones = null)
        {
            float rest = BlenderArtBuilder.ModelTop(model);
            List<float> tops = CycleTops(model, art, null, crestBones);
            return tops.Count == 0 ? rest : Mathf.Max(rest, Covered(tops));
        }

        /// <summary>
        /// The baked top of every sampled pose of the travel and idle cycles, relative to the model root. With
        /// <paramref name="heldScales"/> (bone name to uniform scale) those bones are set after each sample, as a boss's
        /// phase look holds them over its clips in play (BoneScaleView), so a look can be checked against the bar. With
        /// <paramref name="crestBones"/> each pose gives one top per heading, its crest projected as the camera sees it.
        /// </summary>
        public static List<float> CycleTops(GameObject model, ModelArtDefinition art, IReadOnlyDictionary<string, float> heldScales = null, IReadOnlyCollection<string> crestBones = null)
        {
            var clips = new List<AnimationClip>();
            foreach (ModelClipRole role in Cycles)
            {
                int index = art.IndexOf(role);
                if (index >= 0)
                {
                    clips.Add(art.Clips[index].Clip);
                }
            }

            return Tops(model, clips, heldScales, crestBones, Headings);
        }

        /// <summary>
        /// The baked top of every sampled pose of one clip (round 58: a power that flares a crest past the cycles' bar), one top
        /// per heading with <paramref name="crestBones"/>, as <see cref="CycleTops"/> measures the cycles.
        /// </summary>
        public static List<float> ClipTops(GameObject model, AnimationClip clip, IReadOnlyCollection<string> crestBones, float[] headings) =>
            Tops(model, new List<AnimationClip> { clip }, null, crestBones, headings);

        private static List<float> Tops(GameObject model, List<AnimationClip> clips, IReadOnlyDictionary<string, float> heldScales, IReadOnlyCollection<string> crestBones, float[] headings)
        {
            var tops = new List<float>();
            GameObject probe = Object.Instantiate(model);
            var baked = new Mesh();
            try
            {
                SkinnedMeshRenderer[] renderers = probe.GetComponentsInChildren<SkinnedMeshRenderer>();
                List<(Transform Bone, float Scale)> held = HeldBones(probe, heldScales);
                bool[][] crest = CrestMasks(renderers, crestBones);
                foreach (AnimationClip clip in clips)
                {
                    for (int i = 0; i < SamplesPerCycle; i++)
                    {
                        clip.SampleAnimation(probe, clip.length * i / SamplesPerCycle);
                        foreach ((Transform bone, float scale) in held)
                        {
                            bone.localScale = Vector3.one * scale;
                        }

                        float ground = probe.transform.position.y;
                        if (crest == null)
                        {
                            tops.Add(BakedTop(renderers, baked) - ground);
                            continue;
                        }

                        foreach (float top in ProjectedTops(renderers, crest, baked, probe.transform.position, headings))
                        {
                            tops.Add(top - ground);
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(baked);
                Object.DestroyImmediate(probe);
            }

            return tops;
        }

        private static List<(Transform Bone, float Scale)> HeldBones(GameObject probe, IReadOnlyDictionary<string, float> heldScales)
        {
            var held = new List<(Transform Bone, float Scale)>();
            if (heldScales == null || heldScales.Count == 0)
            {
                return held;
            }

            foreach (Transform bone in probe.GetComponentsInChildren<Transform>())
            {
                if (heldScales.TryGetValue(bone.name, out float scale))
                {
                    held.Add((bone, scale));
                }
            }

            return held;
        }

        // Per renderer, which of its bones are crest bones; null when the art names none (or none of them is in the rig).
        private static bool[][] CrestMasks(SkinnedMeshRenderer[] renderers, IReadOnlyCollection<string> crestBones)
        {
            if (crestBones == null || crestBones.Count == 0)
            {
                return null;
            }

            var names = new HashSet<string>(crestBones);
            var masks = new bool[renderers.Length][];
            bool any = false;
            for (int r = 0; r < renderers.Length; r++)
            {
                Transform[] bones = renderers[r].bones;
                masks[r] = new bool[bones.Length];
                for (int b = 0; b < bones.Length; b++)
                {
                    masks[r][b] = bones[b] != null && names.Contains(bones[b].name);
                    any |= masks[r][b];
                }
            }

            return any ? masks : null;
        }

        // One top per heading: the plain height of the body, or a crest vertex's height plus how much higher the looking-down
        // camera draws it for its depth away from the camera at that heading, whichever stands higher in the picture.
        private static float[] ProjectedTops(SkinnedMeshRenderer[] renderers, bool[][] crest, Mesh baked, Vector3 axis, float[] headings)
        {
            float lean = Mathf.Tan(PlayCameraPitchDegrees * Mathf.Deg2Rad);
            var away = new Vector3[headings.Length];
            var tops = new float[headings.Length];
            for (int h = 0; h < headings.Length; h++)
            {
                away[h] = Quaternion.Euler(0f, headings[h], 0f) * Vector3.forward;
                tops[h] = float.MinValue;
            }

            for (int r = 0; r < renderers.Length; r++)
            {
                renderers[r].BakeMesh(baked, true);
                Matrix4x4 toWorld = renderers[r].transform.localToWorldMatrix;
                Vector3[] vertices = baked.vertices;
                BoneWeight[] weights = renderers[r].sharedMesh.boneWeights;
                for (int v = 0; v < vertices.Length; v++)
                {
                    Vector3 point = toWorld.MultiplyPoint3x4(vertices[v]);
                    bool crestVertex = v < weights.Length && crest[r][weights[v].boneIndex0];
                    Vector3 flat = new Vector3(point.x - axis.x, 0f, point.z - axis.z);
                    for (int h = 0; h < headings.Length; h++)
                    {
                        float depth = crestVertex ? Mathf.Max(0f, Vector3.Dot(flat, away[h])) : 0f;
                        tops[h] = Mathf.Max(tops[h], point.y + depth * lean);
                    }
                }
            }

            return tops;
        }

        /// <summary>
        /// The bar heights that clear one clip played over the cycles (a power, a roar), one per camera heading round the model,
        /// <see cref="ClipHeadingCount"/> of them evenly from the model's +Z (heading i at i * 360 / count degrees): the highest of
        /// its sampled tops at each. A power's flare is the reason it is measured and lasts only part of the clip, so no share of
        /// it is let through. Four headings were too coarse: a flared halo stands highest between them (round 58, the Adapt
        /// clip 0.6 above its nearest sample at 45 degrees). Without crest bones every heading gets the same top.
        /// </summary>
        public static float[] ClipBars(GameObject model, AnimationClip clip, IReadOnlyCollection<string> crestBones)
        {
            var headings = new float[ClipHeadingCount];
            for (int h = 0; h < headings.Length; h++)
            {
                headings[h] = h * 360f / ClipHeadingCount;
            }

            List<float> tops = ClipTops(model, clip, crestBones, headings);
            // One top per pose, or one per heading of each pose when the crest bones were found in the rig.
            int perPose = Mathf.Max(1, tops.Count / SamplesPerCycle);
            var bars = new float[headings.Length];
            for (int i = 0; i < tops.Count; i++)
            {
                for (int h = 0; h < headings.Length; h++)
                {
                    if (perPose == 1 || i % perPose == h)
                    {
                        bars[h] = Mathf.Max(bars[h], tops[i]);
                    }
                }
            }

            return bars;
        }

        // The clips a creature plays in place in a fight, on top of its cycles, where a crest may flare over the bar. A flinch is
        // left out (a boss is hit many times a second; the bar would bob), and so are the mechanics that carry the body itself
        // (climbs, dives, burrows, the rise onto the base) and the fall (the corpse has no bar).
        private static readonly ModelClipRole[] InPlaceFightRoles =
        {
            ModelClipRole.Attack, ModelClipRole.GroundedAttack, ModelClipRole.Power, ModelClipRole.Roar, ModelClipRole.Enrage,
            ModelClipRole.Adapt, ModelClipRole.PhaseBreak, ModelClipRole.Split, ModelClipRole.Rejoin, ModelClipRole.Smash,
            ModelClipRole.SporeVolley, ModelClipRole.LayEggs, ModelClipRole.FreezeGate, ModelClipRole.ArmourBreak,
        };

        /// <summary>True for a clip played in place over the cycles in a fight (see <see cref="InPlaceFightRoles"/>).</summary>
        public static bool FightsInPlace(ModelClipRole role) => System.Array.IndexOf(InPlaceFightRoles, role) >= 0;

        /// <summary>The height the bar must reach to clear <see cref="CoverShare"/> of the sampled tops.</summary>
        public static float Covered(List<float> tops)
        {
            tops.Sort();
            int index = Mathf.Clamp(Mathf.CeilToInt(CoverShare * tops.Count) - 1, 0, tops.Count - 1);
            return tops[index];
        }

        /// <summary>
        /// The world height of the highest skinned vertex as drawn now. The baked mesh is in the renderer's local space, so
        /// it goes through the renderer's whole matrix: position and rotation alone read a model under a scaled parent (a
        /// boss at 0.8 in play) at its unscaled size, which made round 52 see the Shattered's top 0.6 m above where it is.
        /// </summary>
        public static float BakedTop(SkinnedMeshRenderer[] renderers, Mesh baked)
        {
            float top = float.MinValue;
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                renderer.BakeMesh(baked, true);
                Matrix4x4 toWorld = renderer.transform.localToWorldMatrix;
                foreach (Vector3 vertex in baked.vertices)
                {
                    top = Mathf.Max(top, toWorld.MultiplyPoint3x4(vertex).y);
                }
            }

            return top;
        }
    }
}
