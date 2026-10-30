Game.Core.Services.TryGet(out Game.Levels.LevelContext context);
var level = context.Level;
var field = new Game.Levels.TerrainField(level, new Game.Levels.RoadShape(level));
var cam = UnityEngine.Camera.main;
var foot = Game.Levels.FixedCameraRig.GroundFootprint(level, cam.aspect);
return "world " + level.WorldSize + " margins " + field.MarginCellsX + "x" + field.MarginCellsZ + " footprint " + foot + " ortho " + cam.orthographicSize.ToString("0.0") + " aspect " + cam.aspect.ToString("0.00") + " pixels " + cam.pixelWidth + "x" + cam.pixelHeight + " camPos " + cam.transform.position.ToString("0.0");
