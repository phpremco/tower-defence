// Plays one impact effect at the hero from the VfxLibrary, named in vfx_arg.txt as "<name> <radius>" where name is
// a projectile style (Shell, Rocket, Mortar, Bolt), Mine, Barrel (the barrel path object), Death or BossPulse.
string[] parts = System.IO.File.ReadAllText("Tools/PlayTest/vfx_arg.txt").Trim().Split(' ');
string name = parts[0];
float radius = parts.Length > 1 ? float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 1f;
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
var library = context.Projectiles.Vfx;
Game.Combat.ImpactVfxDefinition vfx;
if (name == "Mine") vfx = library.Mine;
else if (name == "Death") vfx = library.Death;
else if (name == "BossPulse") vfx = library.BossPulse;
else if (name == "Barrel") vfx = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.PathObjects.PathObjectDefinition>("Assets/_Project/Data/PathObjects/ExplosiveBarrel.asset").ImpactVfx;
else vfx = library.For((Game.Combat.ProjectileStyle)System.Enum.Parse(typeof(Game.Combat.ProjectileStyle), name));
// Slow motion from here on (time_free.cs restores it): stepping the unfocused editor gives long frames, which
// would run a short effect to its end between two CLI calls.
UnityEngine.Time.timeScale = 0.05f;
UnityEngine.Vector3 at = context.Hero.transform.position + UnityEngine.Vector3.right * 1.5f;
context.Projectiles.PlayBurst(at, radius, vfx, name == "BossPulse" ? new UnityEngine.Color(1f, 0.3f, 0.4f) : default);
return "burst " + name + " r" + radius + " vfx=" + (vfx ? vfx.name : "null");
