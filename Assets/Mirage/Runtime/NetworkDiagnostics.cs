using System;
using UnityEngine;

namespace Mirage
{
    /// <summary>
    /// Provides profiling information from mirror
    /// A profiler can subscribe to these events and
    /// present the data in a friendly way to the user
    /// </summary>
    public static class NetworkDiagnostics
    {
        /// <summary>
        /// Describes an outgoing message
        /// </summary>
        public readonly struct MessageInfo
        {
            /// <summary>
            /// The message being sent
            /// </summary>
            public readonly object message;
            /// <summary>
            /// channel through which the message was sent
            /// </summary>
            public readonly int channel;
            /// <summary>
            /// how big was the message (does not include transport headers)
            /// </summary>
            public readonly int bytes;
            /// <summary>
            /// How many connections was the message sent to
            /// If an object has a lot of observers this count could be high
            /// </summary>
            public readonly int count;

            internal MessageInfo(object message, int channel, int bytes, int count)
            {
                this.message = message;
                this.channel = channel;
                this.bytes = bytes;
                this.count = count;
            }
        }

        #region Out messages
        /// <summary>
        /// Event that gets raised when Mirage sends a message
        /// Subscribe to this if you want to diagnose the network
        /// </summary>
        public static event Action<MessageInfo> OutMessageEvent;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            OutMessageEvent = null;
            InMessageEvent = null;
        }

        internal static void OnSend<T>(T message, int channel, int bytes, int count)
        {
            if (count > 0 && OutMessageEvent != null)
            {
                var outMessage = new MessageInfo(message, channel, bytes, count);
                OutMessageEvent.Invoke(outMessage);
            }
        }
        #endregion

        #region In messages

        /// <summary>
        /// Event that gets raised when Mirage receives a message
        /// Subscribe to this if you want to profile the network
        /// </summary>
        public static event Action<MessageInfo> InMessageEvent;

        internal static void OnReceive<T>(T message, int channel, int bytes)
        {
            if (InMessageEvent != null)
            {
                var inMessage = new MessageInfo(message, channel, bytes, 1);
                InMessageEvent.Invoke(inMessage);
            }
        }

        #endregion
    }
}
