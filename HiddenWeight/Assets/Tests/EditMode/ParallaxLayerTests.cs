using HiddenWeight.World;
using NUnit.Framework;
using UnityEngine;

namespace HiddenWeight.Tests
{
    public class ParallaxLayerTests
    {
        GameObject _gameObject;

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
                Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void ApplyCameraPositionUsesConfiguredMultiplier()
        {
            _gameObject = new GameObject("ParallaxLayerTest");
            var layer = _gameObject.AddComponent<ParallaxLayer>();
            layer.SetMultiplierForTest(0.5f);
            layer.SetAnchor(Vector3.zero);

            layer.ApplyCameraPosition(new Vector3(4f, 2f, 0f));

            Assert.That(_gameObject.transform.position,
                Is.EqualTo(new Vector3(2f, 1f, 0f)));
        }

        [Test]
        public void FollowBoundCameraAppliesItsCurrentPosition()
        {
            _gameObject = new GameObject("ParallaxLayerTest");
            var cameraObject = new GameObject("Camera");
            try
            {
                var layer = _gameObject.AddComponent<ParallaxLayer>();
                layer.SetMultiplierForTest(0.25f);
                layer.BindCameraForTest(cameraObject.transform);
                layer.SetAnchor(Vector3.zero);
                cameraObject.transform.position = new Vector3(8f, 4f, -10f);

                layer.FollowBoundCameraForTest();

                Assert.That(_gameObject.transform.position,
                    Is.EqualTo(new Vector3(2f, 1f, 0f)));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
