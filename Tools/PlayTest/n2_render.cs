// Night 2: renders the main camera as it is now (no seek) at 1920x1080 into the PNG named in Tools/PlayTest/look_arg.txt.
var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
var camera = UnityEngine.Camera.main;
var texture = new UnityEngine.RenderTexture(1920, 1080, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
float aspect = camera.aspect;
camera.targetTexture = texture;
camera.aspect = 1920f / 1080f;
camera.Render();
camera.targetTexture = null;
camera.aspect = aspect;
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0);
image.Apply();
UnityEngine.RenderTexture.active = null;
texture.Release();
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
return "wrote " + path + (player != null ? " " + player.Sequencer.Current.Action + " t=" + player.Sequencer.LocalTime.ToString("0.00") : "");
