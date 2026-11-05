using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Enemies;
using Game.Hero;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The slime that trail layers smear over the road. Every patch lives for a while, hurries creatures of other
    /// species that cross it (never the slugs), and eats at the hero and at gates that stand in it. Towers are out of its reach by
    /// design: the trail is a road hazard, not a siege weapon. Pure logic; the view draws the patches it lists.
    /// </summary>
    public sealed class SlimeField
    {
        /// <summary>One smear on the road.</summary>
        public struct Patch
        {
            public Vector3 Position;
            public float Radius;
            public float Life;
            public float Age;
            public float Haste;
            public float DamagePerSecond;
            public EnemyDefinition Layer;
            public Color Color;

            /// <summary>Share of the patch's life that is over, from 0 to 1.</summary>
            public float Fade => Life > 0f ? Mathf.Clamp01(Age / Life) : 1f;
        }

        private const float HasteRefreshSeconds = 0.35f;
        private const float HeroReach = 0.45f;

        private readonly List<Patch> _patches = new List<Patch>(128);
        private readonly List<Enemy> _enemies;
        private readonly StructureRegistry _structures;
        private readonly float _radius;
        private readonly float _damageMultiplier;
        private Commander _hero;

        /// <param name="enemies">The live enemies, owned by the spawner; the field reads the list and never changes it.</param>
        /// <param name="radius">How wide one patch is.</param>
        public SlimeField(List<Enemy> enemies, StructureRegistry structures, float radius, float damageMultiplier)
        {
            _enemies = enemies;
            _structures = structures;
            _radius = radius;
            _damageMultiplier = damageMultiplier;
        }

        public IReadOnlyList<Patch> Patches => _patches;

        /// <summary>Raised with the new patch whenever one is laid down.</summary>
        public event Action<Patch> Added;

        /// <summary>The hero the slime may burn; null when the level has none.</summary>
        public void SetHero(Commander hero) => _hero = hero;

        public void Add(Enemy layer, Vector3 position) => Add(layer.Definition, position);

        /// <summary>A patch as a creature of this kind leaves it (the layer itself need not be on the field).</summary>
        public void Add(EnemyDefinition definition, Vector3 position)
        {
            var patch = new Patch
            {
                Position = position,
                Radius = _radius * definition.Scale,
                Life = definition.TrailSeconds,
                Haste = definition.TrailHaste,
                DamagePerSecond = definition.TrailDamagePerSecond * _damageMultiplier,
                Layer = definition,
                Color = definition.BloodColor,
            };
            _patches.Add(patch);
            Added?.Invoke(patch);
        }

        public void Step(float dt)
        {
            for (int p = _patches.Count - 1; p >= 0; p--)
            {
                Patch patch = _patches[p];
                patch.Age += dt;
                if (patch.Age >= patch.Life)
                {
                    _patches.RemoveAt(p);
                    continue;
                }

                _patches[p] = patch;
                float reachSqr = patch.Radius * patch.Radius;
                HurryCrossers(patch, reachSqr);
                Burn(patch, dt);
            }
        }

        // The slime is a gift to every other species: those that lay it gain nothing from their own, and the slugs
        // (SlimeImmune in their data) gain nothing from anyone's.
        private void HurryCrossers(Patch patch, float reachSqr)
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                Enemy enemy = _enemies[i];
                if (!enemy.IsAlive || enemy.Definition == patch.Layer || enemy.Definition.SlimeImmune || enemy.Definition.Flies)
                {
                    continue;
                }

                if (FlatSqr(enemy.Position, patch.Position) <= reachSqr)
                {
                    enemy.ApplyHaste(patch.Haste, HasteRefreshSeconds);
                }
            }
        }

        private void Burn(Patch patch, float dt)
        {
            if (patch.DamagePerSecond <= 0f)
            {
                return;
            }

            IStructure gate = _structures.FindBlocker(patch.Position, patch.Radius);
            gate?.TakeDamage(patch.DamagePerSecond * dt);

            if (_hero != null && !_hero.IsDown && FlatSqr(_hero.transform.position, patch.Position) <= (patch.Radius + HeroReach) * (patch.Radius + HeroReach))
            {
                _hero.TakeHazardDamage(patch.DamagePerSecond * dt);
            }
        }

        private static float FlatSqr(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
