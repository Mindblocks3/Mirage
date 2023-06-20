using NUnit.Framework;
using UnityEngine;

namespace Mirage
{
    [TestFixture]
    public class NetworkTransformChildTest
    {
        [Test]
        public void TargetComponentTest()
        {
            NetworkTransformChild networkTransformChild;

            var gameObject = new GameObject(this.GetType().Name);
            networkTransformChild = gameObject.AddComponent<NetworkTransformChild>();

            Assert.That(networkTransformChild.Target == null);

            networkTransformChild.Target = gameObject.transform;

            Assert.That(networkTransformChild.Target == networkTransformChild.transform);

            GameObject.DestroyImmediate(gameObject);
        }
    }
}
