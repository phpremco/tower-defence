// Round 64: the frame hitch of starting a level. Mode from Tools/PlayTest/r64_hitch_arg.txt:
//  "old <node> <difficulty>"  installs a per-frame recorder (Application.onBeforeRender, real time) and starts the level the way
//                             the map did before round 64 (LevelSession + SceneManager.LoadScene, one frame builds everything);
//  "new <node> <difficulty>"  the same recorder, the level started through Game.UI.LevelLoader (the loading screen);
//  "stop"                     removes the recorder and writes Logs/r64/hitch_<label>.txt: every frame over 50 ms after the start
//                             (seconds after the start, length, active scene), the longest frame and the time to the first
//                             frame of the Level scene with a LevelContext.
// Play mode must be running from Boot (the main menu or the map on screen).
string[] hitchArgs = System.IO.File.ReadAllText("Tools/PlayTest/r64_hitch_arg.txt").Trim().Split(' ');
var hitchDomain = System.AppDomain.CurrentDomain;
var hitchOld = hitchDomain.GetData("r64hitch") as UnityEngine.Events.UnityAction;
if (hitchOld != null) { UnityEngine.Application.onBeforeRender -= hitchOld; hitchDomain.SetData("r64hitch", null); }
var hitchInv = System.Globalization.CultureInfo.InvariantCulture;
if (hitchArgs[0] == "stop")
{
    var report = hitchDomain.GetData("r64hitchout") as System.Func<string>;
    if (report == null) return "no recording";
    string text = report();
    System.IO.Directory.CreateDirectory("Logs/r64");
    System.IO.File.WriteAllText("Logs/r64/hitch_" + (hitchArgs.Length > 1 ? hitchArgs[1] : "run") + ".txt", text);
    hitchDomain.SetData("r64hitchout", null);
    return text.Replace("\n", " | ");
}

var frames = new System.Collections.Generic.List<string>();
double[] state = { UnityEngine.Time.realtimeSinceStartupAsDouble, 0, -1, 0, 0, -1 }; // last time, longest, level ready at, frames, start, loading screen last seen
state[4] = state[0];
int[] lastFrame = { UnityEngine.Time.frameCount };
UnityEngine.Events.UnityAction tick = () =>
{
    if (UnityEngine.Time.frameCount == lastFrame[0]) return;
    lastFrame[0] = UnityEngine.Time.frameCount;
    double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
    double length = now - state[0];
    state[0] = now;
    state[3] += 1;
    string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    if (length > state[1]) state[1] = length;
    var screen = UnityEngine.Object.FindAnyObjectByType<Game.UI.LoadingScreen>();
    string step = screen != null ? " reported " + screen.Reported.ToString("0.00", hitchInv) + " shown " + screen.Shown.ToString("0.00", hitchInv) : "";
    if (length > 0.05) frames.Add((now - state[4]).ToString("0.00", hitchInv) + "s " + (length * 1000).ToString("0", hitchInv) + "ms " + scene + step);
    if (screen != null) { state[5] = now - state[4]; }
    if (state[2] < 0 && scene == Game.Core.SceneNames.Level && Game.Core.Services.TryGet(out Game.Levels.LevelContext context) && context != null) state[2] = now - state[4];
};
UnityEngine.Application.onBeforeRender += tick;
hitchDomain.SetData("r64hitch", tick);
hitchDomain.SetData("r64hitchout", (System.Func<string>)(() =>
    "longest frame " + (state[1] * 1000).ToString("0", hitchInv) + " ms\nlevel ready after " + state[2].ToString("0.00", hitchInv) + " s\nframes " + state[3].ToString("0", hitchInv)
    + "\nloading screen gone after " + state[5].ToString("0.00", hitchInv) + " s\nframes over 50 ms: " + frames.Count + "\n" + string.Join("\n", frames)));

var hitchCatalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
var hitchLevel = hitchCatalog.Nodes[int.Parse(hitchArgs[1])].Level;
var hitchDifficulty = hitchCatalog.Difficulties[int.Parse(hitchArgs[2])];
if (hitchArgs[0] == "old")
{
    Game.Core.Services.Register(new Game.Levels.LevelSession(hitchLevel, hitchDifficulty, true, null));
    UnityEngine.SceneManagement.SceneManager.LoadScene(Game.Core.SceneNames.Level);
    return "old load of " + hitchLevel.name + " on " + hitchDifficulty.name;
}

var loader = System.Type.GetType("Game.UI.LevelLoader, Game.UI");
if (loader == null) return "no Game.UI.LevelLoader";
loader.GetMethod("Begin", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { hitchLevel, hitchDifficulty, null });
return "new load of " + hitchLevel.name + " on " + hitchDifficulty.name;
