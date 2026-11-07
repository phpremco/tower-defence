/// <summary>
/// The repair drone's price by the investment rule (round 22d, BalanceSheet.RepairDronePrice).
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyDronePrice.cs --entry ApplyDronePrice.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyDronePrice
{
    public static string Run() => Game.Editor.BalanceStages.ApplyDronePrices();
}
