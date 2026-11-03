// Queues a sample conversation on the running level's dialogue player, for screenshots of the box. The sample name is
// in Tools/PlayTest/dialogue_arg.txt: mercer, ora, voss, nti, pioneer or fx (redacted, struck and a glitching line).
// The level must be running; the player marks the sample as seen, so restore the save afterwards.
string sample = System.IO.File.ReadAllText("Tools/PlayTest/dialogue_arg.txt").Trim();
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context) || context.Dialogue == null)
{
    return "no context";
}

System.Func<string, Game.Levels.SpeakerDefinition> speaker = n => UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.SpeakerDefinition>("Assets/_Project/Data/Story/Speakers/Speaker_" + n + ".asset");
System.Func<string, Game.Levels.SegmentStyle, Game.Levels.DialogueSegment> seg = (t, s) => new Game.Levels.DialogueSegment(t, s);
var normal = Game.Levels.SegmentStyle.Normal;
var lines = new System.Collections.Generic.List<Game.Levels.DialogueLine>();
System.Action<string, float, Game.Levels.DialogueSegment[]> say = (who, glitch, segments) =>
    lines.Add(new Game.Levels.DialogueLine { Id = "probe_" + lines.Count, Speaker = speaker(who), Segments = segments, GlitchIntensity = glitch });

switch (sample)
{
    case "mercer":
        say("Mercer", 0f, new[] { seg("And whatever gets through is mine. Keep the turrets fed and the road clear, and leave the big ones to me.", normal) });
        break;
    case "ora":
        say("Ora", 0f, new[] { seg("Hostile wave approaching. Place turrets along the path. I will mark the landing zones as they come in.", normal) });
        break;
    case "voss":
        say("Voss", 0f, new[] { seg("Those readings are not geological, Commander. Something under the crust is answering our drills, and it is getting louder every hour.", normal) });
        break;
    case "nti":
        say("Nti", 0f, new[] { seg("Quarterly extraction targets remain unchanged. Local resistance is a ", normal), seg("security", Game.Levels.SegmentStyle.Struck), seg(" logistics matter. Proceed.", normal) });
        break;
    case "pioneer":
        say("Pioneer1", 0f, new[] { seg("Day 412. Seismic pulse again at 03:10. Pattern matches the signal from site ", normal), seg("K-7", Game.Levels.SegmentStyle.Redacted), seg(". Recommend the colony ships turn back.", normal) });
        break;
    default:
        say("Ora", 0.6f, new[] { seg("Protocol ", normal), seg("Omega", Game.Levels.SegmentStyle.Redacted), seg(" is not ", normal), seg("available", Game.Levels.SegmentStyle.Struck), seg(" authorised. Please disregard the last ", normal), seg("seventeen", Game.Levels.SegmentStyle.Redacted), seg(" transmissions.", normal) });
        break;
}

var dialogue = UnityEngine.ScriptableObject.CreateInstance<Game.Levels.DialogueDefinition>();
dialogue.name = "Probe_" + sample;
typeof(Game.Levels.DialogueDefinition).GetField("_lines", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(dialogue, lines.ToArray());
context.Dialogue.Clear();
context.Dialogue.Enqueue(dialogue);
return "queued " + dialogue.name + " presenter " + (context.Dialogue.Presenter == null ? "none" : context.Dialogue.Presenter.GetType().Name);
