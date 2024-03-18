using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spawner : MonoBehaviour
{
    public GameObject[] objectPrefabs; // Array of object prefabs to spawn
    public Transform[] spawnPoints; // Array of spawn points
    public float respawnTime = 3f; // Time before respawning an object

    private GameObject[] spawnedObjects; // Array to hold spawned objects
    private Vector3[] lastObjectPositions; // Array to store last known positions of spawned objects

    void Start()
    {
        spawnedObjects = new GameObject[spawnPoints.Length];
        lastObjectPositions = new Vector3[spawnPoints.Length];
        SpawnInitialObjects();
    }

    void SpawnInitialObjects()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            SpawnObject(i);
        }
    }

    void SpawnObject(int index)
    {
        int randomIndex = Random.Range(0, objectPrefabs.Length);
        GameObject newObject = Instantiate(objectPrefabs[randomIndex], spawnPoints[index].position, Quaternion.identity);
        spawnedObjects[index] = newObject;
        lastObjectPositions[index] = newObject.transform.position;
    }

    IEnumerator RespawnObject(int index)
    {
        yield return new WaitForSeconds(respawnTime);
        if (spawnedObjects[index] == null)
        {
            SpawnObject(index);
        }
    }

    void Update()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnedObjects[i] == null)
            {
                StartCoroutine(RespawnObject(i));
            }
            else
            {
                // Check if the object's position has changed since the last update
                if (spawnedObjects[i].transform.position != lastObjectPositions[i])
                {
                    spawnedObjects[i] = null; // Mark the object as removed
                }

                lastObjectPositions[i] = spawnedObjects[i].transform.position; // Update last known position
            }
        }
    }

}
