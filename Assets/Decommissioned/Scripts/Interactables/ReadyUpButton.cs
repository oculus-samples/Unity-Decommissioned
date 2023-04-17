// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using Meta.Decommissioned.Lobby;
using Meta.Utilities;
using ScriptableObjectArchitecture;
using TMPro;
using UnityEngine;

namespace Meta.Decommissioned.Interactables
{
    /// <summary>
    /// A button that, when pressed, toggles the player's readiness state.
    /// <seealso cref="ReadyUp.PlayerStatus"/>
    /// </summary>
    public class ReadyUpButton : MonoBehaviour
    {
        [SerializeField, AutoSetFromChildren] private TMP_Text m_buttonText;
        [SerializeField] private BoolGameEvent m_readyUpEvent;
        [SerializeField] private StringVariable m_phaseSkipText;
        [SerializeField] private StringVariable m_readyUpText;

        private bool m_buttonSelected;
        private string m_phaseSkipDefaultSuffix = " Voting";

        private void Start()
        {
            GameManager.OnGameStateChanged += OnGameStateChanged;
            GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
        }

        private void OnPhaseChanged(Phase newPhase)
        {
            if (GameManager.Instance.State == GameState.GameEnd)
            {
                return;
            }

            var newPhaseButtonString = m_phaseSkipText + " " + newPhase;
            UpdateText(newPhase != Phase.Invalid ? newPhaseButtonString : m_readyUpText);
        }

        private void OnGameStateChanged(GameState newState) =>
            UpdateText(newState == GameState.Gameplay ? m_phaseSkipText + m_phaseSkipDefaultSuffix : m_readyUpText);

        /*
         * Set the button's colors and state to reflect the local
         * player's current readiness.
         * <param name="status">The current status of the local player.</param>
         */
        public void SetReadiness(ReadyUp.ReadyStatus readyStatus) => m_buttonSelected = readyStatus.IsPlayerReady;

        public void RaiseReadyEvent() => m_readyUpEvent.Raise(!m_buttonSelected);

        private void UpdateText(string text) => m_buttonText.text = text.ToUpper();
    }
}
