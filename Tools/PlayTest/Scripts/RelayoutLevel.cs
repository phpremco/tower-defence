/// <summary>
/// One campaign level's map again from its spec (round 29): the first line of Tools/PlayTest/relayout_arg.txt is the level key
/// (C1_05). Layout, terrain bake and the arena copies of that level are rewritten; solve the waves afterwards. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/RelayoutLevel.cs --entry RelayoutLevel.Run --timeout_ms 600000 --timeout 660
/// </summary>
public static class RelayoutLevel
{
    public static string Run()
    {
        const string arg = "Tools/PlayTest/relayout_arg.txt";
        if (!System.IO.File.Exists(arg))
        {
            return "write the level key into " + arg + " first";
        }

        string key = System.IO.File.ReadAllLines(arg)[0].Trim();
        return Game.Editor.BalanceStages.RelayoutLevel(key);
    }
}
