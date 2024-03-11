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
    public class AnchorMeshTests : MonoBehaviour
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
        /// Test that the mesh has the expected number of nodes and triangles associated with it.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator VerifyAnchorMesh()
        {
            Assert.AreEqual(8, _currentRoom._anchorMesh._nodes.Count);
            Assert.AreEqual(7, _currentRoom._anchorMesh._triangles.Count);
            yield return null;
        }

        /// <summary>
        /// Find the closest triangle for the origin and verify it has the expected barycentric coordinates.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator FindClosestTriangle()
        {
            var triangle = _currentRoom._anchorMesh.FindClosestTriangle(Vector2.zero, out Vector3 barycentric);
            Assert.AreEqual(1, barycentric.x + barycentric.y + barycentric.z, 0.001);
            Assert.AreEqual(5, triangle.P1);
            Assert.AreEqual(0, triangle.P2);
            Assert.AreEqual(7, triangle.P3);
            Assert.That(barycentric, Is.EqualTo(new Vector3(0.545034f, 0.1745305f, 0.2804355f)).Using(Vector3EqualityComparer.Instance));
            yield return null;
        }
    }
}
