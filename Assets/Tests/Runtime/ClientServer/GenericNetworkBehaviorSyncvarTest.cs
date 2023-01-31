using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.ClientServer
{
    public class GenericBehaviourWithSyncVar<T> : NetworkBehaviour
    {
        [SyncVar]
        public int baseValue { get; set; }
        public int _baseValueWithHook;
        [SyncVar]
        public int baseValueWithHook { get => _baseValueWithHook; set { _baseValueWithHook = value; OnSyncedBaseValueWithHook(value); } }
        [SyncVar]
        public NetworkBehaviour target { get; set; }
        [SyncVar]
        public NetworkIdentity targetIdentity { get; set; }

        public Action<int> onBaseValueChanged;

        // Not used, just here to trigger possible errors.
        private T value;
        private T[] values;

        public void OnSyncedBaseValueWithHook( int newValue)
        {
            onBaseValueChanged?.Invoke(newValue);
        }
    }

    public class GenericBehaviourWithSyncVarImplement : GenericBehaviourWithSyncVar<UnityEngine.Vector3>
    {
        [SyncVar]
        public int childValue { get; set; }
        private int _childValueWithHook;
        [SyncVar]
        public int childValueWithHook { get => _childValueWithHook; set { _childValueWithHook = value; OnSyncedChildValueWithHook(value); } }
        [SyncVar]
        public NetworkBehaviour childTarget { get; set; }
        [SyncVar]
        public NetworkIdentity childIdentity { get; set; }

        public Action<int> onChildValueChanged;

        public void OnSyncedChildValueWithHook(int newValue)
        {
            onChildValueChanged?.Invoke(newValue);
        }
    }

    public class GenericNetworkBehaviorSyncvarTest : ClientServerSetup<GenericBehaviourWithSyncVarImplement>
    {
        [Test]
        public void IsZeroByDefault()
        {
            Assert.AreEqual(clientComponent.baseValue, 0);
            Assert.AreEqual(clientComponent.baseValueWithHook, 0);
            Assert.AreEqual(clientComponent.childValue, 0);
            Assert.AreEqual(clientComponent.childValueWithHook, 0);
            Assert.IsNull(clientComponent.target);
            Assert.IsNull(clientComponent.targetIdentity);
            Assert.IsNull(clientComponent.childTarget);
            Assert.IsNull(clientComponent.childIdentity);
        }

        [UnityTest]
        public IEnumerator ChangeValue() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.baseValue = 2;

            await UniTask.WaitUntil(() => clientComponent.baseValue != 0);

            Assert.AreEqual(clientComponent.baseValue, 2);
        });

        [UnityTest]
        public IEnumerator ChangeValueHook() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.baseValueWithHook = 2;
            clientComponent.onBaseValueChanged += (newValue) =>
            {
                Assert.AreEqual(2, newValue);
            };

            await UniTask.WaitUntil(() => clientComponent.baseValueWithHook != 0);
        });

        [UnityTest]
        public IEnumerator ChangeTarget() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.target = serverComponent;

            await UniTask.WaitUntil(() => clientComponent.target != null);

            Assert.That(clientComponent.target, Is.SameAs(clientComponent));
        });

        [UnityTest]
        public IEnumerator ChangeNetworkIdentity() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.targetIdentity = serverIdentity;

            await UniTask.WaitUntil(() => clientComponent.targetIdentity != null);

            Assert.That(clientComponent.targetIdentity, Is.SameAs(clientIdentity));
        });

        [UnityTest]
        public IEnumerator ChangeChildValue() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.childValue = 2;

            await UniTask.WaitUntil(() => clientComponent.childValue != 0);

            Assert.AreEqual(clientComponent.childValue, 2);
        });

        [UnityTest]
        public IEnumerator ChangeChildValueHook() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.childValueWithHook = 2;
            clientComponent.onChildValueChanged += (newValue) =>
            {
                Assert.AreEqual(2, newValue);
            };

            await UniTask.WaitUntil(() => clientComponent.childValueWithHook != 0);
        });

        [UnityTest]
        public IEnumerator ChangeChildTarget() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.childTarget = serverComponent;

            await UniTask.WaitUntil(() => clientComponent.childTarget != null);

            Assert.That(clientComponent.childTarget, Is.SameAs(clientComponent));
        });

        [UnityTest]
        public IEnumerator ChangeChildNetworkIdentity() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.childIdentity = serverIdentity;

            await UniTask.WaitUntil(() => clientComponent.childIdentity != null);

            Assert.That(clientComponent.childIdentity, Is.SameAs(clientIdentity));
        });

        [UnityTest]
        public IEnumerator SpawnWithValue() => UniTask.ToCoroutine(async () =>
        {
            // create an object, set the target and spawn it
            UnityEngine.GameObject newObject = UnityEngine.Object.Instantiate(playerPrefab);
            GenericBehaviourWithSyncVarImplement newBehavior = newObject.GetComponent<GenericBehaviourWithSyncVarImplement>();
            newBehavior.baseValue = 2;
            newBehavior.childValue = 22;
            newBehavior.target = serverComponent;
            newBehavior.targetIdentity = serverIdentity;
            newBehavior.childTarget = serverComponent;
            newBehavior.childIdentity = serverIdentity;
            serverObjectManager.Spawn(newObject);

            // wait until the client spawns it
            ushort newObjectId = newBehavior.NetId;

            NetworkIdentity newClientObject = await AsyncUtil.WaitUntilSpawn(client.World, newObjectId);
            // check if the target was set correctly in the client

            GenericBehaviourWithSyncVarImplement newClientBehavior = newClientObject.GetComponent<GenericBehaviourWithSyncVarImplement>();
            Assert.AreEqual(newClientBehavior.baseValue, 2);
            Assert.AreEqual(newClientBehavior.childValue, 22);
            Assert.That(newClientBehavior.target, Is.SameAs(clientComponent));
            Assert.That(newClientBehavior.targetIdentity, Is.SameAs(clientIdentity));
            Assert.That(newClientBehavior.childTarget, Is.SameAs(clientComponent));
            Assert.That(newClientBehavior.childIdentity, Is.SameAs(clientIdentity));

            // cleanup
            serverObjectManager.Destroy(newObject);
        });
    }
}
