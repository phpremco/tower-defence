// Round 51: an edit-mode lab for the beam strip's shape: lays a LineKit curve (the ray boss's halo width) with a bend near its
// source in front of an orthographic camera at the game's pitch and renders it in flat white, once with round corners and
// once without, side by side into the PNG named in Tools/PlayTest/look_arg.txt. Leaves nothing behind.
var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
var root = new UnityEngine.GameObject("LineLab");
var camGo = new UnityEngine.GameObject("LabEye");
camGo.transform.SetParent(root.transform, false);
var eye = camGo.AddComponent<UnityEngine.Camera>();
eye.orthographic = true;
eye.orthographicSize = 2.2f;
eye.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
eye.backgroundColor = UnityEngine.Color.black;
eye.transform.rotation = UnityEngine.Quaternion.Euler(44f, 0f, 0f);
var centre = new UnityEngine.Vector3(1.5f, 1.2f, 0f);
eye.transform.position = centre - eye.transform.forward * 20f;
var material = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Unlit"));
material.color = UnityEngine.Color.white;
var image = new UnityEngine.Texture2D(1600, 800, UnityEngine.TextureFormat.RGB24, false);
var texture = new UnityEngine.RenderTexture(800, 800, 24);
eye.targetTexture = texture;
var spec = System.IO.File.Exists("Tools/PlayTest/r51_lab.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r51_lab.txt").Trim().Split(' ') : new[] { "0", "-1", "0" };
var lead = new UnityEngine.Vector3(float.Parse(spec[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(spec[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(spec[2], System.Globalization.CultureInfo.InvariantCulture)).normalized;
string note = "";
for (int pass = 0; pass < 2; pass++)
{
    var go = new UnityEngine.GameObject("Line" + pass);
    go.transform.SetParent(root.transform, false);
    var line = go.AddComponent<UnityEngine.LineRenderer>();
    line.useWorldSpace = true;
    line.widthMultiplier = 0.95f;
    Game.Core.LineKit.ShapeEnds(line);
    line.sharedMaterial = material;
    line.numCornerVertices = pass == 0 ? Game.Core.LineKit.CornerVertices : 0;
    var origin = new UnityEngine.Vector3(0f, 1.9f, 0f);
    Game.Core.LineKit.Curve(line, origin, origin + lead * (spec.Length > 3 ? float.Parse(spec[3], System.Globalization.CultureInfo.InvariantCulture) : 1.8f), new UnityEngine.Vector3(4f, 0.5f, -0.5f));
    eye.Render();
    UnityEngine.RenderTexture.active = texture;
    image.ReadPixels(new UnityEngine.Rect(0, 0, 800, 800), pass * 800, 0);
    UnityEngine.RenderTexture.active = null;
    note += " pass" + pass + " corners " + line.numCornerVertices;
    UnityEngine.Object.DestroyImmediate(go);
}

image.Apply();
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
eye.targetTexture = null;
texture.Release();
UnityEngine.Object.DestroyImmediate(root);
UnityEngine.Object.DestroyImmediate(material);
UnityEngine.Object.DestroyImmediate(image);
return "wrote lab" + note + " lead " + lead.ToString("0.00");
