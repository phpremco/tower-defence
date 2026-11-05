using System.Collections.Generic;

namespace Game.Bosses
{
    /// <summary>
    /// The eggs a boss has laid and their clocks (Docs/BOSSES.md: "hatch as fire bugs after 10 s unless you shoot them"):
    /// each egg ripens on its own, a shot egg is struck off, and the eggs whose time has come are handed out once. Pure
    /// logic over handles of type <typeparamref name="T"/> (the egg creatures in the game, plain numbers in the tests).
    /// </summary>
    public sealed class EggClutch<T> where T : class
    {
        private readonly List<T> _eggs = new List<T>(8);
        private readonly List<float> _ages = new List<float>(8);
        private readonly float _hatchSeconds;

        public EggClutch(float hatchSeconds)
        {
            _hatchSeconds = hatchSeconds;
        }

        public int Count => _eggs.Count;

        public float HatchSeconds => _hatchSeconds;

        public void Lay(T egg)
        {
            _eggs.Add(egg);
            _ages.Add(0f);
        }

        /// <summary>The egg was shot (or is gone): it never hatches.</summary>
        public bool Remove(T egg)
        {
            int index = _eggs.IndexOf(egg);
            if (index < 0)
            {
                return false;
            }

            _eggs.RemoveAt(index);
            _ages.RemoveAt(index);
            return true;
        }

        /// <summary>Seconds an egg still needs to hatch, or a negative number for an egg not in the clutch.</summary>
        public float SecondsLeft(T egg)
        {
            int index = _eggs.IndexOf(egg);
            return index >= 0 ? _hatchSeconds - _ages[index] : -1f;
        }

        /// <summary>Ages every egg; those that ripened leave the clutch into <paramref name="hatched"/> (cleared first).</summary>
        public void Step(float deltaTime, List<T> hatched)
        {
            hatched.Clear();
            for (int i = _eggs.Count - 1; i >= 0; i--)
            {
                _ages[i] += deltaTime;
                if (_ages[i] >= _hatchSeconds)
                {
                    hatched.Add(_eggs[i]);
                    _eggs.RemoveAt(i);
                    _ages.RemoveAt(i);
                }
            }
        }

        public void Clear()
        {
            _eggs.Clear();
            _ages.Clear();
        }
    }
}
