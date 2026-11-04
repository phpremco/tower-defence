using Game.Combat;
using Game.Economy;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class WalletAndHealthTests
    {
        [Test]
        public void Wallet_SpendsAndRefusesWhenTooPoor()
        {
            var wallet = new Wallet(100, false);

            Assert.IsTrue(wallet.TrySpend(60));
            Assert.AreEqual(40, wallet.Money);
            Assert.IsFalse(wallet.TrySpend(60));
            Assert.AreEqual(40, wallet.Money);
        }

        [Test]
        public void Wallet_InfiniteNeverRunsOut()
        {
            var wallet = new Wallet(0, true);

            Assert.IsTrue(wallet.TrySpend(9999));
            Assert.AreEqual(0, wallet.Money);
        }

        [Test]
        public void Wallet_EarnsItsIncomeShare_EvenOfOneCoinBounties()
        {
            var wallet = new Wallet(0, false, 0.85f);

            for (int i = 0; i < 100; i++)
            {
                wallet.Earn(1);
            }

            Assert.AreEqual(85, wallet.Money, "a hundred one-coin kills pay 85 at an 85% income share");
            wallet.Add(10);
            Assert.AreEqual(95, wallet.Money, "a refund is not scaled");
        }

        [Test]
        public void Health_RaisesDiedOnceAtZero()
        {
            var health = new Health(10f);
            int died = 0;
            health.Died += () => died++;

            health.TakeDamage(6f);
            health.TakeDamage(6f);
            health.TakeDamage(6f);

            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(1, died);
        }
    }
}
