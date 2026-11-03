using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// The kill money a player really has when a wave starts. A kill pays when the creature dies, and creatures die where
    /// the defence holds them: at the first gate every road passes (the towers around it, the hero on its post). So a
    /// wave's money arrives only after its creatures have walked from their spawn to that gate, which on a long road is
    /// two or more waves later for walkers and many more for slow heavy creatures. Paying a wave's kills before the next
    /// wave (the old rule) gave the model a third more money in the opening waves than the PlayMode bot ever had.
    /// The clock runs on the game's own rhythm: a wave starts its delay after the previous one finished spawning.
    /// </summary>
    public sealed class KillPurse
    {
        private readonly LevelInput _level;
        private readonly DifficultyStats _difficulty;
        private readonly BalanceRules _rules;
        private readonly float _defenceRemaining;
        private readonly List<(float Time, float Money, float Health)> _pending = new List<(float, float, float)>();
        private float _clock;
        private float _waveStart;
        private float _paid;

        public KillPurse(LevelInput level, DifficultyStats difficulty, BalanceRules rules)
        {
            _level = level;
            _difficulty = difficulty;
            _rules = rules;
            _defenceRemaining = DefenceRemaining(level.Shape);
        }

        /// <summary>Kill money paid so far (after leaks), as of the start of the current wave.</summary>
        public float Paid => _paid;

        /// <summary>Seconds from the first wave's countdown to the start of the current wave.</summary>
        public float WaveStart => _waveStart;

        /// <summary>Starts the next wave's clock (its countdown runs out) and collects what has died by then.</summary>
        public void Begin(WaveSpec wave)
        {
            _clock += wave.StartDelay;
            _waveStart = _clock;
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                if (_pending[i].Time <= _waveStart)
                {
                    _paid += _pending[i].Money;
                    _pending.RemoveAt(i);
                }
            }
        }

        /// <summary>Money before the current wave: start, the kills paid by now and the mines, less one harvester.</summary>
        public int Money(int waveIndex, int waveCount) => Ledger.MoneyAt(_level, waveIndex, waveCount, _paid, _rules);

        /// <summary>
        /// Books the wave's kills for the moment each creature reaches the defence. A wave over pressure one leaks in
        /// proportion, and creatures that get through pay nothing.
        /// </summary>
        public void Add(WaveSpec wave, WaveLine line)
        {
            float kept = Math.Min(1f, line.Pressure > 0f ? 1f / line.Pressure : 1f);
            float scale = _difficulty.Health * _level.HealthScale * wave.HealthMultiplier;
            float span = 0f;
            foreach (GroupSpec group in wave.Groups)
            {
                EnemyStats enemy = group.Enemy;
                if (enemy == null)
                {
                    continue;
                }

                int count = enemy.IsBoss ? group.Count : Math.Max(1, (int)Math.Ceiling(group.Count * _difficulty.Count));
                float travel = Travel(group) / Math.Max(0.01f, enemy.Speed);
                // A boss is its own single target (WaveLoad.BossHealth), not part of a later wave's stream.
                bool ground = Layer.Has(enemy.Layer, Layer.Ground) && !enemy.IsBoss;
                float health = enemy.EffectiveHealth * scale;
                for (int k = 0; k < count; k++)
                {
                    float spawn = group.Delay + (group.Stagger + k * group.Interval) * _difficulty.Interval;
                    span = Math.Max(span, spawn);
                    _pending.Add((_waveStart + spawn + travel, enemy.Reward * kept, ground ? health : 0f));
                }
            }

            _clock = _waveStart + span;
        }

        /// <summary>
        /// Ground health of earlier waves that reaches the defence while this wave does. Creatures walk at their own pace, so
        /// on a long road the slow heavy ones of a few waves ago arrive together with the fast ones of this wave, and the
        /// towers share their time between both (Crysium Vein wave 9: rollers and prowlers on top of the cragbacks of waves
        /// 3 to 6, where the bot lost with the ledger at 0.74). Call after <see cref="Begin"/>.
        /// </summary>
        public float Carried(WaveSpec wave)
        {
            float first = float.MaxValue;
            float last = float.MinValue;
            foreach (GroupSpec group in wave.Groups)
            {
                EnemyStats enemy = group.Enemy;
                if (enemy == null || enemy.IsBoss || !Layer.Has(enemy.Layer, Layer.Ground))
                {
                    continue;
                }

                int count = enemy.IsBoss ? group.Count : Math.Max(1, (int)Math.Ceiling(group.Count * _difficulty.Count));
                float travel = Travel(group) / Math.Max(0.01f, enemy.Speed);
                first = Math.Min(first, _waveStart + group.Delay + group.Stagger * _difficulty.Interval + travel);
                last = Math.Max(last, _waveStart + group.Delay + (group.Stagger + (count - 1) * group.Interval) * _difficulty.Interval + travel);
            }

            float carried = 0f;
            foreach ((float time, float _, float health) in _pending)
            {
                if (time >= first && time <= last)
                {
                    carried += health;
                }
            }

            return carried;
        }

        // Cells a group walks from where it appears to the defence.
        private float Travel(GroupSpec group)
        {
            LevelShape shape = _level.Shape;
            float toBase;
            if (!Layer.Has(group.Enemy.Layer, Layer.Ground) || group.Source == 0)
            {
                LevelShape.Lane lane = Layer.Has(group.Enemy.Layer, Layer.Ground) ? shape.LaneFor(group.Path) : null;
                toBase = lane != null ? lane.Length : shape.MainLength;
            }
            else if (group.Source == WaveLoad.EntranceSource)
            {
                toBase = shape.EntranceRemaining * shape.MainLength;
            }
            else
            {
                toBase = MeanNestRemaining(shape) * shape.MainLength;
            }

            return Math.Max(0f, toBase - _defenceRemaining);
        }

        /// <summary>
        /// Cells from the defence to the base along the main road: from the first gate on the trunk (every lane passes it),
        /// else the first gate, else the middle of the road.
        /// </summary>
        public static float DefenceRemaining(LevelShape shape)
        {
            LevelShape.Gate best = null;
            foreach (LevelShape.Gate gate in shape.Gates)
            {
                if (gate.OnTrunk && (best == null || gate.Progress < best.Progress))
                {
                    best = gate;
                }
            }

            if (best == null && shape.Gates.Count > 0)
            {
                best = shape.Gates[0];
            }

            float progress = best != null ? best.Progress : 0.5f;
            return (1f - progress) * shape.MainLength;
        }

        private static float MeanNestRemaining(LevelShape shape)
        {
            if (shape.NestRemaining.Count == 0)
            {
                return 1f;
            }

            float sum = 0f;
            foreach (float remaining in shape.NestRemaining)
            {
                sum += remaining;
            }

            return sum / shape.NestRemaining.Count;
        }
    }
}
