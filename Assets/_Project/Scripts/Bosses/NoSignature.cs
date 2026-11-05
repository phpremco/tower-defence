using System.Collections;
using Game.Combat;

namespace Game.Bosses
{
    /// <summary>A boss whose signature is not built yet: it only walks, climbs and dives with the shared body.</summary>
    public sealed class NoSignature : IBossSignature
    {
        public bool WantsOffStructure => false;

        public void Begin()
        {
        }

        public IEnumerator Entrance() => null;

        public IEnumerator NextAct() => null;

        public IEnumerator AtStructure(IStructure structure) => null;

        public void OnRoad(float deltaTime)
        {
        }

        public void Always(float deltaTime)
        {
        }

        public void End()
        {
        }
    }
}
