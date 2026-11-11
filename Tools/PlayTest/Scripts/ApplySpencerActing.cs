using System.Reflection;
using Game.Core;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 40: imports Spencer's acting pass only. Re-syncs Mercer (and his weapon files) and rebuilds his hero art: the six
/// acting clips by role, the Aim_Hold loop, and the acting data (root offsets and face tracks) from the report. Reports the
/// clips, the acting rows and the face meshes' blend shapes. No other export is touched. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplySpencerActing.cs --entry ApplySpencerActing.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplySpencerActing
{
    public static string Run()
    {
        int copied = Game.Editor.BlenderSync.Sync(new[] { Game.Editor.BlenderStoryCatalog.HeroAsset }, Game.Editor.BlenderStoryCatalog.ExtraModels());
        MethodInfo build = typeof(Game.Editor.BlenderStoryBuilder).GetMethod("BuildHero", BindingFlags.NonPublic | BindingFlags.Static);
        var hero = (Game.Hero.HeroArtDefinition)build.Invoke(null, null);
        AssetDatabase.SaveAssets();
        string report = $"copied {copied}; hero {(hero != null ? hero.name : "none")}";
        if (hero == null || hero.Body == null)
        {
            return report;
        }

        foreach (ModelClip clip in hero.Body.Clips)
        {
            report += $"\n{clip.Role} {clip.Length:0.00}s ev {clip.EventSeconds:0.000}{(clip.UpperBody ? " upper" : string.Empty)} loop {(clip.Clip != null && clip.Clip.isLooping)} far {(clip.FarClip != clip.Clip)}";
        }

        foreach (ModelClipRole role in new[] { ModelClipRole.CapsuleExit, ModelClipRole.StartleBackstep, ModelClipRole.DrawAimNervous, ModelClipRole.AimHoldNervous, ModelClipRole.TurnLookLeft, ModelClipRole.TurnLookRight })
        {
            Game.Hero.HeroActingClip acting = hero.ActingFor(role);
            report += $"\nacting {role}: " + (acting == null ? "none" : $"back {acting.StartBack} turn {acting.StartTurn} face {acting.Face.Length}");
            if (acting != null)
            {
                foreach (FaceCurve curve in acting.Face)
                {
                    report += $" {curve.Shape}[{curve.Keys.Length}]";
                }
            }
        }

        foreach (GameObject model in new[] { hero.Body.Model, hero.Body.Lod1 })
        {
            if (model == null)
            {
                continue;
            }

            foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = renderer.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                string shapes = string.Empty;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    shapes += " " + mesh.GetBlendShapeName(i);
                }

                report += $"\n{model.name}/{renderer.name} tris {mesh.triangles.Length / 3} bones {renderer.bones.Length} shapes:{shapes}";
            }
        }

        report += "\nface shapes driven: " + string.Join(",", hero.FaceShapes());
        return report;
    }
}
