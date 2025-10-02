// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.ScriptableObjects;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Lobby
{
    /// <summary>
    /// GameStart is a class for managing the state of the match before it officially starts. When certain
    /// conditions are met (i.e. all players Ready), the actual match can be started from here.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    [RequireComponent(typeof(ReadyUp))]
    public class GameStart : NetworkBehaviour
    {
        [SerializeField] private int m_minimumNumberOfPlayers = 1;
        [SerializeField, AutoSet] private ReadyUp m_readyUp;
        [SerializeField] private PlayerReadyUpEvent m_clientReadyUpEvent;
        [SerializeField] private UnityEvent m_onAllPlayersReady;
        private NetworkList<PlayerId> m_readiedPlayers;

        private void Awake()
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            m_readiedPlayers = new();
        }

        public override void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            }

            base.OnDestroy();
        }

        private void OnEnable() => m_readyUp.OnPlayerReady += OnPlayerReady;

        private void OnDisable() => m_readyUp.OnPlayerReady -= OnPlayerReady;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            m_readiedPlayers.OnListChanged += OnReadyPlayersChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkSpawn();
            m_readiedPlayers.OnListChanged -= OnReadyPlayersChanged;
        }

        private void OnReadyPlayersChanged(NetworkListEvent<PlayerId> listEvent)
        {
            var playerId = listEvent.Value;
            if (!m_readiedPlayers.Contains(playerId))
            {
                m_clientReadyUpEvent.Raise(new ReadyUp.ReadyStatus { PlayerId = playerId, IsPlayerReady = false });
                return;
            }

            m_clientReadyUpEvent.Raise(new ReadyUp.ReadyStatus { PlayerId = playerId, IsPlayerReady = true });

            if (CheckReadyPlayers())
            {
                m_onAllPlayersReady.Invoke();
            }
        }

        private void OnPlayerReady(bool isReady, PlayerId playerId) => ReadyUpServerRpc(playerId, isReady);


        [ServerRpc(RequireOwnership = false)]
        private void ReadyUpServerRpc(PlayerId readiedPlayerId, bool isReady)
        {
            switch (isReady, m_readiedPlayers.Contains(readiedPlayerId))
            {
                case (true, false):
                    m_readiedPlayers.Add(readiedPlayerId);
                    PhaseSkipReadyCheck();
                    break;
                case (false, true):
                    _ = m_readiedPlayers.Remove(readiedPlayerId);
                    break;
                case (false, false):
                    _ = m_readiedPlayers.Remove(readiedPlayerId);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        ///  Compare connected clients to everyone who is ready to make sure they have the same values.
        ///  If all conditions are met, start the game.
        /// </summary>
        public void TryToStartGame()
        {
            if (!IsHost)
            {
                return;
            }

            var allPlayers = PlayerManager.Instance.AllPlayerIds.ToList();

            if (allPlayers.Count < m_minimumNumberOfPlayers)
            {
                Debug.LogWarning($"{gameObject.name}: There are not enough players in the game!");
                return;
            }

            if (m_readiedPlayers.Count < m_minimumNumberOfPlayers)
            {
                Debug.LogWarning($"{gameObject.name}: Everyone must ready up before starting the game.");
                return;
            }

            if (!CheckReadyPlayers())
            {
                return;
            }

            m_readiedPlayers.Clear();
            GameManager.Instance.UpdateGameState(GameState.Gameplay);
            RoleManager.Instance.AssignPlayerRoles();
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }

            var playerId = PlayerManager.Instance.GetPlayerIdByClientId(clientId);
            if (!playerId.HasValue)
            {
                return;
            }

            if (m_readiedPlayers.Contains(playerId.Value))
            {
                _ = m_readiedPlayers.Remove(playerId.Value);
            }
        }

        private void PhaseSkipReadyCheck()
        {
            if (GameManager.Instance.State != GameState.Gameplay || !IsHost || !CheckReadyPlayers())
            {
                return;
            }

            m_readiedPlayers.Clear();
            GamePhaseManager.Instance.AdvancePhase();
        }

        private bool CheckReadyPlayers()
        {
            foreach (var id in PlayerManager.Instance.AllPlayerIds.ToList())
            {
                if (!m_readiedPlayers.Contains(id))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
