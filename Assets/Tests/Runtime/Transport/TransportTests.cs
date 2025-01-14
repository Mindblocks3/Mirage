using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Cysharp.Threading.Tasks;
using Mirage.KCP;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Mirage.Tests.Runtime
{
    [TestFixture(typeof(KcpTransport), "kcp", "kcp://localhost", 7778)]
    public class TransportTests<T> where T : Transport
    {
        #region SetUp

        private T transport;
        private GameObject transportObj;
        private readonly Uri uri;
        private readonly int port;
        private readonly string scheme;

        public TransportTests(string scheme, string uri, int port)
        {
            this.scheme = scheme;
            this.uri = new Uri(uri);
            this.port = port;
        }

        IConnection clientConnection;
        IConnection serverConnection;

        UniTask listenTask;

        [UnitySetUp]
        public IEnumerator Setup() => UniTask.ToCoroutine(async () =>
        {
            transportObj = new GameObject(this.GetType().Name);

            transport = transportObj.AddComponent<T>();

            transport.Connected.AddListener((connection) =>
                serverConnection = connection);

            transport.Listen();
            clientConnection = await transport.ConnectAsync(uri);

            await UniTask.WaitUntil(() => serverConnection != null);
        });


        [TearDown]
        public void TearDown()
        {
            clientConnection.Disconnect();
            serverConnection.Disconnect();
            transport.Disconnect();

            Object.Destroy(transportObj);
        }

        #endregion

        [UnityTest]
        public IEnumerator ClientToServerTest() => UniTask.ToCoroutine(async () =>
        {
            Encoding utf8 = Encoding.UTF8;
            string message = "Hello from the client";
            byte[] data = utf8.GetBytes(message);
            clientConnection.Send(data);

            var stream = new MemoryStream();

            await serverConnection.ReceiveAsync(stream);
            byte[] received = stream.ToArray();
            Assert.That(received, Is.EqualTo(data));
        });

        [Test]
        public void EndpointAddress()
        {
            // should give either IPv4 or IPv6 local address
            var endPoint = (IPEndPoint)serverConnection.GetEndPointAddress();

            IPAddress ipAddress = endPoint.Address;

            if (ipAddress.IsIPv4MappedToIPv6)
            {
                // mono IsLoopback seems buggy,
                // it does not detect loopback with mapped ipv4->ipv6 addresses
                // so map it back down to IPv4
                ipAddress = ipAddress.MapToIPv4();
            }

            Assert.That(IPAddress.IsLoopback(ipAddress), "Expected loopback address but got {0}", ipAddress);
            // random port
        }

        [UnityTest]
        public IEnumerator ClientToServerMultipleTest() => UniTask.ToCoroutine(async () =>
        {
            Encoding utf8 = Encoding.UTF8;
            string message = "Hello from the client 1";
            byte[] data = utf8.GetBytes(message);
            clientConnection.Send(data);

            string message2 = "Hello from the client 2";
            byte[] data2 = utf8.GetBytes(message2);
            clientConnection.Send(data2);

            var stream = new MemoryStream();

            await serverConnection.ReceiveAsync(stream);
            byte[] received = stream.ToArray();
            Assert.That(received, Is.EqualTo(data));

            stream.SetLength(0);
            await serverConnection.ReceiveAsync(stream);
            byte[] received2 = stream.ToArray();
            Assert.That(received2, Is.EqualTo(data2));
        });

        [UnityTest]
        public IEnumerator ServerToClientTest() => UniTask.ToCoroutine(async () =>
        {
            Encoding utf8 = Encoding.UTF8;
            string message = "Hello from the server";
            byte[] data = utf8.GetBytes(message);
            serverConnection.Send(data);

            var stream = new MemoryStream();

            await clientConnection.ReceiveAsync(stream);
            byte[] received = stream.ToArray();
            Assert.That(received, Is.EqualTo(data));
        });

        [UnityTest]
        [Repeat(50)]
        public IEnumerator DisconnectServerTest() => UniTask.ToCoroutine(async () =>
        {
            serverConnection.Disconnect();

            var stream = new MemoryStream();
            try
            {
                await clientConnection.ReceiveAsync(stream);
                Assert.Fail("ReceiveAsync should have thrown EndOfStreamException");
            }
            catch (EndOfStreamException)
            {
                // good to go
            }
        });

        [UnityTest]
        public IEnumerator DisconnectClientTest() => UniTask.ToCoroutine(async () =>
        {
            clientConnection.Disconnect();

            var stream = new MemoryStream();
            try
            {
                await serverConnection.ReceiveAsync(stream);
                Assert.Fail("ReceiveAsync should have thrown EndOfStreamException");
            }
            catch (EndOfStreamException)
            {
                // good to go
            }
        });

        [UnityTest]
        public IEnumerator DisconnectClientTest2() => UniTask.ToCoroutine(async () =>
        {
            clientConnection.Disconnect();

            var stream = new MemoryStream();
            try
            {
                await clientConnection.ReceiveAsync(stream);
                Assert.Fail("ReceiveAsync should have thrown EndOfStreamException");
            }
            catch (EndOfStreamException)
            {
                // good to go
            }
        });

        [Test]
        public void TestServerUri()
        {
            Uri serverUri = transport.ServerUri().First();

            Assert.That(serverUri.Port, Is.EqualTo(port));
            Assert.That(serverUri.Host, Is.EqualTo(Dns.GetHostName()).IgnoreCase);
            Assert.That(serverUri.Scheme, Is.EqualTo(uri.Scheme));
        }

        [Test]
        public void TestScheme()
        {
            Assert.That(transport.Scheme, Is.EquivalentTo(new []{scheme}));
        }
    }
}

