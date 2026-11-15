// Round 63: lengths of Spencer's acting clips (seconds), for the reaction shot's angry beat and his line's delay.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/Story/Mercer.asset");
var sb = new System.Text.StringBuilder();
foreach (var role in new[] { Game.Core.ModelClipRole.TurnLookLeft, Game.Core.ModelClipRole.StartleBackstep, Game.Core.ModelClipRole.PointShout, Game.Core.ModelClipRole.DrawAimNervous, Game.Core.ModelClipRole.AimHoldNervous, Game.Core.ModelClipRole.Idle })
{
    int i = art.IndexOf(role);
    if (i < 0) { sb.Append(role).Append(" none; "); continue; }
    var c = art.Clips[i];
    sb.Append(role).Append(' ').Append(c.Length.ToString("0.000")).Append(" ev ").Append(c.EventSeconds.ToString("0.00")).Append("; ");
}
return sb.ToString();
