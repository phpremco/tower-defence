// QA round 32: the placement state of a running level (mode, cursor, selection, menus, pause, money, towers).
var pc = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var f = typeof(Game.Levels.PlacementController).GetField("_context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var c = (Game.Levels.LevelContext)f.GetValue(pc);
var cur = typeof(Game.Levels.PlacementController).GetField("_cursor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(pc);
var towers = (System.Collections.IDictionary)typeof(Game.Levels.PlacementController).GetField("_towers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(pc);
string sel = c.SelectedTower != null ? c.SelectedTower.DisplayName : "none";
string obj = c.SelectedPathObject != null ? c.SelectedPathObject.DisplayName : "none";
string placed = c.SelectedPlacedTower != null ? c.SelectedPlacedTower.Definition.DisplayName + " L" + c.SelectedPlacedTower.LevelIndex + " spent " + c.SelectedPlacedTower.TotalSpent : "none";
return "mode=" + c.Mode + " cursor=" + cur + " tower=" + sel + " gadget=" + obj + " placed=" + placed + " heroMenu=" + c.HeroMenuPost + " baseMenu=" + c.BaseMenuOpen + " paused=" + c.Paused + " blocked=" + c.InputBlocked + " money=" + c.Wallet.Money + " towers=" + towers.Count + " status='" + c.Status + "' ts=" + UnityEngine.Time.timeScale;
