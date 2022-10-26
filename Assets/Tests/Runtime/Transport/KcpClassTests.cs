using System;
using NUnit.Framework;
using UnityAssertionException = UnityEngine.Assertions.AssertionException;

namespace Mirage.Tests.Runtime
{

    [TestFixture]
    public class KcpClassTests : KcpSetup
    {
        [Test]
        public void SendExceptionTest()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                server.Send(new Span<byte>());
            });
        }

        [Test]
        public void SetMtuExceptionTest()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                server.Mtu = 0;
            });

            Assert.Throws<ArgumentException>(() =>
            {
                server.Mtu = uint.MaxValue;
            });
        }

        [Test]
        public void ReserveExceptionTest()
        {
            Assert.Throws<UnityAssertionException>(() =>
            {
                server.Reserved = int.MaxValue;
            }, "Reserved must be less than MTU - OVERHEAD");
        }
    }
}
