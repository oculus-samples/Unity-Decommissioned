// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Linq;
using Meta.Decommissioned.Game;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Timers
{
    /// <summary>
    /// Class representing a clock tracking the current time left in a phase. Provides a string showing the time remaining
    /// in each phase, as well as the number of rounds that have passed since the start of the game.
    /// </summary>
    public class PhaseClock : NetworkBehaviour
    {
        [SerializeField] private bool m_startClockOnAllPhases;
        [SerializeField] private Phase[] m_trackedPhases;
        [Tooltip("Occurs when the clock requests to change the timer display.")]
        [SerializeField] private UnityEvent<string> m_onTimerStringChanged;

        private Coroutine m_timerCoroutine;

        private void Start() => GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChange;

        protected new void OnDestroy()
        {
            base.OnDestroy();
            GamePhaseManager.Instance.OnPhaseChanged -= OnPhaseChange;
        }

        private void OnPhaseChange(Phase newPhase)
        {
            if (m_startClockOnAllPhases)
            {
                StartTimerOnNewPhase();
                return;
            }
            if (m_trackedPhases.Contains(newPhase)) { StartTimerOnNewPhase(); }
        }

        private void StartTimerOnNewPhase()
        {
            if (m_timerCoroutine != null) { StopCoroutine(m_timerCoroutine); }
            m_timerCoroutine = StartCoroutine(ObserveTimer());
        }

        private IEnumerator ObserveTimer()
        {
            var waitOneSecond = new WaitForSeconds(1);

            //Waiting here to allow clients to catch up
            yield return new WaitUntil(() => GamePhaseManager.Instance.PhaseTimer != null);

            while (true)
            {
                var phaseTimer = GamePhaseManager.Instance.PhaseTimer;
                if (phaseTimer != null)
                {
                    var time = (int)phaseTimer.TimeRemaining;
                    CreateTimerString(time);
                }
                yield return waitOneSecond;
            }
        }

        private void CreateTimerString(int time)
        {
            if (time <= 0)
            {
                m_onTimerStringChanged.Invoke("0:00");
                return;
            }

            var minutes = Mathf.FloorToInt(time / 60);
            var seconds = time % 60;
            var secondsPadding = "";
            if (seconds < 10) { secondsPadding = "0"; }
            m_onTimerStringChanged.Invoke(string.Format("{0}:{2}{1}", minutes, seconds, secondsPadding));
        }
    }
}
