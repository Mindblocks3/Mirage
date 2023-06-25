using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.TestTools;

namespace Mirage.Tests.Performance.Runtime
{
    [Category("Performance")]
    [Category("Benchmark")]
    public class MultipleClients
    {
        const string SCENE_PATH = "Assets/Tests/Performance/Runtime/MultipleClients/Scenes/Scene.unity";
        const string MONSTER_PATH = "Assets/Tests/Performance/Runtime/MultipleClients/Prefabs/Monster.prefab";
        const int WARMUP = 50;
        const int MEASURE_COUNT = 256;

        const int CLIENT_COUNT = 10;
        const int MONSTER_COUNT = 10;

        [FormerlySerializedAs("server")]
        public NetworkServer Server;
        [FormerlySerializedAs("serverObjectManager")]
        public ServerObjectManager ServerObjectManager;
        [FormerlySerializedAs("transport")]
        public Transport Transport;

        public NetworkIdentity MonsterPrefab;
        private readonly List<GameObject> _clients = new();

        [UnitySetUp]
        public IEnumerator SetUp() => UniTask.ToCoroutine(async () =>
        {
            // load scene
            await EditorSceneManager.LoadSceneAsyncInPlayMode(SCENE_PATH, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive });
            var scene = SceneManager.GetSceneByPath(SCENE_PATH);
            SceneManager.SetActiveScene(scene);

            MonsterPrefab = AssetDatabase.LoadAssetAtPath<NetworkIdentity>(MONSTER_PATH);
            // load host
            Server = Object.FindFirstObjectByType<NetworkServer>();
            ServerObjectManager = Object.FindFirstObjectByType<ServerObjectManager>();

            Server.Authenticated.AddListener(conn => ServerObjectManager.SetClientReady(conn));

            var started = new UniTaskCompletionSource();
            Server.Started.AddListener(() => started.TrySetResult());

            // wait 1 frame before Starting server to give time for Unity to call "Start"
            await UniTask.Yield();
            Server.Listen();

            await started.Task;

            Transport = Object.FindFirstObjectByType<Transport>();

            _clients.Clear();
            // connect from a bunch of clients
            for (var i = 0; i < CLIENT_COUNT; i++) {
                var client = await StartClient(i, Transport);
                _clients.Add(client);
            }

            // spawn a bunch of monsters
            for (var i = 0; i < MONSTER_COUNT; i++)
                SpawnMonster(i);
            
            while (Object.FindObjectsByType<MonsterBehavior>(FindObjectsSortMode.None).Count() < MONSTER_COUNT * (CLIENT_COUNT + 1))
                await UniTask.Delay(10);
        });

        private async UniTask<GameObject> StartClient(int i, Transport transport)
        {
            var clientGo = new GameObject($"Client {i}", typeof(NetworkClient), typeof(ClientObjectManager));
            var client = clientGo.GetComponent<NetworkClient>();
            var objectManager = clientGo.GetComponent<ClientObjectManager>();
            objectManager.Client = client;
            client.Transport = transport;

            objectManager.RegisterPrefab(MonsterPrefab);
            await client.ConnectAsync("localhost");
            return clientGo;
        }

        private void SpawnMonster(int i)
        {
            var monster = Object.Instantiate(MonsterPrefab);

            monster.GetComponent<MonsterBehavior>().MonsterId = i;
            monster.gameObject.name = $"Monster {i}";
            ServerObjectManager.Spawn(monster.gameObject);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // shutdown
            Server.Stop();
            yield return null;

            foreach (var client in _clients) {
                Object.Destroy(client);
            }

            // unload scene
            var scene = SceneManager.GetSceneByPath(SCENE_PATH);
            yield return SceneManager.UnloadSceneAsync(scene);

            var networkManager = GameObject.Find("NetworkManager");
            if (networkManager != null)
                Object.Destroy(networkManager);

        }

        [UnityTest]
        [Performance]
        public IEnumerator SyncMonsters()
        {
            yield return 
                Measure
                .Frames()
                .MeasurementCount(MEASURE_COUNT)
                .WarmupCount(WARMUP)
                .Run();
        }
    }
}

