// Round 51: where the hero's pistol barrel really points. Prints the pistol root's socket and turn, the extent of its meshes
// along the grip axes (the barrel is the long end of Z), the muzzle transform against that extent, the bolt's start and end,
// and the painted muzzle flash card's position.
var view = UnityEngine.Object.FindAnyObjectByType<Game.Hero.HeroArtView>();
if (view == null) { return "no hero art view"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var pistol = (Game.Core.ArtModel)typeof(Game.Hero.HeroArtView).GetField("_pistol", flags).GetValue(view);
var muzzle = (UnityEngine.Transform)typeof(Game.Hero.HeroArtView).GetField("_muzzle", flags).GetValue(view);
var bolt = (UnityEngine.LineRenderer)typeof(Game.Hero.HeroArtView).GetField("_bolt", flags).GetValue(view);
var root = pistol.Root;
var socket = root.parent;
var sb = new System.Text.StringBuilder();
sb.Append("socket=").Append(socket != null ? socket.name : "none").Append(" rootRot=").Append(root.localEulerAngles.ToString("0")).Append(" rootScale=").Append(root.localScale.x.ToString("0.00"));
float minZ = float.MaxValue, maxZ = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
UnityEngine.Vector3 tipPlus = UnityEngine.Vector3.zero, tipMinus = UnityEngine.Vector3.zero;
foreach (var filter in root.GetComponentsInChildren<UnityEngine.MeshFilter>(false))
{
    var r = filter.GetComponent<UnityEngine.Renderer>();
    if (r == null || !r.enabled || filter.sharedMesh == null ) { continue; }
    var mb = filter.sharedMesh.bounds;
    for (int i = 0; i < 8; i++)
    {
        var v = new UnityEngine.Vector3((i & 1) == 0 ? mb.min.x : mb.max.x, (i & 2) == 0 ? mb.min.y : mb.max.y, (i & 4) == 0 ? mb.min.z : mb.max.z);
        var world = filter.transform.TransformPoint(v);
        var grip = socket.InverseTransformPoint(world);
        if (grip.z > maxZ) { maxZ = grip.z; tipPlus = world; }
        if (grip.z < minZ) { minZ = grip.z; tipMinus = world; }
        minY = UnityEngine.Mathf.Min(minY, grip.y); maxY = UnityEngine.Mathf.Max(maxY, grip.y);
    }
}
foreach (var skinned in root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(false))
{
    var b = skinned.localBounds;
    sb.Append(" skinned ").Append(skinned.name).Append(" localBounds=").Append(b.min.ToString("0.000")).Append("..").Append(b.max.ToString("0.000"));
}

sb.Append(" gripZ=").Append(minZ.ToString("0.000")).Append("..").Append(maxZ.ToString("0.000")).Append(" gripY=").Append(minY.ToString("0.000")).Append("..").Append(maxY.ToString("0.000"));
var m = socket.InverseTransformPoint(muzzle.position);
sb.Append(" muzzleGrip=").Append(m.ToString("0.000")).Append(" muzzleFwdGrip=").Append(socket.InverseTransformDirection(muzzle.forward).ToString("0.00"));
sb.Append(" muzzleWorld=").Append(muzzle.position.ToString("0.000")).Append(" tip+Z=").Append(tipPlus.ToString("0.000")).Append(" tip-Z=").Append(tipMinus.ToString("0.000"));
if (bolt != null && bolt.positionCount > 0)
{
    sb.Append(" bolt=").Append(bolt.enabled).Append(" p0=").Append(bolt.GetPosition(0).ToString("0.000")).Append(" p0-muzzle=").Append((bolt.GetPosition(0) - muzzle.position).magnitude.ToString("0.000"));
    sb.Append(" pN=").Append(bolt.GetPosition(bolt.positionCount - 1).ToString("0.000"));
    var boltTarget = (UnityEngine.Vector3)typeof(Game.Hero.HeroArtView).GetField("_boltTarget", flags).GetValue(view);
    sb.Append(" barrel-to-target=").Append(UnityEngine.Vector3.Angle(muzzle.forward, boltTarget - muzzle.position).ToString("0.0")).Append("deg");
}

foreach (var flash in UnityEngine.Object.FindObjectsByType<Game.Combat.MuzzleFlashView>(UnityEngine.FindObjectsSortMode.None))
{
    var card = flash.transform.Find("MuzzleFlash");
    if (card != null && card.gameObject.activeInHierarchy) { sb.Append(" flashCard=").Append(card.position.ToString("0.000")).Append(" flash-muzzle=").Append((card.position - muzzle.position).magnitude.ToString("0.000")).Append(" flashAxis=").Append(card.right.ToString("0.00")); }
}

return sb.ToString();
