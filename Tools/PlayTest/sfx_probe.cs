// Builds the looping sound effects outside Play Mode, so a generator fault shows up as a plain exception.
var sb = new System.Text.StringBuilder();
sb.Append("laser ").Append(Game.Core.ProceduralSfx.LaserHum().length.ToString("0.00"));
sb.Append(" bossLaser ").Append(Game.Core.ProceduralSfx.BossLaserHum().length.ToString("0.00"));
sb.Append(" calm ").Append(Game.Core.ProceduralMusic.Calm().length.ToString("0.00"));
return sb.ToString();
