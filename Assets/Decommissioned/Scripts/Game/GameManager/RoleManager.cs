// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using Meta.Decommissioned.Player;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game
{
    /// <summary>
    /// Component responsible for managing the various roles available to players during the game.
    /// Assigns and retrieves information about the roles of players in a given match.
    /// </summary>
    public class RoleManager : NetworkSingleton<RoleManager>
    {
        [SerializeField] private int m_maxMolePlayerThreshold = 6;
        [SerializeField] private int m_minMoles = 1;
        [SerializeField] private int m_maxMoles = 2;

        private readonly NetworkVariable<int> m_maxMoleCount = new(writePerm: NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> m_currentCrewCount = new(writePerm: NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> m_currentMoleCount = new(writePerm: NetworkVariableWritePermission.Server);

        public event Action<int> OnMoleCountChanged;
        public event Action<int> OnCrewCountChanged;

        public bool EnableLogging;

        private IEnumerable<PlayerRole> GetCurrentPlayers() =>
            NetworkManager.ConnectedClients.Values.Select(client => PlayerRole.GetByPlayerObject(client?.PlayerObject))
                .Where(playerRole => playerRole != null);


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (NetworkManager.Singleton.IsHost)
            {
                NetworkObject.RemoveOwnership();
                PlayerManager.Instance.OnPlayerExit += OnPlayerDisconnect;
            }

            m_currentCrewCount.OnValueChanged += OnCrewPlayerCountChanged;
            m_currentMoleCount.OnValueChanged += OnMolePlayerCountChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            m_currentCrewCount.OnValueChanged -= OnCrewPlayerCountChanged;
            m_currentMoleCount.OnValueChanged -= OnMolePlayerCountChanged;
        }


        /// <summary>
        /// Assigns player roles. The ratio of crew mates to moles varies depending on how many players there are
        /// in total.
        /// </summary>
        public void AssignPlayerRoles()
        {
            if (!IsHost) { return; }

            var currentPlayers = GetCurrentPlayers().ToArray();

            m_maxMoleCount.Value = currentPlayers.Length < m_maxMolePlayerThreshold ? m_minMoles : m_maxMoles;
            m_currentMoleCount.Value = Mathf.Min(m_maxMoleCount.Value, currentPlayers.Length);

            var playerIsSaboteurList = currentPlayers.Select(_ => false).ToArray();
            int MoleCount() => playerIsSaboteurList.Count(isMole => isMole);

            while (MoleCount() < m_currentMoleCount.Value) { playerIsSaboteurList.RandomElement() = true; }

            foreach (var (i, isSab) in playerIsSaboteurList.Enumerate())
            {
                currentPlayers[i].CurrentRole = isSab ? Role.Mole : Role.Crewmate;
            }
        }

        /// <summary>
        /// When players disconnect, the RoleManager checks how many players remain in each role; at a certain threshold,
        /// it will force the game to end early. 
        /// </summary>
        /// <param name="playerId"></param>
        private void OnPlayerDisconnect(PlayerId playerId)
        {
            if (!IsServer) { return; }

            var player = GetCurrentPlayers().FirstOrDefault(x => x.NetworkObject.GetOwnerPlayerId() == playerId);
            var stopGame = false; // If true, stops and resets the game
            var molesWin = false;

            if (GameManager.Instance.State == GameState.ReadyUp || (player == null))
            {
                if (EnableLogging)
                {
                    Debug.Log($"Player {playerId} disconnected during {GameManager.Instance.State}.");
                }
                return;
            }

            switch (player.CurrentRole)
            {
                case Role.Crewmate:
                    stopGame = m_currentCrewCount.Value == 1;
                    molesWin = m_currentCrewCount.Value == 1;
                    m_currentCrewCount.Value--;
                    break;

                case Role.Mole:
                    stopGame = m_currentMoleCount.Value == 1;
                    m_currentMoleCount.Value--;
                    break;

                case Role.Unknown:
                    break;

                default:
                    break;
            }

            if (stopGame) { GameManager.Instance.StopGame(molesWin); }
        }

        private void OnMolePlayerCountChanged(int previous, int current) => OnMoleCountChanged?.Invoke(current);

        private void OnCrewPlayerCountChanged(int previous, int current) => OnCrewCountChanged?.Invoke(current);
    }
}
