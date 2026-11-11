// Round 42: seeks across the opening and lists the playing sources after each, to show a seek neither stacks loops nor
// leaves the last set's loops running (orbit -> cave -> gate -> orbit again).
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var sb = new System.Text.StringBuilder();
int[] shots = { 2, 8, 14, 2, 2 };
float[] at = { 0.3f, 0.5f, 0.5f, 0.3f, 0.3f };
for (int i = 0; i < shots.Length; i++)
{
    player.Seek(shots[i], at[i]);
    sb.Append("{seek ").Append(shots[i]).Append("@").Append(at[i]).Append(": ");
    int playing = 0;
    foreach (var s in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(UnityEngine.FindObjectsSortMode.None))
    {
        if (!s.isPlaying) continue;
        playing++;
        sb.Append(s.gameObject.name).Append("@").Append(s.volume.ToString("0.00")).Append(" ");
    }
    sb.Append("(").Append(playing).Append(" playing)} ");
}
return sb.ToString();
