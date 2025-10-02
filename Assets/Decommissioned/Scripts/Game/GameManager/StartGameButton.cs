// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Audio;
using Meta.Decommissioned.Lobby;
using Meta.XR.Samples;
using NaughtyAttributes;
using ScriptableObjectArchitecture;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game
{
    /// <summary>
    /// Class encapsulating behavior for the "Start Game" button.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class StartGameButton : MonoBehaviour
    {
        [SerializeField, Required] private Renderer m_buttonMaterial;
        [SerializeField, Required] private TMP_Text m_buttonLabel;
        [SerializeField, Required] private GamePosition m_assignedGamePosition;
        [SerializeField, Required] private GameObject m_buttonObject;
        [SerializeField] private Color m_startGameButtonColor;
        [SerializeField] private Color m_newGameButtonColor;
        [SerializeField] private GameEvent m_startGameEvent;
        [SerializeField] private string m_newGameButtonString;
        [SerializeField] private string m_startGameButtonString;
        [SerializeField, Required] private AudioClip m_startGameSound;
        private bool m_isGameEnd;

        private void Start()
        {
            UpdateButtonState();
            m_buttonMaterial.material.color = m_startGameButtonColor;
            GameManager.OnGameStateChanged += OnGameStateChanged;

            if (m_assignedGamePosition == null)
            {
                return;
            }

            m_assignedGamePosition.OnOccupyingPlayerChanged += OnAssignedPlayerChanged;
        }

        private void OnAssignedPlayerChanged(NetworkObject previousPlayer, NetworkObject newPlayer) => UpdateButtonState();

        public void UpdateButtonState()
        {
            if (m_assignedGamePosition == null)
            {
                return;
            }

            var assignedPlayer = m_assignedGamePosition.OccupyingPlayer;
            m_buttonObject.SetActive(assignedPlayer != null && assignedPlayer.IsLocalPlayer
                                                            && assignedPlayer.IsOwnedByServer
                                                            && GameManager.Instance.State != GameState.Gameplay);
        }

        private void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.ReadyUp:
                    m_buttonMaterial.material.color = m_startGameButtonColor;
                    m_isGameEnd = false;
                    break;
                case GameState.GameEnd:
                    m_buttonMaterial.material.color = m_newGameButtonColor;
                    m_isGameEnd = true;
                    break;
                case GameState.Gameplay:
                    break;
                default:
                    break;
            }

            m_buttonLabel.text = m_isGameEnd ? m_newGameButtonString : m_startGameButtonString;
            UpdateButtonState();
        }

        public void OnButtonPressed()
        {
            m_startGameEvent.Raise();
            if (m_startGameSound && AudioManager.Instance != null)
            {
                _ = AudioManager.Instance.PlaySoundInSpace(transform.position, m_startGameSound);
            }
        }
    }
}
