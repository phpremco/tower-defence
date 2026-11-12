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
    /// </summary>
    public static class CreatureBarHeight
    {
        // Share of the sampled cycle poses the bar must clear: a wing tip may still flick past it at the top of a beat,
        // but the bar does not float a full wing span above the body.
        public const float CoverShare = 0.75f;

        private const int SamplesPerCycle = 16;

        private static readonly ModelClipRole[] Cycles = { ModelClipRole.Move, ModelClipRole.Idle };

        /// <summary>The bar height for a creature: the rest-pose top, raised to the cycles' covered top where that is higher.</summary>
        public static float Measure(GameObject model, ModelArtDefinition art)
        {
            float rest = BlenderArtBuilder.ModelTop(model);
            List<float> tops = CycleTops(model, art);
            return tops.Count == 0 ? rest : Mathf.Max(rest, Covered(tops));
        }

        /// <summary>The baked top of every sampled pose of the travel and idle cycles, relative to the model root.</summary>
        public static List<float> CycleTops(GameObject model, ModelArtDefinition art)
        {
            var tops = new List<float>();
            GameObject probe = Object.Instantiate(model);
            var baked = new Mesh();
            try
            {
                SkinnedMeshRenderer[] renderers = probe.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (ModelClipRole role in Cycles)
                {
                    int index = art.IndexOf(role);
                    if (index < 0)
                    {
                        continue;
                    }

                    AnimationClip clip = art.Clips[index].Clip;
                    for (int i = 0; i < SamplesPerCycle; i++)
                    {
                        clip.SampleAnimation(probe, clip.length * i / SamplesPerCycle);
                        tops.Add(BakedTop(renderers, baked) - probe.transform.position.y);
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

        /// <summary>The height the bar must reach to clear <see cref="CoverShare"/> of the sampled tops.</summary>
        public static float Covered(List<float> tops)
        {
            tops.Sort();
            int index = Mathf.Clamp(Mathf.CeilToInt(CoverShare * tops.Count) - 1, 0, tops.Count - 1);
            return tops[index];
        }

        private static float BakedTop(SkinnedMeshRenderer[] renderers, Mesh baked)
        {
            float top = float.MinValue;
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                renderer.BakeMesh(baked, true);
                Quaternion rotation = renderer.transform.rotation;
                Vector3 position = renderer.transform.position;
                foreach (Vector3 vertex in baked.vertices)
                {
                    top = Mathf.Max(top, (position + rotation * vertex).y);
                }
            }

            return top;
        }
    }
}
