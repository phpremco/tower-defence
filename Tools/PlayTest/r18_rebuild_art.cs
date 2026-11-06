// Round 18: syncs and rebuilds the creature art rows whose export asset is named in Tools/PlayTest/r18_arg.txt
// (space separated, e.g. "Gravemaw FrostWarden"), via BlenderArtBuilder.RebuildOnly; every other creature stays.
var names = new System.Collections.Generic.HashSet<string>(System.IO.File.ReadAllText("Tools/PlayTest/r18_arg.txt").Trim().Split(' '));
var ids = new System.Collections.Generic.List<string>();
foreach (var entry in Game.Editor.BlenderArtCatalog.Creatures) { if (names.Contains(entry.Asset)) { ids.Add(entry.Id); } }
int worn = Game.Editor.BlenderArtBuilder.RebuildOnly(ids.ToArray());
return "rebuilt " + string.Join(", ", ids) + "; worn by " + worn + " enemies";
