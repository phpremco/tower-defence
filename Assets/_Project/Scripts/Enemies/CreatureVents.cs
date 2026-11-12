using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// The smoke and embers rising from an imported creature's vent sockets, built from its art's
    /// <see cref="ModelVentDefinition"/>: one smoke and one ember emitter per socket, parented to the bone so they ride
    /// along with the clips, simulated in world space so the wisps trail behind as it walks. The living view keeps them at
    /// full strength; a corpse turns them down as it dies. Pure view code, built once per pooled body.
    /// </summary>
    public sealed class CreatureVents
    {
        private readonly ParticleSystem[] _smoke;
        private readonly ParticleSystem[] _embers;
        private readonly float _smokeRate;
        private readonly float _emberRate;

        private CreatureVents(ParticleSystem[] smoke, ParticleSystem[] embers, float smokeRate, float emberRate)
        {
            _smoke = smoke;
            _embers = embers;
            _smokeRate = smokeRate;
            _emberRate = emberRate;
        }

        /// <summary>How many vents (sockets) smoke.</summary>
        public int Count => _smoke.Length;

        /// <summary>The definition's seconds over which a dead body's vents die down.</summary>
        public float DeathFadeSeconds { get; private set; }

        /// <summary>
        /// The vents of a model whose art names a vent definition and whose near rig has its sockets, or null (no vents,
        /// no sockets: the creature simply does not smoke).
        /// </summary>
        public static CreatureVents Create(CreatureModel model, float scale, bool lowDetail)
        {
            ModelVentDefinition vents = model.Art.Vents;
            if (vents == null || vents.Problem().Length > 0)
            {
                return null;
            }

            List<Transform> sockets = Sockets(model.Near, vents.SocketPrefix);
            if (sockets.Count == 0)
            {
                return null;
            }

            var smoke = new ParticleSystem[sockets.Count];
            var embers = new ParticleSystem[sockets.Count];
            float share = vents.RateShare(lowDetail);
            for (int i = 0; i < sockets.Count; i++)
            {
                smoke[i] = Smoke(sockets[i], vents, scale, share);
                embers[i] = Embers(sockets[i], vents, scale, share);
            }

            return new CreatureVents(smoke, embers, vents.SmokeRate * share, vents.EmberRate * share) { DeathFadeSeconds = vents.DeathFadeSeconds };
        }

        /// <summary>Sets the vents to a share of their rate: one while alive, falling to zero as the body dies.</summary>
        public void SetStrength(float strength)
        {
            for (int i = 0; i < _smoke.Length; i++)
            {
                ParticleKit.SetRate(_smoke[i], _smokeRate * strength);
                ParticleKit.SetRate(_embers[i], _emberRate * strength);
            }
        }

        /// <summary>Empties every emitter (a pooled body laid somewhere new) and sets them to full strength.</summary>
        public void Restart()
        {
            for (int i = 0; i < _smoke.Length; i++)
            {
                _smoke[i].Clear(true);
                _embers[i].Clear(true);
            }

            SetStrength(1f);
        }

        /// <summary>Hands every emitter to the view's attachments, so they stop and empty when the creature leaves the field.</summary>
        public void TrackIn(EnemyAttachments attachments)
        {
            for (int i = 0; i < _smoke.Length; i++)
            {
                attachments.Track(_smoke[i]);
                attachments.Track(_embers[i]);
            }
        }

        private static List<Transform> Sockets(Transform near, string prefix)
        {
            var sockets = new List<Transform>(3);
            foreach (Transform node in near.GetComponentsInChildren<Transform>(true))
            {
                if (node.name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    sockets.Add(node);
                }
            }

            return sockets;
        }

        // A soft mote of the atlas drawn covering: a grey puff that hides a little of the field behind it, never a glow.
        private static ParticleSystem Smoke(Transform socket, ModelVentDefinition vents, float scale, float share)
        {
            Vector2 size = vents.SmokeSize * scale;
            Vector2 speed = vents.SmokeSpeed * scale;
            ParticleSystem system = ParticleKit.Create(socket, "VentSmoke", Vector3.zero, vents.SmokeColor, vents.SmokeLife.x, vents.SmokeLife.y, size.x, size.y,
                speed.x, speed.y, vents.SmokeBuoyancy, Budget(vents.SmokeMax, share), vents.Radius * scale);
            return Detach(ParticleKit.UseCoveringCell(system, MarkerArt.ParticleCell.Mote));
        }

        private static ParticleSystem Embers(Transform socket, ModelVentDefinition vents, float scale, float share)
        {
            Vector2 size = vents.EmberSize * scale;
            Vector2 speed = vents.EmberSpeed * scale;
            ParticleSystem system = ParticleKit.Create(socket, "VentEmbers", Vector3.zero, vents.EmberColor, vents.EmberLife.x, vents.EmberLife.y, size.x, size.y,
                speed.x, speed.y, vents.EmberBuoyancy, Budget(vents.EmberMax, share), vents.Radius * scale);
            return Detach(ParticleKit.UseCell(system, MarkerArt.ParticleCell.Ember));
        }

        // The bones sit inside the scaled model; the emitters take their size from the definition alone, not the rig's scale.
        private static ParticleSystem Detach(ParticleSystem system)
        {
            ParticleSystem.MainModule main = system.main;
            main.scalingMode = ParticleSystemScalingMode.Local;
            ParticleKit.SetRate(system, 0f);
            return system;
        }

        private static int Budget(int max, float share) => Mathf.Max(1, Mathf.CeilToInt(max * share));
    }
}
