// Round 23d: writes the jetpack values (the generator's numbers) into the hero asset without a full generator run.
var hero = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Hero.HeroDefinition>("Assets/_Project/Data/Hero/Commander.asset");
var so = new UnityEditor.SerializedObject(hero);
so.FindProperty("_flightSpeed").floatValue = 13f;
so.FindProperty("_walkWithin").floatValue = 4f;
so.FindProperty("_takeoffSeconds").floatValue = 0.4f;
so.FindProperty("_flightHeight").floatValue = 2.5f;
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(hero);
UnityEditor.AssetDatabase.SaveAssets();
return $"flight {hero.FlightSpeed} m/s, walk within {hero.WalkWithin} m, takeoff {hero.TakeoffSeconds} s, height {hero.FlightHeight} m";
