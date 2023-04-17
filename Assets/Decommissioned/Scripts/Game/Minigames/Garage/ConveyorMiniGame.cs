// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Lobby;
using Meta.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    /// <summary>
    /// Base logic for the Conveyor Belt station. Increases or decreases the associated MiniGame's health
    /// based on input from other Conveyor Belt components.
    /// <seealso cref="ConveyorObjectSpawner"/>
    /// <seealso cref="ConveyorObject"/>
    /// <seealso cref="ConveyorBelt"/>
    /// </summary>
    public class ConveyorMiniGame : NetworkBehaviour
    {
        [SerializeField, AutoSetFromParent]
        private MiniGame m_miniGame
            ;
        [SerializeField] private ConveyorObjectSpawner m_miniGameSpawnerA;
        [SerializeField] private ConveyorObjectSpawner m_miniGameSpawnerB;

        [SerializeField] private GamePosition m_assignedPosition;

        [SerializeField] private ConveyerBeltVisuals m_conveyerBeltVisuals;

        private void Start()
        {
            m_miniGame.MiniGameInit = StartMiniGameServerRpc;
            GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
            m_assignedPosition.OnOccupyingPlayerChanged += OnPlayerChanged;
        }

        private void OnPlayerChanged(NetworkObject oldPlayer, NetworkObject player)
        {
            if (!m_assignedPosition.IsOccupied)
            {
                RestartMiniGame();
            }
        }

        private void OnPhaseChanged(Phase newPhase)
        {
            switch (newPhase)
            {
                case Phase.Discussion:
                    RestartMiniGame();
                    break;
                case Phase.Night:
                    StartConveyors();
                    break;
            }
        }

        [ContextMenu("Start Conveyor MiniGame")]
        [ServerRpc(RequireOwnership = false)]
        public void StartMiniGameServerRpc()
        {
            if (!IsServer)
            {
                return;
            }

            m_miniGameSpawnerA.StartSpawningServerRpc();
            m_miniGameSpawnerB.StartSpawningServerRpc();
        }

        /// <summary>
        /// Behavior executed when a Conveyor_Object reaches a destination object.
        /// </summary>
        /// <param name="objectReachedCorrectReceptacle">Boolean value indicating whether or not the object
        /// in question reached the correct destination. This determines if the task is damaged or not.</param>
        public void OnObjectReachedDestination(bool objectReachedCorrectReceptacle)
        {
            if (objectReachedCorrectReceptacle)
            {
                HealTaskServerRpc();
            }
            else
            {
                DamageTaskServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void HealTaskServerRpc()
        {
            m_miniGame.IncreaseHealth(m_miniGame.Config.HealthChangeOnAction / 2);
        }

        [ServerRpc(RequireOwnership = false)]
        private void DamageTaskServerRpc()
        {
            m_miniGame.DecreaseHealth();
        }

        [ContextMenu("Reset Conveyor MiniGame")]
        public void RestartMiniGame()
        {
            StopConveyors();
            m_miniGameSpawnerA.StopSpawningServerRpc();
            m_miniGameSpawnerB.StopSpawningServerRpc();
        }

        private void StartConveyors()
        {
            m_conveyerBeltVisuals.StartConveyorBelts();
        }

        private void StopConveyors()
        {
            m_conveyerBeltVisuals.StopConveyorBelts();
        }

    }
}
