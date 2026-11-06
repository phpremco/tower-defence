// Round 16: with "old" in r16_furrow_arg.txt the boss's lava or rot ribbon stops where the trail is laid (behind the
// body, as before round 16); with "new" it runs on in under the body again. Takes effect on the next frame.
string mode = System.IO.File.ReadAllText("Tools/PlayTest/r16_furrow_arg.txt").Trim();
var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>();
int n = 0;
foreach (var view in UnityEngine.Object.FindObjectsByType<Game.Bosses.LavaTrailView>(UnityEngine.FindObjectsSortMode.None))
{
    view.ReachUnder(mode == "old" || brain == null ? null : brain.transform);
    n++;
}
return mode + " on " + n + " trail views";
