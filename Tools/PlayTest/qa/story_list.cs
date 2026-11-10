// QA round 32: the story triggers of the first campaign levels: kind, wave, creature, dialogue id, speakers and first line.
var catalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
var sb = new System.Text.StringBuilder();
for (int n = 0; n < 4 && n < catalog.Nodes.Count; n++)
{
    var level = catalog.Nodes[n].Level;
    sb.Append("== ").Append(level.name).Append(" triggers ").Append(level.StoryTriggers.Length).Append('\n');
    foreach (var tr in level.StoryTriggers)
    {
        string first = "";
        var d = tr.Dialogue;
        if (d != null && d.Lines.Count > 0) { var l = d.Lines[0]; first = (l.Speaker != null ? l.Speaker.DisplayName : "?") + ": " + (l.Segments.Length > 0 ? l.Segments[0].Text : ""); }
        if (first.Length > 90) first = first.Substring(0, 90);
        sb.Append(tr.Kind).Append(" w").Append(tr.Wave).Append(tr.Enemy != null ? " " + tr.Enemy.name : "").Append(" ").Append(d != null ? d.name : "null").Append(" lines=").Append(d != null ? d.Lines.Count : 0).Append(" | ").Append(first).Append('\n');
    }
}
return sb.ToString();
