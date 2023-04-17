// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Multiplayer.Networking;
using Meta.Utilities;
using ScriptableObjectArchitecture;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game
{
    /// <summary>
    /// The GameManager is responsible for tracking and managing the overall state of the game; stores and advances the current
    /// round, updates and raises events for game state changes, and regularly checks for match win conditions.
    /// </summary>
    public class GameManager : NetworkSingleton<GameManager>
    {
        public static GamePhaseManager PhaseManager => GamePhaseManager.Instance;
        [field: AutoSet, SerializeField] public GameEnd GameEnd { get; private set; }
        [field: SerializeField] public int MaxRounds { get; private set; } = 5;
        [SerializeField]
        private EnumDictionary<NumberOfPlayers, int> m_stationsToDestroyByPlayerCount = new();

        [SerializeField] private GameEvent m_gameStartEvent;

        [SerializeField] private GameEvent m_gameEndEvent;

        [SerializeField] private GameEvent m_gamePhaseChangedEvent;

        public int StationsToDestroy => m_stationsToDestroy.Value;

        public int StationsRemaining => m_stationsRemaining.Value;

        public GameState State
        {
            get => m_currentState.Value;
            private set => m_currentState.Value = value;
        }

        public int CurrentRoundCount => m_currentRoundCount.Value;

        private readonly NetworkVariable<int>
            m_currentRoundCount = new(1, writePerm: NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int>
            m_stationsToDestroy = new(3, writePerm: NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int>
            m_stationsRemaining = new(3, writePerm: NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<GameState> m_currentState = new();

        public event Action<int> OnRoundChanged;
        public event Action<int> OnStationsRemainingChanged;
        private Action<GameState> m_onGameStateChanged;

        private new void Awake()
        {
            base.Awake();
            m_currentState.OnValueChanged += OnStateChanged;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Application.Instance.UpdateGroupPresence();
            m_stationsRemaining.OnValueChanged += OnStationCountChanged;
        }

        private new void OnEnable()
        {
            PhaseManager.OnPhaseChanged += OnPhaseChanged;
            m_currentRoundCount.OnValueChanged += OnCurrentRoundChanged;
        }

        private void OnDisable()
        {
            PhaseManager.OnPhaseChanged -= OnPhaseChanged;
            m_currentRoundCount.OnValueChanged -= OnCurrentRoundChanged;
        }

        private void OnStateChanged(GameState previousValue, GameState newValue)
        {
            SubmitNewState(newValue);

            switch (newValue)
            {
                case GameState.Gameplay:
                    SetStartGameState();
                    break;
                case GameState.GameEnd:
                    SetGameEndState();
                    break;
                case GameState.ReadyUp:
                    break;
                default:
                    break;
            }
        }

        private void OnPhaseChanged(Phase newPhase)
        {
            if (!IsServer) { return; }

            switch (newPhase)
            {
                case Phase.Invalid:
                    break;
                case Phase.Planning:
                    break;
                case Phase.Night:
                    break;
                case Phase.Discussion:
                    m_stationsRemaining.Value = m_stationsToDestroy.Value - MiniGameManager.Instance.GetNumberOfMiniGamesDead();
                    if (m_currentRoundCount.Value > 1) { CheckForGameEnd(); }
                    break;
                default:
                    break;
            }
        }

        private void OnStationCountChanged(int previousStationCount, int newStationCount)
        {
            OnStationsRemainingChanged?.Invoke(newStationCount);
        }

        /// <summary>
        /// Signal that a new round has begun.
        /// </summary>
        /// <remarks>Rounds are NOT the same as GamePhases; rather, a round is a full Vote Phase-to-Night Phase cycle.</remarks>
        public void StartNewRound() => m_currentRoundCount.Value += 1;

        private void OnCurrentRoundChanged(int prevRound, int currentRound) => OnRoundChanged?.Invoke(currentRound);

        public static event Action<GameState> OnGameStateChanged
        {
            add => WhenInstantiated(gm => gm.m_onGameStateChanged += value);
            remove => WhenInstantiated(gm => gm.m_onGameStateChanged -= value);
        }

        public void UpdateGameState(GameState nextState)
        {
            if (!IsServer) { return; }

            State = nextState;
            switch (nextState)
            {
                case GameState.Gameplay:
                    Application.Instance.NetworkLayer.SetBroadcastLobby(false);
                    PhaseManager.BeginGameplay();
                    m_stationsToDestroy.Value = m_stationsToDestroyByPlayerCount[(NumberOfPlayers)NetworkManager.ConnectedClients.Count];
                    m_stationsRemaining.Value = m_stationsToDestroy.Value;
                    break;
                case GameState.GameEnd:
                    GameEnd.TriggerGameIsOver();
                    Application.Instance.NetworkLayer.SetBroadcastLobby(true);
                    break;
                case GameState.ReadyUp:
                    break;
                default:
                    break;
            }
        }

        private void SetStartGameState()
        {
            if (IsHost) { m_currentRoundCount.Value = 1; }

            m_gameStartEvent.Raise();
        }

        private void SubmitNewState(GameState newState)
        {
            m_onGameStateChanged?.Invoke(newState);
            Application.Instance.UpdateGroupPresence();
            m_gamePhaseChangedEvent.Raise();
        }

        private void SetGameEndState()
        {
            m_gameEndEvent.Raise();
            GamePhaseManager.DestroyPhase();
        }

        private void CheckForGameEnd()
        {
            if (!CheckGameEndConditions()) { return; }
            UpdateGameState(GameState.GameEnd);
            m_onGameStateChanged = m_onGameStateChanged.FixEvent();
        }

        private bool CheckGameEndConditions()
        {
            if (m_stationsRemaining.Value <= 0)
            {
                GameEnd.CurrentGameEndReason = GameEnd.GameEndReason.MiniGameDied;
                return true;
            }

            if (m_currentRoundCount.Value > MaxRounds)
            {
                m_currentRoundCount.Value = MaxRounds;
                GameEnd.CurrentGameEndReason = GameEnd.GameEndReason.MaxRoundsReached;
                return true;
            }

            return false;
        }

        public void StopGame(bool saboteursWin)
        {
            GameEnd.CurrentGameEndReason =
                saboteursWin ? GameEnd.GameEndReason.CrewmatesLeft : GameEnd.GameEndReason.MolesLeft;
            UpdateGameState(GameState.GameEnd);
        }

        private enum NumberOfPlayers
        {
            None,
            OnePlayer,
            TwoPlayers,
            ThreePlayers,
            FourPlayers,
            FivePlayers,
            SixPlayers,
            SevenPlayers,
            EightPlayers
        }
    }
}
