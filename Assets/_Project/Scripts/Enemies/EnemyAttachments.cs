using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Keeps track of everything a view hangs beside a pooled enemy rather than under it (particles that must not
    /// scale with the body, beams, rings, bolts), so that all of it can be taken off the field in one call the
    /// moment the enemy is. Views register what they build and clear on <see cref="Enemy.Deactivated"/>; nothing
    /// of a dead creature may linger.
    /// </summary>
    public sealed class EnemyAttachments
    {
        private readonly List<ParticleSystem> _particles = new List<ParticleSystem>();
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<EnemyBoltView> _bolts = new List<EnemyBoltView>();

        public ParticleSystem Track(ParticleSystem system)
        {
            if (system != null)
            {
                _particles.Add(system);
            }

            return system;
        }

        /// <summary>A renderer that the view switches on and off itself; clearing switches it off.</summary>
        public T Track<T>(T renderer) where T : Renderer
        {
            if (renderer != null)
            {
                _renderers.Add(renderer);
            }

            return renderer;
        }

        /// <summary>An object the view shows and hides as a whole; clearing hides it.</summary>
        public GameObject Track(GameObject gameObject)
        {
            if (gameObject != null)
            {
                _objects.Add(gameObject);
            }

            return gameObject;
        }

        public EnemyBoltView Track(EnemyBoltView bolt)
        {
            if (bolt != null)
            {
                _bolts.Add(bolt);
            }

            return bolt;
        }

        /// <summary>Stops and empties every particle system, hides every object and renderer, cancels every bolt.</summary>
        public void Clear()
        {
            for (int i = 0; i < _particles.Count; i++)
            {
                ParticleKit.SetRate(_particles[i], 0f);
                _particles[i].Clear(true);
            }

            for (int i = 0; i < _renderers.Count; i++)
            {
                _renderers[i].enabled = false;
            }

            for (int i = 0; i < _objects.Count; i++)
            {
                _objects[i].SetActive(false);
            }

            for (int i = 0; i < _bolts.Count; i++)
            {
                _bolts[i].Cancel();
            }
        }
    }
}
