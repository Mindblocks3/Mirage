using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mirage
{

    public class CharacterSpawnerTest
    {
        private GameObject go;
        private NetworkClient client;
        private NetworkServer server;
        private CharacterSpawner spawner;
        private ServerObjectManager serverObjectManager;
        private ClientObjectManager clientObjectManager;
        private GameObject playerPrefab;

        private Transform pos1;
        private Transform pos2;

        [SetUp]
        public void Setup()
        {
            go = new GameObject();
            client = go.AddComponent<NetworkClient>();
            server = go.AddComponent<NetworkServer>();
            spawner = go.AddComponent<CharacterSpawner>();
            serverObjectManager = go.AddComponent<ServerObjectManager>();
            clientObjectManager = go.AddComponent<ClientObjectManager>();
            serverObjectManager.Server = server;
            clientObjectManager.Client = client;
            spawner.Client = client;
            spawner.Server = server;
            spawner.ServerObjectManager = serverObjectManager;
            spawner.ClientObjectManager = clientObjectManager;

            playerPrefab = new GameObject();
            NetworkIdentity playerId = playerPrefab.AddComponent<NetworkIdentity>();

            spawner.PlayerPrefab = playerId;

            pos1 = new GameObject().transform;
            pos2 = new GameObject().transform;
            spawner.startPositions.Add(pos1);
            spawner.startPositions.Add(pos2);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(playerPrefab);

            Object.DestroyImmediate(pos1.gameObject);
            Object.DestroyImmediate(pos2.gameObject);
        }

        [Test]
        public void AutoConfigureClient()
        {
            spawner.Start();
            Assert.That(spawner.Client, Is.SameAs(client));
        }

        [Test]
        public void AutoConfigureServer()
        {
            spawner.Start();
            Assert.That(spawner.Server, Is.SameAs(server));
        }

        [Test]
        public void GetStartPositionRoundRobinTest()
        {
            spawner.Start();

            spawner.playerSpawnMethod = CharacterSpawner.PlayerSpawnMethod.RoundRobin;
            Assert.That(spawner.GetStartPosition(), Is.SameAs(pos1.transform));
            Assert.That(spawner.GetStartPosition(), Is.SameAs(pos2.transform));
            Assert.That(spawner.GetStartPosition(), Is.SameAs(pos1.transform));
            Assert.That(spawner.GetStartPosition(), Is.SameAs(pos2.transform));
        }

        [Test]
        public void GetStartPositionRandomTest()
        {
            spawner.Start();

            spawner.playerSpawnMethod = CharacterSpawner.PlayerSpawnMethod.Random;
            Assert.That(spawner.GetStartPosition(), Is.SameAs(pos1.transform) | Is.SameAs(pos2.transform));
        }

        [Test]
        public void GetStartPositionNullTest()
        {
            spawner.Start();

            spawner.startPositions.Clear();
            Assert.That(spawner.GetStartPosition(), Is.SameAs(null));
        }
    }
}
