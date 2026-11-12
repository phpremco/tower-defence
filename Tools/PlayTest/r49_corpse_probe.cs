// Round 49: lists the pooled creature corpses (active or parked) with their position, and the game clock, to follow a
// death in stepped frames.
var sb = new System.Text.StringBuilder("t " + UnityEngine.Time.time.ToString("0.00") + " dt " + UnityEngine.Time.deltaTime.ToString("0.000") + ": ");
foreach (var corpse in UnityEngine.Resources.FindObjectsOfTypeAll<Game.Enemies.ModelCorpse>())
{
    if (corpse.gameObject.scene.name == null) { continue; }
    sb.Append(corpse.name + (corpse.gameObject.activeInHierarchy ? " active" : " parked") + " at " + corpse.transform.position.ToString("0.0") + "; ");
}

return sb.ToString();
