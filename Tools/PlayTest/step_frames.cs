// Advances the paused editor a number of frames from Tools/PlayTest/frames_arg.txt, so the map animates while the editor is unfocused.
// "<frames> hold" leaves the editor paused afterwards, so game time only moves in steps (no free frames between CLI calls).
string[] stepArgs = System.IO.File.ReadAllText("Tools/PlayTest/frames_arg.txt").Trim().Split(' ');
int frames = int.Parse(stepArgs[0]);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < frames; i++) { UnityEditor.EditorApplication.Step(); }
UnityEditor.EditorApplication.isPaused = stepArgs.Length > 1 && stepArgs[1] == "hold";
return "stepped " + frames;
