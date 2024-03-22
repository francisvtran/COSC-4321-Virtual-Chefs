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

using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using Random = System.Random;
using UnityEngine.Serialization;

namespace Meta.XR.MRUtilityKit
{
    // tool for swapping scene prefabs with standardized unity objects
    public class AnchorPrefabSpawner : MonoBehaviour
    {
        public enum ScalingMode
        {
            /// Stretch each axis to exactly match the size of the Plane/Volume.
            Stretch,
            /// Scale each axis by the same amount to maintain the correct aspect ratio.
            UniformScaling,
            /// Scale the X and Z axes uniformly but the Y scale can be different.
            UniformXZScale,
            /// Don't perform any scaling.
            NoScaling
        }

        public enum AlignMode
        {
            /// For volumes align to the base, for planes align to the center.
            Automatic,
            /// Align the bottom of the prefab with the bottom of the volume or plane
            Bottom,
            /// Align the center of the prefab with the center of the volume or plane
            Center,
            /// Don't add any local offset to the prefab.
            NoAlignment
        }

        [System.Serializable]
        public struct AnchorPrefabGroup
        {
            [FormerlySerializedAs("_include")]
            [SerializeField, Tooltip("Anchors to include.")]
            public MRUKAnchor.SceneLabels Labels;
            [SerializeField, Tooltip("Prefab(s) to spawn (randomly chosen from list.)")]
            public List<GameObject> Prefabs;
            [SerializeField, Tooltip("When enabled, the prefab will be rotated to try and match the aspect ratio of the volume as closely as possible. This is most useful for long and thin volumes, keep this disabled for objects with an aspect ratio close to 1:1. Only applies to volumes.")]
            public bool MatchAspectRatio;
            [SerializeField, Tooltip("When calculate facing direction is enabled the prefab will be rotated to face away from the closest wall. If match aspect ratio is also enabled then that will take precedence and it will be constrained to a choice between 2 directions only.Only applies to volumes.")]
            public bool CalculateFacingDirection;
            [SerializeField, Tooltip("Set what scaling mode to apply to the prefab. By default the prefab will be stretched to fit the size of the plane/volume. But in some cases this may not be desirable and can be customized here.")]
            public ScalingMode Scaling;
            [SerializeField, Tooltip("Spawn new object at the center, top or bottom of the anchor.")]
            public AlignMode Alignment;
            [SerializeField, Tooltip("Don't analyze prefab, just assume a default scale of 1.")]
            public bool IgnorePrefabSize;
        }

        public List<AnchorPrefabGroup> PrefabsToSpawn;
        List<GameObject> _spawnedPrefabs = new();

        public List<GameObject> SpawnedPrefabs
        {
            get { return _spawnedPrefabs; }
            private set { _spawnedPrefabs = value; }
        }

        public UnityEvent onPrefabSpawned;
        private void Start()
        {
#if UNITY_EDITOR
            OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadAnchorPrefabSpawner).Send();
#endif
        }

        void ClearPrefabs()
        {
            foreach (GameObject prefab in _spawnedPrefabs)
                Destroy(prefab);

            _spawnedPrefabs.Clear();
            Debug.Log("Cleared anchor prefab spawned objects");
        }

        private Bounds RotateVolumeBounds(Bounds bounds, int rotation)
        {
            var center = bounds.center;
            var size = bounds.size;
            switch (rotation)
            {
                default:
                    return bounds;
                case 1:
                    return new Bounds(new Vector3(-center.y, center.x, center.z), new Vector3(size.y, size.x, size.z));
                case 2:
                    return new Bounds(new Vector3(-center.x, -center.x, center.z), size);
                case 3:
                    return new Bounds(new Vector3(center.y, -center.x, center.z), new Vector3(size.y, size.x, size.z));
            }
        }

