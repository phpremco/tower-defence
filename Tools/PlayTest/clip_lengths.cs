// Lists the length of every recorded clip in the sound library, so a lingering sound can be traced to its clip.
var lib = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.SfxLibrary>("Assets/_Project/Data/Audio/SfxLibrary.asset");
var sb = new System.Text.StringBuilder();
foreach (var clip in lib.EnemyDeaths) sb.Append("death ").Append(clip.name).Append(' ').Append(clip.length.ToString("0.00")).Append("s\n");
foreach (var clip in lib.Explosions) sb.Append("explosion ").Append(clip.name).Append(' ').Append(clip.length.ToString("0.00")).Append("s\n");
foreach (var clip in lib.StructureHits) sb.Append("hit ").Append(clip.name).Append(' ').Append(clip.length.ToString("0.00")).Append("s\n");
return sb.ToString();
