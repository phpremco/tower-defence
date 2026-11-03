using Game.Levels;

namespace Game.Editor
{
    /// <summary>Generator part for the difficulty profiles.</summary>
    public static partial class ProjectGenerator
    {
        // Difficulty profiles come from the balance sheet; the waves are written once the whole campaign exists.
        private static void GenerateDifficulties(GeneratorContext context)
        {
            var sheet = BalanceSheet.Difficulties;
            var difficulties = new DifficultyProfile[sheet.Length];
            for (int d = 0; d < sheet.Length; d++)
            {
                difficulties[d] = CreateDifficulty(sheet[d].Id, sheet[d].DisplayName, sheet[d].Health, sheet[d].Count, sheet[d].Interval, sheet[d].Research, sheet[d].Power, sheet[d].StructureDamage);
            }

            context.Difficulties = difficulties;
        }
    }
}
