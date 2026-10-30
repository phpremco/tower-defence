// Advances a hundred frames while the Editor is unfocused, with time running three times as fast, so creatures
// spread out along the road between screenshots. Run through: unity command eval_file --file Tools/PlayTest/step100.cs
UnityEngine.Time.timeScale = 3f;
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 100; i++)
{
    UnityEditor.EditorApplication.Step();
}

UnityEngine.Time.timeScale = 1f;
return "frame " + UnityEngine.Time.frameCount + " time " + UnityEngine.Time.time.ToString("0.0");
