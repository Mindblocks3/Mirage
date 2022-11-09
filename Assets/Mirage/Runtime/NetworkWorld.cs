using System;
using System.Collections.Generic;
using Mirage.Logging;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Mirage
{
    public class SpawnUnityEvent : UnityEvent<NetworkIdentity> { }

    public class NetworkWorld : IObjectLocator
    {
        static readonly ILogger logger = LogFactory.GetLogger<NetworkWorld>();

        /// <summary>
        /// Raised when the client spawns an object
        /// </summary>
        public SpawnUnityEvent onSpawn = new SpawnUnityEvent();

        /// <summary>
        /// Raised when the client unspawns an object
        /// </summary>
        public SpawnUnityEvent onUnspawn = new SpawnUnityEvent();

        private readonly Dictionary<uint, NetworkIdentity> SpawnedObjects = new Dictionary<uint, NetworkIdentity>();

        public IReadOnlyCollection<NetworkIdentity> SpawnedIdentities => SpawnedObjects.Values;

        public bool TryGetIdentity(uint netId, out NetworkIdentity identity)
        {
            return SpawnedObjects.TryGetValue(netId, out identity) && identity != null;
        }
        /// <summary>
        /// Adds Identity to world and invokes spawned event
        /// </summary>
        /// <param name="netId"></param>
        /// <param name="identity"></param>
        internal void AddIdentity(uint netId, NetworkIdentity identity)
        {
            Assert.IsTrue(netId != 0, "NetId cannot be zero");
            Assert.IsNotNull(identity, "Identity cannot be null");
            Assert.IsFalse(SpawnedObjects.ContainsKey(netId), $"NetId {netId} already exists");
            Assert.AreEqual(netId, identity.NetId, $"NetId {netId} does not match identity's netId {identity.NetId}");

            SpawnedObjects.Add(netId, identity);
            onSpawn.Invoke(identity);
        }
        internal void RemoveIdentity(NetworkIdentity identity)
        {
            uint netId = identity.NetId;
            SpawnedObjects.Remove(netId);
            onUnspawn.Invoke(identity);
        }
        internal void RemoveIdentity(uint netId)
        {
            Assert.IsTrue(netId != 0, "id can not be zero");


            SpawnedObjects.TryGetValue(netId, out NetworkIdentity identity);
            SpawnedObjects.Remove(netId);
            onUnspawn.Invoke(identity);
        }


        internal void ClearSpawnedObjects()
        {
            SpawnedObjects.Clear();
        }

        public NetworkWorld()
        {

        }
    }
}
