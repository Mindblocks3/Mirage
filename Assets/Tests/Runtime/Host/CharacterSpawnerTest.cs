using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.Host
{
    public class CharacterSpawnerTest : HostSetup<MockComponent>
    {
        AssetBundle bundle;
        GameObject player;
        CharacterSpawner spawner;

        public override void ExtraSetup()
        {
            bundle = AssetBundle.LoadFromFile("Assets/Tests/Runtime/TestScene/testscene");

            spawner = networkManagerGo.AddComponent<CharacterSpawner>();

            spawner.Client = client;
            spawner.Server = server;
            spawner.ClientObjectManager = clientObjectManager;
            spawner.ServerObjectManager = serverObjectManager;

            player = new GameObject();
            NetworkIdentity identity = player.AddComponent<NetworkIdentity>();
            spawner.PlayerPrefab = identity;

            spawner.AutoSpawn = false;

            spawner.Start();
        }

        public override void ExtraTearDown()
        {
            bundle.Unload(true);
            Object.Destroy(player);
        }

        [Test]
        public void ManualSpawnTest()
        {
            bool invokeAddPlayerMessage = false;
            server.LocalPlayer.RegisterHandler<AddCharacterMessage>(msg => invokeAddPlayerMessage = true);

            spawner.RequestServerSpawnPlayer();

            Assert.That(invokeAddPlayerMessage, Is.False);
        }
    }
}
