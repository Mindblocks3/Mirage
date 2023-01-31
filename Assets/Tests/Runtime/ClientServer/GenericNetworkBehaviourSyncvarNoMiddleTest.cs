using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.ClientServer
{
    public class GenericBehaviourWithSyncVarNoMiddleBase<T> : NetworkBehaviour
    {
        [SyncVar]
        public int baseValue { get; set; }
        private int _baseValueWithHook;
        [SyncVar]
        public int baseValueWithHook { get => _baseValueWithHook; set { _baseValueWithHook = value; OnSyncedBaseValueWithHook(_baseValueWithHook); } }
        [SyncVar]
        public NetworkBehaviour target { get; set; }
        [SyncVar]
        public NetworkIdentity targetIdentity { get; set; }

        public Action<int> onBaseValueChanged;

        // Not used, just here to trigger possible errors.
        private T value;
        private T[] values;

        public void OnSyncedBaseValueWithHook(int newValue)
        {
            onBaseValueChanged?.Invoke(newValue);
        }
    }

    public class GenericBehaviourWithSyncVarNoMiddleMiddle<T> : GenericBehaviourWithSyncVarNoMiddleBase<T>
    {
        // Not used, just here to trigger possible errors.
        private T middleValue;
        private T[] middleValues;
    }

    public class GenericBehaviourWithSyncVarNoMiddleImplement : GenericBehaviourWithSyncVarNoMiddleMiddle<UnityEngine.Vector3>
    {
        [SyncVar]
        public int implementValue { get; set; }
        private int _implementValueWithHook;
        [SyncVar]
        public int implementValueWithHook { get => _implementValueWithHook; set { _implementValueWithHook = value; OnSyncedImplementValueWithHook(value); } }
        [SyncVar]
        public NetworkBehaviour implementTarget { get; set; }
        [SyncVar]
        public NetworkIdentity implementIdentity { get; set; }

        public Action<int> onImplementValueChanged;

        public void OnSyncedImplementValueWithHook(int newValue)
        {
            onImplementValueChanged?.Invoke(newValue);
        }
    }

    public class GenericNetworkBehaviorSyncvarNoMiddleTest : ClientServerSetup<GenericBehaviourWithSyncVarNoMiddleImplement>
    {
        [Test]
        public void IsZeroByDefault()
        {
            Assert.AreEqual(clientComponent.baseValue, 0);
            Assert.AreEqual(clientComponent.baseValueWithHook, 0);
            Assert.AreEqual(clientComponent.implementValue, 0);
            Assert.AreEqual(clientComponent.implementValueWithHook, 0);
            Assert.IsNull(clientComponent.target);
            Assert.IsNull(clientComponent.targetIdentity);
            Assert.IsNull(clientComponent.implementTarget);
            Assert.IsNull(clientComponent.implementIdentity);
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
            clientComponent.onBaseValueChanged += newValue =>
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
        public IEnumerator ChangeImplementValue() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.implementValue = 2;

            await UniTask.WaitUntil(() => clientComponent.implementValue != 0);

            Assert.AreEqual(clientComponent.implementValue, 2);
        });

        [UnityTest]
        public IEnumerator ChangeImplementValueHook() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.implementValueWithHook = 2;
            clientComponent.onImplementValueChanged += newValue =>
            {
                Assert.AreEqual(2, newValue);
            };

            await UniTask.WaitUntil(() => clientComponent.implementValueWithHook != 0);
        });

        [UnityTest]
        public IEnumerator ChangeImplementTarget() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.implementTarget = serverComponent;

            await UniTask.WaitUntil(() => clientComponent.implementTarget != null);

            Assert.That(clientComponent.implementTarget, Is.SameAs(clientComponent));
        });

        [UnityTest]
        public IEnumerator ChangeImplementNetworkIdentity() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.implementIdentity = serverIdentity;

            await UniTask.WaitUntil(() => clientComponent.implementIdentity != null);

            Assert.That(clientComponent.implementIdentity, Is.SameAs(clientIdentity));
        });

        [UnityTest]
        public IEnumerator SpawnWithValue() => UniTask.ToCoroutine(async () =>
        {
            // create an object, set the target and spawn it
            UnityEngine.GameObject newObject = UnityEngine.Object.Instantiate(playerPrefab);
            GenericBehaviourWithSyncVarNoMiddleImplement newBehavior = newObject.GetComponent<GenericBehaviourWithSyncVarNoMiddleImplement>();
            newBehavior.baseValue = 2;
            newBehavior.implementValue = 222;
            newBehavior.target = serverComponent;
            newBehavior.targetIdentity = serverIdentity;
            newBehavior.implementTarget = serverComponent;
            newBehavior.implementIdentity = serverIdentity;
            serverObjectManager.Spawn(newObject);

            // wait until the client spawns it
            ushort newObjectId = newBehavior.NetId;
            NetworkIdentity newClientObject = await AsyncUtil.WaitUntilSpawn(client.World, newObjectId);

            // check if the target was set correctly in the client
            GenericBehaviourWithSyncVarNoMiddleImplement newClientBehavior = newClientObject.GetComponent<GenericBehaviourWithSyncVarNoMiddleImplement>();
            Assert.AreEqual(newClientBehavior.baseValue, 2);
            Assert.AreEqual(newClientBehavior.implementValue, 222);
            Assert.That(newClientBehavior.target, Is.SameAs(clientComponent));
            Assert.That(newClientBehavior.targetIdentity, Is.SameAs(clientIdentity));
            Assert.That(newClientBehavior.implementTarget, Is.SameAs(clientComponent));
            Assert.That(newClientBehavior.implementIdentity, Is.SameAs(clientIdentity));

            // cleanup
            serverObjectManager.Destroy(newObject);
        });
    }
}
