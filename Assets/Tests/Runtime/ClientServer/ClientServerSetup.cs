using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

using Object = UnityEngine.Object;

namespace Mirage.Tests.Runtime.ClientServer
{
    // set's up a client and a server
    public class ClientServerSetup<T> where T : NetworkBehaviour
    {

        #region Setup
        protected GameObject serverGo;
        protected NetworkServer server;
        protected ServerObjectManager serverObjectManager;
        protected GameObject serverPlayerGO;
        protected NetworkIdentity serverIdentity;
        protected T serverComponent;

        protected GameObject clientGo;
        protected NetworkClient client;
        protected ClientObjectManager clientObjectManager;
        protected GameObject clientPlayerGO;
        protected NetworkIdentity clientIdentity;
        protected T clientComponent;

        protected GameObject playerPrefab;

        protected Transport testTransport;
        protected INetworkPlayer connectionToServer;
        protected INetworkPlayer connectionToClient;

        public virtual void ExtraSetup() { }

        [UnitySetUp]
        public IEnumerator Setup() => UniTask.ToCoroutine(async () =>
        {
            serverGo = new GameObject("server", typeof(ServerObjectManager), typeof(NetworkServer));
            clientGo = new GameObject("client", typeof(ClientObjectManager), typeof(NetworkClient));

            testTransport = serverGo.AddComponent<LoopbackTransport>();

            server = serverGo.GetComponent<NetworkServer>();
            client = clientGo.GetComponent<NetworkClient>();

            server.Transport = testTransport;
            client.Transport = testTransport;

            serverObjectManager = serverGo.GetComponent<ServerObjectManager>();
            serverObjectManager.Server = server;

            clientObjectManager = clientGo.GetComponent<ClientObjectManager>();
            clientObjectManager.Client = client;

            await UniTask.Delay(1);

            ExtraSetup();

            // create and register a prefab
            playerPrefab = new GameObject("serverPlayer", typeof(NetworkIdentity), typeof(T));
            NetworkIdentity identity = playerPrefab.GetComponent<NetworkIdentity>();
            identity.AssetId = Guid.NewGuid();
            clientObjectManager.RegisterPrefab(identity);

            // wait for client and server to initialize themselves
            await UniTask.Delay(1);

            // start the server
            var started = new UniTaskCompletionSource();
            server.Started.AddListener(() => started.TrySetResult());
            server.StartAsync().Forget();

            await started.Task;

            // now start the client
            await client.ConnectAsync("localhost");

            await AsyncUtil.WaitUntilWithTimeout(() => server.Players.Count > 0);

            // get the connections so that we can spawn players
            connectionToClient = server.Players.First();
            connectionToServer = client.Player;

            // create a player object in the server
            serverPlayerGO = Object.Instantiate(playerPrefab);
            serverIdentity = serverPlayerGO.GetComponent<NetworkIdentity>();
            serverComponent = serverPlayerGO.GetComponent<T>();
            serverObjectManager.AddCharacter(connectionToClient, serverPlayerGO);

            // wait for client to spawn it
            await AsyncUtil.WaitUntilWithTimeout(() => connectionToServer.Identity != null);

            clientIdentity = connectionToServer.Identity;
            clientPlayerGO = clientIdentity.gameObject;
            clientComponent = clientPlayerGO.GetComponent<T>();
        });

        public virtual void ExtraTearDown() { }

        [UnityTearDown]
        public IEnumerator ShutdownHost() => UniTask.ToCoroutine(async () =>
        {
            client.Disconnect();
            server.Stop();

            await AsyncUtil.WaitUntilWithTimeout(() => !client.Active);
            await AsyncUtil.WaitUntilWithTimeout(() => !server.Active);

            Object.Destroy(playerPrefab);
            Object.Destroy(serverGo);
            Object.Destroy(clientGo);
            Object.Destroy(serverPlayerGO);
            Object.Destroy(clientPlayerGO);

            ExtraTearDown();
        });

        #endregion
    }
}
