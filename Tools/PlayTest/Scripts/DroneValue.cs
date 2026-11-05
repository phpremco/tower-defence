using System;
using System.Text;
using Game.Levels;
using UnityEditor;
using UnityEngine;

/// <summary>
/// What a harvest drone is worth per campaign level (no assets written): the trip from the base to each ore node and back
/// at the drone's speed plus its mining time, the ore per minute that gives, the level's length on Medium (every wave's
/// start delay and spawn span, the game's clock), how long one drone takes to earn its price back, and the net ore over
/// the level (capped by the nodes' stock, with each difficulty's income share). Also counts, per level, the creatures that
/// attack guns (not only gates) and the flyers, for the repair and attack drones. Writes Tools/PlayTest/drone_value.txt.
/// unity command run_script --file Tools/PlayTest/Scripts/DroneValue.cs --entry DroneValue.Run --timeout_ms 600000 --timeout 660
/// </summary>
public static class DroneValue
{
    private const string OutPath = "Tools/PlayTest/drone_value.txt";

    public static string Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
        var harvester = AssetDatabase.LoadAssetAtPath<Game.Drones.DroneDefinition>("Assets/_Project/Data/Drones/HarvestDrone.asset");
        var sb = new StringBuilder();
        sb.AppendLine($"Harvest drone: cost {harvester.Cost}, speed {harvester.Speed}, {harvester.HarvestValue} ore per trip, {harvester.HarvestSeconds}s mining");
        foreach (MapNode node in catalog.Nodes)
        {
            LevelDefinition level = node.Level;
            if (level == null)
            {
                continue;
            }

            Vector2Int[] road = level.MainRoad;
            Vector3 home = level.Road.CentreOf(road[road.Length - 1]);
            float nearest = float.MaxValue;
            float sum = 0f;
            foreach (Vector2Int cell in level.MineCells)
            {
                Vector3 d = level.Grid.CellToWorld(cell) - home;
                d.y = 0f;
                nearest = Mathf.Min(nearest, d.magnitude);
                sum += d.magnitude;
            }

            float mean = level.MineCells.Length > 0 ? sum / level.MineCells.Length : 0f;
            float trip = 2f * mean / harvester.Speed + harvester.HarvestSeconds;
            float perMinute = harvester.HarvestValue / trip * 60f;
            int stock = level.MineCells.Length * level.MineStock;
            sb.AppendLine($"{level.name} ({level.DisplayName}): {level.MineCells.Length} nodes x {level.MineStock}, distance nearest {nearest:0.0} mean {mean:0.0}, trip {trip:0.0}s, {perMinute:0} ore/min per drone");

            foreach (DifficultyWaves set in level.DifficultyWaves)
            {
                float seconds = 0f;
                int gunBiters = 0;
                int flyers = 0;
                int creatures = 0;
                foreach (WaveDefinition wave in set.Waves)
                {
                    float span = 0f;
                    foreach (SpawnGroup group in wave.Groups)
                    {
                        span = Mathf.Max(span, group.Delay + group.Stagger + group.Count * group.Interval);
                        creatures += group.Count;
                        if (group.Enemy != null && group.Enemy.AttackDamage > 0f)
                        {
                            gunBiters += group.Count;
                        }

                        if (group.Enemy != null && group.Enemy.Layer == Game.Core.TargetLayers.Air)
                        {
                            flyers += group.Count;
                        }
                    }

                    seconds += wave.StartDelay + span;
                }

                float income = set.Difficulty != null ? set.Difficulty.IncomeMultiplier : 1f;
                float onePerSecond = perMinute / 60f * income;
                float payback = onePerSecond > 0f ? harvester.Cost / onePerSecond : float.PositiveInfinity;
                // Bought as soon as it pays: the first wave's kills cover it; it works from then to the end.
                float mined = Mathf.Min(stock * income, onePerSecond * seconds);
                float twoMined = Mathf.Min(stock * income, 2f * onePerSecond * seconds);
                string name = set.Difficulty != null ? set.Difficulty.name : "?";
                sb.AppendLine($"  {name,-7} {set.Waves.Length,2} waves, {seconds / 60f:0.0} min; payback {payback:0}s; one drone nets {mined - harvester.Cost:0} (of {stock * income:0} ore), two net {twoMined - 2 * harvester.Cost:0}; creatures {creatures}, gun-biters {gunBiters}, flyers {flyers}");
            }
        }

        System.IO.File.WriteAllText(OutPath, sb.ToString());
        return sb.ToString();
    }
}
