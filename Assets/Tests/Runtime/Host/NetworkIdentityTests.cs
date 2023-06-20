using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using InvalidOperationException = System.InvalidOperationException;
using Object = UnityEngine.Object;
using UnityAssertionException = UnityEngine.Assertions.AssertionException;

namespace Mirage.Tests.Runtime.Host
{
    public class NetworkIdentityTests : HostSetup<MockComponent>
    {
        #region SetUp

        GameObject gameObject;
        NetworkIdentity testIdentity;

        public override void ExtraSetup()
        {
            gameObject = new GameObject(this.GetType().Name);
            testIdentity = gameObject.AddComponent<NetworkIdentity>();
        }

        public override void ExtraTearDown()
        {
            Object.Destroy(gameObject);
        }

        #endregion

        [Test]
        public void AssignClientAuthorityNoServer()
        {
            Assert.Throws<UnityAssertionException>(
                () => testIdentity.AssignClientAuthority(server.LocalPlayer),
                "AssignClientAuthority can only be called on the server for spawned objects.");
        }

        [Test]
        public void IsServer()
        {
            Assert.That(testIdentity.IsServer, Is.False);
            // create a networkidentity with our test component
            serverObjectManager.Spawn(gameObject);

            Assert.That(testIdentity.IsServer, Is.True);
        }

        [Test]
        public void IsClient()
        {
            Assert.That(testIdentity.IsClient, Is.False);
            // create a networkidentity with our test component
            serverObjectManager.Spawn(gameObject);

            Assert.That(testIdentity.IsClient, Is.True);
        }

        [Test]
        public void IsLocalPlayer()
        {
            Assert.That(testIdentity.IsLocalPlayer, Is.False);
            // create a networkidentity with our test component
            serverObjectManager.Spawn(gameObject);

            Assert.That(testIdentity.IsLocalPlayer, Is.False);
        }

        [Test]
        public void AssignClientAuthorityCallback()
        {
            // create a networkidentity with our test component
            serverObjectManager.Spawn(gameObject);

            // test the callback too
            int callbackCalled = 0;

            void Callback(INetworkPlayer player, NetworkIdentity networkIdentity, bool state)
            {
                ++callbackCalled;
                Assert.That(networkIdentity, Is.EqualTo(testIdentity));
                Assert.That(state, Is.True);
            }

            NetworkIdentity.clientAuthorityCallback += Callback;

            // assign authority
            testIdentity.AssignClientAuthority(server.LocalPlayer);

            Assert.That(callbackCalled, Is.EqualTo(1));

            NetworkIdentity.clientAuthorityCallback -= Callback;
        }

        [Test]
        public void DefaultAuthority()
        {
            // create a networkidentity with our test component
            serverObjectManager.Spawn(gameObject);
            Assert.That(testIdentity.ConnectionToClient, Is.Null);
        }

        [Test]
        public void AssignAuthority()
        {
            // create a networkidentity with our test component
            serverObjectManager.Spawn(gameObject);
            testIdentity.AssignClientAuthority(server.LocalPlayer);

            Assert.That(testIdentity.ConnectionToClient, Is.SameAs(server.LocalPlayer));
        }

        [Test]
        public void SpawnWithAuthority()
        {
            serverObjectManager.Spawn(gameObject, server.LocalPlayer);
            Assert.That(testIdentity.ConnectionToClient, Is.SameAs(server.LocalPlayer));
        }

        [Test]
        public void SpawnWithAssetId()
        {
            var replacementGuid = Guid.NewGuid();
            serverObjectManager.Spawn(gameObject, replacementGuid, server.LocalPlayer);
            Assert.That(testIdentity.AssetId, Is.EqualTo(replacementGuid));
        }

