// Import stage 4b: points one campaign level's theme at another continent's environment set IN MEMORY ONLY (reflection,
// nothing marked dirty or saved), so a continent-1 level can be shot with a continent 2-6 set; "restore" puts the
// theme's own set back. Tools/PlayTest/imp4b_swap.txt: "<level index> <SetName|restore>". Play mode must run from Boot.
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/imp4b_swap.txt").Trim().Split(' ');
var catalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
var theme = catalog.Nodes[int.Parse(args[0])].Level.Theme;
var field = typeof(Game.Levels.VisualTheme).GetField("_environment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
string set = args[1] == "restore" ? "CrystalPlains" : args[1];
var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.EnvironmentSet>("Assets/_Project/Data/Art/Environment/" + set + ".asset");
if (asset == null) return "no set " + set;
field.SetValue(theme, asset);
return theme.name + " wears " + asset.name;
