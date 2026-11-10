var pc = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var c = (Game.Levels.LevelContext)typeof(Game.Levels.PlacementController).GetField("_context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(pc);
c.Wallet.Add(int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/qa/money_arg.txt").Trim()));
return "money " + c.Wallet.Money;
