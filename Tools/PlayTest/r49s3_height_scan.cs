// Round 49 import (set 3b): for every enemy that wears a model, compares the stored bar height (ModelArtDefinition.Height,
// model units) with the real top of its skinned body: at rest (baked vertices) and over its travel and idle cycles (each
// sampled at 12 points, baked). A cycle top well above the stored height puts the health bar inside the drawn body.
// Writes Logs/r49s3_height_scan.txt and returns the rows whose cycle top exceeds the stored height by more than 10 %.
const int Samples = 12;
const float Tolerance = 1.1f;
var sb = new System.Text.StringBuilder();
var flagged = new System.Text.StringBuilder();
sb.AppendLine("enemy | art | boss | stored | restBaked | moveTop | idleTop | cycle/stored");
var seen = new System.Collections.Generic.HashSet<string>();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:EnemyDefinition"))
{
    var def = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    if (def == null || def.Art == null || def.Art.Model == null) continue;
    var art = def.Art;
    var probe = UnityEngine.Object.Instantiate(art.Model);
    try
    {
        var renderers = probe.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>();
        var baked = new UnityEngine.Mesh();
        System.Func<float> top = () =>
        {
            float best = float.MinValue;
            foreach (var r in renderers)
            {
                r.BakeMesh(baked, true);
                var toWorld = r.transform.localToWorldMatrix; // round 53: the whole matrix, scale included
                foreach (var v in baked.vertices) { best = UnityEngine.Mathf.Max(best, toWorld.MultiplyPoint3x4(v).y); }
            }

            return best - probe.transform.position.y;
        };
        float rest = top();
        System.Func<Game.Core.ModelClipRole, float> cycle = role =>
        {
            int index = art.IndexOf(role);
            if (index < 0) return float.NaN;
            var clip = art.Clips[index].Clip;
            float best = float.MinValue;
            for (int i = 0; i < Samples; i++)
            {
                clip.SampleAnimation(probe, clip.length * i / Samples);
                best = UnityEngine.Mathf.Max(best, top());
            }

            return best;
        };
        float move = cycle(Game.Core.ModelClipRole.Move);
        float idle = cycle(Game.Core.ModelClipRole.Idle);
        float worst = UnityEngine.Mathf.Max(float.IsNaN(move) ? rest : move, float.IsNaN(idle) ? rest : idle);
        float ratio = worst / art.Height;
        string row = $"{def.name} | {art.name} | {def.IsBoss} | {art.Height:0.000} | {rest:0.000} | {move:0.000} | {idle:0.000} | {ratio:0.00}";
        sb.AppendLine(row);
        if (ratio > Tolerance) flagged.Append(row).Append("; ");
        UnityEngine.Object.DestroyImmediate(baked);
    }
    finally
    {
        UnityEngine.Object.DestroyImmediate(probe);
    }
}

System.IO.File.WriteAllText("Logs/r49s3_height_scan.txt", sb.ToString());
return flagged.Length == 0 ? "none flagged" : flagged.ToString();
