using System;
using UnityEngine;

namespace Mirage.Examples.Chat
{
    public class Player : NetworkBehaviour
    {
        public string playerName { get; set; }

        public static event Action<Player, string> OnMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            OnMessage = null;
        }

        [ServerRpc]
        public void CmdSend(string message)
        {
            if (message.Trim() != "")
                RpcReceive(message.Trim());
        }

        [ClientRpc]
        public void RpcReceive(string message)
        {
            OnMessage?.Invoke(this, message);
        }
    }
}
