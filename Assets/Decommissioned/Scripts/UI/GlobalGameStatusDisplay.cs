// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using Meta.XR.Samples;
using ScriptableObjectArchitecture;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.UI
{
    /// <summary>
    /// The GameGlobalStatusIndicator is a UI object (diegetic or otherwise) that displays important information about the
    /// match as it proceeds, such as how many moles there are and how many stations they must destroy in order to win.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class GlobalGameStatusDisplay : MonoBehaviour
    {
        [Tooltip("The role to display on this indicator for counting.")]
        [SerializeField] private Role m_displayedRoleCount = Role.Mole;

        [Tooltip("String displayed before player role counts are initialized.")]
        [SerializeField] private StringVariable m_defaultRoleCountString;

        [Tooltip("String displayed before station count is initialized..")]
        [SerializeField] private StringVariable m_defaultStationCountString;

        [Tooltip("The string displaying the number stations that must be destroyed for a mole victory.")]
        [SerializeField] private StringVariable m_stationCountString;

        [Tooltip("The string displaying the current count of players in the selected role.")]
        [SerializeField] private StringVariable m_roleCountString;

        [Tooltip("Invokes when the role count text has been updated.")]
        [SerializeField] private UnityEvent<string> m_onRoleCountChanged;

        [Tooltip("Invokes when the station count text has been updated.")]
        [SerializeField] private UnityEvent<string> m_onStationCountChanged;

        private void OnEnable()
        {
            m_onRoleCountChanged?.Invoke(m_defaultRoleCountString);
            m_onStationCountChanged?.Invoke(m_defaultStationCountString);
            RoleManager.WhenInstantiated(InitializeRoleListeners);
            InitializeStationListeners();
        }

        private void OnDisable()
        {
            if (RoleManager.Instance != null)
            {
                if (m_displayedRoleCount == Role.Mole) { RoleManager.Instance.OnMoleCountChanged -= OnSelectedRoleCountChanged; }
                else if (m_displayedRoleCount == Role.Crewmate)
                {
                    RoleManager.Instance.OnCrewCountChanged -= OnSelectedRoleCountChanged;
                }
            }

            if (m_displayedRoleCount == Role.Mole) { RoleManager.Instance.OnMoleCountChanged -= OnSelectedRoleCountChanged; }
            else { RoleManager.Instance.OnCrewCountChanged -= OnSelectedRoleCountChanged; }
        }

        private void InitializeRoleListeners(RoleManager roleManager)
        {
            if (m_displayedRoleCount == Role.Mole) { roleManager.OnMoleCountChanged += OnSelectedRoleCountChanged; }
            else if (m_displayedRoleCount == Role.Crewmate) { roleManager.OnCrewCountChanged += OnSelectedRoleCountChanged; }
        }

        private void InitializeStationListeners()
        {
            GameManager.OnGameStateChanged += OnGameStateChanged;
            GameManager.Instance.OnStationsRemainingChanged += UpdateStationCount;
        }

        private void OnGameStateChanged(GameState state)
        {
            if (GameManager.Instance.State == GameState.Gameplay)
            {
                UpdateStationCount(GameManager.Instance.StationsToDestroy);
                return;
            }
            UpdateStationCount(GameManager.Instance.StationsRemaining);
        }

        private void OnSelectedRoleCountChanged(int newValue)
        {
            UpdateRoleCount(newValue);
        }

        private void UpdateRoleCount(int newValue)
        {
            m_onRoleCountChanged?.Invoke(string.Format(m_roleCountString, newValue).ToUpper());
        }

        private void UpdateStationCount(int stationsToDestroy)
        {
            var newString = string.Format(m_stationCountString, stationsToDestroy).ToUpper();
            m_onStationCountChanged?.Invoke(newString);
        }
    }
}
