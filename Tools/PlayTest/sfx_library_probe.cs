// Reports whether the recorded sound library is registered and how many clips each list holds.
if (!Game.Core.Services.TryGet(out Game.Core.SfxLibrary library))
{
    return "no library registered";
}

return "library " + library.name
    + " pulse=" + library.PulseShots.Count + " heavy=" + library.HeavyShots.Count + " rocket=" + library.RocketLaunches.Count
    + " cryo=" + library.CryoShots.Count + " tesla=" + library.TeslaShots.Count + " rail=" + library.RailShots.Count
    + " explosion=" + library.Explosions.Count + " big=" + library.BigExplosions.Count + " hit=" + library.StructureHits.Count
    + " death=" + library.EnemyDeaths.Count + " base=" + library.BaseHits.Count + " blaster=" + library.HeroBlasters.Count;
