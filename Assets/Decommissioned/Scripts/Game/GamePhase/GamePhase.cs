// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Decommissioned.ScriptableObjects;
using Meta.Multiplayer.Networking;
using Meta.Utilities;
using UnityEngine;

namespace Meta.Decommissioned.Game
{
    /// <summary>
    /// A network singleton representing a single phase in the game; determines a phase's duration, callbacks, and state.
    /// Specific phases and their unique behavior are encapsulated by a number of unique child classes -- see examples in
    /// <see cref="VotePhase"/>,
    /// <see cref="PlanningPhase"/>,
    /// <see cref="DiscussionPhase"/>
    /// and <see cref="NightPhase"/>.
    /// </summary>
    public abstract class GamePhase : NetworkSingleton<GamePhase>
    {
        [SerializeField] protected PhaseConfig m_config;

        public GamePhaseEvent OnPhaseBegan;

        public GamePhaseEvent OnPhaseEnded;
        [SerializeField] private bool m_phaseIsTimed = true;

        public bool IsPhaseEnding { get; set; }

        [SerializeField] private List<PreGameplayStep> m_preGameplaySteps;

        protected virtual float DurationSeconds => m_config.DurationSeconds;

        public abstract Phase Phase { get; }

        [field: SerializeField, AutoSet]
        public NetworkTimer Timer { get; private set; }

        private PreGameplayStep m_currentStep;

        protected new virtual void Awake() => base.Awake();

        protected new void OnDestroy() => base.OnDestroy();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Begin();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            OnPhaseBegan.RemoveAllListeners();
            OnPhaseEnded.RemoveAllListeners();
            if (m_phaseIsTimed) { Timer.OnTimerExpiredOnServer.RemoveListener(AdvanceWhenTimerExpires); }
        }

        private IEnumerator RunBeginStepsRoutine()
        {
            if (IsServer)
            {
                foreach (var step in m_preGameplaySteps.Select(s => Instantiate(s)))
                {
                    m_currentStep = step;
                    step.NetworkObject.Spawn(true);
                    yield return step.Run();
                    step.NetworkObject.Despawn();
                }
            }

            Execute();
        }

        protected virtual void Begin()
        {
            RaisePhaseBeginEvent();
            GamePhaseManager.Instance.RaiseOnPhaseChanged();
            GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
            if (TransitionalFade.Instance != null)
            {
                Timer.OnClientTransitionTimeReached.AddListener(OnTimerTransitionTimeReached);
            }

            _ = StartCoroutine(RunBeginStepsRoutine());
        }

        public virtual void End() => RaisePhaseEndEvent();

        protected virtual void Execute()
        {
            if (!m_phaseIsTimed) { return; }

            Timer.StartTimer(DurationSeconds);
            Timer.OnTimerExpiredOnServer.AddListener(AdvanceWhenTimerExpires);
        }

        private void RaisePhaseBeginEvent() => OnPhaseBegan.Invoke(this);

        private void RaisePhaseEndEvent() => OnPhaseEnded.Invoke(this);

        private void OnPhaseChanged(Phase phase)
        {
            if (phase == Phase) { return; }

            if (m_currentStep) { m_currentStep.NetworkObject.Despawn(); }
        }

        public void SetPhaseAsEnding()
        {
            if (!IsServer) { return; }

            IsPhaseEnding = true;
        }

        public void ForceSkipPhase()
        {
            EndPreGameplaySteps();
            IsPhaseEnding = true;
        }

        private void EndPreGameplaySteps()
        {
            StopAllCoroutines();
            if (m_currentStep != null && m_currentStep.IsSpawned)
            {
                m_currentStep.StopAllCoroutines();
                m_currentStep.End();
                m_currentStep.NetworkObject.Despawn();
            }

            Execute();
        }

        private void OnTimerTransitionTimeReached() => TransitionalFade.Instance.OnPhaseTimerTransition(false);

        private void AdvanceWhenTimerExpires(NetworkTimer _)
        {
            IsPhaseEnding = true;
            GamePhaseManager.Instance.AdvancePhase();
        }
    }
}
