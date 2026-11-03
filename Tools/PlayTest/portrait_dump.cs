// Writes the portrait the dialogue box shows now, at its true size and proportions, to the file named in
// Tools/PlayTest/portrait_arg.txt (an absolute path).
var view = UnityEngine.Object.FindAnyObjectByType<Game.UI.DialogueView>();
if (view == null)
{
    return "no view";
}

var image = (UnityEngine.UI.Image)typeof(Game.UI.DialogueView).GetField("_portrait", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(view);
if (image == null || image.sprite == null)
{
    return "no portrait";
}

string path = System.IO.File.ReadAllText("Tools/PlayTest/portrait_arg.txt").Trim();
System.IO.File.WriteAllBytes(path, UnityEngine.ImageConversion.EncodeToPNG(image.sprite.texture));
return "wrote " + path;
