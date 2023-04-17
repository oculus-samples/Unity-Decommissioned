// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections;
using Meta.Multiplayer.Networking;
using Meta.Utilities;
using NaughtyAttributes;
using ScriptableObjectArchitecture;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace Meta.Decommissioned.Game
{
    /// <summary>
    /// Class for managing the phases of the game; manages phase changes, phase timers, and prefab spawns.
    /// </summary>
    public class GamePhaseManager : NetworkSingleton<GamePhaseManager>
    {
        [SerializeField] private EnumDictionary<Phase, GamePhase> m_phases;

        [ShowNativeProperty] public static GamePhase ActivePhase => GamePhase.Instance;

        [SerializeField] private GameEvent m_onBeforePhaseEndEvent;

        public static Phase CurrentPhase => ActivePhase != null ? ActivePhase.Phase : Phase.Invalid;

        [ShowNativeProperty]
        public NetworkTimer PhaseTimer => ActivePhase?.Timer;

        [SerializeField] protected InputActionProperty m_debugSkipAudio;

        public bool DebugSkipAudio => m_debugSkipAudio.action?.ReadValue<float>() > 0.5f;

        public event Action<Phase> OnPhaseChanged;

        private const double COUNTDOWN_DURATION_SECONDS = 3;

        protected new void OnEnable()
        {
            base.OnEnable();
            m_debugSkipAudio.action?.Enable();
        }

        protected void OnDisable() => m_debugSkipAudio.action?.Disable();

        /// <summary>
        /// Begin a new game loop from the starting phase.
        /// </summary>
        /// <remarks>Only call this once! Implementation is brittle.</remarks>    
        public void BeginGameplay()
        {
            if (!IsServer) { return; }
            BeginPhase(Phase.Voting);
        }

        /// <summary>
        /// Advance the game to the next phase.
        /// </summary>
        public void AdvancePhase()
        {
            //Remove all listeners for now when advancing phase. This will fix a recursion error where the phase will repeatedly invoke this method.
            m_onBeforePhaseEndEvent.Raise();
            ActivePhase.OnPhaseEnded.RemoveAllListeners();
            _ = StartCoroutine(NewPhaseRoutine());
        }

        private IEnumerator NewPhaseRoutine()
        {
            if (!IsServer) { yield break; }
            Assert.IsTrue(IsServer, "Only Server can change Phase!");

            var shouldSkip = TrySkipToCountdown();

            if (shouldSkip)
            {
                ActivePhase.IsPhaseEnding = true;
                // Abort the phase transition process.
                // We'll come back to it when the adjusted timer expires.
                yield break;
            }

            if (!ActivePhase.IsPhaseEnding)
            {
                ForceSkipToCountdown();
                yield break;
            }

            var next = GetNextPhase();
            EndPhase();
            DestroyPhase();
            yield return new WaitUntil(() => ActivePhase == null);

            // wait a tick to make sure the destroy goes through first
            var nextTick = NetworkManager.NetworkTickSystem.LocalTime.TimeTicksAgo(-1).Tick;
            yield return new WaitUntil(() => NetworkManager.NetworkTickSystem.LocalTime.Tick >= nextTick);

            BeginPhase(next);
        }

        private void BeginPhase(Phase newPhase)
        {
            var phase = Instantiate(m_phases[newPhase]);
            phase.OnPhaseEnded.AddListener(_ => AdvancePhase());
            phase.NetworkObject.Spawn();
            _ = phase.NetworkObject.TrySetParent(transform);
        }

        private static Phase GetNextPhase() => CurrentPhase switch
        {
            Phase.Voting => Phase.Planning,
            Phase.Planning => Phase.Night,
            Phase.Night => Phase.Discussion,
            Phase.Discussion => Phase.Voting,
            Phase.Invalid => Phase.Invalid,
            _ => Phase.Invalid
        };

        private static void EndPhase() { if (ActivePhase != null) { ActivePhase.End(); } }

        public static void DestroyPhase()
        {
            if (ActivePhase == null) { return; }
            var endingPhase = ActivePhase;  // Try to workaround possible race condition?
            endingPhase.NetworkObject.Despawn();
        }

        /// <summary>
        /// Start the countdown timer if it is not currently running.
        /// </summary>
        /// <returns>`false` if the countdown is currently running, `true` otherwise.</returns>
        private bool TrySkipToCountdown()
        {
            var shouldSkipToCountdown = PhaseTimer.TimeRemaining > COUNTDOWN_DURATION_SECONDS;
            if (shouldSkipToCountdown) { PhaseTimer.SetTimer((float)COUNTDOWN_DURATION_SECONDS); }
            return shouldSkipToCountdown;
        }

        private void ForceSkipToCountdown()
        {
            ActivePhase.ForceSkipPhase();
            PhaseTimer.SetTimer((float)COUNTDOWN_DURATION_SECONDS);
        }

        public void RaiseOnPhaseChanged()
        {
            try
            {
                OnPhaseChanged?.Invoke(CurrentPhase);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
