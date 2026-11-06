// Round 15: opens the codex's enemy page on the entry whose name is in r15_codex_name.txt, steps a few frames so the
// turntable settles, and writes the entry's preview (the display stage's own render texture) to the PNG in look_arg.txt.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/r15_codex_name.txt").Trim();
var codex = UnityEngine.Object.FindAnyObjectByType<Game.UI.CodexView>();
if (codex == null) { return "no codex view"; }
codex.Open();
var type = typeof(Game.UI.CodexView);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var pageType = type.GetNestedType("Page", flags);
type.GetMethod("ShowPage", flags).Invoke(codex, new[] { System.Enum.Parse(pageType, "Enemies") });
var shown = (System.Collections.Generic.List<Game.Enemies.EnemyDefinition>)type.GetField("_shownEnemies", flags).GetValue(codex);
int row = shown.FindIndex(e => e.name == wanted);
var previewList = (System.Collections.Generic.List<Game.UI.ModelPreview>)type.GetField("_previews", flags).GetValue(codex);
if (row >= 0) { type.GetMethod("Show", flags).Invoke(codex, new object[] { row }); }
else
{
    // A boss the save has not met is not listed: its preview is shown straight from the catalog (the save stays untouched).
    var catalog = type.GetField("_catalog", flags).GetValue(codex);
    var enemies = (System.Collections.Generic.IReadOnlyList<Game.Enemies.EnemyDefinition>)catalog.GetType().GetProperty("Enemies").GetValue(catalog);
    Game.Enemies.EnemyDefinition found = null;
    foreach (var e in enemies) { if (e != null && e.name == wanted) { found = e; } }
    if (found == null) { return "no entry " + wanted; }
    type.GetMethod("Show", flags).Invoke(codex, new object[] { 0 });
    previewList[0].ShowEnemy(found);
}
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 12; i++) { UnityEditor.EditorApplication.Step(); }
var previews = (System.Collections.Generic.List<Game.UI.ModelPreview>)type.GetField("_previews", flags).GetValue(codex);
var texture = (UnityEngine.RenderTexture)previews[0].GetComponent<UnityEngine.UI.RawImage>().texture;
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(texture.width, texture.height, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, texture.width, texture.height), 0, 0);
image.Apply();
UnityEngine.RenderTexture.active = null;
string path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
return "wrote " + wanted + " row " + row + " to " + path;
