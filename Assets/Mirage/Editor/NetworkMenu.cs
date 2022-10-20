using System;
using Mirage.KCP;
using Mirage.Logging;
using UnityEditor;
using UnityEngine;

namespace Mirage
{

    public static class NetworkMenu
    {
        // Start is called before the first frame update
        [MenuItem("GameObject/Network/NetworkManager", priority = 7)]
        public static GameObject CreateNetworkManager()
        {
            var components = new Type[]
            {
                typeof(NetworkManager),
                typeof(NetworkServer),
                typeof(NetworkClient),
                typeof(ServerObjectManager),
                typeof(ClientObjectManager),
                typeof(CharacterSpawner),
                typeof(KcpTransport),
                typeof(LogSettings)
            };
            var go = new GameObject("NetworkManager", components);

            KcpTransport transport = go.GetComponent<KcpTransport>();

            NetworkClient networkClient = go.GetComponent<NetworkClient>();
            networkClient.Transport = transport;

            NetworkServer networkServer = go.GetComponent<NetworkServer>();
            networkServer.Transport = transport;

            ServerObjectManager serverObjectManager = go.GetComponent<ServerObjectManager>();
            serverObjectManager.Server = networkServer;

            ClientObjectManager clientObjectManager = go.GetComponent<ClientObjectManager>();
            clientObjectManager.Client = networkClient;

            NetworkManager networkManager = go.GetComponent<NetworkManager>();
            networkManager.Client = networkClient;
            networkManager.Server = networkServer;
            networkManager.ServerObjectManager = serverObjectManager;
            networkManager.ClientObjectManager = clientObjectManager;

            CharacterSpawner playerSpawner = go.GetComponent<CharacterSpawner>();
            playerSpawner.Client = networkClient;
            playerSpawner.Server = networkServer;
            playerSpawner.ServerObjectManager = serverObjectManager;
            playerSpawner.ClientObjectManager = clientObjectManager;

            return go;
        }
    }
}
