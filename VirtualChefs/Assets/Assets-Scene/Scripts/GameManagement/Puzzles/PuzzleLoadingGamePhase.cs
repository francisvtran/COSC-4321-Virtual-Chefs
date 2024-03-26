// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Linq;
using CrypticCabinet.Photon;
using Fusion;
using UnityEngine;

namespace CrypticCabinet.GameManagement.Puzzles
{
    [CreateAssetMenu(fileName = "New CrypticCabinet Game Phase", menuName = "CrypticCabinet/Sand Puzzle GamePhase")]
    public class SandPuzzleGamePhase : GamePhase
    {
        [SerializeField] private GameObject[] m_prefabSandPuzzlePrefabs;

        protected override void InitializeInternal()
        {
            if (m_prefabSandPuzzlePrefabs == null || m_prefabSandPuzzlePrefabs.Length <= 0)
            {
                Debug.LogError("No Prefabs specified!");
                return;
            }

            if (PhotonConnector.Instance != null && PhotonConnector.Instance.Runner != null)
            {
                _ = GameManager.Instance.StartCoroutine(HandleSpawn());
            }
            else
            {
                Debug.LogWarning("Couldn't instantiate sand puzzle prefab!");
            }
        }

        private IEnumerator HandleSpawn()
        {
            // Grabs each prefab from the prefabSandPuzzlePrefabs list and waits until the Spawn method returns true,
            // once it does it continues iterating over the list until all prefabs are spawned
            return m_prefabSandPuzzlePrefabs.Select(puzzlePrefab => new WaitUntil(() => Spawn(puzzlePrefab))).GetEnumerator();

        }

        private bool Spawn(GameObject instance)
        {
            var spawned = false;
            _ = PhotonConnector.Instance.Runner.Spawn(instance, onBeforeSpawned: delegate (NetworkRunner runner,
                NetworkObject o)
            {
                spawned = true;
            });
            return spawned;
        }

    }
}