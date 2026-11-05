using Game.Enemies;

namespace Game.Bosses
{
    /// <summary>
    /// Picks the signature mechanic a boss's data names (<see cref="BossDefinition.Signature"/>): the boss's name never
    /// decides it. A signature that is not built yet leaves the boss with the shared body only (walk, climb, dive).
    /// </summary>
    public static class BossSignatures
    {
        public static IBossSignature For(BossSignature signature, ContinentBossBrain brain)
        {
            switch (signature)
            {
                case BossSignature.Burrow: return new BurrowSignature(brain);
                case BossSignature.HuntTheCold: return new HuntSignature(brain);
                case BossSignature.GateBreaker: return new GateBreakerSignature(brain);
                default: return new NoSignature();
            }
        }
    }
}
