// Round 58: for every living creature named in enemy_arg.txt (first word): the clip on top and its time, the bar's height now
// (_barY) against the walk's (_barHeight), and on the 1920x1080 still framing the bar's screen row against the topmost
// pixel row of the halo (vertices skinned mostly to Ring_1..3) and of the whole body; "clear" when the bar's lower edge
// (its plate) stands above the halo's top.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ')[0];
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var camera = UnityEngine.Camera.main;
float aspect = camera.aspect;
camera.aspect = 1920f / 1080f;
var sb = new System.Text.StringBuilder();
var baked = new UnityEngine.Mesh();
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != wanted) { continue; }
    var view = enemy.GetComponent<Game.Enemies.EnemyView>();
    var model = (Game.Enemies.CreatureModel)typeof(Game.Enemies.EnemyView).GetField("_model", flags).GetValue(view);
    var bar = (Game.Core.WorldBar)typeof(Game.Enemies.EnemyView).GetField("_worldBar", flags).GetValue(view);
    float barY = (float)typeof(Game.Enemies.EnemyView).GetField("_barY", flags).GetValue(view);
    float barHeight = (float)typeof(Game.Enemies.EnemyView).GetField("_barHeight", flags).GetValue(view);
    var player = model.Player;
    sb.Append(wanted).Append(" once=").Append(player.Once >= 0 ? model.Art.Clips[player.Once].Clip.name + "@" + player.OnceTime.ToString("0.00") : "none");
    sb.Append(" barY ").Append(barY.ToString("0.00")).Append(" walkBar ").Append(barHeight.ToString("0.00"));
    System.Func<UnityEngine.Vector3, float> row = p => (1f - camera.WorldToViewportPoint(p).y) * 1080f;
    // The plate is about 3.2 bar heights tall round the line; its lower edge sits ~1.6 bar heights under the line's centre.
    float barRow = row(bar.Root.position - bar.Root.up * 0.085f * 1.6f);
    // Where the plate would be with the walk's bar (no lift), as before round 58.
    float walkRow = row(bar.Root.position - UnityEngine.Vector3.up * (barY - barHeight) - bar.Root.up * 0.085f * 1.6f);
    float haloTop = float.MaxValue, bodyTop = float.MaxValue;
    foreach (var renderer in model.Root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(false))
    {
        if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) { continue; }
        renderer.BakeMesh(baked, true);
        var bones = renderer.bones;
        var weights = renderer.sharedMesh.boneWeights;
        var vertices = baked.vertices;
        var toWorld = renderer.transform.localToWorldMatrix;
        for (int v = 0; v < vertices.Length; v += 3)
        {
            float r = row(toWorld.MultiplyPoint3x4(vertices[v]));
            bodyTop = UnityEngine.Mathf.Min(bodyTop, r);
            var bone = v < weights.Length ? bones[weights[v].boneIndex0] : null;
            if (bone != null && bone.name.StartsWith("Ring_")) { haloTop = UnityEngine.Mathf.Min(haloTop, r); }
        }
    }

    var barScreen = camera.WorldToViewportPoint(bar.Root.position);
    sb.Append(" barPx ").Append((barScreen.x * 1920f).ToString("0")).Append(",").Append(((1f - barScreen.y) * 1080f).ToString("0"));
    sb.Append(" | rows: bar plate bottom ").Append(barRow.ToString("0")).Append(" halo top ").Append(haloTop.ToString("0")).Append(" body top ").Append(bodyTop.ToString("0"));
    sb.Append(barRow < haloTop ? " clear" : " CROSSED").Append(" by ").Append((haloTop - barRow).ToString("0")).Append(" px; walk bar would be ").Append(walkRow < haloTop ? "clear" : "CROSSED").Append(" by ").Append((haloTop - walkRow).ToString("0")).Append(" px; ");
}

UnityEngine.Object.DestroyImmediate(baked);
camera.aspect = aspect;
return sb.Length == 0 ? "no living " + wanted : sb.ToString();
