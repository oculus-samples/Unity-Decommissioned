// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Linq;
using Meta.Decommissioned.Lobby;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using ScriptableObjectArchitecture;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    /**
     * Class for managing the hydroponics mini games; determine conditions for mini game
     * health increase or decrease at both parts of the station (A and B).
     *
     * <seealso cref="HydroponicsMoistureManager"/>
     * <seealso cref="HydroponicsMoistureObject"/>
     */
    public class HydroponicsMiniGame : NetworkBehaviour
    {
        [SerializeField] private HydroponicsMoistureManager m_moistureManager;
        [SerializeField] private MiniGame m_miniGame_A;
        [SerializeField] private MiniGame m_miniGame_B;

        [SerializeField] private BoolGameEvent m_toggleNutrientChangeEvent;
        [SerializeField] private NetworkObject m_grabbableHose;
        [SerializeField] private GamePosition m_hosePlayerPosition;
        private int m_healthChange = 1;
        private int m_nutrientHealthChange;

        private void OnEnable()
        {
            GamePhaseManager.Instance.OnPhaseChanged += CheckCurrentGamePhase;

            if (m_hosePlayerPosition) { m_hosePlayerPosition.OnOccupyingPlayerChanged += RequestChangeOwnership; }

            m_miniGame_A.MiniGameInit = StartMiniGameServerRpc;
        }

        private void OnDisable()
        {
            GamePhaseManager.Instance.OnPhaseChanged -= CheckCurrentGamePhase;
        }

        private void CheckCurrentGamePhase(Phase phase)
        {
            if (!IsServer) { return; }
            if (phase == Phase.Discussion) { ResetMiniGame(); }
        }

        /**
         * Start the Hydroponics mini game; begin pressure decay at both mini game positions (A & B).
         * */
        [ContextMenu("Initialize Hydroponics")]
        [ServerRpc(RequireOwnership = false)]
        public void StartMiniGameServerRpc()
        {
            if (!IsServer) { return; }
            m_moistureManager.StartMoistureDecay();
            m_toggleNutrientChangeEvent.Raise(true);
        }

        /**
         * Behavior executed each time the pressure of the two sets of pipes are
         * checked.
         * */
        public void OnMoistureChecked(bool[] plantMoistureConditions)
        {
            if (!IsServer) { return; }

            var correctMoistures = plantMoistureConditions.Count(moisture => moisture);
            m_healthChange = correctMoistures switch
            {
                0 => -2,
                1 => -1,
                2 => 1,
                3 => 2,
                _ => 0,
            };

            if (m_nutrientHealthChange >= 0) { m_healthChange++; }
            else if (m_nutrientHealthChange < 0) { m_healthChange--; }

            m_nutrientHealthChange = 0;

            if (m_healthChange > 0) { AddHealthToMiniGame(); }
            else if (m_healthChange < 0)
            {
                m_healthChange = Mathf.Abs(m_healthChange);
                DamageMiniGame();
            }
        }

        /**
         * Behavior executed each time a plant receives a nutrient.
         * */
        public void OnNutrientChecked(bool correctNutrientReceived)
        {
            if (!IsServer) { return; }

            if (correctNutrientReceived) { m_nutrientHealthChange++; }
            else { m_nutrientHealthChange--; }
        }

        /**
         * Reset the Hydroponics mini game to its default state.
         */
        [ContextMenu("Reset Hydroponics")]
        public void ResetMiniGame()
        {
            m_moistureManager.ResetMoistureDecay();
            m_toggleNutrientChangeEvent.Raise(false);
        }

        /**
         * Add health to the attached Minigame.
         * <param name="isStationB">Boolean indicating if health will be
         * added to the mini game assigned to position A or B.</param>
         */
        public void AddHealthToMiniGame()
        {
            if (!IsServer) { return; }
            m_miniGame_B.IncreaseHealth(m_healthChange);
            m_miniGame_A.IncreaseHealth(m_healthChange);
        }

        /**
         * Subtract health from one of the attached Minigames.
         * <param name="isStationB">Boolean indicating if health will be
         * taken from the mini game assigned to position A or B.</param>
         */
        public void DamageMiniGame()
        {
            if (!IsServer) { return; }
            m_miniGame_B.DecreaseHealth(m_healthChange);
            m_miniGame_A.DecreaseHealth(m_healthChange);
        }

        private void RequestChangeOwnership(NetworkObject prevPlayer, NetworkObject player)
        {
            if (player == null || !m_hosePlayerPosition.IsOccupied)
            {
                ChangeHoseOwnerServerRpc(PlayerId.ServerPlayerId());
                return;
            }
            ChangeHoseOwnerServerRpc(player.GetOwnerPlayerId() ?? PlayerId.New());
        }

        [ServerRpc(RequireOwnership = false)]
        private void ChangeHoseOwnerServerRpc(PlayerId newPlayer) => m_grabbableHose.ChangeOwnership(newPlayer);

    }
}
