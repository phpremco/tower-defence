using System.Collections;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// Splitting (the Shattered, Docs/BOSSES.md): it comes out of a sealed gate in the old ruins. Near its split point cracks
    /// of light pulse on its body; at the split it bursts (a flash, one kick) into three shards that each take a different
    /// route (its own road, the air, the second road), each with an equal share of what it had left, and each shard's route
    /// lights up briefly. While it is split the boss bar shows what the shards have left together. At its rejoin point the
    /// surviving shards pull back together where the one nearest the base stands; every shard that died takes a share off
    /// the giant that forms, and its core spire stays shrunk.
    /// </summary>
    public sealed class SplitSignature : IBossSignature
    {
        private const float EntranceSeconds = 1.6f;
        private const int EntranceCracks = 5;
        private const float EntranceCrackLength = 2.6f;
        private const float EntranceBurstRadius = 2f;
        private const float WarnIntensity = 6f;
        private const float WarnLift = 1.6f;
        private const float SplitBurstRadius = 2.2f;
        private const float ChestHeight = 2f;
        private const float GateRumbleSeconds = 0.8f;
        // The gate stands this far behind the spot the giant appears on, so the giant steps out of its threshold.
        private const float GateBehind = 0.6f;
        // The split flash across (README round 29: about 6 m at chest height).
        private const float FlashSize = 6f;
        private static readonly Color Crystal = new Color(0.72f, 0.88f, 1f);

        private sealed class Shard
        {
            public Enemy Enemy;
            public ShardSpec Spec;
            public float SpawnLeft;
            public bool Dead;
            public bool Gone;
        }

        private readonly ContinentBossBrain _brain;
        private readonly List<Shard> _shards = new List<Shard>(3);
        private readonly List<CrackLineView> _lines = new List<CrackLineView>(8);
        private readonly TowerAuraView _glow;
        private readonly FlipbookFx _flash;
        private readonly FlipbookFx _rejoinFlash;
        private readonly BoneScaleView _shells;
        private readonly BoneScaleView _crystals;
        private readonly BoneScaleView _deadCores;
        private bool _split;

        public SplitSignature(ContinentBossBrain brain)
        {
            _brain = brain;
            _glow = TowerAuraView.Create(brain.Field.Effects, "SplitCracks");
            _flash = FlipbookFx.Create(brain.Field.Effects, "ShardFlash", BossFxArt.Sheet.ShardFlash, false);
            _rejoinFlash = FlipbookFx.Create(brain.Field.Effects, "RejoinFlash", BossFxArt.Sheet.ShardFlash, false, true);
            _shells = brain.gameObject.AddComponent<BoneScaleView>();
            _crystals = brain.gameObject.AddComponent<BoneScaleView>();
            _deadCores = brain.gameObject.AddComponent<BoneScaleView>();
        }

        private BossField Field => _brain.Field;
        private Enemy Enemy => _brain.Enemy;
        private EnemyView View => _brain.View;
        private BossDefinition Boss => _brain.Boss;
        private SplitPower Power => _brain.Boss.Split;
        private Vector3 Position => _brain.transform.position;

        /// <summary>True while it is split into its shards.</summary>
        public bool IsSplit => _split && !_brain.Record.Rejoined;

        public bool WantsOffStructure => false;

        public void Begin()
        {
            _split = false;
            _shards.Clear();
            _glow.Hide();
            FadeLines();
            foreach (BoneScaleView look in new[] { _shells, _crystals, _deadCores })
            {
                look.Holding = false;
                look.SetBones(new Transform[0]);
            }
        }

        // The sealed gate in the ruins opens for the first and only time: the ground cracks with light round it, it rumbles,
        // and the giant breaks out in a burst of crystal.
        public IEnumerator Entrance()
        {
            _brain.State = "Entrance";
            Enemy.SetDirected(true);
            Enemy.SetConcealed(true, 0f);
            View.SetHidden(true);
            Vector3 spot = _brain.OnGround(Position);
            Vector3 towardRoad = BossGeometry.Flat(OffRoadRoute.NearestRoadCellWorld(Field.Level, spot) - spot);
            // The gate faces the fixed camera, so its opening and the light inside it are seen (side on it reads as a wall).
            Camera view = Camera.main;
            Vector3 facing = view != null ? BossGeometry.Flat(-view.transform.forward) : towardRoad;
            RuinGateView gate = RuinGateView.Create(Field.Effects, spot - facing * GateBehind, facing, Crystal);
            if (gate != null)
            {
                yield return ThroughTheGate(gate, spot, towardRoad);
                yield break;
            }

            for (int i = 0; i < EntranceCracks; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 360f / EntranceCracks, 0f) * towardRoad;
                _lines.Add(CrackLineView.Create(Field.Effects, Field.Draped(new List<Vector3> { spot, spot + dir * EntranceCrackLength }), CrackStyle.Light));
            }

            float elapsed = 0f;
            while (elapsed < EntranceSeconds && _brain.Alive)
            {
                elapsed += Time.deltaTime;
                float share = Mathf.Clamp01(elapsed / EntranceSeconds);
                Field.Context.BossRumble = share;
                _glow.Show(spot, Crystal, WarnIntensity * share, true);
                foreach (CrackLineView line in _lines)
                {
                    line.OpenTo(line.Length * share);
                }

                yield return null;
            }

            Field.Context.BossRumble = 0f;
            _glow.Hide();
            FadeLines();
            Field.BreakDecor(spot, EntranceBurstRadius);
            _brain.Impact(spot, EntranceBurstRadius, Crystal);
            Field.Context.BossImpact.Raise(Boss.ImpactKick);
            Enemy.Place(spot, towardRoad);
            View.SetHidden(false);
            Enemy.SetConcealed(false);
            yield return _brain.Roar();
            yield return _brain.BackToRoad();
        }

        // README round 29: the sealed gate rumbles shut, its seal cracks (shards), light floods the seams and the giant
        // stands in the flash as the petals fold back; it walks out once the gate stands open.
        private IEnumerator ThroughTheGate(RuinGateView gate, Vector3 spot, Vector3 towardRoad)
        {
            Field.BreakDecor(spot, EntranceBurstRadius);
            float stirred = 0f;
            while (stirred < GateRumbleSeconds && _brain.Alive)
            {
                stirred += Time.deltaTime;
                Field.Context.BossRumble = Mathf.Clamp01(stirred / GateRumbleSeconds);
                yield return null;
            }

            gate.Open();
            yield return _brain.Wait(RuinGateView.SealCrackSeconds);
            _brain.Impact(gate.SealPosition, EntranceBurstRadius * 0.6f, Crystal, false);
            yield return _brain.Wait(RuinGateView.FirstLightSeconds - RuinGateView.SealCrackSeconds);
            Field.Context.BossRumble = 0f;
            _flash?.Play(spot + Vector3.up * (ChestHeight * _brain.Scale), FlashSize * _brain.Scale, 0f, Color.white);
            _brain.Impact(spot, EntranceBurstRadius, Crystal);
            Field.Context.BossImpact.Raise(Boss.ImpactKick);
            Enemy.Place(spot, towardRoad);
            View.SetHidden(false);
            Enemy.SetConcealed(false);
            yield return _brain.Roar();
            float wait = RuinGateView.FullyOpenSeconds - gate.Opened;
            if (wait > 0f)
            {
                yield return _brain.Wait(wait);
            }

            yield return _brain.BackToRoad();
        }

        public IEnumerator NextAct()
        {
            if (!_split && Boss.PhaseAt(Enemy.Health.Fraction) >= Power.SplitPhase && Power.Shards.Length > 0 && Field.Spawner != null)
            {
                return Split();
            }

            return null;
        }

        public IEnumerator AtStructure(IStructure structure) => null;

        public void OnRoad(float deltaTime)
        {
        }

        // Cracks of light pulse on it as it nears its split point.
        public void Always(float deltaTime)
        {
            if (_split)
            {
                return;
            }

            float splitAt = Boss.Phases.Length > Power.SplitPhase ? Boss.Phases[Power.SplitPhase].StartsAt : 0f;
            float health = Enemy.Health.Fraction;
            if (health <= splitAt + Power.WarnShare && !Enemy.IsConcealed)
            {
                Vector3 chest = Position + Vector3.up * (WarnLift * _brain.Scale);
                _glow.Show(new Vector3(chest.x, Position.y, chest.z), Crystal, WarnIntensity * Mathf.InverseLerp(splitAt + Power.WarnShare, splitAt, health), true);
            }
        }

        public void End()
        {
            _glow.Hide();
            FadeLines();
            Field.Context.BossRumble = 0f;
        }

        // ---------------------------------------------------------------- the split

        private IEnumerator Split()
        {
            _split = true;
            _brain.State = "Splitting";
            _brain.Record.Splits++;
            Enemy.SetDirected(true);
            _glow.Hide();
            BossClip clip = BossClip.Of(View, ModelClipRole.Split, 1f);
            if (clip.Exists)
            {
                View.PlayScripted(ModelClipRole.Split, clip.Rate, true);
            }

            yield return _brain.Wait(clip.Moment);
            if (!_brain.Alive)
            {
                yield break;
            }

            Vector3 at = Position;
            _brain.Impact(at, SplitBurstRadius, Crystal);
            _flash?.Play(at + Vector3.up * (ChestHeight * _brain.Scale), FlashSize * _brain.Scale, 0f, Color.white);
            Field.Context.BossImpact.Raise(Boss.ImpactKick);
            Field.Context.BossSplit.Raise(at);
            // Its health is the shards' now: towers cannot reach the giant until it forms again.
            Enemy.SetConcealed(true, 0f);
            SpawnShards(at);
            yield return _brain.Wait(clip.Seconds - clip.Moment);
            View.ReleaseScript();
            View.SetHidden(true);
            _brain.State = "Split";
            float rejoinAt = Boss.Phases.Length > Power.RejoinPhase ? Boss.Phases[Power.RejoinPhase].StartsAt : 0f;
            while (_brain.Alive)
            {
                StepShards(Time.deltaTime);
                Shard lead = Leader();
                if (lead == null || Enemy.Health.Fraction <= rejoinAt)
                {
                    break;
                }

                Enemy.Place(_brain.OnGround(lead.Enemy.Position), lead.Enemy.Heading);
                yield return null;
            }

            if (_brain.Alive)
            {
                yield return Rejoin();
            }
        }

        private void SpawnShards(Vector3 at)
        {
            float each = ShardSplit.ShardHealth(Enemy.Health.Current, Power.Shards.Length);
            Vector3 forward = Enemy.Heading;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float scale = _brain.Scale;
            foreach (ShardSpec spec in Power.Shards)
            {
                if (spec == null || spec.Enemy == null)
                {
                    continue;
                }

                Vector3 point = at + (right * spec.Offset.x + Vector3.up * spec.Offset.y + forward * spec.Offset.z) * scale;
                int path = PathFor(spec.Route, at);
                Enemy shard = Field.Spawner.SpawnOnPath(spec.Enemy, point, path);
                if (shard == null)
                {
                    continue;
                }

                shard.Health.Refill(each);
                shard.SetDirected(true);
                shard.Place(spec.Enemy.Flies ? point : _brain.OnGround(point), forward);
                var view = shard.GetComponent<EnemyView>();
                view?.SkipSpawn();
                BossClip spawn = BossClip.Of(view, ModelClipRole.Spawn, 1f);
                if (spawn.Exists)
                {
                    view.PlayScripted(ModelClipRole.Spawn, spawn.Rate, true);
                }

                shard.Killed += OnShardKilled;
                shard.ReachedEnd += OnShardReachedEnd;
                _shards.Add(new Shard { Enemy = shard, Spec = spec, SpawnLeft = spawn.Seconds });
                _brain.Record.Shards.Add(shard);
                LightRoute(point, path);
            }
        }

        // A shard waits out its spawn clip where it burst out, then takes its route; the giant's health follows theirs.
        private void StepShards(float deltaTime)
        {
            float together = 0f;
            foreach (Shard shard in _shards)
            {
                if (shard.Dead || shard.Gone || !shard.Enemy.IsAlive)
                {
                    continue;
                }

                together += shard.Enemy.Health.Current;
                if (shard.SpawnLeft > 0f)
                {
                    shard.SpawnLeft -= deltaTime;
                    if (shard.SpawnLeft <= 0f)
                    {
                        shard.Enemy.GetComponent<EnemyView>()?.ReleaseScript();
                        shard.Enemy.SetDirected(false);
                    }
                }
            }

            float lost = Enemy.Health.Current - together;
            if (lost > 0f)
            {
                Enemy.Health.TakeDamage(lost);
            }
        }

        // The living shard nearest the base: the giant forms where it stands.
        private Shard Leader()
        {
            Vector3 home = Field.Context.Base != null ? Field.Context.Base.Position : Position;
            Shard lead = null;
            float nearest = float.PositiveInfinity;
            foreach (Shard shard in _shards)
            {
                if (shard.Dead || shard.Gone || !shard.Enemy.IsAlive)
                {
                    continue;
                }

                float distance = BossGeometry.FlatDistance(shard.Enemy.Position, home);
                // A ground shard leads before the flyer at the same distance: the giant forms on the ground.
                if (shard.Enemy.Definition.Flies)
                {
                    distance += Field.Grid.CellSize;
                }

                if (distance < nearest)
                {
                    nearest = distance;
                    lead = shard;
                }
            }

            return lead;
        }

        private void OnShardKilled(Enemy enemy)
        {
            Shard shard = ShardOf(enemy);
            if (shard != null && !shard.Gone)
            {
                shard.Dead = true;
                _brain.Record.ShardsKilled++;
            }

            Unhook(enemy);
        }

        private void OnShardReachedEnd(Enemy enemy)
        {
            Shard shard = ShardOf(enemy);
            if (shard != null)
            {
                shard.Gone = true;
            }

            Unhook(enemy);
        }

        private Shard ShardOf(Enemy enemy)
        {
            foreach (Shard shard in _shards)
            {
                if (shard.Enemy == enemy)
                {
                    return shard;
                }
            }

            return null;
        }

        private void Unhook(Enemy enemy)
        {
            enemy.Killed -= OnShardKilled;
            enemy.ReachedEnd -= OnShardReachedEnd;
        }

        // ---------------------------------------------------------------- the rejoin

        // The survivors crack into light, fly to where the leader stands and the giant forms there; each dead shard takes its
        // share off the giant, and its spire stays shrunk.
        private IEnumerator Rejoin()
        {
            _brain.State = "Rejoining";
            Shard lead = Leader();
            Vector3 spot = lead.Enemy.Definition.Flies ? OffRoadRoute.NearestRoadCellWorld(Field.Level, lead.Enemy.Position) : lead.Enemy.Position;
            spot = _brain.OnGround(spot);
            var survivors = new List<Shard>(_shards.Count);
            float shardSeconds = 0f;
            foreach (Shard shard in _shards)
            {
                if (shard.Dead || shard.Gone || !shard.Enemy.IsAlive)
                {
                    continue;
                }

                survivors.Add(shard);
                shard.Enemy.SetDirected(true);
                var view = shard.Enemy.GetComponent<EnemyView>();
                BossClip rejoin = BossClip.Of(view, ModelClipRole.Rejoin, 1f);
                if (rejoin.Exists)
                {
                    view.PlayScripted(ModelClipRole.Rejoin, rejoin.Rate, true);
                    shardSeconds = Mathf.Max(shardSeconds, rejoin.Seconds);
                }
            }

            yield return _brain.Wait(shardSeconds);
            if (!_brain.Alive)
            {
                yield break;
            }

            Vector3 home = Field.Context.Base != null ? Field.Context.Base.Position : spot + Enemy.Heading;
            Enemy.Place(spot, BossGeometry.Flat(home - spot));
            View.SetHidden(false);
            Enemy.SetConcealed(false);
            BossClip clip = BossClip.Of(View, ModelClipRole.Rejoin, 1f);
            if (clip.Exists)
            {
                View.PlayScripted(ModelClipRole.Rejoin, clip.Rate, true);
            }

            if (_rejoinFlash != null)
            {
                // The split played backwards: the fragments and the ring converge and end on the forming frame.
                _brain.StartCoroutine(FlashAt(spot + Vector3.up * (ChestHeight * _brain.Scale), (clip.Exists ? clip.Moment : 0f) - _rejoinFlash.Lifetime));
            }

            yield return FlyBack(survivors, spot + Vector3.up * (ChestHeight * _brain.Scale), Mathf.Min(Power.FlyBackSeconds, clip.Exists ? clip.Moment : Power.FlyBackSeconds));
            yield return _brain.Wait((clip.Exists ? clip.Moment : 0f) - Power.FlyBackSeconds);
            // What the survivors still have as they melt into it (they were shot on the way in too).
            float survivorsHealth = 0f;
            foreach (Shard shard in survivors)
            {
                survivorsHealth += shard.Enemy.IsAlive ? shard.Enemy.Health.Current : 0f;
            }

            foreach (Shard shard in survivors)
            {
                Unhook(shard.Enemy);
                shard.Gone = true;
                Field.Spawner?.Withdraw(shard.Enemy);
            }

            int dead = 0;
            foreach (Shard shard in _shards)
            {
                dead += shard.Dead ? 1 : 0;
            }

            float health = ShardSplit.RejoinHealth(survivorsHealth, dead, Power.DeadShardPenalty);
            float lost = Enemy.Health.Current - health;
            if (lost > 0f)
            {
                Enemy.Health.TakeDamage(lost);
            }

            _brain.Record.Rejoined = true;
            _brain.Record.RejoinedShare = Enemy.Health.Fraction;
            _brain.Impact(spot, SplitBurstRadius, Crystal, false);
            WearRejoinedLook();
            yield return _brain.Wait(clip.Seconds - clip.Moment);
            View.ReleaseScript();
            _brain.State = "Road";
            yield return _brain.BackToRoad();
        }

        private IEnumerator FlyBack(List<Shard> survivors, Vector3 into, float seconds)
        {
            var from = new List<Vector3>(survivors.Count);
            foreach (Shard shard in survivors)
            {
                from.Add(shard.Enemy.Position);
            }

            float elapsed = 0f;
            while (elapsed < seconds && _brain.Alive)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, seconds));
                for (int i = 0; i < survivors.Count; i++)
                {
                    if (survivors[i].Enemy.IsAlive)
                    {
                        survivors[i].Enemy.Place(Vector3.Lerp(from[i], into, t * t), BossGeometry.Flat(into - from[i]));
                    }
                }

                yield return null;
            }
        }

        // The rejoined giant (README round 28 phase 3): no shells, the crystals grown, a dead shard's spire shrunk.
        private void WearRejoinedLook()
        {
            _shells.SetBones(BonesOf(Power.ShellBones));
            _shells.SetScale(0f, true);
            _shells.Holding = true;
            _crystals.SetBones(BonesOf(Power.CrystalBones));
            _crystals.SetScale(Power.RejoinedCrystalScale, true);
            _crystals.Holding = true;
            var dead = new List<string>(_shards.Count);
            foreach (Shard shard in _shards)
            {
                if (shard.Dead && !string.IsNullOrEmpty(shard.Spec.Spire))
                {
                    dead.Add(shard.Spec.Spire);
                }
            }

            _deadCores.SetBones(BonesOf(dead.ToArray()));
            _deadCores.SetScale(Power.DeadCoreScale, true);
            _deadCores.Holding = dead.Count > 0;
        }

        private Transform[] BonesOf(string[] names)
        {
            var bones = new List<Transform>(names.Length * 2);
            foreach (string bone in names)
            {
                bones.AddRange(View.Bones(bone));
            }

            return bones.ToArray();
        }

        // ---------------------------------------------------------------- routes

        // The level path a shard takes: the ground path nearest the giant, another ground path, or the first flight path.
        private int PathFor(ShardRoute route, Vector3 at)
        {
            EnemyPath[] paths = Field.Level.Paths;
            int own = -1;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i].Flying)
                {
                    continue;
                }

                float distance = DistanceToPath(i, at);
                if (distance < nearest)
                {
                    nearest = distance;
                    own = i;
                }
            }

            switch (route)
            {
                case ShardRoute.OtherRoad:
                    for (int i = 0; i < paths.Length; i++)
                    {
                        if (!paths[i].Flying && i != own)
                        {
                            return i;
                        }
                    }

                    return own;
                case ShardRoute.Air:
                    for (int i = 0; i < paths.Length; i++)
                    {
                        if (paths[i].Flying)
                        {
                            return i;
                        }
                    }

                    return own;
                default:
                    return own;
            }
        }

        private float DistanceToPath(int path, Vector3 at)
        {
            IReadOnlyList<Vector3> points = Field.Spawner.PathPoints(path);
            float best = float.PositiveInfinity;
            foreach (Vector3 point in points)
            {
                best = Mathf.Min(best, BossGeometry.FlatDistance(point, at));
            }

            return best;
        }

        // The shard's route lights up briefly from where it burst out to the base.
        private void LightRoute(Vector3 from, int path)
        {
            IReadOnlyList<Vector3> points = Field.Spawner.PathPoints(path);
            if (points.Count == 0)
            {
                return;
            }

            int start = 0;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < points.Count; i++)
            {
                float distance = BossGeometry.FlatDistance(points[i], from);
                if (distance < nearest)
                {
                    nearest = distance;
                    start = i;
                }
            }

            var route = new List<Vector3> { new Vector3(from.x, 0f, from.z) };
            for (int i = start; i < points.Count; i++)
            {
                route.Add(new Vector3(points[i].x, 0f, points[i].z));
            }

            CrackLineView line = CrackLineView.Create(Field.Effects, Field.Draped(route), CrackStyle.Light);
            line.OpenTo(line.Length);
            _lines.Add(line);
            _brain.StartCoroutine(FadeAfter(line, Power.RouteLightSeconds));
        }

        private IEnumerator FadeAfter(CrackLineView line, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (line != null)
            {
                line.Fade();
                _lines.Remove(line);
            }
        }

        private IEnumerator FlashAt(Vector3 at, float delay)
        {
            if (delay > 0f)
            {
                yield return _brain.Wait(delay);
            }

            _rejoinFlash.Play(at, FlashSize * _brain.Scale, 0f, Color.white);
        }

        private void FadeLines()
        {
            foreach (CrackLineView line in _lines)
            {
                if (line != null)
                {
                    line.Fade();
                }
            }

            _lines.Clear();
        }
    }
}
