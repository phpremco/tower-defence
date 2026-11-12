// Round 51: an edit-mode lab for the cutscene's SpaceKit lines: draws the same plasma-trail line (the capsule trail's width
// curve and a gradient that fades its colour and alpha to the tail) twice in front of an orthographic camera, once in this
// round's SpaceKit material and once in the round-49 material (the URP default particle material set additive), and reports
// the summed brightness of each and of their far halves; writes both side by side into the PNG in look_arg.txt.
var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
float LabIntensity = System.IO.File.Exists("Tools/PlayTest/r51_lab.txt") ? float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r51_lab.txt").Trim().Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture) : 2f;
var library = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Combat.VfxLibrary>("Assets/_Project/Data/Vfx/VfxLibrary.asset");
bool had = Game.Core.Services.TryGet(out Game.Core.MarkerArt before);
Game.Core.Services.Register(library.Markers);
var fx = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CutsceneFxSet>("Assets/_Project/Data/Art/Story/CutsceneFxSet.asset");
var fxBefore = Game.Levels.CutsceneFxSet.Active;
Game.Levels.CutsceneFxSet.Active = fx;
var root = new UnityEngine.GameObject("SpaceLab");
var eye = new UnityEngine.GameObject("LabEye").AddComponent<UnityEngine.Camera>();
eye.transform.SetParent(root.transform, false);
eye.orthographic = true; eye.orthographicSize = 1.5f;
eye.clearFlags = UnityEngine.CameraClearFlags.SolidColor; eye.backgroundColor = UnityEngine.Color.black;
eye.transform.position = new UnityEngine.Vector3(1.5f, 0f, -10f);
eye.cullingMask = 1 << 31;
var texture = new UnityEngine.RenderTexture(600, 600, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
eye.targetTexture = texture;
var image = new UnityEngine.Texture2D(1200, 600, UnityEngine.TextureFormat.RGB24, false);
var gradient = new UnityEngine.Gradient();
gradient.SetKeys(new[] { new UnityEngine.GradientColorKey(new UnityEngine.Color(1f, 0.8f, 0.6f), 0f), new UnityEngine.GradientColorKey(new UnityEngine.Color(1f, 0.3f, 0.1f), 1f) }, new[] { new UnityEngine.GradientAlphaKey(1f, 0f), new UnityEngine.GradientAlphaKey(0f, 1f) });
string report = "";
for (int pass = 0; pass < 2; pass++)
{
    var line = Game.UI.SpaceKit.Line(root.transform, "Lab" + pass, 8, false, LabIntensity, Game.Levels.CutsceneSprite.PlasmaTrail);
    line.gameObject.layer = 31;
    line.colorGradient = gradient;
    line.widthCurve = new UnityEngine.AnimationCurve(new UnityEngine.Keyframe(0f, 0.3f), new UnityEngine.Keyframe(1f, 0f));
    for (int i = 0; i < 8; i++) { line.SetPosition(i, new UnityEngine.Vector3(i * 3f / 7f, 0f, 0f)); }
    if (pass == 1)
    {
        var original = line.sharedMaterial;
        var old = new UnityEngine.Material(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultParticleMaterial);
        old.SetFloat("_Surface", 1f); old.SetFloat("_Blend", 2f);
        old.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One); old.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        old.SetFloat("_ZWrite", 0f); old.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        old.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        old.SetTexture("_BaseMap", original.GetTexture("_MainTex"));
        old.SetColor("_BaseColor", UnityEngine.Color.white * LabIntensity);
        line.sharedMaterial = old;
    }

    eye.Render();
    UnityEngine.RenderTexture.active = texture;
    var read = new UnityEngine.Texture2D(600, 600, UnityEngine.TextureFormat.RGBAHalf, false);
    read.ReadPixels(new UnityEngine.Rect(0, 0, 600, 600), 0, 0);
    read.Apply();
    UnityEngine.RenderTexture.active = null;
    float near = 0f, far = 0f;
    var pixels = read.GetPixels();
    for (int y = 0; y < 600; y++)
    {
        for (int x = 0; x < 600; x++)
        {
            var c = pixels[y * 600 + x];
            float l = c.r + c.g + c.b;
            if (x < 300) { near += l; } else { far += l; }
            image.SetPixel(pass * 600 + x, y, c);
        }
    }

    report += (pass == 0 ? " r51" : " r49") + " near " + near.ToString("0") + " far " + far.ToString("0");
    UnityEngine.Object.DestroyImmediate(read);
    UnityEngine.Object.DestroyImmediate(line.gameObject);
}

image.Apply();
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
eye.targetTexture = null; texture.Release();
UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(image);
Game.Levels.CutsceneFxSet.Active = fxBefore;
if (had) { Game.Core.Services.Register(before); } else { Game.Core.Services.Unregister<Game.Core.MarkerArt>(); }
return "space lab:" + report;
