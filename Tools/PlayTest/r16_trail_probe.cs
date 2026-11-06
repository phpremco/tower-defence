// Round 16: reports the boss's ground trail ribbon (LavaTrail / RotTrail): live patches, ribbon vertices and bounds, the
// boss pivot and the ground height under it, to see whether the ribbon is drawn and where its fresh end lies.
var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>();
string result = brain != null ? "boss " + brain.State + " at " + brain.transform.position.ToString("0.00") + "\n" : "no boss\n";
foreach (var view in UnityEngine.Object.FindObjectsByType<Game.Bosses.LavaTrailView>(UnityEngine.FindObjectsSortMode.None))
{
    var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    var trail = (Game.Bosses.LavaTrail)typeof(Game.Bosses.LavaTrailView).GetField("_trail", flags).GetValue(view);
    var mesh = (UnityEngine.Mesh)typeof(Game.Bosses.LavaTrailView).GetField("_mesh", flags).GetValue(view);
    int live = 0; for (int i = 0; i < trail.Count; i++) { if (trail.IsLive(trail.SlotInOrder(i))) { live++; } }
    result += view.name + " live " + live + "/" + trail.Count + " walking " + trail.IsWalking + " head " + trail.Head.ToString("0.00");
    if (mesh != null) { var r = view.GetComponentInChildren<UnityEngine.MeshRenderer>(); result += " verts " + mesh.vertexCount + " bounds " + r.bounds.ToString() + " mat " + r.sharedMaterial.name; }
    if (brain != null)
    {
        var field = typeof(Game.Bosses.ContinentBossBrain).GetProperty("Field", flags).GetValue(brain);
        var ground = field.GetType().GetMethod("GroundAt");
        result += " ground at head " + ((float)ground.Invoke(field, new object[] { trail.Head })).ToString("0.00") + " at pivot " + ((float)ground.Invoke(field, new object[] { brain.transform.position })).ToString("0.00");
    }
    result += "\n";
}
return result;
