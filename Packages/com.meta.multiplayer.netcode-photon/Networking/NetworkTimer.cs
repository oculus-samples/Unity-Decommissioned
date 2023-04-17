// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Multiplayer.Networking
{
    /// <summary>
    /// Class representing a timer that is synchronized over the network.
    /// </summary>
    public class NetworkTimer : NetworkBehaviour
    {
        [SerializeField] private double m_transitionTriggerTime = 1.5f;
        public UnityEvent<NetworkTimer> OnTimerStartedOnServer;
        public UnityEvent<NetworkTimer> OnTimerExpiredOnServer;
        public UnityEvent OnClientTransitionTimeReached;

        private readonly NetworkVariable<double> m_duration = new(-1);
        private readonly NetworkVariable<double> m_startTime = new(float.PositiveInfinity);

        private bool m_transitionTriggered;

#if HAS_NAUGHTY_ATTRIBUTES
        [NaughtyAttributes.ShowNativeProperty]
#endif
        public double TimeRemaining => HasStarted ? Duration + m_startTime.Value - LocalTime : -1;

#if HAS_NAUGHTY_ATTRIBUTES
        [NaughtyAttributes.ShowNativeProperty]
#endif
        public double Duration => m_duration.Value;
        public double LocalTime => NetworkObject == null ? float.NaN : NetworkManager.LocalTime.Time;

#if HAS_NAUGHTY_ATTRIBUTES
        [NaughtyAttributes.ShowNativeProperty]
#endif
        public bool HasStarted => LocalTime >= m_startTime.Value;

#if HAS_NAUGHTY_ATTRIBUTES
        [NaughtyAttributes.ShowNativeProperty]
#endif
        public bool IsCompleted => LocalTime >= m_startTime.Value + Duration;

        private void Update()
        {
            if (HasStarted && TimeRemaining <= m_transitionTriggerTime && !m_transitionTriggered)
            {
                m_transitionTriggered = true;
                OnClientTransitionTimeReached?.Invoke();
            }

            if (!IsServer || !HasStarted || !IsCompleted) { return; }
            m_startTime.Value = float.PositiveInfinity;
            OnTimerExpiredOnServer?.Invoke(this);
        }

        /// <summary>
        ///  Sets the timer's current duration.
        /// </summary>
        /// <param name="timerDurationInSeconds">The duration of the timer.</param>
        public void SetTimer(double timerDurationInSeconds)
        {
            if (!IsServer) { return; }

            // Set the duration so that the expiration time is `timerDurationInSeconds` from now,
            // without modifying the start time.
            m_duration.Value = LocalTime + timerDurationInSeconds - m_startTime.Value;
        }

        /// <summary>
        ///  Starts the timer.
        /// </summary>
        public void StartTimer(double? duration = null)
        {
            if (!IsServer || HasStarted) { return; }
            if (duration.HasValue) { m_duration.Value = duration.Value; }
            m_startTime.Value = LocalTime;
            OnTimerStartedOnServer?.Invoke(this);
        }
    }
}
