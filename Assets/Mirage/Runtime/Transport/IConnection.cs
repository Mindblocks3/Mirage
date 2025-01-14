using System;
using System.IO;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Mirage
{

    public interface IConnection
    {
        void Send(ReadOnlySpan<byte> data, int channel = Channel.Reliable);

        /// <summary>
        /// reads a message from connection
        /// </summary>
        /// <param name="buffer">buffer where the message will be written</param>
        /// <returns>The channel where we got the message</returns>
        /// <remark> throws System.IO.EndOfStreamException if the connetion has been closed</remark>
        UniTask<int> ReceiveAsync(MemoryStream buffer);

        /// <summary>
        /// Disconnect this connection
        /// </summary>
        void Disconnect();

        /// <summary>
        /// the address of endpoint we are connected to
        /// Note this can be IPEndPoint or a custom implementation
        /// of EndPoint, which depends on the transport
        /// </summary>
        /// <returns></returns>
        EndPoint GetEndPointAddress();
    }
}
