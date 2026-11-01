// Advances the paused editor a number of frames from Tools/PlayTest/frames_arg.txt, so the map animates while the editor is unfocused.
int frames = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/frames_arg.txt").Trim());
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < frames; i++) { UnityEditor.EditorApplication.Step(); }
UnityEditor.EditorApplication.isPaused = false;
return "stepped " + frames;
