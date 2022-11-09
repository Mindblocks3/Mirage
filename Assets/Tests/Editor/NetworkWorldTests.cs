using System;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using UnityAssertionException = UnityEngine.Assertions.AssertionException;

namespace Mirage.Tests
{
    public class NetworkWorldTests
    {
        private NetworkWorld world;
        private UnityAction<NetworkIdentity> spawnListener;
        private UnityAction<NetworkIdentity> unspawnListener;
        HashSet<uint> existingIds;

        [SetUp]
        public void SetUp()
        {
            world = new NetworkWorld();
            spawnListener = Substitute.For<UnityAction<NetworkIdentity>>();
            unspawnListener = Substitute.For<UnityAction<NetworkIdentity>>();
            world.onSpawn.AddListener(spawnListener);
            world.onUnspawn.AddListener(unspawnListener);
            existingIds = new HashSet<uint>();
        }

        void AddValidIdentity(out uint id, out NetworkIdentity identity)
        {
            id = getValidId();

            identity = new GameObject("WorldTest").AddComponent<NetworkIdentity>();
            identity.NetId = id;
            world.AddIdentity(id, identity);
        }

        private uint getValidId()
        {
            uint id;
            do
            {
                id = (uint)Random.Range(1, 10000);
            }
            while (existingIds.Contains(id));

            existingIds.Add(id);
            return id;
        }

        [Test]
        public void StartsEmpty()
        {
            Assert.That(world.SpawnedIdentities.Count, Is.Zero);
        }

        [Test]
        public void TryGetReturnsFalseIfNotFound()
        {
            uint id = getValidId();
            bool found = world.TryGetIdentity(id, out NetworkIdentity _);
            Assert.That(found, Is.False);
        }
        [Test]
        public void TryGetReturnsFalseIfNull()
        {
            AddValidIdentity(out uint id, out NetworkIdentity identity);

            Object.DestroyImmediate(identity);

            bool found = world.TryGetIdentity(id, out NetworkIdentity _);
            Assert.That(found, Is.False);
        }
        [Test]
        public void TryGetReturnsTrueIfFound()
        {
            AddValidIdentity(out uint id, out NetworkIdentity identity);

            bool found = world.TryGetIdentity(id, out NetworkIdentity _);
            Assert.That(found, Is.True);
        }

