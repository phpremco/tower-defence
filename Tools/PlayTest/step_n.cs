// Advances the number of frames in Tools/PlayTest/step_arg.txt ("frames timescale"), for catching short animations.
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/step_arg.txt").Trim().Split(' ');
int frames = int.Parse(args[0]);
UnityEngine.Time.timeScale = args.Length > 1 ? float.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 1f;
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < frames; i++)
{
    UnityEditor.EditorApplication.Step();
}

UnityEngine.Time.timeScale = 1f;
return "frame " + UnityEngine.Time.frameCount + " time " + UnityEngine.Time.time.ToString("0.00");
