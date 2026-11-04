using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Chooses what an imported creature model plays: the travel cycle at a rate that keeps its feet planted (ground
    /// speed over the distance one loop covers), idle when it stands, curl, roll and uncurl for a roller, land and lift
    /// off for a floater, a flinch on a hit (a raised shield while plates are left), its own strike timed so the hit
    /// lands on the attack's peak, and a boss's power clip started early enough that its release frame falls on the
    /// moment the power fires. View code only; it reads the creature's state and never changes it.
    /// </summary>
    public sealed class CreatureClips
    {
        private const float MinRate = 0.3f;
        private const float MaxRate = 4f;
        private const float LandAfterSeconds = 0.35f;
        // A power cycle that has passed its moment is ready again once its timer has wound well past the clip's lead.
        private const float RearmMargin = 0.5f;

        private readonly ModelClipPlayer _player;
        private readonly ModelArtDefinition _art;
        private readonly int _move;
        private readonly int _idle;
        private readonly int _hit;
        private readonly int _attack;
        private readonly int _block;
        private readonly int _curl;
        private readonly int _roll;
        private readonly int _uncurl;
        private readonly int _land;
        private readonly int _grounded;
        private readonly int[] _powers;
        private readonly bool[] _armed;

        private bool _rolled;
        private bool _landed;
        private float _still;
        private int _pending = -1;
        private float _pendingDelay;
        private float _pendingStart;
        private int _eventClip = -1;
        private float _lastOnceTime;
        private int _lastOnce = -1;

        public CreatureClips(ModelClipPlayer player, ModelArtDefinition art, string[] powerTriggers)
        {
            _player = player;
            _art = art;
            _move = art.IndexOf(ModelClipRole.Move);
            _idle = art.IndexOf(ModelClipRole.Idle);
            _hit = art.IndexOf(ModelClipRole.Hit);
            _attack = art.IndexOf(ModelClipRole.Attack);
            _block = art.IndexOf(ModelClipRole.Block);
            _curl = art.IndexOf(ModelClipRole.Curl);
            _roll = art.IndexOf(ModelClipRole.Roll);
            _uncurl = art.IndexOf(ModelClipRole.Uncurl);
            _land = art.IndexOf(ModelClipRole.Land);
            _grounded = art.IndexOf(ModelClipRole.Grounded);
            _powers = new int[powerTriggers.Length];
            _armed = new bool[powerTriggers.Length];
            for (int i = 0; i < powerTriggers.Length; i++)
            {
                _powers[i] = art.IndexOfTrigger(powerTriggers[i]);
                _armed[i] = true;
            }
        }

        /// <summary>True for a creature that settles on the ground when it stops (a floating wisp).</summary>
        public bool Lands => _land >= 0 && _grounded >= 0;

        /// <summary>True when the model has a strike clip of its own (a sting, a tail lash).</summary>
        public bool HasOwnStrike => _attack >= 0;

        /// <summary>The clip whose moment passed this frame (a power's release, a strike's hit), or -1.</summary>
        public int EventClip => _eventClip;

        /// <summary>Back to the start for a pooled creature sent out again.</summary>
        public void Restart()
        {
            _player.Restart();
            _rolled = false;
            _landed = false;
            _still = 0f;
            _pending = -1;
            _eventClip = -1;
            for (int i = 0; i < _armed.Length; i++)
            {
                _armed[i] = true;
            }

            _player.Loop(_idle >= 0 ? _idle : _move, 1f);
        }

        /// <param name="groundSpeed">World units per second the creature covers.</param>
        /// <param name="scale">The model's world scale, to turn model units per loop into world units.</param>
        public void Tick(float deltaTime, float groundSpeed, bool moving, bool rolling, float scale)
        {
            if (rolling != _rolled)
            {
                _rolled = rolling;
                PlayIfFree(rolling ? _curl : _uncurl);
            }

            ChooseCycle(deltaTime, groundSpeed, moving, rolling, scale);
            StartPending(deltaTime);
            _player.Tick(deltaTime);
            DetectEvent();
        }

        /// <summary>A hit: a shield goes up while plates are left, otherwise a short flinch. Never cuts a strike or a power.</summary>
        public void Hit(bool armoured)
        {
            int clip = armoured && _block >= 0 ? _block : _hit;
            if (clip < 0 || IsBusy())
            {
                return;
            }

            _player.PlayOnce(clip);
        }

        /// <summary>The creature's own strike, timed so its hit frame falls a peak delay after now.</summary>
        public void Attack(float peakDelay)
        {
            if (_attack < 0)
            {
                return;
            }

            float lead = _art.Clips[_attack].EventSeconds;
            if (lead <= peakDelay)
            {
                _pending = _attack;
                _pendingDelay = peakDelay - lead;
                _pendingStart = 0f;
                return;
            }

            _player.PlayOnce(_attack, lead - peakDelay);
        }

        /// <summary>
        /// Starts the clip of a power once it is due within its own lead time, at the point that makes its release
        /// frame land when the power fires.
        /// </summary>
        public void Anticipate(int slot, float secondsUntil)
        {
            int clip = _powers[slot];
            if (clip < 0)
            {
                return;
            }

            float lead = _art.Clips[clip].EventSeconds;
            if (!_armed[slot])
            {
                _armed[slot] = secondsUntil > lead + RearmMargin;
                return;
            }

            if (secondsUntil <= lead)
            {
                _armed[slot] = false;
                _player.PlayOnce(clip, Mathf.Max(0f, lead - secondsUntil));
            }
        }

        /// <summary>The fall: the death clip, held on its last frame.</summary>
        public void Die()
        {
            _pending = -1;
            _player.PlayOnce(_art.IndexOf(ModelClipRole.Death), 0f, true);
        }

        private void ChooseCycle(float deltaTime, float groundSpeed, bool moving, bool rolling, float scale)
        {
            if (rolling && _roll >= 0)
            {
                _player.Loop(_roll, Rate(_roll, groundSpeed, scale));
                return;
            }

            _still = moving ? 0f : _still + deltaTime;
            if (Lands)
            {
                bool settle = _still >= LandAfterSeconds;
                if (settle != _landed)
                {
                    _landed = settle;
                    // Landing plays forwards; lifting off is the same clip backwards.
                    PlayIfFree(_land, settle ? 1f : -1f);
                }

                _player.Loop(_landed ? _grounded : _move, _landed ? 1f : Rate(_move, groundSpeed, scale));
                return;
            }

            if (moving && _move >= 0)
            {
                _player.Loop(_move, Rate(_move, groundSpeed, scale));
                return;
            }

            _player.Loop(_idle >= 0 ? _idle : _move, 1f);
        }

        // Clip seconds per real second so one loop covers exactly the ground it was authored for.
        private float Rate(int clip, float groundSpeed, float scale)
        {
            float distance = _art.Clips[clip].DistancePerLoop * scale;
            if (distance <= 0f)
            {
                return 1f;
            }

            float loopsPerSecond = groundSpeed / distance;
            return Mathf.Clamp(loopsPerSecond * _art.Clips[clip].Length, MinRate, MaxRate);
        }

        private void StartPending(float deltaTime)
        {
            if (_pending < 0)
            {
                return;
            }

            _pendingStart += deltaTime;
            if (_pendingStart >= _pendingDelay)
            {
                _player.PlayOnce(_pending);
                _pending = -1;
            }
        }

        private void DetectEvent()
        {
            int once = _player.Once;
            float time = _player.OnceTime;
            _eventClip = -1;
            if (once != _lastOnce)
            {
                _lastOnce = once;
                _lastOnceTime = -1f;
            }

            if (once >= 0)
            {
                float moment = _art.Clips[once].EventSeconds;
                if (moment > 0f && _lastOnceTime < moment && time >= moment)
                {
                    _eventClip = once;
                }
            }

            _lastOnceTime = once >= 0 ? time : 0f;
        }

        private bool IsBusy()
        {
            int once = _player.Once;
            if (once < 0)
            {
                return false;
            }

            ModelClipRole role = _art.Clips[once].Role;
            return role != ModelClipRole.Hit && role != ModelClipRole.Block;
        }

        private void PlayIfFree(int clip, float rate = 1f)
        {
            if (clip >= 0 && !IsBusy())
            {
                _player.PlayOnce(clip, 0f, false, rate);
            }
        }
    }
}
