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

// Importing the necessary namespaces
using Meta.XR.MRUtilityKit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Start of the FindSpawnPositions class
public class FindSpawnPositions : MonoBehaviour
{
    // Serialized field for the prefab or object to be spawned or moved
    [SerializeField, Tooltip("Prefab to be placed into the scene, or object in the scene to be moved around.")]
    public GameObject SpawnObject;
    // Serialized field for the number of SpawnObjects to place into the scene (only applies to prefabs)
    [SerializeField, Tooltip("Number of SpawnObject(s) to place into the scene, only applies to Prefabs.")]
    public int SpawnAmount = 8;
    // Serialized field for the maximum number of attempts to spawn/move an object before giving up
    [SerializeField, Tooltip("Maximum number of times to attempt spawning/moving an object before giving up.")]
    public int MaxIterations = 1000;

    // Enum for different spawn locations
    public enum SpawnLocation
    {
        Floating,           // Spawn somewhere floating in the free space within the room
        AnySurface,         // Spawn on any surface (i.e. a combination of all 3 options below)
        VerticalSurfaces,   // Spawn only on vertical surfaces such as walls, windows, wall art, doors, etc...
        OnTopOfSurfaces,    // Spawn on surfaces facing upwards such as ground, top of tables, beds, couches, etc...
        HangingDown         // Spawn on surfaces facing downwards such as the ceiling
    }

    // Serialized field for the selected spawn location (previously serialized as "selectedSnapOption")
    [FormerlySerializedAs("selectedSnapOption")]
    [SerializeField, Tooltip("Attach content to scene surfaces.")]
    public SpawnLocation SpawnLocations = SpawnLocation.Floating;

    // Serialized field for filtering anchor labels when using surface spawning
    [SerializeField, Tooltip("When using surface spawning, use this to filter which anchor labels should be included. Eg, spawn only on TABLE or OTHER.")]
    public MRUKAnchor.SceneLabels Labels = ~(MRUKAnchor.SceneLabels)0;

    // Serialized field for enabling/disabling overlap checking for spawn positions
    [SerializeField, Tooltip("If enabled then the spawn position will check colliders to make sure there is no overlap.")]
    public bool CheckOverlaps = true;

    // Serialized field for the required free space for the object (set to negative for auto-detect)
    [SerializeField, Tooltip("Required free space for the object (Set negative to auto-detect using GetPrefabBounds)")]
    public float OverrideBounds = -1; // default to auto-detect. This value is doubled when generating bounds (assume user wants X distance away from objects)

    // Serialized field for the layer mask used in physics bounding box checks (previously serialized as "layerMask")
    [FormerlySerializedAs("layerMask")]
    [SerializeField, Tooltip("Set the layer(s) for the physics bounding box checks, collisions will be avoided with these layers.")]
    public LayerMask LayerMask = -1;

    // Create a list to store the colliders of spawned instances
    private List<Collider> spawnedColliders = new List<Collider>();


