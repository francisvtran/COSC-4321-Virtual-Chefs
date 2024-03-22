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

using System.Collections.Generic;
using UnityEngine;

namespace Meta.XR.MRUtilityKit
{
    public class RoomGuardian : MonoBehaviour
    {
        Material guardianMaterial;
        [Tooltip("How far the camera should be from a Scene API object before the grid appears.")]
        public float guardianDistance = 1.0f;

        private void Start()
        {
            // required for passthrough blending to work properly
            OVRPlugin.eyeFovPremultipliedAlphaModeEnabled = false;
#if UNITY_EDITOR
            OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadRoomGuardian).Send();
#endif
        }

        void Update()
        {
            if (guardianMaterial != null)
            {
                // get the closest distance of all the surfaces
                // should avoid raycasting if possible
                var room = MRUK.Instance?.GetCurrentRoom();
                if (!room)
                {
                    return;
                }
                bool insideRoom = room.IsPositionInRoom(Camera.main.transform.position);

                // instead of using the head position, we actually want a position near the feet
                // (to catch short volumes like a bed, which are a tripping hazard)
                Vector3 testPosition = new Vector3(Camera.main.transform.position.x, 0.2f, Camera.main.transform.position.z);

                float closestDistance = room.TryGetClosestSurfacePosition(testPosition, out Vector3 closestPoint, out _, LabelFilter.Excluded(new List<string> { OVRSceneManager.Classification.Floor, OVRSceneManager.Classification.Ceiling }));

                bool outsideVolume = !room.IsPositionInSceneVolume(testPosition);

                float guardianFade = insideRoom && outsideVolume ? Mathf.Clamp01(1 - (closestDistance / guardianDistance)) : 1.0f;
                guardianMaterial.SetFloat("_GuardianFade", guardianFade);

                Color lineColor = insideRoom ? Color.green : Color.red;
                Debug.DrawLine(testPosition, closestPoint, lineColor);
            }
        }

        public void GetEffectMeshMaterial()
        {
            MeshRenderer msh = GetComponentInChildren<MeshRenderer>();
            if (msh)
            {
                guardianMaterial = msh.sharedMaterial;
            }
        }
    }
}
