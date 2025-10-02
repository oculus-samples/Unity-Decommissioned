// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using Meta.Decommissioned.Player;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game
{
    [MetaCodeSample("Decommissioned")]
    public class CommanderCandidateManager : NetworkSingleton<CommanderCandidateManager>
    {
        protected struct CommanderData : INetworkSerializeByMemcpy
        {
            public PlayerId Commander;
            public PlayerId CandidateA;
            public PlayerId CandidateB;
        }

        private NetworkVariable<CommanderData> m_commanderData;
        private Dictionary<PlayerId, CapsuleCollider> m_votingColliders = new();

        public event Action<PlayerId, PlayerId> OnNewCandidates;
        public event Action<PlayerId> OnNewCommander;

        public PlayerId CommanderCandidateA => m_commanderData.Value.CandidateA;
        public PlayerId CommanderCandidateB => m_commanderData.Value.CandidateB;
        public PlayerId Commander => m_commanderData.Value.Commander;
        public PlayerId[] CommanderCandidates => new[] { CommanderCandidateA, CommanderCandidateB };

        private new void Awake()
        {
            base.Awake();
            m_commanderData = new();

            m_commanderData.OnValueChanged += OnCommanderDataChanged;

            PlayerManager.WhenInstantiated(sessionMan => sessionMan.OnPlayerExit += OnPlayerExit);
        }

        private void OnPlayerExit(PlayerId playerId)
        {
            if (m_votingColliders.ContainsKey(playerId))
            {
                _ = m_votingColliders.Remove(playerId);
            }
        }

        private void OnCommanderDataChanged(CommanderData previousValue, CommanderData newValue)
        {
            if (newValue.CandidateA != default)
            {
                OnNewCandidates?.Invoke(newValue.CandidateA, newValue.CandidateB);
                PopulateVotingColliders();
                UpdateVotingColliders();
            }

            if (newValue.Commander != default)
            {
                OnNewCommander?.Invoke(newValue.Commander);
            }
        }

        private void PopulateVotingColliders()
        {
            m_votingColliders.Clear();
            var playersInGame = PlayerManager.Instance.AllPlayerIds;
            foreach (var player in playersInGame)
            {
                if (m_votingColliders.ContainsKey(player))
                {
                    Debug.LogWarning("Tried to populate the player voting colliders list with a player that has already been added!");
                    continue;
                }

                var playerObject = player.Object;
                if (playerObject == null)
                {
                    Debug.LogError("Tried to populate the player voting colliders list with a player that has no player object!");
                    continue;
                }

                var playerVotingCollider = playerObject.GetComponentInChildren<CapsuleCollider>();
                if (playerVotingCollider == null)
                {
                    Debug.LogError("Tried to populate the player voting colliders list with a player that has no voting collider!");
                    continue;
                }

                m_votingColliders.Add(player, playerVotingCollider);
            }
        }

        private void UpdateVotingColliders()
        {
            foreach (var player in m_votingColliders.Keys)
            {
                if (player == null || m_votingColliders[player] == null)
                {
                    continue;
                }

                m_votingColliders[player].enabled = CommanderCandidateA == player || CommanderCandidateB == player;
            }
        }

        public (PlayerId, PlayerId) GetCommanderCandidates() => (CommanderCandidateA, CommanderCandidateB);

        public PlayerId GetCommander() => Commander;

        public void SetNewCommander(PlayerId newCommander)
        {
            m_commanderData.Value = new() { Commander = newCommander };

            var playerStatus = PlayerStatus.GetByPlayerId(newCommander);
            if (playerStatus != null)
            {
                playerStatus.CurrentStatus = PlayerStatus.Status.Commander;
            }
        }

        public void SetUpNewCommanderCandidates()
        {
            var clients = PlayerManager.Instance.AllPlayerIds.AsEnumerable().ToList();
            var filteredGamePositions = LocationManager.Instance.GetAllGamePositions().Where(x => x.IsOccupied).ToList();

            var previousCommander = Commander;
            var startingIndex = UnityEngine.Random.Range(0, clients.Count());

            //if this was not the first round, get the last commanders position and add one as the assignment position
            if (previousCommander != default)
            {
                var lastCommanderPosition = filteredGamePositions.FirstOrDefault(x => x.OccupyingPlayer.GetOwnerPlayerId() == previousCommander);
                if (lastCommanderPosition != null)
                {
                    startingIndex = filteredGamePositions.IndexOf(lastCommanderPosition) + 1;
                }
            }

            //sort out any out of bounds exceptions
            if (startingIndex > filteredGamePositions.Count - 1) { startingIndex = 0; }
            var nextIndex = startingIndex + 1;
            if (nextIndex > filteredGamePositions.Count - 1) { nextIndex = 0; }

            var commanderA = filteredGamePositions[startingIndex].OccupyingPlayer.GetOwnerPlayerId();
            var commanderB = filteredGamePositions[nextIndex].OccupyingPlayer.GetOwnerPlayerId();

            m_commanderData.Value = new()
            {
                CandidateA = commanderA.Value,
                CandidateB = commanderB.Value,
            };
        }
    }
}