    // Start method called when the script starts
    private void Start()
    {
        // Preprocessor directive for Unity Editor
#if UNITY_EDITOR
        // Start OVR telemetry with a specific marker ID and send the data
        OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadFindSpawnPositions).Send();
#endif
    }

    // Public method to start the spawning process
        public void StartSpawn()
    {
        // Get the current room from the MRUK instance
        var room = MRUK.Instance.GetCurrentRoom();
        // Get the bounds of the prefab using the Utilities class
        var prefabBounds = Utilities.GetPrefabBounds(SpawnObject);
        // Initialize variables for minimum radius, clearance distance, base offset, and center offset
        float minRadius = 0.0f;
        const float clearanceDistance = 0.01f;
        float baseOffset = -prefabBounds?.min.y ?? 0.0f;
        float centerOffset = prefabBounds?.center.y ?? 0.0f;
        // Create a new Bounds object for adjusted bounds
        Bounds adjustedBounds = new();

        // Check if prefabBounds has a value
        if (prefabBounds.HasValue)
        {
            // Calculate the minimum radius based on the prefab bounds
            minRadius = Mathf.Min(-prefabBounds.Value.min.x, -prefabBounds.Value.min.z, prefabBounds.Value.max.x, prefabBounds.Value.max.z);
            
            // Ensure minRadius is not negative
            if (minRadius < 0f)
            {
                minRadius = 0f;
            }
            // Get the minimum and maximum values of the prefab bounds
            var min = prefabBounds.Value.min;
            var max = prefabBounds.Value.max;
            // Add clearance distance to the minimum Y value
            min.y += clearanceDistance;
            // Ensure the maximum Y value is not less than the minimum Y value
            if (max.y < min.y)
            {
                max.y = min.y;
            }
            // Set the adjusted bounds using the modified minimum and maximum values
            adjustedBounds.SetMinMax(min, max);
            // Check if OverrideBounds is greater than 0
            if (OverrideBounds > 0)
            {
                // Create a new center vector for the adjusted bounds
                Vector3 center = new Vector3(0f, clearanceDistance, 0f);
                // Create a new extents vector for the adjusted bounds
                Vector3 extents = new Vector3((OverrideBounds * 2f), clearanceDistance, (OverrideBounds * 2f)); // assuming user intends to input X distance from other colliders
                // Create new adjusted bounds using the center and extents vectors
                adjustedBounds = new Bounds(center, extents);
            }
        }

        // Loop through the specified number of spawn attempts
        for (int i = 0; i < SpawnAmount; i++)
        {
            Debug.Log("New table about to be spawned");

            // Loop through the maximum number of iterations for each spawn attempt
            for (int j = 0; j < MaxIterations; ++j)
            {
                // Initialize variables for spawn position and spawn normal
                Vector3 spawnPosition = Vector3.zero;
                Vector3 spawnNormal = Vector3.zero;
                // Check if the selected spawn location is Floating
                if (SpawnLocations == SpawnLocation.Floating)
                {
                    // Generate a random position in the room using the minimum radius
                    var randomPos = room.GenerateRandomPositionInRoom(minRadius, true);

                    // Check if a random position was not found
                    if (!randomPos.HasValue)
                    {
                        // Break out of the loop and try again with another iteration
                        break;
                    }

                    // Assign the found random position to the spawn position
                    spawnPosition = randomPos.Value;
                }
                else
                {
                    // Initialize a variable for surface type
                    MRUK.SurfaceType surfaceType = 0;
                    // Switch statement to determine the surface type based on the selected spawn location
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
                    // Generate a random position on the specified surface type using the minimum radius and label filter
                    if (room.GenerateRandomPositionOnSurface(surfaceType, minRadius, LabelFilter.FromEnum(Labels), out var pos, out var normal))
                    {
                        // Calculate the spawn position by offsetting the generated position with the base offset
                        spawnPosition = pos + normal * baseOffset;
                        // Assign the generated normal to the spawn normal
                        spawnNormal = normal;
                        // Check if the center of the prefab will be inside the room
                        if (!room.IsPositionInRoom(spawnPosition + normal * centerOffset))
                        {
                            // If the center is outside the room, continue to the next iteration
                            continue;
                        }
                    }
                }

                // Calculate the spawn rotation based on the spawn normal
                Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, spawnNormal);
                // Check if overlap checking is enabled and prefab bounds have a value
                if (CheckOverlaps && prefabBounds.HasValue)
                {
                    // Check if there is any collision at the spawn position using the adjusted bounds and layer mask
                    if (Physics.CheckBox(spawnPosition + spawnRotation * adjustedBounds.center, adjustedBounds.extents, spawnRotation, LayerMask, QueryTriggerInteraction.Ignore))
                    {
                        // If there is a collision, continue to the next iteration
                        continue;
                    }
                }


                // NEW IF CHECK HAS TO GO HERE, IF THE SPAWN POSITION IF SOME WHERE NEAR THE ANY OF THE COLLIDERS IN THE spawnedColliders list then break the loop and go to next iteration
                // NEW IF CHECK: Check if the spawn position is too close to any existing colliders
                bool isTooClose = false;
                foreach (Collider existingCollider in spawnedColliders)
                {
                    // Calculate the minimum distance required between the new spawn position and the existing collider
                    float minDistance = existingCollider.bounds.extents.magnitude + adjustedBounds.extents.magnitude;

                    // Check if the distance between the spawn position and the existing collider is less than the minimum distance
                    if (Vector3.Distance(spawnPosition, existingCollider.bounds.center) < minDistance)
                    {
                        // If the distance is too small, set the isTooClose flag to true and break the loop
                        isTooClose = true;
                        break;
                    }
                }

                // If the spawn position is too close to any existing collider, continue to the next iteration
                if (isTooClose)
                {
                    continue;
                }

                // Check if the SpawnObject is not already in the scene (i.e., it's a prefab)
                if (SpawnObject.gameObject.scene.path == null)
                {
                    // Instantiate the SpawnObject prefab at the spawn position and rotation
                    GameObject spawnedObject = Instantiate(SpawnObject);
                    // Set the spawned object's parent to the current transform
                    spawnedObject.transform.parent = transform;
                    // Set the spawned object's position to the spawn position
                    spawnedObject.transform.position = spawnPosition;
                    // Set the spawned object's rotation to the spawn rotation
                    spawnedObject.transform.rotation = spawnRotation;

                    // Add the collider of the spawned instance to the list
                    spawnedColliders.Add(spawnedObject.GetComponent<Collider>());

                }
                else
                {
                    // If the SpawnObject is already in the scene, set its position and rotation to the spawn position and rotation
                    SpawnObject.transform.position = spawnPosition;
                    SpawnObject.transform.rotation = spawnRotation;
                    // Return from the method, ignoring the SpawnAmount for an existing object in the scene
                    return;
                }
                // Break out of the iteration loop since a valid spawn position was found
                break;
            }
        }
    }
}