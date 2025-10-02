// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Decommissioned.Lobby;
using Meta.Decommissioned.ScriptableObjects;
using Meta.Multiplayer.Networking;
using Meta.XR.Samples;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Game.MiniGames
{
    /// <summary>
    /// Defines an object as a MiniGame object. This means the object will receive and send the data necessary for the MiniGame to be completed.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    [RequireComponent(typeof(NetworkObject))]
    public class MiniGame : NetworkMultiton<MiniGame>
    {
        [field: SerializeField, Required] public MiniGameConfig Config { get; private set; }

        [Tooltip("The spawn point that is meant for this MiniGame.")]
        [field: SerializeField, Required] public GamePosition SpawnPoint { get; private set; }

        [Tooltip("The list of Minigames that this MiniGame is linked to. For this MiniGame to be considered complete by " +
                 "the MiniGame manager, all linked MiniGames must be complete as well.")]
        [SerializeField] private List<MiniGame> m_linkedMiniGames = new();

        /// <summary>
        /// This MiniGame's current health.
        /// </summary>
        public int CurrentHealth => m_currentHealth.Value;

        public int HealthAtRoundStart { get; private set; }

        public float HealthDecreaseCap =>
        Config.MaxHealthDecreasePerRound + (SpawnPoint.IsOccupied ? Config.HealthDecreasePlayerBonus : 0);


        [SerializeField] private UnityEvent m_onMiniGameStarted;

        [SerializeField] private UnityEvent m_onMiniGameStartedClient;

        [SerializeField] private UnityEvent<int> m_onMiniGameHealthChanged;

        public event Action OnHealthChanged;

        /// <summary>
        /// Executes this MiniGame's initialization method. Used for debugging purposes only.
        /// </summary>
        public Action MiniGameInit;
        private readonly NetworkVariable<bool> m_miniGameStarted = new();

        private readonly NetworkVariable<int> m_currentHealth = new(100);
        private bool m_runHealthDrain;
        private float m_healthDrainInterval = 5f;

        private readonly NetworkVariable<int> m_currentAttempts = new();

        private Coroutine m_healthDrainRoutine;

        public void RaiseMiniGameStartEvent() => m_onMiniGameStarted.Invoke();

        public static MiniGame GetByMiniGameRoom(MiniGameRoom room) => Instances.FirstOrDefault(mg => mg.SpawnPoint.MiniGameRoom == room);

        public override void OnNetworkSpawn()
        {
            if (IsServer) { m_currentHealth.Value = Config.MaxHealth; }
            HealthAtRoundStart = Config.MaxHealth;
            m_currentHealth.OnValueChanged += OnHealthChange;

            ValidateLinkedMiniGames();
            GamePhaseManager.Instance.OnPhaseChanged += OnGamePhaseChanged;
            m_miniGameStarted.OnValueChanged += MiniGameStartStatusChanged;

            base.OnNetworkSpawn();
        }

        private void MiniGameStartStatusChanged(bool previousStatus, bool miniGameStarted)
        {
            if (!IsServer) { return; }
            m_onMiniGameStartedClient.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            GamePhaseManager.Instance.OnPhaseChanged -= OnGamePhaseChanged;
            m_currentHealth.OnValueChanged -= OnHealthChange;
            base.OnNetworkDespawn();
        }

        public void InitializeMiniGameValues()
        {
            HealthAtRoundStart = Config.MaxHealth;
            if (!IsHost) { return; }
            m_currentHealth.Value = Config.MaxHealth;
            m_currentAttempts.Value = 0;
        }

        private void OnHealthChange(int oldValue, int newValue) => OnHealthChanged?.Invoke();

        private void OnGamePhaseChanged(Phase phase)
        {
            if (phase == Phase.Night) { HealthAtRoundStart = m_currentHealth.Value; }
            if (!IsServer) { return; }

            switch (phase)
            {
                case Phase.Planning:
                    if (GameManager.Instance.CurrentRoundCount > 1)
                    {
                        Config.CanBeAssigned = true;
                    }
                    break;
                case Phase.Night:
                    if (Config.HealthDrainsOverTime)
                    {
                        m_runHealthDrain = true;
                    }
                    break;
                default:
                    if (m_healthDrainRoutine != null)
                    {
                        StopCoroutine(m_healthDrainRoutine);
                    }
                    m_healthDrainRoutine = null;
                    m_miniGameStarted.Value = false;
                    break;
            }
        }

        public void StartHealthDrain()
        {
            if (!m_runHealthDrain) { return; }
            _ = StartCoroutine(WaitForPhaseTimerStart());
        }

        private IEnumerator WaitForPhaseTimerStart()
        {
            yield return new WaitUntil(() => GamePhaseManager.Instance?.PhaseTimer?.HasStarted ?? false);
            m_healthDrainInterval = (int)GamePhaseManager.Instance.PhaseTimer.Duration / (Config.MaxHealthDecreasePerRound + 1);
            m_healthDrainRoutine = StartCoroutine(RunHealthDrain());
            m_miniGameStarted.Value = true;
        }

        private IEnumerator RunHealthDrain()
        {
            YieldInstruction wait = new WaitForSeconds(m_healthDrainInterval);
            while (m_runHealthDrain)
            {
                yield return wait;
                DecreaseHealth(1);
            }
        }

        /// <summary>
        /// Resets this current MiniGame to its state before assignment.
        /// </summary>
        /// <param name="resetHealth">Should this MiniGame also reset it's health to the max?</param>
        [ContextMenu("Reset MiniGame")]
        public void ResetMiniGame(bool resetHealth = true) { if (resetHealth) { m_currentHealth.Value = Config.MaxHealth; } }

        public MiniGame[] GetLinkedMiniGames() => m_linkedMiniGames.ToArray();

        private void AddLinkedMiniGame(MiniGame miniGame)
        {
            if (m_linkedMiniGames.Contains(miniGame))
            {
                Debug.LogWarning("Tried to link a miniGame to another miniGame that was already linked!");
                return;
            }

            m_linkedMiniGames.Add(miniGame);
        }

        private void ValidateLinkedMiniGames()
        {
            foreach (var miniGame in m_linkedMiniGames)
            {
                if (miniGame == null) { continue; }

                var newLinkedMiniGames = miniGame.GetLinkedMiniGames();

                //If this linked miniGame is not linked to this current miniGame, link them
                if (!Array.Exists(newLinkedMiniGames, linkedMiniGame => linkedMiniGame == this)) { miniGame.AddLinkedMiniGame(this); }
            }
        }

        /// <summary>
        /// Increases this miniGame's current health.
        /// </summary>
        /// <param name="amount">If specified, the health will be increased by this value. Otherwise, it will increase by <see cref="FailureHealthPenalty"/></param>
        public void IncreaseHealth(int amount = 0)
        {
            if (GamePhaseManager.CurrentPhase != Phase.Night || m_currentHealth.Value >= HealthAtRoundStart || m_currentHealth.Value <= 0) { return; }
            var totalHealthIncrease = amount <= 0 ? Config.HealthChangeOnAction : amount;

            m_currentHealth.Value = Math.Clamp(m_currentHealth.Value + totalHealthIncrease, 0, HealthAtRoundStart);

            if (m_currentHealth.Value < HealthAtRoundStart)
            {
                m_onMiniGameHealthChanged.Invoke(totalHealthIncrease);
            }
        }

        /// <summary>
        /// Decreases this miniGame's current health.
        /// </summary>
        /// <param name="amount">If specified, the health will be decreased by this value. Otherwise, it will decrease by <see cref="FailureHealthPenalty"/>.</param>
        public void DecreaseHealth(int amount = 0)
        {
            if (GamePhaseManager.CurrentPhase != Phase.Night) { return; }

            if (m_currentHealth.Value <= 0) { return; }

            var healthDecreaseCapBonus = SpawnPoint.IsOccupied ? Config.HealthDecreasePlayerBonus : 0;
            var healthDrainCap = Config.MaxHealthDecreasePerRound + healthDecreaseCapBonus;
            var healthDrainFloor = Math.Clamp(HealthAtRoundStart - healthDrainCap, 0, HealthAtRoundStart);

            if (GetHealthChange() >= healthDrainCap)
            {
                m_currentHealth.Value = HealthAtRoundStart - healthDrainCap;
                return;
            }

            var totalHealthDecrease = amount == 0 ? Config.HealthChangeOnAction : amount;
            m_currentHealth.Value = Math.Clamp(m_currentHealth.Value - totalHealthDecrease, healthDrainFloor, HealthAtRoundStart);
            m_onMiniGameHealthChanged.Invoke(-totalHealthDecrease);
        }

        public float GetMiniGameHealthRatio() => (float)CurrentHealth / Config.MaxHealth;

        public float GetHealthChange() =>
            HealthAtRoundStart - m_currentHealth.Value;

        [Button("Start Game", EButtonEnableMode.Playmode)]
        private void Debug_StartGame() => MiniGameInit?.Invoke();
    }
}
