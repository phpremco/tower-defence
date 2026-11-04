// Samples each clip of the cutscene drones and reports how far the rig's Root and Body tilt from level, to see whether a clip rocks the drone.
var sb = new System.Text.StringBuilder();
foreach (var path in new[] { "Assets/_Project/Data/Art/Story/Drone_Hauler.asset", "Assets/_Project/Data/Art/Story/Drone_Builder.asset" })
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>(path);
    var go = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(art.Model);
    foreach (var clip in art.Clips)
    {
        float worst = 0f;
        for (int i = 0; i <= 20; i++)
        {
            clip.Clip.SampleAnimation(go, clip.Length * i / 20f);
            foreach (var t in go.GetComponentsInChildren<UnityEngine.Transform>())
            {
                if (t.name != "Body" && t.name != "Root") continue;
                worst = UnityEngine.Mathf.Max(worst, UnityEngine.Vector3.Angle(t.up, UnityEngine.Vector3.up));
            }
        }
        sb.Append(art.name + " " + clip.Role + " max tilt " + worst.ToString("0.0") + "; ");
    }
    UnityEngine.Object.DestroyImmediate(go);
}
return sb.ToString();
