using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mirage.Tests.Performance.Runtime
{
    [Category("Performance")]
    [Category("Benchmark")]
    public class BenchmarkPerformanceLight
    {
        const string ScenePath = "Assets/Tests/Performance/Runtime/10KL/Scenes/Scene.unity";

        private NetworkManager benchmarker;

        [UnitySetUp]
        public IEnumerator SetUp() => UniTask.ToCoroutine(async () =>
        {
            // load scene
            await EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive });
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            SceneManager.SetActiveScene(scene);

            // load host
            benchmarker = Object.FindFirstObjectByType<NetworkManager>();

            benchmarker.Server.Listen(benchmarker.Client);

        });

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // shutdown
            benchmarker.Server.Stop();
            yield return null;

            // unload scene
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        [Performance]
        public IEnumerator Benchmark10KLight()
        {
            yield return Measure.Frames().MeasurementCount(240).WarmupCount(50).Run();
        }

    }
}

