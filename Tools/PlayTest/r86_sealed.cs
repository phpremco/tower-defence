// Round 86: the Sealed One back under the health its Psycho finale player beats (the finale stage sizes on Medium; at
// 29200 Psycho held x0.92, at 33300 Hard and Psycho were lost). Body 26800, armour a quarter of it (BalanceRules.BossArmourShare).
var enemy = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/SealedOne.asset");
var so = new UnityEditor.SerializedObject(enemy);
so.FindProperty("_maxHealth").floatValue = 26800f;
so.FindProperty("_armor").floatValue = Game.Balance.BalanceRules.Default.BossArmourFor(26800f, 0f);
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(enemy);
UnityEditor.AssetDatabase.SaveAssets();
return "SealedOne " + enemy.MaxHealth + " armour " + enemy.Armor;
