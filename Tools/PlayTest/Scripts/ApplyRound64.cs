using System.Text;
using Game.Balance;
using Game.Enemies;
using Game.Levels;
using UnityEditor;

/// <summary>
/// Round 64 without the full generator (Remco's rule): writes what the changed generator now would, and nothing else.
///  1. The armour rule: every weapon's damage type and the armour factor that follows from it
///     (ProjectGenerator.WeaponDamageTypes, BalanceSheet.ArmourFactorOf): towers, the hero's weapons and shockwave, the
///     attack drone (its shots ignored armour), mines, barrel, lava ball and ground drone.
///  2. The armoured troops: most of their toughness is armour now (BalanceSheet.TroopArmourShare); the toughness itself and
///     everything else on the creature stay, the kill money follows the threat as the generator derives it.
///  3. The wisp voices' presence (the Core Wisp's static loop) and the music loops streaming from disk.
/// The waves follow with the solve stage (ApplyWaves.cs). Safe to run again. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRound64.cs --entry ApplyRound64.Run --timeout_ms 300000 --timeout 360
/// </summary>
public static class ApplyRound64
{
    public static string Run()
    {
        var report = new StringBuilder();
        report.Append(Game.Editor.ProjectGenerator.ApplyDamageTypes());
        foreach ((string Id, float Toughness) troop in Game.Editor.ProjectGenerator.ArmouredTroops)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Project/Data/Enemies/{troop.Id}.asset");
            var so = new SerializedObject(enemy);
            float health = so.FindProperty("_maxHealth").floatValue;
            float armour = so.FindProperty("_armor").floatValue;
            int reward = so.FindProperty("_reward").intValue;
            so.FindProperty("_maxHealth").floatValue = Game.Editor.BalanceSheet.BodyOf(troop.Toughness);
            so.FindProperty("_armor").floatValue = Game.Editor.BalanceSheet.ArmourOf(troop.Toughness);
            so.ApplyModifiedPropertiesWithoutUndo();
            // Kill money follows threat (Threat.Reward on the creature's own numbers), as in the generator's bestiary step.
            EnemyStats stats = BalanceLedger.Stats(enemy);
            so.Update();
            so.FindProperty("_reward").intValue = Threat.Reward(stats, Game.Editor.BalanceSheet.Rules);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
            report.Append($"{troop.Id}: {health:0}+{armour:0} reward {reward} -> {enemy.MaxHealth:0}+{enemy.Armor:0} reward {enemy.Reward}\n");
        }

        report.Append(Game.Editor.ProjectGenerator.ApplyWispPresence());
        AssetDatabase.SaveAssets();
        report.Append(Game.Editor.ProjectGenerator.ApplyMusicImport());
        return report.ToString();
    }
}
