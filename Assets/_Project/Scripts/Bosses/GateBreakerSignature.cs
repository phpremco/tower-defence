using System.Collections;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Towers;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// Gate Breaker (the Frost Warden, Docs/BOSSES.md): frost creeps over the ground ahead of it, the gate it is heading
    /// for gets a blue haze and a crack; at the gate it breathes it into an ice block (the gate stands but holds its fire
    /// for the freeze), climbs onto the block (it sags, and the walkers follow over it) and, as the freeze runs out, breaks
    /// down through it: the gate is a wreck that can be rebuilt. Every broken gate raises its rage (faster, the icicles on
    /// its back grow). The hero's repair thaws a frozen gate: the Warden slips off over the far side and the gate is saved.
    /// Its ice armour lets only a share of each hit through; fire cracks it for double damage for a few seconds; in its
    /// exposed phase the armour bursts off for good and it runs.
    /// </summary>
    public sealed class GateBreakerSignature : IBossSignature
    {
        private const float FreezeReachMargin = 0.35f;
        private const float AheadDot = 0.5f;
        private const float HazeIntensity = 5f;
        private const float CreepIntensity = 2.5f;
        private const float CreepAhead = 2.2f;
        private const float ChillMistHeight = 0.25f;
        private const float ChillMistRadius = 1.4f;
        private const float ChillMistRate = 12f;
        private const float ChillLightHeight = 3f;
        private const float ChillLightRange = 6f;
        private const float ChillLightIntensity = 2.5f;
        private static readonly Color ChillMist = new Color(0.75f, 0.9f, 1f, 0.28f);
        private const float BreathBurstRadius = 1.2f;
        private const float ShardBurstRadius = 1.8f;
        private static readonly Color Haze = new Color(0.5f, 0.82f, 1f);
        private static readonly Color Creep = new Color(0.62f, 0.9f, 1f);
        private static readonly Color IceShards = new Color(0.8f, 0.95f, 1f);

        private readonly ContinentBossBrain _brain;
        private readonly Dictionary<Tower, float> _refreeze = new Dictionary<Tower, float>();
        private readonly List<Tower> _expired = new List<Tower>(4);
        private readonly List<Transform> _rageBones = new List<Transform>(10);
        private readonly TowerAuraView _haze;
        private readonly TowerAuraView _creep;
        private readonly FrostCreepView _frost;
        private readonly ParticleSystem _chill;
        private readonly Light _chillLight;
        private readonly BoneScaleView _icicles;
        private GateRage _rage = new GateRage(0f, 1);
        private IceArmour _armour = new IceArmour(1f, 1f, 1f);
        private Tower _warned;
        private bool _exposed;

        public GateBreakerSignature(ContinentBossBrain brain)
        {
            _brain = brain;
            _haze = TowerAuraView.Create(brain.Field.Effects, "GateHaze");
            _creep = TowerAuraView.Create(brain.Field.Effects, "FrostCreep");
            _frost = FrostCreepView.Create(brain.Field.Effects);
            // Cold rolls off it: a low mist round its feet and a pale light on its chest, so it reads as ice from above.
            _chill = ParticleKit.Create(brain.transform, "ColdMist", Vector3.up * ChillMistHeight, ChillMist, 1.6f, 2.6f, 0.7f, 1.4f, 0.05f, 0.25f, -0.01f, 90, ChillMistRadius);
            ParticleKit.SetRate(_chill, ChillMistRate);
            var glow = new GameObject("ColdLight");
            glow.transform.SetParent(brain.transform, false);
            glow.transform.localPosition = Vector3.up * ChillLightHeight;
            _chillLight = glow.AddComponent<Light>();
            _chillLight.type = LightType.Point;
            _chillLight.color = Haze;
            _chillLight.range = ChillLightRange;
            _chillLight.intensity = ChillLightIntensity;
            _chillLight.shadows = LightShadows.None;
            _icicles = brain.gameObject.AddComponent<BoneScaleView>();
            brain.Enemy.DamagedBy += OnDamaged;
        }

        private BossField Field => _brain.Field;
        private Enemy Enemy => _brain.Enemy;
        private EnemyView View => _brain.View;
        private BossDefinition Boss => _brain.Boss;
        private GateBreakerPower Power => _brain.Boss.GateBreaker;
        private Vector3 Position => _brain.transform.position;

        /// <summary>Its rage and armour, for the tests.</summary>
        public GateRage Rage => _rage;

        public IceArmour Armour => _armour;

        public bool WantsOffStructure => false;

        public void Begin()
        {
            ParticleKit.SetRate(_chill, ChillMistRate);
            _chillLight.enabled = true;
            _rage = new GateRage(Power.RagePerGate, Power.MaxRage);
            _armour = new IceArmour(Power.ArmourSoak, Power.CrackedFactor, Power.CrackSeconds);
            _exposed = false;
            _warned = null;
            _refreeze.Clear();
            Enemy.SetDamageScale(_armour.DamageFactor);
            if (!string.IsNullOrEmpty(Power.ArmourPart))
            {
                View.SetPartVisible(Power.ArmourPart, true);
            }

            _rageBones.Clear();
            foreach (string bone in Power.RageBones)
            {
                _rageBones.AddRange(View.Bones(bone));
            }

            _icicles.SetBones(_rageBones.ToArray());
            _icicles.SetScale(_rage.BoneScale(Power.RageScale), true);
        }

        public IEnumerator Entrance() => null;

        public IEnumerator AtStructure(IStructure structure) => null;

        public IEnumerator NextAct()
        {
            if (!_exposed && Boss.PhaseAt(Enemy.Health.Fraction) >= Power.ExposedPhase)
            {
                return BreakArmour();
            }

            Tower gate = GateAhead(FreezeReach());
            return gate != null ? BreakGate(gate) : null;
        }

        // On the road: the frost creeps ahead of it, and the next gate gets its haze and its crack.
        public void OnRoad(float deltaTime)
        {
            Vector3 ahead = Position + Enemy.Heading * CreepAhead;
            _creep.Show(new Vector3(ahead.x, Field.GroundAt(ahead), ahead.z), Creep, CreepIntensity, true);
            _frost?.Creep(new Vector3(ahead.x, Field.GroundAt(ahead), ahead.z));
            Tower gate = GateAhead(Power.WarnDistance);
            if (gate == _warned)
            {
                return;
            }

            _warned = gate;
            if (gate == null)
            {
                _haze.Hide();
                return;
            }

            _haze.Show(gate.Position, Haze, HazeIntensity, true);
            Field.Context.BossGateWarned.Raise(gate.Position);
        }

        public void Always(float deltaTime)
        {
            if (_armour.Step(deltaTime))
            {
                // The crack froze over again.
                ShowArmour(true);
            }

            Enemy.SetDamageScale(_armour.DamageFactor);
            if (_refreeze.Count == 0)
            {
                return;
            }

            _expired.Clear();
            _expired.AddRange(_refreeze.Keys);
            foreach (Tower gate in _expired)
            {
                float left = _refreeze[gate] - deltaTime;
                if (left <= 0f)
                {
                    _refreeze.Remove(gate);
                }
                else
                {
                    _refreeze[gate] = left;
                }
            }
        }

        public void End()
        {
            ParticleKit.SetRate(_chill, 0f);
            _chillLight.enabled = false;
            _haze.Hide();
            _creep.Hide();
            _warned = null;
        }

        // ---------------------------------------------------------------- the armour

        // Fire on the ice armour cracks it: for a few seconds every hit lands double.
        private void OnDamaged(Enemy enemy, object source, float amount)
        {
            if (!(source is IBurningSource burning) || !burning.Burns || !_armour.Burn())
            {
                return;
            }

            _brain.Record.ArmourCracks++;
            ShowArmour(false);
            Enemy.SetDamageScale(_armour.DamageFactor);
        }

        private void ShowArmour(bool on)
        {
            if (!string.IsNullOrEmpty(Power.ArmourPart))
            {
                View.SetPartVisible(Power.ArmourPart, on);
            }

            _brain.Impact(Position + Vector3.up, ShardBurstRadius * 0.6f, IceShards, false);
        }

        // Its exposed phase: the plates burst off for good, every hit lands in full and it runs.
        private IEnumerator BreakArmour()
        {
            _exposed = true;
            _armour.Break();
            _brain.Record.ArmourBroken = true;
            Enemy.SetDamageScale(_armour.DamageFactor);
            _brain.State = "ArmourBreak";
            Enemy.SetDirected(true);
            _creep.Hide();
            BossClip clip = BossClip.Of(View, ModelClipRole.ArmourBreak, Boss.MechanicClipRate);
            if (clip.Exists)
            {
                View.PlayScripted(ModelClipRole.ArmourBreak, clip.Rate, true);
                yield return _brain.Wait(clip.Moment);
            }

            if (!string.IsNullOrEmpty(Power.ArmourPart))
            {
                View.SetPartVisible(Power.ArmourPart, false);
            }

            _brain.Impact(Position, ShardBurstRadius, IceShards);
            Field.Context.BossImpact.Raise(Boss.ImpactKick);
            // Its icicles leave with the armour (README round 28).
            _icicles.SetScale(0f);
            if (clip.Exists)
            {
                yield return _brain.Wait(clip.Seconds - clip.Moment);
            }

            View.ReleaseScript();
            View.UseMoveCycle(ModelClipRole.EnragedMove);
            Enemy.SetPace(Pace());
            _brain.JoinRoad();
        }

        private float Pace() => _rage.Pace * (_exposed ? Power.ExposedPace : 1f);

        // ---------------------------------------------------------------- the gates

        private float FreezeReach() => BossClip.Of(View, ModelClipRole.FreezeGate, Boss.MechanicClipRate).Anchor + FreezeReachMargin;

        // The living, unfrozen gate on the road ahead within a distance (none it may not freeze again yet).
        private Tower GateAhead(float distance)
        {
            if (Enemy.IsDirected)
            {
                return null;
            }

            if (!(Field.Context.Structures.FindBlocker(Position, distance) is Tower gate) || gate.IsFrozen || _refreeze.ContainsKey(gate))
            {
                return null;
            }

            return Vector3.Dot(BossGeometry.Flat(gate.Position - Position), Enemy.Heading) > AheadDot ? gate : null;
        }

        private IEnumerator BreakGate(Tower gate)
        {
            _brain.State = "Freezing";
            Enemy.SetDirected(true);
            _creep.Hide();
            BossClip freeze = BossClip.Of(View, ModelClipRole.FreezeGate, Boss.MechanicClipRate);
            Vector3 dir = BossGeometry.Flat(gate.Position - Position);
            Vector3 stand = gate.Position - dir * freeze.Anchor;
            Enemy.Place(_brain.OnGround(stand), dir);
            _haze.Show(gate.Position, Haze, HazeIntensity, true);
            View.PlayScripted(ModelClipRole.FreezeGate, freeze.Rate, true);
            yield return _brain.Wait(freeze.Moment);
            if (!_brain.Alive)
            {
                yield break;
            }

            Vector3 breath = View.SocketPosition(View.ClipOf(ModelClipRole.FreezeGate)?.Socket ?? string.Empty);
            _brain.Impact(Vector3.Lerp(breath, gate.Position, 0.5f), BreathBurstRadius, IceShards, false);
            gate.Freeze(Power.FreezeSeconds);
            _brain.Record.GatesFrozen++;
            RecordShare();
            IceBlockView.Create(Field.Effects, gate, Field.Level.Road.DirectionAt(Field.Grid.WorldToCell(gate.Position)), IceMaterial(), Field.Context.Projectiles, _frost);
            Field.Context.BossGateFrozen.Raise(gate.Position);
            _haze.Hide();
            _warned = null;
            yield return _brain.Wait(freeze.Seconds - freeze.Moment);
            View.ReleaseScript();
            if (!gate.IsFrozen)
            {
                // Thawed before it could climb: it batters the gate like any creature.
                Thawed(gate);
                _brain.JoinRoad();
                yield break;
            }

            yield return OntoTheIce(gate, dir, stand);
        }

        // Up onto the ice block: it sags and the walkers follow over; as the freeze runs out it breaks down through it.
        private IEnumerator OntoTheIce(Tower gate, Vector3 dir, Vector3 stand)
        {
            _brain.State = "ClimbingGate";
            _brain.Record.Climbs++;
            BossClip up = BossClip.Of(View, ModelClipRole.ClimbUp, Boss.Climb.ClipRate);
            BossClip shatter = BossClip.Of(View, ModelClipRole.ShatterThrough, Boss.MechanicClipRate);
            View.PlayScripted(ModelClipRole.ClimbUp, up.Rate, true);
            yield return _brain.Wait(up.Seconds);
            Vector3 top = new Vector3(gate.Position.x, ClimbLayer.GateHeight, gate.Position.z);
            Enemy.Place(top, dir);
            View.LoopScripted(ModelClipRole.ClimbLoop);
            _brain.OnStructure = true;
            gate.SetBurden(1f);
            while (_brain.Alive && gate.IsFrozen && gate.FrozenRemaining > shatter.Moment)
            {
                yield return null;
            }

            if (!_brain.Alive)
            {
                gate.SetBurden(0f);
                _brain.OnStructure = false;
                yield break;
            }

            if (!gate.IsFrozen)
            {
                // The hero thawed it under its feet: it slips off over the far side and the gate is saved.
                gate.SetBurden(0f);
                Thawed(gate);
                yield return _brain.ClimbDown(top, dir);
                yield break;
            }

            yield return ShatterThrough(gate, dir, top, shatter);
        }

        private IEnumerator ShatterThrough(Tower gate, Vector3 dir, Vector3 top, BossClip shatter)
        {
            _brain.State = "Shattering";
            BossClip down = BossClip.Of(View, ModelClipRole.ClimbDown, Boss.Climb.ClipRate);
            Vector3 land = new Vector3(top.x, 0f, top.z) + dir * Mathf.Max(shatter.Forward - shatter.Anchor, down.Forward * 0.5f);
            View.PlayScripted(ModelClipRole.ShatterThrough, shatter.Rate, true);
            float elapsed = 0f;
            bool broken = false;
            while (elapsed < shatter.Seconds && _brain.Alive)
            {
                elapsed += Time.deltaTime;
                // It crashes down through the block: from its top to the ground beyond by the impact.
                float t = shatter.Moment > 0f ? Mathf.Clamp01(elapsed / shatter.Moment) : 1f;
                Vector3 at = Vector3.Lerp(top, land, t * t);
                Enemy.Place(new Vector3(at.x, Mathf.Lerp(top.y, Field.GroundAt(land), t * t), at.z), dir);
                if (!broken && elapsed >= shatter.Moment)
                {
                    broken = true;
                    Break(gate);
                }

                yield return null;
            }

            if (!broken)
            {
                Break(gate);
            }

            _brain.OnStructure = false;
            Enemy.Place(_brain.OnGround(land), dir);
            View.ReleaseScript();
            BossClip enrage = BossClip.Of(View, ModelClipRole.Enrage, Boss.MechanicClipRate);
            if (enrage.Exists && _brain.Alive && !_exposed)
            {
                _brain.State = "Enrage";
                yield return _brain.PlayClip(ModelClipRole.Enrage, Boss.MechanicClipRate);
            }

            _brain.JoinRoad();
        }

        private void Break(Tower gate)
        {
            gate.SetBurden(0f);
            gate.Smash(Position);
            _brain.Record.GatesShattered++;
            _brain.Impact(gate.Position, ShardBurstRadius, IceShards);
            Field.Context.BossImpact.Raise(Boss.ImpactKick);
            if (_rage.GateBroken())
            {
                _brain.Record.Rage = _rage.Level;
                Enemy.SetPace(Pace());
                if (!_exposed)
                {
                    _icicles.SetScale(_rage.BoneScale(Power.RageScale));
                }
            }
        }

        private void Thawed(Tower gate)
        {
            _brain.Record.GatesThawed++;
            _refreeze[gate] = Power.RefreezeSeconds;
        }

        // One frozen gate against everything the player has standing: the share of the field the freeze reaches.
        private void RecordShare()
        {
            int standing = 0;
            int frozen = 0;
            StructureRegistry structures = Field.Context.Structures;
            for (int i = 0; i < structures.Count; i++)
            {
                if (structures[i] is Tower tower && tower.IsAlive)
                {
                    standing++;
                    frozen += tower.IsFrozen ? 1 : 0;
                }
            }

            _brain.Record.MostFieldShare = Mathf.Max(_brain.Record.MostFieldShare, frozen / (float)Mathf.Max(1, standing));
        }

        // The level's own ice sheet (the Blender liquid), or none: the block then wears the default material.
        private Material IceMaterial()
        {
            Game.Levels.LiquidLook look = Field.Level.Theme != null && Field.Level.Theme.Liquids != null ? Field.Level.Theme.Liquids.For(Game.Levels.TerrainKind.Ice) : null;
            return look != null ? look.Material : null;
        }
    }
}
