using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage.Logging;
using UnityEngine;

namespace Mirage.KCP
{
    public abstract class KcpConnection : IConnection
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(KcpConnection));

        const int MinimumKcpTickInterval = 10;

        protected Socket socket;
        protected EndPoint remoteEndpoint;
        protected Kcp kcp;
        protected Unreliable unreliable;
        private readonly int sendWindowSize;
        private readonly int receiveWindowSize;

        readonly KcpDelayMode delayMode;

        protected CancellationTokenSource cancellationTokenSource;

        public int CHANNEL_SIZE = 4;

        internal event Action Disconnected;

        // If we don't receive anything these many milliseconds
        // then consider us disconnected
        public int Timeout { get; set; } = 15000;

        private static readonly Stopwatch stopWatch = new Stopwatch();

        static KcpConnection()
        {
            stopWatch.Start();
        }

        private long lastReceived;

        /// <summary>
        /// Space for CRC64
        /// </summary>
        public const int RESERVED = sizeof(ulong);

        internal static readonly byte[] Hello = { 0 };
        private static readonly byte[] Goodby = { 1 };

        protected KcpConnection(KcpDelayMode delayMode, int sendWindowSize, int receiveWindowSize)
        {
            this.delayMode = delayMode;
            this.sendWindowSize = sendWindowSize;
            this.receiveWindowSize = receiveWindowSize;
        }

        protected void SetupKcp(CancellationToken cancellationToken)
        {
            unreliable = new Unreliable(SendWithChecksum)
            {
                Reserved = RESERVED
            };

            kcp = new Kcp(0, SendWithChecksum)
            {
                Reserved = RESERVED
            };

            kcp.SetNoDelay(delayMode);
            kcp.SetWindowSize((uint)sendWindowSize, (uint)receiveWindowSize);

            Tick(cancellationToken).Forget();
        }

        async UniTaskVoid Tick(CancellationToken cancellationToken)
        {
            try
            {
                Thread.VolatileWrite(ref lastReceived, stopWatch.ElapsedMilliseconds);

                while (!cancellationToken.IsCancellationRequested)
                {
                    long now = stopWatch.ElapsedMilliseconds;
                    long received = Thread.VolatileRead(ref lastReceived);
                    if (now > received + Timeout)
                        break;

                    kcp.Update((uint)now);

                    uint check = kcp.Check((uint)now);

                    int delay = (int)(check - now);

                    if (delay <= 0)
                        delay = MinimumKcpTickInterval;

                    await UniTask.Delay(delay, cancellationToken: cancellationToken);
                }
            }
            catch (SocketException)
            {
                // this is ok, the connection was closed
            }
            catch (OperationCanceledException)
            {
                // fine,  socket was closed,  no more ticking needed
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }
            finally
            {
                cancellationTokenSource.Cancel();
                dataAvailable?.TrySetResult();
                Close();
            }
        }

        protected virtual void Close()
        {
        }

        volatile bool isWaiting;

        AutoResetUniTaskCompletionSource dataAvailable;

        internal void RawInput(ReadOnlySpan<byte> buffer)
        {
            // check packet integrity
            if (!Validate(buffer))
                return;

            int channel = GetChannel(buffer);
            ReadOnlySpan<byte> data = buffer.Slice(RESERVED);

            if (channel == Channel.Reliable)
                InputReliable(data);
            else if (channel == Channel.Unreliable)
                InputUnreliable(data);
        }

        private void InputUnreliable(ReadOnlySpan<byte> buffer)
        {
            unreliable.Input(buffer);
            Thread.VolatileWrite(ref lastReceived, stopWatch.ElapsedMilliseconds);

            if (isWaiting && unreliable.PeekSize() > 0)
            {
                dataAvailable?.TrySetResult();
            }
        }

        private void InputReliable(ReadOnlySpan<byte> buffer)
        {
            kcp.Input(buffer);

            Thread.VolatileWrite(ref lastReceived, stopWatch.ElapsedMilliseconds);

            if (isWaiting && kcp.PeekSize() > 0)
            {
                // we just got a full message
                // Let the receivers know
                dataAvailable?.TrySetResult();
            }
        }

        private bool Validate(ReadOnlySpan<byte> buffer)
        {
            // Recalculate CRC64 and check against checksum in the head
            var decoder = new Decoder(buffer);
            ulong receivedCrc = decoder.Decode64U();
            ulong calculatedCrc = Crc64.Compute(buffer.Slice(decoder.Position));
            return receivedCrc == calculatedCrc;
        }

        protected abstract void RawSend(byte[] data, int length);

        private void SendWithChecksum(byte[] data, int length)
        {
            // add a CRC64 checksum in the reserved space
            ulong crc = Crc64.Compute(data.AsSpan(RESERVED, length - RESERVED));
            var encoder = new Encoder(data);
            encoder.Encode64U(crc);
            RawSend(data, length);

            if (kcp.WaitSnd > 1000 && logger.WarnEnabled())
            {
                logger.LogWarning("Too many packets waiting in the send queue " + kcp.WaitSnd + ", you are sending too much data,  the transport can't keep up");
            }
        }

        public void Send(ReadOnlySpan<byte> data, int channel = Channel.Reliable)
        {
            if (channel == Channel.Reliable)
                kcp.Send(data);
            else if (channel == Channel.Unreliable)
                unreliable.Send(data);
        }

        /// <summary>
        ///     reads a message from connection
        /// </summary>
        /// <param name="buffer">buffer where the message will be written</param>
        /// <returns>true if we got a message, false if we got disconnected</returns>
        public async UniTask<int> ReceiveAsync(MemoryStream buffer)
        {
            await WaitForMessages(cancellationTokenSource.Token);

            if (cancellationTokenSource.IsCancellationRequested)
            {
                Disconnected?.Invoke();
                throw new EndOfStreamException();
            }

            if (unreliable.PeekSize() >= 0)
            {
                return ReadUnreliable(buffer);
            }
            else
            {
                return ReadReliable(buffer);
            }
        }

        private async UniTask WaitForMessages(CancellationToken cancellationToken)
        {
            while (kcp.PeekSize() < 0 && unreliable.PeekSize() < 0 && !cancellationToken.IsCancellationRequested)
            {
                isWaiting = true;
                dataAvailable = AutoResetUniTaskCompletionSource.Create();
                await dataAvailable.Task;
                isWaiting = false;
            }
        }


        private int ReadUnreliable(MemoryStream buffer)
        {
            // we got a message in the unreliable channel
            int msgSize = unreliable.PeekSize();
            buffer.SetLength(msgSize);
            unreliable.Receive(buffer.GetBuffer(), (int)buffer.Length);
            buffer.Position = msgSize;
            return Channel.Unreliable;
        }

        private int ReadReliable(MemoryStream buffer)
        {
            int msgSize = kcp.PeekSize();
            // we have some data,  return it
            buffer.SetLength(msgSize);
            kcp.Receive(buffer.GetBuffer());
            buffer.Position = msgSize;

            // if we receive a disconnect message,  then close everything

            var dataSegment = new ReadOnlyMemory<byte>(buffer.GetBuffer(), 0, msgSize);
            if (Utils.Equal(dataSegment, Goodby))
            {
                cancellationTokenSource.Cancel();
                Disconnected?.Invoke();
                throw new EndOfStreamException();
            }
            return Channel.Reliable;
        }

        /// <summary>
        ///     Disconnect this connection
        /// </summary>
        public virtual void Disconnect()
        {
            // send a disconnect message and disconnect
            if (!cancellationTokenSource.IsCancellationRequested && socket.Connected)
            {
                try
                {
                    Send(Goodby);
                    kcp.Flush();
                }
                catch (SocketException)
                {
                    // this is ok,  the connection was already closed
                }
                catch (ObjectDisposedException)
                {
                    // this is normal when we stop the server
                    // the socket is stopped so we can't send anything anymore
                    // to the clients

                    // the clients will eventually timeout and realize they
                    // were disconnected
                }
            }
            cancellationTokenSource.Cancel();
    
            // EOF is now available
            dataAvailable?.TrySetResult();
        }

        /// <summary>
        ///     the address of endpoint we are connected to
        ///     Note this can be IPEndPoint or a custom implementation
        ///     of EndPoint, which depends on the transport
        /// </summary>
        /// <returns></returns>
        public EndPoint GetEndPointAddress()
        {
            return remoteEndpoint;
        }

        public static int GetChannel(ReadOnlySpan<byte> data)
        {
            var decoder = new Decoder(data.Slice(RESERVED));
            return (int)decoder.Decode32U();
        }
    }
}
