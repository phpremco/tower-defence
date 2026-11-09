using Game.Combat;
using Game.Hero;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The commander seen as something a boss can aim at, beside the towers and gates: alive while standing, hurt
    /// through the hero's own health. Repair is the hero's revive, so it does nothing here.
    /// </summary>
    public sealed class HeroTarget : IStructure
    {
        private readonly Commander _hero;

        public HeroTarget(Commander hero)
        {
            _hero = hero;
        }

        public Vector3 Position => _hero.transform.position;

        // A hero in jetpack flight is out of every creature's reach (ground and air, a boss's laser and blast): the flight
        // lasts a few seconds and costs him the fight meanwhile, so it is never a target nor a road block.
        public bool IsAlive => _hero != null && !_hero.IsDown && !_hero.IsFlying;

        // A hero on its feet holds the road like a gate: ground creatures must get past it, flyers pass over.
        public bool BlocksGround => IsAlive && !_hero.Definition.Flies;
        public bool IsBase => false;

        public float HealthFraction => _hero.Health.Fraction;

        public void TakeDamage(float amount)
        {
            _hero.TakeHazardDamage(amount);
        }

        public void Repair(float amount)
        {
        }
    }
}
