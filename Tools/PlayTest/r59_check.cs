// Round 59: is the salvo code compiled in, and what does the cast hold now.
var cast = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CutsceneCast>("Assets/_Project/Data/Story/Cutscene/CutsceneCast.asset");
return "count " + cast.CapsuleCount + " gap " + cast.ReleaseGap + " cargo " + cast.CargoFlights.Length + " pathfinder aim " + cast.PathfinderFlight.Aim + " release " + Game.UI.CapsuleDrop.PathfinderRelease(cast.CapsuleCount, cast.ReleaseGap);
