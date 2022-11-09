using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NSubstitute.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using UnityAssertionException = UnityEngine.Assertions.AssertionException;

namespace Mirage.Tests.Runtime.ClientServer
{

    [TestFixture]
    public class ServerObjectManagerTests : ClientServerSetup<MockComponent>
    {

        [Test]
        public void ThrowsIfSpawnCalledWhenServerIsNotAcctive()
        {
            var obj = CreatePrefab<MockComponent>();

            server.Stop();

            Assert.Throws<UnityAssertionException>(
                () => serverObjectManager.Spawn(obj, connectionToClient),
                "Spawn() called when server is not active");
            
            GameObject.Destroy(obj);
        }

        [Test]
        public void ThrowsIfSpawnCalledOwnerHasNoNetworkIdentity()
        {
            var badObject = new GameObject();
            var badOwner = new GameObject();

            Assert.Throws<UnityAssertionException>(
                () => { serverObjectManager.Spawn(badObject, badOwner); },
                "Onwer object must have an identity");

            GameObject.Destroy(badObject);
            GameObject.Destroy(badOwner);
        }

        [UnityTest]
        public IEnumerator SpawnByIdentityTest() => UniTask.ToCoroutine(async () =>
        {
            serverObjectManager.Spawn(serverIdentity);

            await AsyncUtil.WaitUntilWithTimeout(() => (NetworkServer)serverIdentity.Server == server);
        });

        [Test]
        public void ThrowsIfSpawnCalledWithOwnerWithNoOwnerTest()
        {
            var badObject = new GameObject();
            var badOwner = new GameObject();
            badOwner.AddComponent<NetworkIdentity>();

            Assert.Throws<UnityAssertionException>(
                () => { serverObjectManager.Spawn(badObject, badOwner); },
                "Player object is not a player in the connection");

            GameObject.Destroy(badOwner);
            GameObject.Destroy(badObject);
        }

        [UnityTest]
        public IEnumerator ShowForConnection() => UniTask.ToCoroutine(async () =>
        {
            bool invoked = false;

            connectionToServer.RegisterHandler<SpawnMessage>(msg => invoked = true);

            connectionToClient.IsReady = true;

            // call ShowForConnection
            serverObjectManager.ShowForConnection(serverIdentity, connectionToClient);

            connectionToServer.ProcessMessagesAsync().Forget();

            await AsyncUtil.WaitUntilWithTimeout(() => invoked);
        });

