using System.Globalization;
using System.IO;
using System.Text;
using Game.Levels;
using UnityEditor;

/// <summary>
/// Round 88e: a level's own start money per difficulty (LevelDefinition._startMoney, StartMoneyRule). The level asset is
/// the only place the numbers live: the generator neither writes nor clears the list, and it can be edited in the
/// Inspector as well.
/// Set:   writes the sums listed in Tools/PlayTest/r88e_start_money_arg.txt, one "LevelKey DifficultyName Money" a line
///        (for example "C1_03 Hard 400"); a money of -1 removes the level's entry, so the shared rule counts again.
/// Table: writes Tools/PlayTest/r88e_start_money.txt, the start money of every campaign level on every difficulty
///        (an asterisk marks a sum the level set itself, with the shared rule's sum behind it).
/// Run: bash Tools/PlayTest/stage.sh run Round88eStartMoney.cs Round88eStartMoney.Set     (then re-stamp the calibration)
///      bash Tools/PlayTest/stage.sh run Round88eStartMoney.cs Round88eStartMoney.Table
/// </summary>
public static class Round88eStartMoney
{
    private const string CatalogPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
    private const string ArgPath = "Tools/PlayTest/r88e_start_money_arg.txt";
    private const string TablePath = "Tools/PlayTest/r88e_start_money.txt";
    private const int Remove = -1;

    public static string Set()
    {
        var report = new StringBuilder();
        foreach (string raw in File.ReadAllLines(ArgPath))
        {
            string[] words = raw.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (words.Length != 3 || raw.TrimStart().StartsWith("#"))
            {
                continue;
            }

            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"Assets/_Project/Data/Levels/Level_{words[0]}.asset");
            var difficulty = AssetDatabase.LoadAssetAtPath<DifficultyProfile>($"Assets/_Project/Data/Difficulty/{words[1]}.asset");
            if (level == null || difficulty == null)
            {
                return $"unknown level or difficulty in: {raw}";
            }

            Write(level, difficulty, int.Parse(words[2], CultureInfo.InvariantCulture));
            report.Append($"{level.name} {difficulty.name}: {StartMoneyRule.For(level, difficulty)} (shared rule {StartMoneyRule.Shared(level, difficulty)}); ");
        }

        AssetDatabase.SaveAssets();
        return report.Length > 0 ? report.ToString() : "nothing set";
    }

    private static void Write(LevelDefinition level, DifficultyProfile difficulty, int money)
    {
        var so = new SerializedObject(level);
        SerializedProperty list = so.FindProperty("_startMoney");
        int index = -1;
        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).FindPropertyRelative("Difficulty").objectReferenceValue == difficulty)
            {
                index = i;
            }
        }

        if (money == Remove)
        {
            if (index >= 0)
            {
                list.DeleteArrayElementAtIndex(index);
            }
        }
        else
        {
            if (index < 0)
            {
                index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
            }

            SerializedProperty entry = list.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("Difficulty").objectReferenceValue = difficulty;
            entry.FindPropertyRelative("Money").intValue = money;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(level);
    }

    public static string Table()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
        var text = new StringBuilder("Round 88e: start money per level and difficulty (StartMoneyRule.For). * = the level's own sum, with the shared rule's sum in brackets.\n\n| Level |");
        foreach (DifficultyProfile difficulty in catalog.Difficulties)
        {
            text.Append($" {difficulty.name} |");
        }

        text.Append("\n| --- |");
        foreach (DifficultyProfile _ in catalog.Difficulties)
        {
            text.Append(" --- |");
        }

        text.Append('\n');
        foreach (MapNode node in catalog.Nodes)
        {
            if (node.Level == null)
            {
                continue;
            }

            text.Append($"| {node.Level.name.Replace("Level_", string.Empty)} {node.Level.DisplayName} |");
            foreach (DifficultyProfile difficulty in catalog.Difficulties)
            {
                bool own = node.Level.TryGetStartMoney(difficulty, out int _);
                int money = StartMoneyRule.For(node.Level, difficulty);
                text.Append(own ? $" {money}* ({StartMoneyRule.Shared(node.Level, difficulty)}) |" : $" {money} |");
            }

            text.Append('\n');
        }

        File.WriteAllText(TablePath, text.ToString());
        return text.ToString();
    }
}
