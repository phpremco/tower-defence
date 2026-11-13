// Round 59: the pathfinder capsule's old lane (CapsuleDrop.Target for index 0), so its data keeps the round-44 path.
float lane = (Game.UI.CutsceneMath.Hash(0, 63) - 0.5f) * 0.9f;
float x = lane * 0.7f;
float z = 0.25f + Game.UI.CutsceneMath.Hash(0, 65) * 0.2f;
return "x " + x.ToString("R") + " z " + z.ToString("R") + " h63 " + Game.UI.CutsceneMath.Hash(0, 63).ToString("R") + " h65 " + Game.UI.CutsceneMath.Hash(0, 65).ToString("R");
