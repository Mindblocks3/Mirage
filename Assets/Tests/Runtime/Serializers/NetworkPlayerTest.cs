using System;
using System.IO;
using System.Net;
using Mirage.Serialization;
using NSubstitute;
using NUnit.Framework;

namespace Mirage.Tests.Runtime
{
     [NetworkMessage]
    public struct SceneMessage
    {
        public string scenePath;
        // Normal = 0, LoadAdditive = 1, UnloadAdditive = 2
        public string[] additiveScenes;
    }

    public class NetworkPlayerTest
    {
        private NetworkPlayer player;
        private byte[] serializedMessage;
        private IConnection mockTransportConnection;

        private SceneMessage data;

        private NotifyPacket lastSent;

        private Action<INetworkPlayer, object> delivered;

        private Action<INetworkPlayer, object> lost;

        private byte[] lastSerializedPacket;

        class MockConnection : IConnection
        {
            private NetworkPlayerTest testSuite;

            public MockConnection(NetworkPlayerTest testSuite)
            {
                this.testSuite = testSuite;
            }
            public void Disconnect()
            {
                throw new NotImplementedException();
            }

            public EndPoint GetEndPointAddress() => new IPEndPoint(IPAddress.Loopback, 0);

            public Cysharp.Threading.Tasks.UniTask<int> ReceiveAsync(MemoryStream buffer)
            {
                throw new NotImplementedException();
            }

            public void Send(ReadOnlySpan<byte> data, int channel = 0)
            {
                var reader = new NetworkReader(data.ToArray());
                _ = MessagePacker.UnpackId(reader);
                testSuite.lastSent = reader.ReadNotifyPacket();

                testSuite.lastSerializedPacket = data.ToArray();
            }
        }

        [SetUp]
        public void SetUp()
        {
            data = new SceneMessage();
            mockTransportConnection = new MockConnection(this);

            player = new NetworkPlayer(mockTransportConnection);

            serializedMessage = MessagePacker.Pack(new ReadyMessage());
            player.RegisterHandler<ReadyMessage>(message => { });

            delivered = Substitute.For<Action<INetworkPlayer, object>>();
            lost = Substitute.For<Action<INetworkPlayer, object>>();

            player.NotifyDelivered += delivered;
            player.NotifyLost += lost;

        }

        [Test]
        public void NoHandler()
        {
            int messageId = MessagePacker.GetId<SceneMessage>();
            var reader = new NetworkReader(new byte[] { 1, 2, 3, 4 });
            InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            {
                player.InvokeHandler(messageId, reader, 0);
            });

            Assert.That(exception.Message, Does.StartWith("Unexpected message Mirage.Tests.Runtime.SceneMessage received"));
        }

        [Test]
        public void UnknownMessage()
        {
            _ = MessagePacker.GetId<SceneMessage>();
            var reader = new NetworkReader(new byte[] { 1, 2, 3, 4 });
            InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            {
                // some random id with no message
                player.InvokeHandler(1234, reader, 0);
            });

