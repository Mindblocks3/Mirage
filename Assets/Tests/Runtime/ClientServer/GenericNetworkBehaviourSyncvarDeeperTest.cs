using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.ClientServer
{
    public class GenericBehaviourWithSyncVarDeeperBase<T> : NetworkBehaviour
    {
        [SyncVar]
        public int baseValue { get; set; }

        private int _baseValueWithHook;
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

        public void OnSyncedBaseValueWithHook(int newValue)
        {
            onBaseValueChanged?.Invoke(newValue);
        }
    }

    public class GenericBehaviourWithSyncVarDeeperMiddle<T> : GenericBehaviourWithSyncVarDeeperBase<T>
    {
        [SyncVar]
        public int middleValue { get; set; }
        private int _middleValueWithHook;
        [SyncVar]
        public int middleValueWithHook { get => _middleValueWithHook; set { _middleValueWithHook = value; OnSyncedMiddleValueWithHook(value); } }
        [SyncVar]
        public NetworkBehaviour middleTarget { get; set; }
        [SyncVar]
        public NetworkIdentity middleIdentity { get; set; }

        public Action<int> onMiddleValueChanged;

        // Not used, just here to trigger possible errors.
        private T middleGenericValue;
        private T[] middleGenericValues;

        public void OnSyncedMiddleValueWithHook(int newValue)
        {
            onMiddleValueChanged?.Invoke(newValue);
        }
    }

    public class GenericBehaviourWithSyncVarDeeperImplement : GenericBehaviourWithSyncVarDeeperMiddle<UnityEngine.Vector3>
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

    public class GenericNetworkBehaviorSyncvarDeeperTest : ClientServerSetup<GenericBehaviourWithSyncVarDeeperImplement>
    {
        [Test]
        public void IsZeroByDefault()
        {
            Assert.AreEqual(clientComponent.baseValue, 0);
            Assert.AreEqual(clientComponent.baseValueWithHook, 0);
            Assert.AreEqual(clientComponent.middleValue, 0);
            Assert.AreEqual(clientComponent.middleValueWithHook, 0);
            Assert.AreEqual(clientComponent.implementValue, 0);
            Assert.AreEqual(clientComponent.implementValueWithHook, 0);
            Assert.IsNull(clientComponent.target);
            Assert.IsNull(clientComponent.targetIdentity);
            Assert.IsNull(clientComponent.middleTarget);
            Assert.IsNull(clientComponent.middleIdentity);
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
        public IEnumerator ChangeMiddleValue() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.middleValue = 2;

            await UniTask.WaitUntil(() => clientComponent.middleValue != 0);

            Assert.AreEqual(clientComponent.middleValue, 2);
        });

        [UnityTest]
        public IEnumerator ChangeMiddleValueHook() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.middleValueWithHook = 2;
            clientComponent.onMiddleValueChanged += newValue =>
            {
                Assert.AreEqual(2, newValue);
            };

            await UniTask.WaitUntil(() => clientComponent.middleValueWithHook != 0);
        });

        [UnityTest]
        public IEnumerator ChangeMiddleTarget() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.middleTarget = serverComponent;

            await UniTask.WaitUntil(() => clientComponent.middleTarget != null);

            Assert.That(clientComponent.middleTarget, Is.SameAs(clientComponent));
        });

        [UnityTest]
        public IEnumerator ChangeMiddleNetworkIdentity() => UniTask.ToCoroutine(async () =>
        {
            serverComponent.middleIdentity = serverIdentity;

            await UniTask.WaitUntil(() => clientComponent.middleIdentity != null);

            Assert.That(clientComponent.middleIdentity, Is.SameAs(clientIdentity));
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
            GenericBehaviourWithSyncVarDeeperImplement newBehavior = newObject.GetComponent<GenericBehaviourWithSyncVarDeeperImplement>();
            newBehavior.baseValue = 2;
            newBehavior.middleValue = 22;
            newBehavior.implementValue = 222;
            newBehavior.target = serverComponent;
            newBehavior.targetIdentity = serverIdentity;
            newBehavior.middleTarget = serverComponent;
            newBehavior.middleIdentity = serverIdentity;
            newBehavior.implementTarget = serverComponent;
            newBehavior.implementIdentity = serverIdentity;
            serverObjectManager.Spawn(newObject);

            // wait until the client spawns it
            ushort newObjectId = newBehavior.NetId;
            NetworkIdentity newClientObject = await AsyncUtil.WaitUntilSpawn(client.World, newObjectId);

            // check if the target was set correctly in the client
            GenericBehaviourWithSyncVarDeeperImplement newClientBehavior = newClientObject.GetComponent<GenericBehaviourWithSyncVarDeeperImplement>();
            Assert.AreEqual(newClientBehavior.baseValue, 2);
            Assert.AreEqual(newClientBehavior.middleValue, 22);
            Assert.AreEqual(newClientBehavior.implementValue, 222);
            Assert.That(newClientBehavior.target, Is.SameAs(clientComponent));
            Assert.That(newClientBehavior.targetIdentity, Is.SameAs(clientIdentity));
            Assert.That(newClientBehavior.middleTarget, Is.SameAs(clientComponent));
            Assert.That(newClientBehavior.middleIdentity, Is.SameAs(clientIdentity));
            Assert.That(newClientBehavior.implementTarget, Is.SameAs(clientComponent));
            Assert.That(newClientBehavior.implementIdentity, Is.SameAs(clientIdentity));

            // cleanup
            serverObjectManager.Destroy(newObject);
        });
    }
}
