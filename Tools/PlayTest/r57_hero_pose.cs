// Round 57: one frame of the hero's upper body for the repeated-fire check. Compares every bone under the art's upper-body
// bone with its rotation at the last call (kept in Tools/PlayTest/r57_pose_prev.txt; "reset" in r57_pose_arg.txt starts
// over) and prints the summed and the largest single turn since then, the gun arm's turn, the upper clip and its time, the
// angle between the barrel and the struck point, the bolt start's distance from the muzzle and the shots counted so far.
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var view = UnityEngine.Object.FindAnyObjectByType<Game.Hero.HeroArtView>();
if (view == null) { return "no hero art view"; }
var type = typeof(Game.Hero.HeroArtView);
var body = (Game.Core.ArtModel)type.GetField("_body", flags).GetValue(view);
var player = (Game.Core.ModelClipPlayer)type.GetField("_player", flags).GetValue(view);
var muzzle = (UnityEngine.Transform)type.GetField("_muzzle", flags).GetValue(view);
var gunArm = (UnityEngine.Transform)type.GetField("_gunArm", flags).GetValue(view);
var bolt = (UnityEngine.LineRenderer)type.GetField("_bolt", flags).GetValue(view);
var target = (UnityEngine.Vector3)type.GetField("_boltTarget", flags).GetValue(view);
var art = body.Art;
var upper = body.Find(art.UpperBodyBone);
var bones = upper.GetComponentsInChildren<UnityEngine.Transform>(true);
string prevPath = "Tools/PlayTest/r57_pose_prev.txt";
string argPath = "Tools/PlayTest/r57_pose_arg.txt";
bool reset = System.IO.File.Exists(argPath) && System.IO.File.ReadAllText(argPath).Trim() == "reset";
if (reset) { System.IO.File.WriteAllText(argPath, "-"); }
var previous = !reset && System.IO.File.Exists(prevPath) ? System.IO.File.ReadAllLines(prevPath) : new string[0];
float sum = 0f, largest = 0f, arm = 0f;
string largestName = "-";
var lines = new string[bones.Length];
var inv = System.Globalization.CultureInfo.InvariantCulture;
for (int i = 0; i < bones.Length; i++)
{
    var q = bones[i].localRotation;
    lines[i] = q.x.ToString("R", inv) + " " + q.y.ToString("R", inv) + " " + q.z.ToString("R", inv) + " " + q.w.ToString("R", inv);
    if (i >= previous.Length) { continue; }
    var p = previous[i].Split(' ');
    var before = new UnityEngine.Quaternion(float.Parse(p[0], inv), float.Parse(p[1], inv), float.Parse(p[2], inv), float.Parse(p[3], inv));
    float turn = UnityEngine.Quaternion.Angle(before, q);
    sum += turn;
    if (turn > largest) { largest = turn; largestName = bones[i].name; }
    if (bones[i] == gunArm) { arm = turn; }
}

System.IO.File.WriteAllLines(prevPath, lines);
string clip = player.Upper >= 0 ? art.Clips[player.Upper].Clip.name : "none";
float barrel = UnityEngine.Vector3.Angle(muzzle.forward, target - muzzle.position);
string boltStart = bolt != null && bolt.enabled && bolt.positionCount > 0 ? UnityEngine.Vector3.Distance(bolt.GetPosition(0), muzzle.position).ToString("0.0000") : "off";
string counts = System.IO.File.Exists("Tools/PlayTest/r50_hero_counts.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r50_hero_counts.txt") : "";
int shots = (counts.Length - counts.Replace("shot@", "").Length) / 5;
return "t=" + UnityEngine.Time.time.ToString("0.000") + " shots=" + shots + " upper=" + clip + " sum=" + sum.ToString("0.0") + " max=" + largest.ToString("0.0") + "(" + largestName + ") arm=" + arm.ToString("0.0")
    + " barrel=" + barrel.ToString("0.00") + " boltStart=" + boltStart;
