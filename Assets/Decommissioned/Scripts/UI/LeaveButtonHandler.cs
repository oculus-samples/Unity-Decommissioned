// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.Decommissioned.UI
{
    /** Handles the Leave Button interaction. */
    public class LeaveButtonHandler : MonoBehaviour
    {
        [SerializeField] private int m_secondsBeforeLeaving = 3;
        [SerializeField] internal LeaveMenu m_leaveMenu;

        private bool m_leaving;
        private WaitForSeconds m_wait1Sec = new(1.0f);
        private Coroutine m_leavingCoroutine;

        /**
         * Execute behavior on leave button press.
         */
        [ContextMenu("Trigger Leave Button")]
        public void OnButtonClicked()
        {
            if (m_leaving) { return; }
            m_leaveMenu.ShowMenu();
        }

        public void StartLeaveProcess() { if (!m_leaving) { m_leavingCoroutine = StartCoroutine(LeaveProcess()); } }

        public void CancelLeaveProcess()
        {
            if (!m_leaving) { return; }
            m_leaving = false;
            TransitionalFade.Instance.FadeFromBlack();
            if (m_leavingCoroutine != null) { StopCoroutine(m_leavingCoroutine); }
        }

        private IEnumerator LeaveProcess()
        {
            m_leaving = true;
            var leavingCountDown = m_secondsBeforeLeaving;
            while (leavingCountDown > 0)
            {
                var minutes = Mathf.FloorToInt(leavingCountDown / 60);
                var seconds = Mathf.FloorToInt(leavingCountDown % 60);
                var timeString = string.Format("{0:0}:{1:00}", minutes, seconds);
                m_leaveMenu.SetTimerText($"{timeString}");

                leavingCountDown--;
                if (leavingCountDown == 0)
                {
                    TransitionalFade.Instance.FadeToBlack();
                }
                yield return m_wait1Sec;
            }
            m_leaveMenu.gameObject.SetActive(false);
            if (SceneManager.GetActiveScene().name == Application.MAIN_MENU_SCENE)
            {
                UnityEngine.Application.Quit();
            }
            else
            {
                Application.Instance.GoToMainMenu();
            }
        }
    }
}