        [Test]
        public void AddToCollection()
        {
            AddValidIdentity(out uint id, out NetworkIdentity expected);

            IReadOnlyCollection<NetworkIdentity> collection = world.SpawnedIdentities;

            world.TryGetIdentity(id, out NetworkIdentity actual);

            Assert.That(collection.Count, Is.EqualTo(1));
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void CanAddManyObjects()
        {
            AddValidIdentity(out uint id1, out NetworkIdentity expected1);
            AddValidIdentity(out uint id2, out NetworkIdentity expected2);
            AddValidIdentity(out uint id3, out NetworkIdentity expected3);
            AddValidIdentity(out uint id4, out NetworkIdentity expected4);

            IReadOnlyCollection<NetworkIdentity> collection = world.SpawnedIdentities;

            world.TryGetIdentity(id1, out NetworkIdentity actual1);
            world.TryGetIdentity(id2, out NetworkIdentity actual2);
            world.TryGetIdentity(id3, out NetworkIdentity actual3);
            world.TryGetIdentity(id4, out NetworkIdentity actual4);

            Assert.That(collection.Count, Is.EqualTo(4));
            Assert.That(actual1, Is.EqualTo(expected1));
            Assert.That(actual2, Is.EqualTo(expected2));
            Assert.That(actual3, Is.EqualTo(expected3));
            Assert.That(actual4, Is.EqualTo(expected4));
        }
        [Test]
        public void AddInvokesEvent()
        {
            AddValidIdentity(out uint id, out NetworkIdentity expected);

            spawnListener.Received(1).Invoke(expected);
        }
        [Test]
        public void AddInvokesEventOncePerAdd()
        {
            AddValidIdentity(out uint id1, out NetworkIdentity expected1);
            AddValidIdentity(out uint id2, out NetworkIdentity expected2);
            AddValidIdentity(out uint id3, out NetworkIdentity expected3);
            AddValidIdentity(out uint id4, out NetworkIdentity expected4);

            spawnListener.Received(1).Invoke(expected1);
            spawnListener.Received(1).Invoke(expected2);
            spawnListener.Received(1).Invoke(expected3);
            spawnListener.Received(1).Invoke(expected4);
        }
        [Test]
        public void AddThrowsIfIdentityIsNull()
        {
            uint id = getValidId();

            Assert.Throws<UnityAssertionException>(() =>
            {
                world.AddIdentity(id, null);
            }, "Identity cannot be null");
            spawnListener.DidNotReceiveWithAnyArgs().Invoke(default);
        }
        [Test]
        public void AddThrowsIfIdAlreadyInCollection()
        {
            AddValidIdentity(out uint id, out NetworkIdentity identity);

            Assert.Throws<UnityAssertionException>(() =>
            {
                world.AddIdentity(id, identity);
            }, $"NetId {id} already exists");
        }
        [Test]
        public void AddThrowsIfIdIs0()
        {
            uint id = 0;
            NetworkIdentity identity = new GameObject("WorldTest").AddComponent<NetworkIdentity>();
            identity.NetId = id;

            Assert.Throws<UnityAssertionException>(() =>
            {
                world.AddIdentity(id, identity);
            }, "NetId cannot be zero");

            spawnListener.DidNotReceiveWithAnyArgs().Invoke(default);
        }
        [Test]
        public void AddAssertsIfIdentityDoesNotHaveMatchingId()
        {
            uint id1 = getValidId();
            uint id2 = getValidId();

            NetworkIdentity identity = new GameObject("WorldTest").AddComponent<NetworkIdentity>();
            identity.NetId = id1;


            Assert.Throws<UnityAssertionException>(() =>
            {
                world.AddIdentity(id2, identity);
            }, $"NetId {id2} does not match identity's netId {id1}");

            spawnListener.DidNotReceiveWithAnyArgs().Invoke(default);
        }

        [Test]
        public void RemoveFromCollectionUsingIdentity()
        {
            AddValidIdentity(out uint id, out NetworkIdentity identity);
            Assert.That(world.SpawnedIdentities.Count, Is.EqualTo(1));

            world.RemoveIdentity(identity);
            Assert.That(world.SpawnedIdentities.Count, Is.EqualTo(0));

            Assert.That(world.TryGetIdentity(id, out NetworkIdentity identityOut), Is.False);
            Assert.That(identityOut, Is.EqualTo(null));
        }
        [Test]
        public void RemoveFromCollectionUsingNetId()
        {
            AddValidIdentity(out uint id, out NetworkIdentity identity);
            Assert.That(world.SpawnedIdentities.Count, Is.EqualTo(1));

            world.RemoveIdentity(id);
            Assert.That(world.SpawnedIdentities.Count, Is.EqualTo(0));

            Assert.That(world.TryGetIdentity(id, out NetworkIdentity identityOut), Is.False);
            Assert.That(identityOut, Is.EqualTo(null));
        }
        [Test]
        public void RemoveOnlyRemovesCorrectItem()
        {
            AddValidIdentity(out uint id1, out NetworkIdentity identity1);
            AddValidIdentity(out uint id2, out NetworkIdentity identity2);
            AddValidIdentity(out uint id3, out NetworkIdentity identity3);
            Assert.That(world.SpawnedIdentities.Count, Is.EqualTo(3));

            world.RemoveIdentity(identity2);
            Assert.That(world.SpawnedIdentities.Count, Is.EqualTo(2));

            Assert.That(world.TryGetIdentity(id1, out NetworkIdentity identityOut1), Is.True);
            Assert.That(world.TryGetIdentity(id2, out NetworkIdentity identityOut2), Is.False);
            Assert.That(world.TryGetIdentity(id3, out NetworkIdentity identityOut3), Is.True);
            Assert.That(identityOut1, Is.EqualTo(identity1));
            Assert.That(identityOut2, Is.EqualTo(null));
            Assert.That(identityOut3, Is.EqualTo(identity3));
        }
        [Test]
        public void RemoveInvokesEvent()
        {
            AddValidIdentity(out uint id1, out NetworkIdentity identity1);
            AddValidIdentity(out uint id2, out NetworkIdentity identity2);
            AddValidIdentity(out uint id3, out NetworkIdentity identity3);
            Assert.That(world.SpawnedIdentities.Count, Is.EqualTo(3));

            world.RemoveIdentity(identity3);
            unspawnListener.Received(1).Invoke(identity3);

            world.RemoveIdentity(id1);
            unspawnListener.Received(1).Invoke(identity1);

            world.RemoveIdentity(id2);
            unspawnListener.Received(1).Invoke(identity2);
        }

        [Test]
        public void RemoveThrowsIfIdIs0()
        {
            uint id = 0;

            Assert.Throws<UnityAssertionException>(() =>
            {
                world.RemoveIdentity(id);
            }, "id can not be zero");

            unspawnListener.DidNotReceiveWithAnyArgs().Invoke(default);
        }

        [Test]
        public void ClearRemovesAllFromCollection()
        {
            AddValidIdentity(out uint id1, out NetworkIdentity identity1);
            AddValidIdentity(out uint id2, out NetworkIdentity identity2);
            AddValidIdentity(out uint id3, out NetworkIdentity identity3);
            Assert.That(world.SpawnedIdentities.Count, Is.EqualTo(3));

            world.ClearSpawnedObjects();
            Assert.That(world.SpawnedIdentities.Count, Is.EqualTo(0));
        }
        [Test]
        public void ClearDoesNotInvokeEvent()
        {
            AddValidIdentity(out uint id1, out NetworkIdentity identity1);
            AddValidIdentity(out uint id2, out NetworkIdentity identity2);
            AddValidIdentity(out uint id3, out NetworkIdentity identity3);
            Assert.That(world.SpawnedIdentities.Count, Is.EqualTo(3));

            world.ClearSpawnedObjects();
            unspawnListener.DidNotReceiveWithAnyArgs().Invoke(default);
        }
    }
}
