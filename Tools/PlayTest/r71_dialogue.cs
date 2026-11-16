// Round 71: queues a real dialogue asset by name (Tools/PlayTest/r71_dialogue_arg.txt, e.g. Dlg_L02_RepairDrone) on the
// running level, so a line with button tokens can be seen. The player marks it seen: restore the save afterwards.
string name = System.IO.File.ReadAllText("Tools/PlayTest/r71_dialogue_arg.txt").Trim();
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context) || context.Dialogue == null)
{
    return "no context";
}

string[] found = UnityEditor.AssetDatabase.FindAssets(name + " t:DialogueDefinition");
foreach (string guid in found)
{
    var dialogue = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.DialogueDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    if (dialogue != null && dialogue.name == name)
    {
        context.Dialogue.Clear();
        context.Dialogue.Enqueue(dialogue);
        return "queued " + name;
    }
}

return "not found " + name;
