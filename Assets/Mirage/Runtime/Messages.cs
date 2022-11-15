using System;
using UnityEngine;

namespace Mirage
{

    #region Public System Messages

    [NetworkMessage]
    public struct ReadyMessage { }

    [NetworkMessage]
    public struct NotReadyMessage { }

    [NetworkMessage]
    public struct AddCharacterMessage { }

    [NetworkMessage]
    public struct SceneReadyMessage { }

    #endregion

    #region System Messages requried for code gen path
    [NetworkMessage]
    public struct ServerRpcMessage
    {
        public ushort netId;
        public int componentIndex;
        public int functionHash;

        // if the server Rpc can return values
        // this then a ServerRpcReply will be sent with this id
        public int replyId;
        // the parameters for the Cmd function
        // -> ReadOnlyMemory to avoid unnecessary allocations
        public ReadOnlyMemory<byte> payload;
    }

    [NetworkMessage]
    public struct ServerRpcReply
    {
        public int replyId;
        public ReadOnlyMemory<byte> payload;
    }

    [NetworkMessage]
    public struct RpcMessage
    {
        public ushort netId;
        public int componentIndex;
        public int functionHash;
        // the parameters for the Cmd function
        // -> ReadOnlyMemory to avoid unnecessary allocations
        public ReadOnlyMemory<byte> payload;
    }
    #endregion

    #region Internal System Messages
    [NetworkMessage]
    public struct SpawnMessage
    {
        /// <summary>
        /// netId of new or existing object
        /// </summary>
        public ushort netId;
        /// <summary>
        /// Is the spawning object the local player. Sets ClientScene.localPlayer
        /// </summary>
        public bool isLocalPlayer;
        /// <summary>
        /// Sets hasAuthority on the spawned object
        /// </summary>
        public bool isOwner;
        /// <summary>
        /// The id of the scene object to spawn
        /// </summary>
        public ulong sceneId;
        /// <summary>
        /// The id of the prefab to spawn
        /// <para>If sceneId != 0 then it is used instead of assetId</para>
        /// </summary>
        public Guid assetId;
        /// <summary>
        /// Local position
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// Local rotation
        /// </summary>
        public Quaternion rotation;
        /// <summary>
        /// Local scale
        /// </summary>
        public Vector3 scale;
        /// <summary>
        /// The serialized component data
        /// <remark>ReadOnlyMemory to avoid unnecessary allocations</remark>
        /// </summary>
        public ReadOnlyMemory<byte> payload;
    }

    [NetworkMessage]
    public struct ObjectDestroyMessage
    {
        public ushort netId;
    }

    [NetworkMessage]
    public struct ObjectHideMessage
    {
        public ushort netId;
    }

    [NetworkMessage]
    public struct UpdateVarsMessage
    {
        public ushort netId;
        // the serialized component data
        // -> ReadOnlyMemory to avoid unnecessary allocations
        public ReadOnlyMemory<byte> payload;
    }

    // A client sends this message to the server
    // to calculate RTT and synchronize time
    [NetworkMessage]
    public struct NetworkPingMessage
    {
        public double clientTime;
    }

    // The server responds with this message
    // The client can use this to calculate RTT and sync time
    [NetworkMessage]
    public struct NetworkPongMessage
    {
        public double clientTime;
        public double serverTime;
    }
    #endregion
}
