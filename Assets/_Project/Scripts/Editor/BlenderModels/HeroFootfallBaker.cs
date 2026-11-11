using System.Collections.Generic;
using Game.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Round 44 (Remco: "footsteps play while Spencer is not walking"): bakes where the commander's feet land into his clips.
    /// Every clip of his body is sampled on the imported model, his foot bones are followed in the model's own space (a travel
    /// cycle gets its travel added back, so a planted foot stands still) and <see cref="Footfalls.Detect"/> finds the moments a
    /// foot comes to rest after a stride. They are written to the clip's footfall list, which the cutscene plays steps on.
    /// Runs inside the hero build and on its own from the menu; running it again gives the same data.
    /// </summary>
    public static class HeroFootfallBaker
    {
        private const float SamplesPerSecond = 60f;
        private const string HeroArtPath = BlenderStoryBuilder.StoryArtFolder + "/" + BlenderStoryCatalog.HeroAsset + "Art.asset";
        private static readonly string[] FootBones = { "Foot_L", "Foot_R" };

        [MenuItem("Tools/Generate/Hero Footfalls")]
        public static void BakeHero()
        {
            var hero = AssetDatabase.LoadAssetAtPath<Game.Hero.HeroArtDefinition>(HeroArtPath);
            if (hero == null || hero.Body == null)
            {
                throw new System.InvalidOperationException($"No hero art at {HeroArtPath}: run the story art step first.");
            }

            Debug.Log(Bake(hero.Body));
            AssetDatabase.SaveAssets();
        }

        /// <summary>Writes every clip's footfalls into the model's art and returns a line per clip.</summary>
        public static string Bake(ModelArtDefinition body)
        {
            var holder = new GameObject("FootfallProbe") { hideFlags = HideFlags.HideAndDontSave };
            var report = new System.Text.StringBuilder();
            try
            {
                ArtModel model = ArtModel.CreateSampled(holder.transform, body, ownMaterial: false);
                var feet = new Transform[FootBones.Length];
                for (int f = 0; f < feet.Length; f++)
                {
                    feet[f] = model.Find(FootBones[f]);
                    if (feet[f] == null)
                    {
                        return $"{body.name}: no {FootBones[f]} bone, nothing baked.";
                    }
                }

                var so = new SerializedObject(body);
                SerializedProperty clips = so.FindProperty("_clips");
                for (int i = 0; i < body.Clips.Length; i++)
                {
                    float[] moments = BakeClip(model, holder.transform, feet, body.Clips[i]);
                    SerializedProperty array = clips.GetArrayElementAtIndex(i).FindPropertyRelative("_footfallSeconds");
                    array.arraySize = moments.Length;
                    for (int m = 0; m < moments.Length; m++)
                    {
                        array.GetArrayElementAtIndex(m).floatValue = moments[m];
                    }

                    report.Append($"{body.Clips[i].Role}: {string.Join(" ", System.Array.ConvertAll(moments, x => x.ToString("0.00")))}\n");
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(body);
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }

            return report.ToString();
        }

        private static float[] BakeClip(ArtModel model, Transform holder, Transform[] feet, ModelClip clip)
        {
            if (clip.Clip == null || clip.Length <= 0f)
            {
                return System.Array.Empty<float>();
            }

            bool loop = clip.Clip.isLooping;
            int frames = Mathf.Max(3, Mathf.RoundToInt(clip.Length * SamplesPerSecond));
            // A travel cycle is keyed in place: the body moves forward (+Z) by its distance per loop.
            float speed = clip.DistancePerLoop > 0f ? clip.DistancePerLoop / clip.Length : 0f;
            var left = new List<Vector3>(frames);
            var right = new List<Vector3>(frames);
            for (int f = 0; f < frames; f++)
            {
                float seconds = f / SamplesPerSecond;
                model.Sample(clip.Role, seconds, loop);
                Vector3 travel = Vector3.forward * (speed * seconds);
                left.Add(holder.InverseTransformPoint(feet[0].position) + travel);
                right.Add(holder.InverseTransformPoint(feet[1].position) + travel);
            }

            return Footfalls.Detect(left, right, SamplesPerSecond, loop, Vector3.forward * clip.DistancePerLoop);
        }
    }
}
