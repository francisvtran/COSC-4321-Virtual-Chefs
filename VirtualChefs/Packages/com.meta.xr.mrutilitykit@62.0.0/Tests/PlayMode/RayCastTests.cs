/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools.Utils;

namespace Meta.XR.MRUtilityKit.Tests
{
    public class RayCastTests : MonoBehaviour
    {
        private const int DefaultTimeoutMs = 10000;
        private MRUKRoom _currentRoom;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Packages\\com.meta.xr.mrutilitykit\\Tests\\RayCastTests.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
            yield return new WaitUntil(() => MRUK.Instance.IsInitialized);
            _currentRoom = MRUK.Instance.GetCurrentRoom();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = SceneManager.sceneCount - 1; i >= 1; i--)
            {
                var asyncOperation = SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i).name); // Clear/reset scene
                yield return new WaitUntil(() => asyncOperation.isDone);
            }
        }

        /// <summary>
        /// Test that the ray cast hits a scene anchor plane and that the hit point and normal are as expected.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator Raycast_Plane_DoesHit()
        {
            Ray mockRay = new Ray(new Vector3(0f, 1.70f, 1f), Vector3.forward);
            bool didHit = _currentRoom.Raycast(mockRay, Mathf.Infinity, out RaycastHit hit, out MRUKAnchor anchorInfo);
            Assert.IsTrue(didHit);
            yield return null;
            Assert.IsTrue(hit.point != Vector3.zero);
            Assert.IsNotNull(anchorInfo);
            Assert.Contains(OVRSceneManager.Classification.WallFace, anchorInfo.AnchorLabels);
            Vector3 expectedHitPoint = new Vector3(0.0000f, 1.7000f, 4.2992f);
            Assert.That(hit.point, Is.EqualTo(expectedHitPoint).Using(Vector3EqualityComparer.Instance));
            Vector3 expectedHitNormal = new Vector3(-0.0864f, 0.0000f, -0.9963f);
            Assert.That(hit.normal, Is.EqualTo(expectedHitNormal).Using(Vector3EqualityComparer.Instance));
        }

        /// <summary>
        /// Test that the ray cast respects the max distance parameter
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator Raycast_Plane_DoesNotHitMaxDist()
        {
            Ray mockRay = new Ray(new Vector3(0f, 1.70f, 1f), Vector3.forward);
            bool didHit = _currentRoom.Raycast(mockRay, 1f, out RaycastHit _, out MRUKAnchor anchorInfo);
            yield return null;
            Assert.IsFalse(didHit);
            Assert.IsNull(anchorInfo);
        }

        /// <summary>
        /// Test that the ray cast respects the label filter parameter
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator Raycast_Plane_DoesNotHitFilter()
        {
            Ray mockRay = new Ray(new Vector3(0f, 1.70f, 1f), Vector3.forward);
            bool didHit = _currentRoom.Raycast(mockRay, Mathf.Infinity, LabelFilter.Excluded(new System.Collections.Generic.List<string> { OVRSceneManager.Classification.WallFace }), out RaycastHit _, out MRUKAnchor anchorInfo);
            yield return null;
            Assert.IsFalse(didHit);
            Assert.IsNull(anchorInfo);
        }

        /// <summary>
        /// Test that the ray cast hits a scene anchor plane and that the hit point and normal are as expected.
        /// The ray cast direction is rotated on all three axis.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator Raycast_PlaneAtAnAngle_DoesHit()
        {
            Ray mockRay = new Ray(new Vector3(0f, 1f, 0f), new Vector3(-0.25f, 0.1f, -0.1f));
            bool didHit = _currentRoom.Raycast(mockRay, Mathf.Infinity, out RaycastHit hit, out MRUKAnchor anchorInfo);
            yield return null;
            Assert.IsTrue(didHit);
            Assert.IsTrue(hit.point != Vector3.zero);
            Assert.IsNotNull(anchorInfo);
            Assert.Contains(OVRSceneManager.Classification.WindowFrame, anchorInfo.AnchorLabels);
            Vector3 expectedHitPoint = new Vector3(-1.7084f, 1.6833f, -0.6833f);
            Assert.That(hit.point, Is.EqualTo(expectedHitPoint).Using(Vector3EqualityComparer.Instance));
            Vector3 expectedHitNormal = new Vector3(0.9917f, 0.0000f, -0.1288f);
            Assert.That(hit.normal, Is.EqualTo(expectedHitNormal).Using(Vector3EqualityComparer.Instance));
        }

        /// <summary>
        /// Test that the ray cast hits a scene anchor volume and that the hit point and normal are as expected
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator Raycast_Volume_DoesHit()
        {
            Ray mockRay = new Ray(new Vector3(0f, 0.5f, 2f), Vector3.left);
            bool didHit = _currentRoom.Raycast(mockRay, Mathf.Infinity, out RaycastHit hit, out MRUKAnchor anchorInfo);
            yield return null;
            Assert.IsTrue(didHit);
            Assert.IsTrue(hit.point != Vector3.zero);
            Assert.IsNotNull(anchorInfo);
            Assert.Contains(OVRSceneManager.Classification.Table, anchorInfo.AnchorLabels);
            Vector3 expectedHitPoint = new Vector3(-3.3530f, 0.500f, 2.0000f);
            Assert.That(hit.point, Is.EqualTo(expectedHitPoint).Using(Vector3EqualityComparer.Instance));
            Vector3 expectedHitNormal = new Vector3(0.9976f, 0.0000f, -0.0699f);
            Assert.That(hit.normal, Is.EqualTo(expectedHitNormal).Using(Vector3EqualityComparer.Instance));
        }

        /// <summary>
        /// Test that the ray cast respects the max distance parameter
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator Raycast_Volume_DoesNotHitMaxDist()
        {
            Ray mockRay = new Ray(new Vector3(0f, 0.5f, 2f), Vector3.left);
            bool didHit = _currentRoom.Raycast(mockRay, 1f, out RaycastHit _, out MRUKAnchor anchorInfo);
            yield return null;
            Assert.IsFalse(didHit);
            Assert.IsNull(anchorInfo);
        }

        /// <summary>
        /// Test that the ray cast respects the max distance parameter
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator Raycast_Volume_DoesNotHitLabelFilter()
        {
            Ray mockRay = new Ray(new Vector3(0f, 0.5f, 2f), Vector3.left);
            bool didHit = _currentRoom.Raycast(mockRay, Mathf.Infinity, LabelFilter.Included(new System.Collections.Generic.List<string> { OVRSceneManager.Classification.Other }), out RaycastHit _, out MRUKAnchor anchorInfo);
            yield return null;
            Assert.IsFalse(didHit);
            Assert.IsNull(anchorInfo);
        }

        /// <summary>
        /// Test that the ray cast hits a scene anchor polygin and that the hit point and normal are as expected
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator Raycast_Polygon_DoesHit()
        {
            Ray mockRay = new Ray(new Vector3(0f, 1f, 1f), Vector3.down);
            _currentRoom.Raycast(mockRay, Mathf.Infinity, out RaycastHit hit, out MRUKAnchor anchorInfo);
            yield return null;
            Assert.IsTrue(hit.point != Vector3.zero);
            Assert.IsNotNull(anchorInfo);
            Assert.Contains(OVRSceneManager.Classification.Floor, anchorInfo.AnchorLabels);
            Vector3 expectedHitPoint = new Vector3(0.0000f, 0.0000f, 1.0000f);
            Assert.That(hit.point, Is.EqualTo(expectedHitPoint).Using(Vector3EqualityComparer.Instance));
            Vector3 expectedHitNormal = new Vector3(0.0000f, 1.0000f, 0.0000f);
            Assert.That(hit.normal, Is.EqualTo(expectedHitNormal).Using(Vector3EqualityComparer.Instance));
        }

        /// <summary>
        /// Test that the ray cast hits a scene anchor,
        /// the ray hits the floor.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator Raycast_OutOfRoom_HitFloor()
        {
            Ray mockRay = new Ray(new Vector3(0f, 5f, 0f), Vector3.down);
            _currentRoom.Raycast(mockRay, Mathf.Infinity, out RaycastHit hit, out MRUKAnchor anchorInfo);
            yield return null;
            Assert.IsTrue(hit.point == Vector3.zero);
            Assert.Contains(OVRSceneManager.Classification.Floor, anchorInfo.AnchorLabels);
            Assert.IsNotNull(anchorInfo);
        }


        /// <summary>
        /// Test that the ray cast does not hit any scene anchors.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator Raycast_OutOfRoom_DoesNotHit()
        {
            Ray mockRay = new Ray(new Vector3(0f, 5f, 0f), Vector3.up);
            _currentRoom.Raycast(mockRay, Mathf.Infinity, out RaycastHit hit, out MRUKAnchor anchorInfo);
            yield return null;
            // Test that the ray cast does not hit any sceen anchors
            Assert.IsTrue(hit.point == Vector3.zero);
            Assert.IsNull(anchorInfo);
        }

        /// <summary>
        /// Test that the ray cast does not hit any scene anchors as they are outside the max. distance.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator Raycast_BeyondMaxDistance_DoesNotHit()
        {
            Ray mockRay = new Ray(new Vector3(0f, 1f, 1f), Vector3.down);
            _currentRoom.Raycast(mockRay, 0.01f, out RaycastHit hit, out MRUKAnchor anchorInfo);
            yield return null;
            // Test that the ray cast does not hit any sceen anchors
            Assert.IsTrue(hit.point == Vector3.zero);
            Assert.IsNull(anchorInfo);
        }
    }
}
