using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Levels;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 88a labs on the assets (no Play Mode). Clearance: per level the cells the spawn points now keep free of towers
/// (SpawnClearance), how many of them were build cells before, the validator's verdict, and every recorded plan spot
/// (Tools/PlayTest/Plans/*.json) that stands on one. Run: bash Tools/PlayTest/stage.sh run Round88Lab.cs Round88Lab.Clearance
/// (writes Tools/PlayTest/r88_clearance.txt).
/// </summary>
public static class Round88Lab
{
    private const string ClearancePath = "Tools/PlayTest/r88_clearance.txt";

    public static string Clearance()
    {
        var text = new StringBuilder("Round 88a: spawn clearance per level (cells kept free under nests, route gates and air pods)\n");
        int totalFreed = 0;
        int totalErrors = 0;
        int planHits = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets/_Project/Data/Levels" }))
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (level == null || !level.IsValid(out _))
            {
                continue;
            }

            var kept = new HashSet<Vector2Int>();
            SpawnClearance.Collect(level, kept);
            var was = new HashSet<Vector2Int>(level.BlockedCells);
            was.UnionWith(level.MineCells);
            foreach (ElementNest nest in level.Nests)
            {
                was.Add(nest.Cell);
            }

            var slope = new HashSet<Vector2Int>(level.SlopeCells);
            var freed = new List<Vector2Int>();
            foreach (Vector2Int cell in kept)
            {
                if (!level.Road.IsRoad(cell) && !level.Terrain.IsLiquid(cell) && !slope.Contains(cell) && !was.Contains(cell))
                {
                    freed.Add(cell);
                }
            }

            var errors = new List<string>();
            LevelValidator.ValidateLayout(level, errors);
            totalFreed += freed.Count;
            totalErrors += errors.Count;
            text.Append(level.name).Append(": ").Append(level.Nests.Length).Append(" nests, ").Append(level.Paths.Length).Append(" paths, kept free ").Append(kept.Count)
                .Append(" cells, of which ").Append(freed.Count).Append(" were build cells (may include base or gate ground); validator errors ").Append(errors.Count).Append('\n');
            foreach (string error in errors)
            {
                text.Append("    ! ").Append(error).Append('\n');
            }

            planHits += PlanHits(level.name, kept, text);
        }

