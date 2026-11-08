// Round 23c: shows the result screen with a chosen outcome, without playing the level out, so its text can be checked.
// Tools/PlayTest/r23c_result.txt: "earned" (a win already paid on this difficulty), "earnedunlocks" (the same with the
// gadget line of every level's gadgets under it), "unlocks" (a win with points, a new weapon and a story) or "lost".
// Writes no save: only the view is shown.
string mode = System.IO.File.ReadAllText("Tools/PlayTest/r23c_result.txt").Trim();
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var result = new Game.Levels.LevelResult { Won = mode != "lost", Stars = 2, ResearchPoints = 140, PowerCurrency = 12 };
if (mode.StartsWith("earned")) { result.RewardsAlreadyEarned = true; }
if (mode == "earnedunlocks")
{
    var gadgets = new System.Collections.Generic.List<Game.PathObjects.PathObjectDefinition>();
    foreach (var node in Game.Core.Services.Get<Game.Levels.CampaignCatalog>().Nodes)
    {
        if (node.Level == null || node.Level.PathObjects == null) { continue; }
        foreach (var gadget in node.Level.PathObjects) { if (gadget != null && !gadgets.Contains(gadget)) { gadgets.Add(gadget); } }
    }
    result.NewPathObjects = gadgets.ToArray();
    result.Credit = "Stolen alien technology";
}
if (mode == "unlocks")
{
    foreach (var tower in context.Level.AvailableTowers) { result.NewTower = tower; }
    result.Credit = "Intel from the home planet";
    result.Story = "Engineers on the orbital platform pieced this design together from the wrecks we recovered at the landing site. It should hold the line against the heavier creatures that are now coming up from the south.";
}
context.Result = result;
context.Outcome = result.Won ? Game.Levels.LevelOutcome.Won : Game.Levels.LevelOutcome.Lost;
context.Changed.Raise();
return "result " + mode + " shown, gadgets " + result.NewPathObjects.Length;
