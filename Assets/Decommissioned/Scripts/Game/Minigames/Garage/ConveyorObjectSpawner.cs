// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Game.MiniGames
{
    /// <summary>
    /// Class for managing object spawns; start and stop spawning Conveyer_Objects.
    /// <seealso cref="ConveyorMiniGame"/>
    /// <seealso cref="ConveyorObject"/>
    /// </summary>
    public class ConveyorObjectSpawner : NetworkBehaviour
    {
        [SerializeField] private Transform m_initialObjectDestination;
        [SerializeField] private GameObject m_objectPool;
        [SerializeField] private float m_spawnWaitTime = 1f;
        [SerializeField] private int m_minBlocksToAllowSpawn = 5;
        [SerializeField] private UnityEvent<GameObject> m_onObjectSpawned;

        private List<ConveyorObject> m_spawnableObjects = new();
        private Coroutine m_spawningCoroutine;

        private void Start()
        {
            m_spawnableObjects = m_objectPool.GetComponentsInChildren<ConveyorObject>().ToList();
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartSpawningServerRpc()
        {
            var newOwner = LocationManager.Instance.GetGamePositionByType(MiniGameRoom.Garage)[0].OccupyingPlayer;
            if (newOwner != null)
            {
                foreach (var spawnObject in m_spawnableObjects)
                {
                    spawnObject.NetworkObject.ChangeOwnership(newOwner.GetOwnerPlayerId() ?? PlayerId.ServerPlayerId());
                }
            }

            if (m_spawningCoroutine != null)
            {
                StopCoroutine(m_spawningCoroutine);
            }

            m_spawningCoroutine = StartCoroutine(SpawnObjectBatch());
        }

        [ContextMenu("Spawn Random Object")]
        public void SpawnRandomObjectFromPool()
        {
            if (m_spawnableObjects.Count < m_minBlocksToAllowSpawn)
            {
                return;
            }

            var freeObjects = m_spawnableObjects.Where(spawnableObject => !spawnableObject.m_isSpawned.Value).ToList();

            if (freeObjects.Count == 0)
            {
                Debug.LogWarning("Unable to spawn a garage object: there were no free objects available!");
                return;
            }

            var spawnedObject = freeObjects[Random.Range(0, freeObjects.Count)];

            if (spawnedObject == null)
            {
                Debug.LogError($"{gameObject.name}: No conveyor_object component on game object. Object will not spawn.");
                return;
            }

            spawnedObject.Destination = m_initialObjectDestination;
            spawnedObject.Spawn(true);
            m_onObjectSpawned.Invoke(spawnedObject.gameObject);
        }

        [ContextMenu("Stop Object Spawn")]
        [ServerRpc(RequireOwnership = false)]
        public void StopSpawningServerRpc()
        {
            if (m_spawningCoroutine != null)
            {
                StopCoroutine(m_spawningCoroutine);
            }

            m_spawningCoroutine = null;

            foreach (var conveyorObject in m_spawnableObjects)
            {
                conveyorObject.Despawn(true);
            }
        }

        private IEnumerator SpawnObjectBatch()
        {
            while (true)
            {
                SpawnRandomObjectFromPool();
                yield return new WaitForSeconds(m_spawnWaitTime);
            }
        }
    }
}