        text.Append("total former build cells kept free: ").Append(totalFreed).Append("; validator errors: ").Append(totalErrors).Append("; plan spots on kept cells: ").Append(planHits).Append('\n');
        File.WriteAllText(ClearancePath, text.ToString());
        return $"freed {totalFreed}, validator errors {totalErrors}, plan spots hit {planHits}; see {ClearancePath}";
    }

    /// <summary>
    /// Wave spans: how long the ground creatures of one wave keep coming out (first to last spawn, at the difficulty's spawn
    /// tempo), per level and difficulty: the time a consumable laid for a wave has to last. Run:
    /// bash Tools/PlayTest/stage.sh run Round88Lab.cs Round88Lab.WaveSpans (writes Tools/PlayTest/r88_wave_spans.txt).
    /// </summary>
    public static string WaveSpans()
    {
        var text = new StringBuilder("Round 88a: seconds from the first to the last ground spawn of a wave (per level and difficulty: least, median, most)\n");
        var all = new List<float>();
        foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets/_Project/Data/Levels" }))
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (level == null || !level.name.StartsWith("Level_"))
            {
                continue;
            }

            foreach (DifficultyWaves set in level.DifficultyWaves)
            {
                var spans = new List<float>();
                foreach (WaveDefinition wave in set.Waves)
                {
                    float first = float.MaxValue;
                    float last = 0f;
                    foreach (SpawnGroup group in wave.Groups)
                    {
                        if (group.Enemy == null || group.Enemy.Flies)
                        {
                            continue;
                        }

                        float start = group.Delay + group.Stagger * set.Difficulty.SpawnIntervalMultiplier;
                        first = Mathf.Min(first, start);
                        last = Mathf.Max(last, start + (group.Count - 1) * group.Interval * set.Difficulty.SpawnIntervalMultiplier);
                    }

                    if (first < float.MaxValue)
                    {
                        spans.Add(last - first);
                    }
                }

                spans.Sort();
                all.AddRange(spans);
                text.Append(level.name).Append(' ').Append(set.Difficulty.name).Append(": ").Append(spans.Count).Append(" ground waves, ")
                    .Append(spans[0].ToString("0")).Append(" / ").Append(spans[spans.Count / 2].ToString("0")).Append(" / ").Append(spans[spans.Count - 1].ToString("0")).Append(" s\n");
            }
        }

        all.Sort();
        string summary = $"all {all.Count} ground waves: least {all[0]:0} s, quartile {all[all.Count / 4]:0} s, median {all[all.Count / 2]:0} s, upper quartile {all[all.Count * 3 / 4]:0} s, most {all[all.Count - 1]:0} s";
        text.Append(summary).Append('\n');
        File.WriteAllText("Tools/PlayTest/r88_wave_spans.txt", text.ToString());
        return summary;
    }

    /// <summary>
    /// Drone sockets: where the work beam of each drone model leaves (its Beam or Mine socket) against the body of the model,
    /// in the frame of the model at its play scale. Run: bash Tools/PlayTest/stage.sh run Round88Lab.cs Round88Lab.DroneSockets
    /// </summary>
    public static string DroneSockets()
    {
        var text = new StringBuilder();
        foreach (string guid in AssetDatabase.FindAssets("t:DroneDefinition", new[] { "Assets/_Project/Data" }))
        {
            var drone = AssetDatabase.LoadAssetAtPath<Game.Drones.DroneDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (drone == null || drone.Art == null || drone.Art.Model == null)
            {
                continue;
            }

            GameObject model = Object.Instantiate(drone.Art.Model);
            try
            {
                var bounds = new Bounds();
                bool any = false;
                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    if (!any) { bounds = renderer.bounds; any = true; } else { bounds.Encapsulate(renderer.bounds); }
                }

                text.Append(drone.name).Append(": scale ").Append(drone.Art.Scale.ToString("0.00")).Append(", body centre ").Append((bounds.center * drone.Art.Scale).ToString("0.00")).Append(" size ").Append((bounds.size * drone.Art.Scale).ToString("0.00"));
                foreach (Transform child in model.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "Beam" || child.name == "Mine" || child.name == "Thruster" || child.name.StartsWith("Muzzle"))
                    {
                        text.Append("; ").Append(child.name).Append(' ').Append((child.position * drone.Art.Scale).ToString("0.00")).Append(" under ").Append(child.parent != null ? child.parent.name : "-");
                    }
                }

                text.Append(" | ");
            }
            finally
            {
                Object.DestroyImmediate(model);
            }
        }

        return text.ToString();
    }

    [System.Serializable]
    private sealed class PlanFile
    {
        public string Level;
        public PlanSpots[] Spots;
    }

    [System.Serializable]
    private sealed class PlanSpots
    {
        public string Tower;
        public int[] X;
        public int[] Y;
    }

    private static int PlanHits(string level, HashSet<Vector2Int> kept, StringBuilder text)
    {
        int hits = 0;
        if (!Directory.Exists("Tools/PlayTest/Plans"))
        {
            return 0;
        }

        foreach (string path in Directory.GetFiles("Tools/PlayTest/Plans", level + "_*.json"))
        {
            PlanFile plan = JsonUtility.FromJson<PlanFile>(File.ReadAllText(path));
            if (plan?.Spots == null)
            {
                continue;
            }

            foreach (PlanSpots spots in plan.Spots)
            {
                for (int i = 0; spots.X != null && i < spots.X.Length; i++)
                {
                    var cell = new Vector2Int(spots.X[i], spots.Y[i]);
                    if (kept.Contains(cell))
                    {
                        hits++;
                        text.Append("    plan ").Append(Path.GetFileName(path)).Append(": ").Append(spots.Tower).Append(" choice ").Append(i + 1).Append(" at ").Append(cell).Append('\n');
                    }
                }
            }
        }

        return hits;
    }
}
