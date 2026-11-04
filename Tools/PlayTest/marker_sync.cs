int copied = 0;
string root = Game.Editor.BlenderSync.SourceRoot;
foreach (string folder in Game.Editor.BlenderMarkerCatalog.Folders)
{
    var files = new System.Collections.Generic.List<string>();
    foreach (string f in System.IO.Directory.GetFiles(System.IO.Path.Combine(root, folder)))
    {
        string n = System.IO.Path.GetFileName(f);
        if (n.EndsWith(".fbx") || n.EndsWith(".png")) files.Add(n);
    }
    copied += Game.Editor.BlenderSync.SyncFiles(folder, files, folder + "_report.json");
}
return copied;