        public void SpawnPrefabs()
        {
            // Perform a cleanup if necessary
            ClearPrefabs();

            foreach (var room in MRUK.Instance.GetRooms())
            {
                foreach (var anchor in room.GetRoomAnchors())
                {
                    var prefabToCreate = LabelToPrefab(anchor.GetLabelsAsEnum(), out AnchorPrefabGroup prefabGroup);

                    if (prefabToCreate == null)
                        continue;

                    Bounds? prefabBounds = prefabGroup.IgnorePrefabSize ? null : Utilities.GetPrefabBounds(prefabToCreate);
                    Vector3 prefabSize = prefabBounds?.size ?? Vector3.one;

                    // Create a new instance of the prefab
                    // We will translate location and scale differently depending on the label.
                    var prefab = Instantiate(prefabToCreate);
                    prefab.name = prefabToCreate.name + "(PrefabSpawner Clone)";
                    prefab.transform.parent = anchor.transform;

                    if (anchor.HasVolume)
                    {
                        int cardinalAxisIndex = 0;
                        if (prefabGroup.CalculateFacingDirection && !prefabGroup.MatchAspectRatio)
                        {
                            room.GetDirectionAwayFromClosestWall(anchor, out cardinalAxisIndex);
                        }
                        Bounds volumeBounds = RotateVolumeBounds(anchor.VolumeBounds.Value, cardinalAxisIndex);

                        Vector3 volumeSize = volumeBounds.size;
                        Vector3 scale = new Vector3(volumeSize.x / prefabSize.x, volumeSize.z / prefabSize.y, volumeSize.y / prefabSize.z);  // flipped z and y to correct orientation

                        if (prefabGroup.MatchAspectRatio)
                        {
                            Vector3 prefabSizeRotated = new Vector3(prefabSize.z, prefabSize.y, prefabSize.x);
                            Vector3 scaleRotated = new Vector3(volumeSize.x / prefabSizeRotated.x, volumeSize.z / prefabSizeRotated.y, volumeSize.y / prefabSizeRotated.z);

                            float distortion = Mathf.Max(scale.x, scale.z) / Mathf.Min(scale.x, scale.z);
                            float distortionRotated = Mathf.Max(scaleRotated.x, scaleRotated.z) / Mathf.Min(scaleRotated.x, scaleRotated.z);

                            bool rotateToMatchAspectRatio = distortion > distortionRotated;
                            if (rotateToMatchAspectRatio)
                            {
                                cardinalAxisIndex = 1;
                            }
                            if (prefabGroup.CalculateFacingDirection)
                            {
                                room.GetDirectionAwayFromClosestWall(anchor, out cardinalAxisIndex, rotateToMatchAspectRatio ? new List<int> { 0, 2 } : new List<int> { 1, 3 });
                            }
                            if (cardinalAxisIndex != 0)
                            {
                                // Update the volume bounds if necessary
                                volumeBounds = RotateVolumeBounds(anchor.VolumeBounds.Value, cardinalAxisIndex);
                                volumeSize = volumeBounds.size;
                                scale = new Vector3(volumeSize.x / prefabSize.x, volumeSize.z / prefabSize.y, volumeSize.y / prefabSize.z);  // flipped z and y to correct orientation
                            }
                        }

                        switch (prefabGroup.Scaling)
                        {
                            case ScalingMode.UniformScaling:
                                scale.x = scale.y = scale.z = Mathf.Min(scale.x, scale.y, scale.z);
                                break;
                            case ScalingMode.UniformXZScale:
                                scale.x = scale.z = Mathf.Min(scale.x, scale.z);
                                break;
                            case ScalingMode.NoScaling:
                                scale = Vector3.one;
                                break;
                        }

                        Vector3 prefabPivot = new();
                        Vector3 volumePivot = new();

                        switch (prefabGroup.Alignment)
                        {
                            case AlignMode.Automatic:
                            case AlignMode.Bottom:
                                if (prefabBounds.HasValue)
                                {
                                    var center = prefabBounds.Value.center;
                                    var min = prefabBounds.Value.min;
                                    prefabPivot = new Vector3(center.x, center.z, min.y);
                                }
                                volumePivot = volumeBounds.center;
                                volumePivot.z = volumeBounds.min.z;
                                break;
                            case AlignMode.Center:
                                if (prefabBounds.HasValue)
                                {
                                    var center = prefabBounds.Value.center;
                                    prefabPivot = new Vector3(center.x, center.z, center.y);
                                }
                                volumePivot = volumeBounds.center;
                                break;
                            case AlignMode.NoAlignment:
                                break;
                        }
                        prefabPivot.x *= scale.x;
                        prefabPivot.y *= scale.z;
                        prefabPivot.z *= scale.y;
                        prefab.transform.localPosition = volumePivot - prefabPivot;
                        prefab.transform.localRotation = Quaternion.Euler((cardinalAxisIndex - 1) * 90, -90, -90);// scene geometry is unusual, we need to swap Y/Z for a more standard prefab structure
                        prefab.transform.localScale = scale;
                    }
                    else if (anchor.HasPlane)
                    {
                        Vector2 planeSize = anchor.PlaneRect.Value.size;
                        Vector2 scale = new Vector2(planeSize.x / prefabSize.x, planeSize.y / prefabSize.y);

                        switch (prefabGroup.Scaling)
                        {
                            case ScalingMode.UniformScaling:
                            case ScalingMode.UniformXZScale:
                                scale.x = scale.y = Mathf.Min(scale.x, scale.y);
                                break;
                            case ScalingMode.NoScaling:
                                scale = Vector2.one;
                                break;
                        }

                        Vector2 planePivot = new();
                        Vector2 prefabPivot = new();
                        switch (prefabGroup.Alignment)
                        {
                            case AlignMode.Automatic:
                            case AlignMode.Center:
                                prefabPivot = prefabBounds?.center ?? Vector3.zero;
                                planePivot = anchor.PlaneRect.Value.center;
                                break;
                            case AlignMode.Bottom:
                                if (prefabBounds.HasValue)
                                {
                                    var center = prefabBounds.Value.center;
                                    var min = prefabBounds.Value.min;
                                    prefabPivot = new Vector3(center.x, min.y);
                                }
                                planePivot = anchor.PlaneRect.Value.center;
                                planePivot.y = anchor.PlaneRect.Value.min.y;
                                break;
                            case AlignMode.NoAlignment:
                                break;
                        }
                        prefabPivot.Scale(scale);
                        prefab.transform.localPosition = new Vector3(planePivot.x - prefabPivot.x, planePivot.y - prefabPivot.y, 0);
                        prefab.transform.localRotation = Quaternion.identity;
                        prefab.transform.localScale = new Vector3(scale.x, scale.y, 0.5f * (scale.x + scale.y));
                    }

                    _spawnedPrefabs.Add(prefab);
                }
            }
            onPrefabSpawned?.Invoke();
        }

        GameObject LabelToPrefab(MRUKAnchor.SceneLabels labels, out AnchorPrefabGroup prefabGroup)
        {
            foreach (AnchorPrefabGroup item in PrefabsToSpawn)
            {
                if ((item.Labels & labels) != 0)
                {
                    int randomIndex; // randomly chooses one from the list, even if list contains a single object
                    Random random = new Random();
                    randomIndex = random.Next(0, item.Prefabs.Count);
                    GameObject randomSelection = item.Prefabs[randomIndex];

                    prefabGroup = item;

                    return randomSelection;
                }
            }
            prefabGroup = new();
            return null;
        }
    }
}