            Assert.That(exception.Message, Does.StartWith("Unexpected message ID 1234 received"));
        }

        #region Notify


        [Test]
        public void SendsNotifyPacket()
        {
            player.SendNotify(data, 1);

            Assert.That(lastSent, Is.EqualTo(new NotifyPacket
            {
                Sequence = 0,
                ReceiveSequence = ushort.MaxValue,
                AckMask = 0
            }));
        }

        [Test]
        public void SendsNotifyPacketWithSequence()
        {
            player.SendNotify(data, 1);
            Assert.That(lastSent, Is.EqualTo(new NotifyPacket
            {
                Sequence = 0,
                ReceiveSequence = ushort.MaxValue,
                AckMask = 0
            }));

            player.SendNotify(data, 1);
            Assert.That(lastSent, Is.EqualTo(new NotifyPacket
            {
                Sequence = 1,
                ReceiveSequence = ushort.MaxValue,
                AckMask = 0
            }));
            player.SendNotify(data, 1);
            Assert.That(lastSent, Is.EqualTo(new NotifyPacket
            {
                Sequence = 2,
                ReceiveSequence = ushort.MaxValue,
                AckMask = 0
            }));
        }

        [Test]
        public void RaisePacketDelivered()
        {
            player.SendNotify(data, 1);
            player.SendNotify(data, 3);
            player.SendNotify(data, 5);

            delivered.DidNotReceiveWithAnyArgs().Invoke(default, default);

            var reply = new NotifyPacket
            {
                Sequence = 0,
                ReceiveSequence = 2,
                AckMask = 0b111
            };

            player.ReceiveNotify(reply, new NetworkReader(serializedMessage), Channel.Unreliable);

            Received.InOrder(() =>
            {
                delivered.Invoke(player, 1);
                delivered.Invoke(player, 3);
                delivered.Invoke(player, 5);
            });
        }

        [Test]
        public void RaisePacketNotDelivered()
        {
            player.SendNotify(data, 1);
            player.SendNotify(data, 3);
            player.SendNotify(data, 5);

            delivered.DidNotReceiveWithAnyArgs().Invoke(default, default);

            var reply = new NotifyPacket
            {
                Sequence = 0,
                ReceiveSequence = 2,
                AckMask = 0b001
            };

            player.ReceiveNotify(reply, new NetworkReader(serializedMessage), Channel.Unreliable);

            Received.InOrder(() =>
            {
                lost.Invoke(player, 1);
                lost.Invoke(player, 3);
                delivered.Invoke(player, 5);
            });
        }

        [Test]
        public void DropDuplicates()
        {
            player.SendNotify(data, 1);

            var reply = new NotifyPacket
            {
                Sequence = 0,
                ReceiveSequence = 0,
                AckMask = 0b001
            };

            player.ReceiveNotify(reply, new NetworkReader(serializedMessage), Channel.Unreliable);
            player.ReceiveNotify(reply, new NetworkReader(serializedMessage), Channel.Unreliable);

            delivered.Received(1).Invoke(player, 1);
        }


        [Test]
        public void LoseOldPackets()
        {
            for (int i = 0; i < 10; i++)
            {
                var packet = new NotifyPacket
                {
                    Sequence = (ushort)i,
                    ReceiveSequence = 100,
                    AckMask = ~0b0ul
                };
                player.ReceiveNotify(packet, new NetworkReader(serializedMessage), Channel.Unreliable);
            }

            var reply = new NotifyPacket
            {
                Sequence = 100,
                ReceiveSequence = 100,
                AckMask = ~0b0ul
            };
            player.ReceiveNotify(reply, new NetworkReader(serializedMessage), Channel.Unreliable);

            player.SendNotify(data, 1);

            Assert.That(lastSent, Is.EqualTo(new NotifyPacket
            {
                Sequence = 0,
                ReceiveSequence = 100,
                AckMask = 1
            }));
        }

        [Test]
        public void SendAndReceive()
        {
            player.SendNotify(data, 1);

            Action<SceneMessage> mockHandler = Substitute.For<Action<SceneMessage>>();
            player.RegisterHandler(mockHandler);

            player.TransportReceive(lastSerializedPacket, Channel.Unreliable);
            mockHandler.Received().Invoke(new SceneMessage());
        }

        [Test]
        public void NotAcknowledgedYet()
        {
            player.SendNotify(data, 1);
            player.SendNotify(data, 3);
            player.SendNotify(data, 5);

            var reply = new NotifyPacket
            {
                Sequence = 0,
                ReceiveSequence = 1,
                AckMask = 0b011
            };

            player.ReceiveNotify(reply, new NetworkReader(serializedMessage), Channel.Unreliable);

            delivered.DidNotReceive().Invoke(Arg.Any<INetworkPlayer>(), 5);

            reply = new NotifyPacket
            {
                Sequence = 1,
                ReceiveSequence = 2,
                AckMask = 0b111
            };

            player.ReceiveNotify(reply, new NetworkReader(serializedMessage), Channel.Unreliable);

            delivered.Received().Invoke(player, 5);
        }

        #endregion
    }
}
