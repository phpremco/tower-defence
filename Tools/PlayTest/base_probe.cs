Game.Core.Services.TryGet(out Game.Levels.LevelContext context);
return "base " + context.BaseHealth.Current.ToString("0") + "/" + context.BaseHealth.Max.ToString("0") + " alive " + context.Structures.FindBlocker(context.Base.Position, 0.1f);
