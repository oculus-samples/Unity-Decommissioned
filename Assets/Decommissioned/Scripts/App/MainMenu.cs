// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.Decommissioned.UI;
using Meta.Multiplayer.Avatar;
using Meta.Multiplayer.Core;
using Meta.Utilities;
using NaughtyAttributes;
using Oculus.Interaction;
using TMPro;
using UnityEngine;

namespace Meta.Decommissioned
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private PokeInteractable m_createLobbyButton;
        [SerializeField] private PokeInteractable m_joinLobbyButton;
        [SerializeField] private TextMeshProUGUI m_infoText;
        [SerializeField] private LeaveMenu m_leaveMenu;
        [SerializeField] private Transform m_spawnPoint;

        private void Start()
        {
            var camTransform = PlayerCamera.Instance.transform;
            camTransform.SetPositionAndRotation(m_spawnPoint.position, m_spawnPoint.rotation);
            DisableButtons();
            _ = StartCoroutine(WaitForAvatarLoadToEnableMenu());
            Application.Instance.UpdateGroupPresence();
        }

        private IEnumerator WaitForAvatarLoadToEnableMenu()
        {
            yield return new WaitUntil(() => FindObjectOfType<AvatarEntity>() != null);

            var avatarEntity = FindObjectOfType<AvatarEntity>();

            yield return new WaitUntil(() => Application.Instance.IsAvatarEntityReady(avatarEntity));

            EnableButtons();
        }

        [Button]
        public void CreateNewGame()
        {
            m_leaveMenu.gameObject.SetActive(false);
            if (!Application.Instance.ConnectToMatch(true))
            {
                EnableButtons();
                _ = StartCoroutine(ResetInfoTextAfterFrame());
            }
        }

        [Button]
        public void JoinGame()
        {
            m_leaveMenu.gameObject.SetActive(false);
            if (!Application.Instance.ConnectToMatch(false))
            {
                EnableButtons();
                _ = StartCoroutine(ResetInfoTextAfterFrame());
            }
        }

        [Button]
        public void QuitGame() => UnityEngine.Application.Quit();

        public void QuitGameConfirmation()
        {
            if (m_leaveMenu)
            {
                m_leaveMenu.ShowMenu();
                return;
            }

            QuitGame();
        }

        /// <summary>
        /// Enables the 'Create Lobby' and 'Join Lobby' buttons.
        /// </summary>
        public void EnableButtons()
        {
            m_createLobbyButton.enabled = true;
            m_joinLobbyButton.enabled = true;
        }

        /// <summary>
        /// Disables the 'Create Lobby' and 'Join Lobby' buttons.
        /// </summary>
        public void DisableButtons()
        {
            m_createLobbyButton.enabled = false;
            m_joinLobbyButton.enabled = false;
        }

        private IEnumerator ResetInfoTextAfterFrame()
        {
            yield return new WaitForEndOfFrame();
            SetInfoText("");
        }

        /// <summary>
        /// Sets the info text located on the main menu control panel.
        /// </summary>
        /// <param name="newText">The new text to set the info panel to display.</param>
        public void SetInfoText(string newText)
        {
            if (!m_infoText)
            {
                Debug.LogWarning("m_infoText was not set up in the MainMenu canvas!");
                return;
            }

            if (newText.IsNullOrEmpty())
            {
                m_infoText.text = "";
                m_infoText.gameObject.SetActive(false);
                return;
            }

            m_infoText.gameObject.SetActive(true);
            m_infoText.text = newText;
        }
    }
}
