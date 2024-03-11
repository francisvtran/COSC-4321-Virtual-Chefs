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

using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Allows for fast generation of valid (inside the room, outside furniture bounds) random positions for content spawning.
// Optional method to pin directly to surfaces
public class FindSpawnPositions : MonoBehaviour
{
    [SerializeField, Tooltip("Prefab to be placed into the scene, or object in the scene to be moved around.")]
    public GameObject SpawnObject;
    [SerializeField, Tooltip("Number of Max SpawnObject(s) to place into the scene, only applies to Prefabs.")]
    public int MaxSpawnAmount = 8;
    [SerializeField, Tooltip("Maximum number of times to attempt spawning/moving an object before giving up.")]
    public int MaxIterations = 1000;

    public enum SpawnLocation
    {
        Floating,           // Spawn somewhere floating in the free space within the room
        AnySurface,         // Spawn on any surface (i.e. a combination of all 3 options below)
        VerticalSurfaces,   // Spawn only on vertical surfaces such as walls, windows, wall art, doors, etc...
        OnTopOfSurfaces,    // Spawn on surfaces facing upwards such as ground, top of tables, beds, couches, etc...
        HangingDown         // Spawn on surfaces facing downwards such as the ceiling
    }

    [FormerlySerializedAs("selectedSnapOption")]
    [SerializeField, Tooltip("Attach content to scene surfaces.")]
    public SpawnLocation SpawnLocations = SpawnLocation.Floating;

    [SerializeField, Tooltip("When using surface spawning, use this to filter which anchor labels should be included. Eg, spawn only on TABLE or OTHER.")]
    public MRUKAnchor.SceneLabels Labels = ~(MRUKAnchor.SceneLabels)0;

    [SerializeField, Tooltip("If enabled then the spawn position will check colliders to make sure there is no overlap.")]
    public bool CheckOverlaps = true;

    [SerializeField, Tooltip("Required free space for the object (Set negative to auto-detect using GetPrefabBounds)")]
    public float OverrideBounds = -1; // default to auto-detect. This value is doubled when generating bounds (assume user wants X distance away from objects)

    [FormerlySerializedAs("layerMask")]
    [SerializeField, Tooltip("Set the layer(s) for the physics bounding box checks, collisions will be avoided with these layers.")]
    public LayerMask LayerMask = -1;

    private void Start()
    {
#if UNITY_EDITOR
        OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadFindSpawnPositions).Send();
#endif
    }

    public void StartSpawn()
    {
        var room = MRUK.Instance.GetCurrentRoom();
        var prefabBounds = Utilities.GetPrefabBounds(SpawnObject);
        float minRadius = 0.0f;
        const float clearanceDistance = 0.01f;
        float baseOffset = -prefabBounds?.min.y ?? 0.0f;
        float centerOffset = prefabBounds?.center.y ?? 0.0f;
        Bounds adjustedBounds = new();

        if (prefabBounds.HasValue)
        {
            minRadius = Mathf.Min(-prefabBounds.Value.min.x, -prefabBounds.Value.min.z, prefabBounds.Value.max.x, prefabBounds.Value.max.z);
            if (minRadius < 0f)
            {
                minRadius = 0f;
            }
            var min = prefabBounds.Value.min;
            var max = prefabBounds.Value.max;
            min.y += clearanceDistance;
            if (max.y < min.y)
            {
                max.y = min.y;
            }
            adjustedBounds.SetMinMax(min, max);
            if (OverrideBounds > 0)
            {
                Vector3 center = new Vector3(0f, clearanceDistance, 0f);
                Vector3 extents = new Vector3((OverrideBounds * 2f), clearanceDistance, (OverrideBounds * 2f)); // assuming user intends to input X distance from other colliders
                adjustedBounds = new Bounds(center, extents);
            }
        }

        // Keep track of how many objects we have spawned
        int spawnedObjects = 0;

        for (int i = 0; i < MaxSpawnAmount; i++)
        {
            // Flag to check if a position to spawn the table was found
            bool positionFound = false;

            for (int j = 0; j < MaxIterations; ++j)
            {
                Vector3 spawnPosition = Vector3.zero;
                Vector3 spawnNormal = Vector3.zero;
                if (SpawnLocations == SpawnLocation.Floating)
                {
                    // Find a random position in the room to spawn object
                    var randomPos = room.GenerateRandomPositionInRoom(minRadius, true);

                    // If you could't find a random position break out of the loop a try again with another iteration
                    if (!randomPos.HasValue)
                    {
                        break;
                    }

                    // Else if you found a position assign it to spawnPosition variable
                    spawnPosition = randomPos.Value;
                }
                else
                {
                    // Else switch to another SpawnLocation if we didn't find location on floor
                    MRUK.SurfaceType surfaceType = 0;
                    switch (SpawnLocations)
                    {
                        case SpawnLocation.AnySurface:
                            surfaceType |= MRUK.SurfaceType.FACING_UP;
                            surfaceType |= MRUK.SurfaceType.VERTICAL;
                            surfaceType |= MRUK.SurfaceType.FACING_DOWN;
                            break;
                        case SpawnLocation.VerticalSurfaces:
                            surfaceType |= MRUK.SurfaceType.VERTICAL;
                            break;
                        case SpawnLocation.OnTopOfSurfaces:
                            surfaceType |= MRUK.SurfaceType.FACING_UP;
                            break;
                        case SpawnLocation.HangingDown:
                            surfaceType |= MRUK.SurfaceType.FACING_DOWN;
                            break;
                    }
                    if (room.GenerateRandomPositionOnSurface(surfaceType, minRadius, LabelFilter.FromEnum(Labels), out var pos, out var normal))
                    {
                        spawnPosition = pos + normal * baseOffset;
                        spawnNormal = normal;
                        // In some cases, surfaces may protrude through walls and end up outside the room
                        // check to make sure the center of the prefab will spawn inside the room
                        if (!room.IsPositionInRoom(spawnPosition + normal * centerOffset))
                        {
                            continue;
                        }
                    }
                }

                Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, spawnNormal);
                if (CheckOverlaps && prefabBounds.HasValue)
                {
                    if (Physics.CheckBox(spawnPosition + spawnRotation * adjustedBounds.center, adjustedBounds.extents, spawnRotation, LayerMask, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                }

                if (SpawnObject.gameObject.scene.path == null)
                {
                    GameObject spawnedObject = Instantiate(SpawnObject);
                    spawnedObject.transform.parent = transform;
                    spawnedObject.transform.position = spawnPosition;
                    spawnedObject.transform.rotation = spawnRotation;
                }
                else
                {
                    SpawnObject.transform.position = spawnPosition;
                    SpawnObject.transform.rotation = spawnRotation;
                    return; // ignore SpawnAmount once we have a successful move of existing object in the scene
                }
                break;
            }
        }
    }
}
