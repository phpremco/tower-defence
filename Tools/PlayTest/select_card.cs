// Picks the tower card whose index is in Tools/PlayTest/card_arg.txt, as a click on the card would, so the
// preview should appear at the cursor straight away.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

int index = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/card_arg.txt").Trim());
context.SelectTower(context.AvailableTowers[index]);
return "selected " + context.SelectedTower.name + " mode " + context.Mode;
