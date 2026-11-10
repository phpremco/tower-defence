// Round 35 font trial (nothing saved): puts every TextMeshPro label on screen (dialogue, cutscene skip chip and title,
// radar) into a dynamic Chakra Petch face for an A/B still against the default Liberation Sans. The weight comes from
// Tools/PlayTest/r35_tmp_font_arg.txt (Regular, Medium, SemiBold or Bold).
string weight = System.IO.File.ReadAllText("Tools/PlayTest/r35_tmp_font_arg.txt").Trim();
var font = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Font>("Assets/_Project/Art/UI/Fonts/ChakraPetch-" + weight + ".ttf");
if (font == null) return "no font " + weight;
var asset = TMPro.TMP_FontAsset.CreateFontAsset(font);
if (asset == null) return "font asset failed";
int changed = 0;
foreach (var text in UnityEngine.Object.FindObjectsByType<TMPro.TextMeshProUGUI>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    text.font = asset;
    changed++;
}
return "Chakra Petch " + weight + " on " + changed + " labels";
