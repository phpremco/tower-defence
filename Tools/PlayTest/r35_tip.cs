// Round 35: drives Voss's gate tips in a level. Tools/PlayTest/r35_tip_arg.txt holds one mode:
// "setup" (no briefing, no recon, waves off, the queue cleared), "upgrade" (money for a gate level, gate 0 to 50 %),
// "move" (Spencer to post 0, gate 0 whole, gate 1 to 50 %), "skip" (ends the conversation on show), "state".
string mode = System.IO.File.ReadAllText("Tools/PlayTest/r35_tip_arg.txt").Trim();
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext c)) return "no context";
var placement = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var gates = placement.Blockades;
var p = c.Dialogue;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
if (mode == "deploy")
{
    var briefing = UnityEngine.Object.FindAnyObjectByType<Game.UI.BriefingView>();
    if (briefing != null) typeof(Game.UI.BriefingView).GetMethod("Deploy", flags).Invoke(briefing, null);
}
else if (mode == "setup")
{
    // The recon fly-over holds the clock until it finishes; finish it as a player's skip would.
    var recon = UnityEngine.Object.FindAnyObjectByType<Game.Levels.ReconView>();
    if (recon != null) typeof(Game.Levels.ReconView).GetMethod("Finish", flags).Invoke(recon, null);
    UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>().enabled = false;
    p.Clear();
}
else if (mode == "upgrade")
{
    c.Wallet.Add(gates[0].NextUpgradeCost + 50);
    var g = gates[0];
    g.TakeDamage(g.Health.Current - g.Health.Max * 0.5f);
    c.StructureHit.Raise(g.Position);
}
else if (mode == "move")
{
    c.Hero.MoveTo(0);
    gates[0].Health.Heal(gates[0].Health.Max);
    var g = gates[gates.Count > 1 ? 1 : 0];
    g.TakeDamage(g.Health.Current - g.Health.Max * 0.5f);
    c.StructureHit.Raise(g.Position);
}
else if (mode == "skip")
{
    p.SkipConversation();
}
var sb = new System.Text.StringBuilder();
for (int i = 0; i < gates.Count; i++) sb.Append(" g").Append(i).Append('=').Append(gates[i].HealthFraction.ToString("0.00")).Append(gates[i].CanUpgrade ? "+" + gates[i].NextUpgradeCost : "");
var seen = Game.Core.Services.Get<Game.Economy.ProgressService>();
return mode + " dlg=" + (p.Current != null ? p.Current.Id + "#" + p.LineIndex : "none") + " queued=" + p.QueuedCount + " money=" + c.Wallet.Money + " post=" + (c.Hero != null ? c.Hero.PostIndex : -1) + sb
    + " seenUpgrade=" + seen.HasSeenStory("Dlg_Tip_GateUpgrade") + " seenMove=" + seen.HasSeenStory("Dlg_Tip_MoveSpencer") + " story=" + c.StoryEnabled;
