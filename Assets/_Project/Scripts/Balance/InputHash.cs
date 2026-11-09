using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Balance
{
    /// <summary>
    /// A fingerprint of everything the balance model reads (towers, creatures, difficulties, upgrades, levels with their
    /// maps, gadgets and drones, the rules). The calibration stores the hash of the inputs it was fitted on; every stage
    /// compares it with the current one and reports a stale calibration instead of reusing it silently (round 22).
    /// Waves are left out on purpose: the builder re-solves them against a frozen calibration.
    /// </summary>
    public static class InputHash
    {
        private const uint Offset = 2166136261;
        private const uint Prime = 16777619;

        public static string Of(CampaignModel model)
        {
            var text = new StringBuilder();
            foreach (KeyValuePair<string, TowerStats> pair in Sorted(model.Towers))
            {
                TowerStats t = pair.Value;
                text.Append(t.Id).Append(t.Targets).Append(F(t.Splash)).Append(t.SplashTargets).Append(F(t.GroundFactor)).Append(F(t.AirFactor)).Append(F(t.ArmourFactor)).Append(F(t.SellRefund)).Append(t.IsSupport).Append(t.BlocksGround).Append(t.IsBaseDefence);
                foreach (TowerLevelStats level in t.Levels)
                {
                    text.Append(level.Cost).Append(F(level.Damage)).Append(F(level.Range)).Append(F(level.Interval)).Append(F(level.Slow));
                }
            }

            foreach (KeyValuePair<string, EnemyStats> pair in Sorted(model.Enemies))
            {
                EnemyStats e = pair.Value;
                text.Append(e.Id).Append(e.Layer).Append(F(e.Health)).Append(F(e.Armor)).Append(F(e.Speed)).Append(F(e.FightSpeed)).Append(e.Reward).Append(F(e.BlockadeDamage)).Append(F(e.AttackInterval)).Append(e.IsBoss).Append(F(e.MechanicLossSeconds));
            }

            foreach (DifficultyStats d in model.Difficulties)
            {
                text.Append(d.Id).Append(F(d.Health)).Append(F(d.Count)).Append(F(d.Interval)).Append(F(d.StructureDamage)).Append(F(d.Income)).Append(F(d.StartMoney)).Append(F(d.BossHealth));
            }

            foreach (UpgradeStats u in model.Upgrades)
            {
                text.Append(u.Id).Append(u.Kind).Append(u.TowerId).Append(F(u.BonusPerLevel)).Append(u.Costs.Length);
            }

            text.Append(F(model.Hero.BladeDamage)).Append(F(model.Hero.BladeInterval)).Append(F(model.Hero.GunDamage)).Append(F(model.Hero.GunInterval)).Append(model.Hero.Magazine).Append(F(model.Hero.ReloadSeconds)).Append(F(model.Hero.MaxHealth)).Append(F(model.Hero.GuardShare)).Append(F(model.Hero.ReviveSeconds));
            text.Append(F(model.Hero.BladeArmourFactor)).Append(F(model.Hero.GunArmourFactor)).Append(F(model.Hero.PulseDamage)).Append(F(model.Hero.PulseCooldown)).Append(F(model.Hero.PulseArmourFactor)).Append(F(model.Hero.RegenPerSecond)).Append(F(model.Hero.RegenDelaySeconds));
            foreach (LevelFacts facts in model.Facts)
            {
                text.Append(facts.Key).Append(facts.StartingMoney).Append(F(facts.HealthScale)).Append(facts.MineCount).Append(facts.MineStock).Append(facts.Harvest).Append(F(facts.GateHealth)).Append(F(facts.BaseHealth)).Append(F(facts.GateRepairCostPerHealth)).Append(facts.GateRebuildCost);
                text.Append(F(facts.Shape.MainLength)).Append(facts.Shape.Lanes.Count).Append(facts.Shape.Gates.Count);
                if (facts.BaseTurret != null)
                {
                    text.Append(facts.BaseTurret.Id).Append(facts.BaseTurretSides).Append(facts.BaseTurret.Targets);
                    foreach (TowerLevelStats level in facts.BaseTurret.Levels)
                    {
                        text.Append(F(level.Damage)).Append(F(level.Range)).Append(F(level.Interval));
                    }
                }

                foreach (GadgetStats g in facts.Gadgets)
                {
                    text.Append(g.Id).Append(g.Kind).Append(g.Cost).Append(F(g.Damage)).Append(F(g.Radius)).Append(F(g.SlowFactor));
                }

                foreach (DroneStats d in facts.Drones)
                {
                    // A drone's price is a plan input, not a capacity input (round 22d: the repair drone is priced from the fit's own busy share).
                    text.Append(d.Id).Append(d.Role).Append(F(d.Dps)).Append(d.Targets).Append(F(d.RepairPerSecond));
                }

                if (facts.Geometry != null)
                {
                    text.Append(facts.Geometry.GroundSamples).Append(facts.Geometry.Spots.Count).Append(F(facts.Geometry.CellSize));
                    foreach (Cell spot in facts.Geometry.Spots)
                    {
                        text.Append(spot.X).Append(',').Append(spot.Y).Append(F(facts.Geometry.SpotRange(spot))).Append(';');
                    }
                }
            }

            BalanceRules r = model.Rules;
            text.Append(F(r.Utilisation)).Append(F(r.HumanUtilisation)).Append(F(r.MaxSplashTargets)).Append(F(r.HeroShare)).Append(F(r.GateWindowShare)).Append(F(r.MaxGateHoldSeconds)).Append(F(r.CarriedShare)).Append(F(r.FrontEfficiency)).Append(F(r.TrunkReference)).Append(r.AttackersPerGate).Append(F(r.BossWaveCap)).Append(r.ShockwaveCrowd).Append(F(r.ArmorThreatShare));
            return Fnv(text.ToString());
        }

        private static string Fnv(string text)
        {
            uint hash = Offset;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= Prime;
            }

            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }

        private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static SortedDictionary<string, T> Sorted<T>(Dictionary<string, T> source) => new SortedDictionary<string, T>(source, System.StringComparer.Ordinal);
    }
}
