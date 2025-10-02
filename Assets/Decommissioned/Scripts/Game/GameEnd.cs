// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Multiplayer.Core;
using Meta.Utilities;
using Meta.XR.Samples;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Game
{
    /// <summary>
    /// GameEnd stores and updates relevant values when a win/loss condition is reached; tracks the reason for the game's end
    /// (i.e. Crew/Mole victory) and invokes events depending on this reason in order to allow components to update their
    /// values and state appropriately.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public partial class GameEnd : NetworkBehaviour
    {
        [SerializeField] private EnumDictionary<GameEndReason, UnityEvent> m_onGameEnd = new();
        [SerializeField] private EnumDictionary<GameWinner, UnityEvent> m_onGameEndWinner = new();
        public event System.Action<GameWinner> OnGameEnd;

        private readonly NetworkVariable<GameEndReason> m_currentGameEndReason = new();

        public GameEndReason CurrentGameEndReason
        {
            get => m_currentGameEndReason.Value;
            set => m_currentGameEndReason.Value = value;
        }

        private GameWinner CurrentGameWinner => CurrentGameEndReason switch
        {
            GameEndReason.Unknown => GameWinner.None,
            GameEndReason.AllPlayersQuit => GameWinner.None,
            GameEndReason.MiniGamesCompleted => GameWinner.Crewmates,
            GameEndReason.CrewmatesOutnumbered => GameWinner.Moles,
            GameEndReason.MiniGamesFailed => GameWinner.Moles,
            GameEndReason.MiniGameDied => GameWinner.Moles,
            GameEndReason.CrewmatesLeft => GameWinner.Moles,
            GameEndReason.MolesLeft => GameWinner.Crewmates,
            GameEndReason.MaxRoundsReached => GameWinner.Crewmates,
            _ => GameWinner.None
        };

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            m_currentGameEndReason.OnValueChanged += OnGameEndReasonChanged;
            GameManager.OnGameStateChanged += ResetOnGameStateChange;
        }

        private void ResetOnGameStateChange(GameState state)
        {
            if (state == GameState.Gameplay && IsServer)
            {
                m_currentGameEndReason.Value = GameEndReason.Unknown;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            m_currentGameEndReason.OnValueChanged -= OnGameEndReasonChanged;
        }

        private void OnGameEndReasonChanged(GameEndReason previousValue, GameEndReason newValue)
        {
            m_onGameEnd[newValue]?.Invoke();
            OnGameEnd?.Invoke(CurrentGameWinner);
            m_onGameEndWinner[CurrentGameWinner]?.Invoke();
            PlayerCamera.Instance.Refocus();
        }


        [Button("End The Game")]
        public void TriggerGameIsOver() => GameIsOver();

        private void GameIsOver()
        {
            if (!IsServer) { return; }

            foreach (var player in NetworkManager.Singleton.ConnectedClientsList)
            {
                LocationManager.Instance.TeleportToMainRoom(player.PlayerObject);
            }
        }
    }
}