        [Test]
        public void SpawnSceneObject()
        {
            serverIdentity.sceneId = 42;
            serverIdentity.gameObject.SetActive(false);
            serverObjectManager.SpawnObjects();
            Assert.That(serverIdentity.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void SpawnPrefabObject()
        {
            serverIdentity.sceneId = 0;
            serverIdentity.gameObject.SetActive(false);
            serverObjectManager.SpawnObjects();
            Assert.That(serverIdentity.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void SpawnEvent()
        {
            var mockHandler = Substitute.For<UnityAction<NetworkIdentity>>();
            server.World.onSpawn.AddListener(mockHandler);
            var newObj = GameObject.Instantiate(playerPrefab);
            serverObjectManager.Spawn(newObj);

            mockHandler.Received().Invoke(Arg.Any<NetworkIdentity>());
            serverObjectManager.Destroy(newObj);
        }

        [UnityTest]
        public IEnumerator ClientSpawnEvent() => UniTask.ToCoroutine(async () =>
        {
            var mockHandler = Substitute.For<UnityAction<NetworkIdentity>>();
            client.World.onSpawn.AddListener(mockHandler);
            var newObj = GameObject.Instantiate(playerPrefab);
            serverObjectManager.Spawn(newObj);

            await UniTask.WaitUntil(() => mockHandler.ReceivedCalls().Any()).Timeout(TimeSpan.FromMilliseconds(200));

            mockHandler.Received().Invoke(Arg.Any<NetworkIdentity>());
            serverObjectManager.Destroy(newObj);
        });

        [UnityTest]
        public IEnumerator ClientUnSpawnEvent() => UniTask.ToCoroutine(async () =>
        {
            var mockHandler = Substitute.For<UnityAction<NetworkIdentity>>();
            client.World.onUnspawn.AddListener(mockHandler);
            var newObj = GameObject.Instantiate(playerPrefab);
            serverObjectManager.Spawn(newObj);
            serverObjectManager.Destroy(newObj);

            await UniTask.WaitUntil(() => mockHandler.ReceivedCalls().Any()).Timeout(TimeSpan.FromMilliseconds(200));
            mockHandler.Received().Invoke(Arg.Any<NetworkIdentity>());
        });

        [Test]
        public void UnSpawnEvent()
        {
            var mockHandler = Substitute.For<UnityAction<NetworkIdentity>>();
            server.World.onUnspawn.AddListener(mockHandler);
            var newObj = GameObject.Instantiate(playerPrefab);
            serverObjectManager.Spawn(newObj);
            serverObjectManager.Destroy(newObj);
            mockHandler.Received().Invoke(newObj.GetComponent<NetworkIdentity>());
        }

        [Test]
        public void ReplacePlayerBaseTest()
        {
            var replacementIdentity = CreatePrefab<MockComponent>();
            var playerReplacement = replacementIdentity.gameObject;

            serverObjectManager.ReplaceCharacter(connectionToClient, playerReplacement);

            Assert.That(connectionToClient.Identity, Is.EqualTo(replacementIdentity));

            GameObject.Destroy(playerReplacement);
        }

        [Test]
        public void ReplacePlayerDontKeepAuthTest()
        {
            var replacementIdentity = CreatePrefab<MockComponent>();
            var playerReplacement = replacementIdentity.gameObject;

            serverObjectManager.ReplaceCharacter(connectionToClient, playerReplacement, true);

            Assert.That(clientIdentity.ConnectionToClient, Is.EqualTo(null));
            GameObject.Destroy(playerReplacement);
        }

        [Test]
        public void ReplacePlayerAssetIdTest()
        {
            var replacementGuid = Guid.NewGuid();
            var playerReplacement = new GameObject("replacement", typeof(NetworkIdentity));
            NetworkIdentity replacementIdentity = playerReplacement.GetComponent<NetworkIdentity>();
            replacementIdentity.AssetId = replacementGuid;
            clientObjectManager.RegisterPrefab(replacementIdentity);

            serverObjectManager.ReplaceCharacter(connectionToClient, playerReplacement, replacementGuid);

            Assert.That(connectionToClient.Identity.AssetId, Is.EqualTo(replacementGuid));

            GameObject.Destroy(playerReplacement);
        }

        [Test]
        public void AddPlayerForConnectionAssetIdTest()
        {
            var replacementGuid = Guid.NewGuid();
            var playerReplacement = new GameObject("replacement", typeof(NetworkIdentity));
            NetworkIdentity replacementIdentity = playerReplacement.GetComponent<NetworkIdentity>();
            replacementIdentity.AssetId = replacementGuid;
            clientObjectManager.RegisterPrefab(replacementIdentity);

            connectionToClient.Identity = null;

            serverObjectManager.AddCharacter(connectionToClient, playerReplacement, replacementGuid);

            Assert.That(replacementIdentity == connectionToClient.Identity);
            GameObject.Destroy(playerReplacement);
        }

        [UnityTest]
        public IEnumerator RemovePlayerForConnectionTest() => UniTask.ToCoroutine(async () =>
        {
            serverObjectManager.RemovePlayerForConnection(connectionToClient);

            await AsyncUtil.WaitUntilWithTimeout(() => !clientIdentity);

            Assert.That(serverPlayerGO);
        });

        [UnityTest]
        public IEnumerator RemovePlayerForConnectionExceptionTest() => UniTask.ToCoroutine(async () =>
        {
            serverObjectManager.RemovePlayerForConnection(connectionToClient);

            await AsyncUtil.WaitUntilWithTimeout(() => !clientIdentity);
            Assert.Throws<UnityAssertionException>(
                () => serverObjectManager.RemovePlayerForConnection(connectionToClient),
                "Received remove player message but connection has no player"
                );
        });

        [UnityTest]
        public IEnumerator RemovePlayerForConnectionDestroyTest() => UniTask.ToCoroutine(async () =>
        {
            serverObjectManager.RemovePlayerForConnection(connectionToClient, true);

            await AsyncUtil.WaitUntilWithTimeout(() => !clientIdentity);

            Assert.That(!serverPlayerGO);
        });

        [Test]
        public void ThrowsIfSpawnedCalledWithoutANetworkIdentity()
        {
            var badObject = new GameObject();

            Assert.Throws<UnityAssertionException>(
                () => serverObjectManager.Spawn(badObject, connectionToServer),
                "Spawning gameobject without identity"
            );

            GameObject.Destroy(badObject);
        }


        [Test]
        public void AddCharacterNoIdentityExceptionTest()
        {
            var character = new GameObject();

            Assert.Throws<UnityAssertionException>(
                () => serverObjectManager.AddCharacter(connectionToClient, character),
                "yada yada");

            GameObject.Destroy(character);
        }

        [Test]
        public void ReplacePlayerNoIdentityExceptionTest()
        {
            var character = new GameObject();

            Assert.Throws<UnityAssertionException>(
                () => serverObjectManager.ReplaceCharacter(connectionToClient, character, true),
                "GameObject New Game Object does not have a network identity");

            GameObject.Destroy(character);
        }

        [UnityTest]
        public IEnumerator SpawnObjectsExceptionTest() => UniTask.ToCoroutine(async () =>
        {
            server.Stop();

            await AsyncUtil.WaitUntilWithTimeout(() => !server.Active);

            Assert.Throws<UnityAssertionException>(
                () => serverObjectManager.SpawnObjects(),
                "SpawnObjects() called when server is not active");
        });
    }
}

