// Round 23a: the level camera's size and position and whether the camera is blocked (codex open), to check that
// input meant for the codex does not move the view.
var camera = UnityEngine.Camera.main;
Game.Core.Services.TryGet(out Game.Levels.LevelContext context);
return "size " + camera.orthographicSize.ToString("0.000") + " pos " + camera.transform.position.ToString("F2") + " blocked " + (context != null && context.CameraBlocked) + " held " + (context != null && context.Held);
