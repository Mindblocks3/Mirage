using System;
using System.Collections.Generic;
using Mirage.Logging;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Mirage
{

    /// <summary>
    /// Spawns a player as soon as the connection is authenticated
    /// </summary>
    public class CharacterSpawner : MonoBehaviour
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(CharacterSpawner));

        [FormerlySerializedAs("client")]
        public NetworkClient Client;
        [FormerlySerializedAs("server")]
        public NetworkServer Server;
        [FormerlySerializedAs("clientObjectManager")]
        public ClientObjectManager ClientObjectManager;
        [FormerlySerializedAs("serverObjectManager")]
        public ServerObjectManager ServerObjectManager;
        [FormerlySerializedAs("playerPrefab")]
        public NetworkIdentity PlayerPrefab;

        /// <summary>
        /// Whether to span the player upon connection automatically
        /// </summary>
        public bool AutoSpawn = true;

        // Start is called before the first frame update
        public virtual void Start()
        {
            Assert.IsNotNull(Client, "Client must be set");
            Assert.IsNotNull(Server, "Server must be set");
            Assert.IsNotNull(ClientObjectManager, "ClientObjectManager must be set");
            Assert.IsNotNull(ServerObjectManager, "ServerObjectManager must be set");

            Client.Authenticated.AddListener(SendCharacterMessage);
            ClientObjectManager.RegisterPrefab(PlayerPrefab);
            Server.Authenticated.AddListener(OnServerAuthenticated);
        }

        private void SendCharacterMessage(INetworkPlayer player)
        {
            RequestServerSpawnPlayer();
        }

        void OnDestroy()
        {
            if (Client != null )
            {
                Client.Authenticated.RemoveListener(SendCharacterMessage);
            }
            if (Server != null)
            {
                Server.Authenticated.RemoveListener(OnServerAuthenticated);
            }
        }

        private void OnServerAuthenticated(INetworkPlayer player)
        {
            // wait for client to send us an AddPlayerMessage
            player.RegisterHandler<AddCharacterMessage>(OnServerAddPlayerInternal);
        }

        public virtual void RequestServerSpawnPlayer()
        {
            Client.Send(new AddCharacterMessage());
        }

        void OnServerAddPlayerInternal(INetworkPlayer player, AddCharacterMessage msg)
        {
            logger.Log("CharacterSpawner.OnServerAddPlayer");

            Assert.IsNull(player.Identity, "Player object already exists for connection");

            OnServerAddPlayer(player);
        }

        /// <summary>
        /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
        /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
        /// </summary>
        /// <param name="player">Connection from client.</param>
        public virtual void OnServerAddPlayer(INetworkPlayer player)
        {
            Transform startPos = GetStartPosition();
            NetworkIdentity character = startPos != null
                ? Instantiate(PlayerPrefab, startPos.position, startPos.rotation)
                : Instantiate(PlayerPrefab);

            ServerObjectManager.AddCharacter(player, character.gameObject);
        }

        /// <summary>
        /// This finds a spawn position based on start position objects in the scene.
        /// <para>This is used by the default implementation of OnServerAddPlayer.</para>
        /// </summary>
        /// <returns>Returns the transform to spawn a player at, or null.</returns>
        public virtual Transform GetStartPosition()
        {
            if (startPositions.Count == 0)
                return null;

            if (playerSpawnMethod == PlayerSpawnMethod.Random)
            {
                return startPositions[UnityEngine.Random.Range(0, startPositions.Count)];
            }
            else
            {
                Transform startPosition = startPositions[startPositionIndex];
                startPositionIndex = (startPositionIndex + 1) % startPositions.Count;
                return startPosition;
            }
        }

        public int startPositionIndex;

        /// <summary>
        /// List of transforms where players can be spawned
        /// </summary>
        public List<Transform> startPositions = new List<Transform>();

        /// <summary>
        /// Enumeration of methods of where to spawn player objects in multiplayer games.
        /// </summary>
        public enum PlayerSpawnMethod { Random, RoundRobin }

        /// <summary>
        /// The current method of spawning players used by the CharacterSpawner.
        /// </summary>
        [Tooltip("Round Robin or Random order of Start Position selection")]
        public PlayerSpawnMethod playerSpawnMethod;
    }
}
