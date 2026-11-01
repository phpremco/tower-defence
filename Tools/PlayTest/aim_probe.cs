// Reports how well the hero's drawn weapon points at the commander's target: the angle between the barrel (the
// weapon's forward) and the line from the muzzle to the target, plus where the weapon hangs.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

Game.Hero.Commander hero = context.Hero;
Game.Combat.ITargetable target = hero.Target;
string report = "target " + (target == null ? "none" : target.Position.ToString("0.0") + (target.IsAlive ? " alive" : " dead"));
foreach (var tip in hero.GetComponentsInChildren<UnityEngine.Transform>())
{
    if (tip.name != "Tip")
    {
        continue;
    }

    UnityEngine.Transform weapon = tip.parent;
    string kind = weapon.Find("Energy").GetComponent<UnityEngine.MeshFilter>().sharedMesh.name;
    report += " | " + kind + " on " + weapon.parent.name;
    if (target != null && weapon.parent.name == "ForearmR")
    {
        UnityEngine.Vector3 to = target.Position + UnityEngine.Vector3.up * 0.4f - tip.position;
        report += " angle " + UnityEngine.Vector3.Angle(weapon.forward, to).ToString("0.0") + " deg, barrel " + weapon.forward.ToString("0.00") + " to " + to.normalized.ToString("0.00");
    }
}

return report;
