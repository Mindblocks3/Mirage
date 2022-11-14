using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage.Serialization;

namespace Mirage
{

    /// <summary>
    /// A connection that is directly connected to another connection
    /// If you send data in one of them,  you receive it on the other one
    /// </summary>
    public class PipeConnection : IConnection
    {

        private PipeConnection connected;

        // should only be created by CreatePipe
        private PipeConnection()
        {
        }

        Queue<byte[]> queue = new Queue<byte[]>();
        // buffer where we can queue up data

        // counts how many messages we have pending
        private readonly SemaphoreSlim MessageCount = new SemaphoreSlim(0);

        public static (IConnection, IConnection) CreatePipe()
        {
            var c1 = new PipeConnection();
            var c2 = new PipeConnection();

            c1.connected = c2;
            c2.connected = c1;

            return (c1, c2);
        }

        public void Disconnect()
        {
            // disconnect both ends of the pipe
            connected.queue.Enqueue(new byte[0]);
            connected.MessageCount.Release();

            queue.Enqueue(new byte[0]);
            MessageCount.Release();
        }

        // technically not an IPEndpoint,  will fix later
        public EndPoint GetEndPointAddress() => new IPEndPoint(IPAddress.Loopback, 0);

        public async UniTask<int> ReceiveAsync(MemoryStream buffer)
        {
            // wait for a message
            await MessageCount.WaitAsync();

            var data = queue.Dequeue();

            if (data.Length == 0)
                throw new EndOfStreamException();

            buffer.SetLength(0);
            buffer.Write(data);
            return 0;
        }

        public void Send(ReadOnlySpan<byte> data, int channel = Channel.Reliable)
        {
            connected.queue.Enqueue(data.ToArray());
            connected.MessageCount.Release();
        }
    }
}
