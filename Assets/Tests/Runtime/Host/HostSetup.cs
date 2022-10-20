using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.Host
{
    // set's up a host
    public class HostSetup<T> where T : NetworkBehaviour
    {

        #region Setup
        protected GameObject networkManagerGo;
        protected NetworkManager manager;
        protected NetworkServer server;
        protected NetworkClient client;
        protected ServerObjectManager serverObjectManager;
        protected ClientObjectManager clientObjectManager;

        protected GameObject playerGO;
        protected NetworkIdentity identity;
        protected T component;

        protected virtual bool AutoStartServer => true;
        protected bool SpawnPlayer = true;

        public virtual void ExtraSetup() { }

        [UnitySetUp]
        public IEnumerator SetupHost() => UniTask.ToCoroutine(async () =>
        {
            networkManagerGo = new GameObject();
            // set gameobject name to test name (helps with debugging)
            networkManagerGo.name = TestContext.CurrentContext.Test.MethodName;

            var transport = networkManagerGo.AddComponent<MockTransport>();
            serverObjectManager = networkManagerGo.AddComponent<ServerObjectManager>();
            clientObjectManager = networkManagerGo.AddComponent<ClientObjectManager>();
            manager = networkManagerGo.AddComponent<NetworkManager>();
            manager.Client = networkManagerGo.GetComponent<NetworkClient>();
            manager.Server = networkManagerGo.GetComponent<NetworkServer>();
            server = manager.Server;
            client = manager.Client;
            serverObjectManager.Server = server;
            clientObjectManager.Client = client;
            server.Transport = transport;
            client.Transport = transport;

            ExtraSetup();

            // wait for all Start() methods to get invoked
            await UniTask.DelayFrame(1);

            if (AutoStartServer)
            {
                StartHost();

                if (SpawnPlayer)
                {
                    playerGO = new GameObject("playerGO", typeof(Rigidbody));
                    identity = playerGO.AddComponent<NetworkIdentity>();
                    component = playerGO.AddComponent<T>();

                    serverObjectManager.AddCharacter(server.LocalPlayer, playerGO);

                    // wait for client to spawn it
                    await AsyncUtil.WaitUntilWithTimeout(() => client.Player.Identity != null);
                }
            }
        });

        protected void StartHost()
        {
            manager.Server.Listen(client);
        }

        public virtual void ExtraTearDown() { }

        [UnityTearDown]
        public IEnumerator ShutdownHost() => UniTask.ToCoroutine(async () =>
        {
            Object.Destroy(playerGO);
            manager.Server.Stop();

            await UniTask.Delay(1);
            Object.Destroy(networkManagerGo);

            ExtraTearDown();
        });

        #endregion
    }
}