        [Test]
        public void ReassignClientAuthority()
        {
            // create a networkidentity with our test component
            serverObjectManager.Spawn(gameObject);
            // assign authority
            testIdentity.AssignClientAuthority(server.LocalPlayer);

            var player = Substitute.For<INetworkPlayer>();

            Assert.Throws<UnityAssertionException>(
                () => testIdentity.AssignClientAuthority(player),
                "AssignClientAuthority cannot change owner while existing owner is still connected.");
        }

        [Test]
        public void AssignNullAuthority()
        {
            // create a networkidentity with our test component
            serverObjectManager.Spawn(gameObject);

            Assert.Throws<UnityAssertionException>(
                () => testIdentity.AssignClientAuthority(null),
                "AssignClientAuthority player object is null.");
        }

        [Test]
        public void RemoveclientAuthorityNotSpawned()
        {
            Assert.Throws<UnityAssertionException>(
                () => testIdentity.RemoveClientAuthority(),
                "RemoveClientAuthority can only be called on the server for spawned objects.");
        }

        [Test]
        public void RemoveClientAuthorityOfOwner()
        {
            serverObjectManager.ReplaceCharacter(server.LocalPlayer, gameObject);

            Assert.Throws<UnityAssertionException>(
                () => testIdentity.RemoveClientAuthority(),
                "RemoveClientAuthority cannot remove authority for a player object.");
        }

        [Test]
        public void RemoveClientAuthority()
        {
            serverObjectManager.Spawn(gameObject);
            testIdentity.AssignClientAuthority(server.LocalPlayer);
            testIdentity.RemoveClientAuthority();
            Assert.That(testIdentity.ConnectionToClient, Is.Null);
        }

        [UnityTest]
        public IEnumerator OnStopServer() => UniTask.ToCoroutine(async () =>
        {
            serverObjectManager.Spawn(gameObject);

            UnityAction mockHandler = Substitute.For<UnityAction>();
            testIdentity.OnStopServer.AddListener(mockHandler);

            serverObjectManager.Destroy(gameObject, false);

            await UniTask.Delay(1);
            mockHandler.Received().Invoke();
        });

        [Test]
        public void IdentityClientValueSet()
        {
            Assert.That(identity.Client, Is.Not.Null);
        }

        [Test]
        public void IdentityServerValueSet()
        {
            Assert.That(identity.Server, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator DestroyOwnedObjectsTest() => UniTask.ToCoroutine(async () =>
        {
            var testObj1 = new GameObject(this.GetType().Name);
            var testObj2 = new GameObject(this.GetType().Name);
            var testObj3 = new GameObject(this.GetType().Name);

            server.LocalPlayer.AddOwnedObject(testObj1.AddComponent<NetworkIdentity>());
            server.LocalPlayer.AddOwnedObject(testObj2.AddComponent<NetworkIdentity>());
            server.LocalPlayer.AddOwnedObject(testObj3.AddComponent<NetworkIdentity>());
            server.LocalPlayer.DestroyOwnedObjects();

            await AsyncUtil.WaitUntilWithTimeout(() => !testObj1);
            await AsyncUtil.WaitUntilWithTimeout(() => !testObj2);
            await AsyncUtil.WaitUntilWithTimeout(() => !testObj3);
        });
    }

    public class NetworkIdentityStartedTests : HostSetup<MockComponent>
    {
        #region SetUp

        GameObject gameObject;
        NetworkIdentity testIdentity;

        public override void ExtraSetup()
        {
            gameObject = new GameObject(this.GetType().Name);
            testIdentity = gameObject.AddComponent<NetworkIdentity>();
            server.Started.AddListener(() => serverObjectManager.Spawn(gameObject));
        }

        public override void ExtraTearDown()
        {
            Object.Destroy(gameObject);
        }

        #endregion

        [UnityTest]
        public IEnumerator ClientNotNullAfterSpawnInStarted() => UniTask.ToCoroutine(async () =>
        {
            await AsyncUtil.WaitUntilWithTimeout(() => testIdentity.Client == (INetworkClient)client);
        });
    }
}
