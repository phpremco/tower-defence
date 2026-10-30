// Advances ten frames while the Editor is unfocused. Run through: unity command eval_file --file Tools/PlayTest/step10.cs
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 10; i++)
{
    UnityEditor.EditorApplication.Step();
}

return "frame " + UnityEngine.Time.frameCount;
