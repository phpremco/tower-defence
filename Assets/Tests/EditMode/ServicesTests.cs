using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class ServicesTests
    {
        private interface ITestService
        {
        }

        private sealed class TestService : ITestService
        {
        }

        [SetUp]
        public void SetUp() => Services.Clear();

        [TearDown]
        public void TearDown() => Services.Clear();

        [Test]
        public void Get_ReturnsRegisteredInstance()
        {
            var service = new TestService();
            Services.Register<ITestService>(service);

            Assert.AreSame(service, Services.Get<ITestService>());
        }

        [Test]
        public void Get_ThrowsWhenNotRegistered()
        {
            Assert.Throws<InvalidOperationException>(() => Services.Get<ITestService>());
        }

        [Test]
        public void TryGet_ReturnsFalseWhenNotRegistered()
        {
            Assert.IsFalse(Services.TryGet(out ITestService _));
        }

        [Test]
        public void Register_ReplacesPreviousInstance()
        {
            Services.Register<ITestService>(new TestService());
            var second = new TestService();
            Services.Register<ITestService>(second);

            Assert.AreSame(second, Services.Get<ITestService>());
        }
    }
}
