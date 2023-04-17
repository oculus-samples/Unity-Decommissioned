// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Multiplayer.Avatar;
using Meta.Multiplayer.Core;
using Meta.Multiplayer.Networking;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned
{
    public partial class Application
    {
        private List<ulong> m_playerJoinRequests = new();
        private Routine m_currentlyLoading = null;
        private Dictionary<ulong, NetworkManager.ConnectionApprovalResponse> m_pendingConnections = new();
        private WaitForSecondsRealtime m_joinTimeout = new(10);

        public void StartPlayerQueueRoutine()
        {
            if (m_currentlyLoading?.IsRunning is not true && m_playerJoinRequests.Count > 0)
            {
                CreateClient(m_playerJoinRequests[0]);
            }
        }

        public void QueueNewClient(ulong clientId)
        {
            if (m_currentlyLoading?.IsRunning is not true)
            {
                CreateClient(clientId);
            }
            else
            {
                m_playerJoinRequests.Add(clientId);
            }
        }

        public void CreateClient(ulong clientId)
        {
            m_currentlyLoading = StartCoroutine(Impl());
            _ = StartCoroutine(GenerateNewGroupPresence(NetworkLayer.CurrentRoom));

            IEnumerator Impl()
            {
                if (NetworkManager.Singleton.IsHost)
                {
                    yield return new WaitUntil(() => SceneLoader.SceneLoaded && NetworkManager.Singleton.IsListening);

                    SetUpPhotonVoiceRoom();

                    yield return new WaitUntil(() => NetworkLayer.IsInRoom);

                    if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
                    {
                        Log($"Client {clientId} is no longer connected, cancelling spawn...");
                    }
                    else
                    {
                        var playerObject = SpawnPlayer(clientId, Vector3.zero, Quaternion.identity, true);
                        var playerEntity = playerObject.GetComponent<AvatarEntity>();

                        yield return new WaitUntil(() => IsAvatarEntityReady(playerEntity) || !playerObject.GetOwnerPlayerId().HasValue);
                    }

                    if (m_playerJoinRequests.Contains(clientId))
                    {
                        _ = m_playerJoinRequests.Remove(clientId);
                    }

                    if (m_playerJoinRequests.Count > 0)
                    {
                        CreateClient(m_playerJoinRequests[0]);
                    }
                    else
                    {
                        m_currentlyLoading = null;
                    }

                    ProcessNextPendingConnection();
                }
            }
        }

        public void SetUpPhotonVoiceRoom()
        {
            _ = StartCoroutine(Impl());

            IEnumerator Impl()
            {
                yield return new WaitWhile(() => NetworkSession.Instance == null || string.IsNullOrEmpty(CurrentRoom));
                UnityEngine.Assertions.Assert.IsNotNull(NetworkSession.Instance);
                NetworkSession.Instance.SetPhotonVoiceRoom(CurrentRoom);
            }
        }

        private NetworkObject SpawnPlayer(ulong clientId, Vector3 position, Quaternion rotation, bool spawn = false)
        {
            if (spawn)
            {
                var player = Spawner.SpawnPlayer(clientId, position, rotation);
                OnSpawnClient(player);
                return player;
            }
            else
            {
                return Spawner.SpawnPlayer(clientId, position, rotation);
            }
        }

        private void ProcessNextPendingConnection()
        {
            if (m_pendingConnections.Count > 0)
            {
                var nextConnection = m_pendingConnections.ElementAt(0);
                if (NetworkManager.Singleton.PendingClients.ContainsKey(nextConnection.Key))
                {
                    nextConnection.Value.Pending = false;
                }
                else
                {
                    // This pending client either left, timed out, or has joined.
                    _ = m_pendingConnections.Remove(nextConnection.Key);
                    if (m_pendingConnections.Count > 0)
                    {
                        ProcessNextPendingConnection();
                    }
                }
            }
        }

        private IEnumerator JoinQueueTimeout(ulong clientId)
        {
            yield return m_joinTimeout;

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
                yield break;

            // After the buffer time, if the player is still in the pending list or not connected then we can consider them disconnected and move on in the queue
            if ((m_pendingConnections.ContainsKey(clientId) && m_pendingConnections[clientId].Pending) || !NetworkManager.Singleton.ConnectedClientsIds.Contains(clientId))
            {
                ProcessNextPendingConnection();
            }
        }

        private bool AnyClientCurrentlyJoining() =>
            m_pendingConnections.Count > 0;
    }
}
