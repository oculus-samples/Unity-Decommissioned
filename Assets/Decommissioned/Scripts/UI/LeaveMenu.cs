// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Utilities;
using Meta.XR.Samples;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meta.Decommissioned.UI
{
    /// <summary>
    /// Handles the logic for the free-floating leave game menu.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class LeaveMenu : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_timerText;
        [SerializeField] private Button m_yesButton;
        [SerializeField] private Button m_noButton;
        [SerializeField] private Image m_noButtonImage;
        [SerializeField] private Sprite[] m_noButtonSprites = new Sprite[4];
        [SerializeField] private Sprite[] m_cancelButtonSprites = new Sprite[4];
        [SerializeField] internal LeaveButtonHandler m_leaveButton;
        [SerializeField] private GameObject m_menuRoot;
        [SerializeField, AutoSetFromChildren] private BoxCollider m_canvasCollider;

        private SpriteState m_noButtonDefaultSpriteState;
        private SpriteState m_noButtonCancellingSpriteState;

        private void Awake()
        {
            m_noButtonDefaultSpriteState.highlightedSprite = m_noButtonSprites[0];
            m_noButtonDefaultSpriteState.pressedSprite = m_noButtonSprites[1];
            m_noButtonDefaultSpriteState.selectedSprite = m_noButtonSprites[2];
            m_noButtonDefaultSpriteState.disabledSprite = m_noButtonSprites[3];

            m_noButtonCancellingSpriteState.highlightedSprite = m_cancelButtonSprites[0];
            m_noButtonCancellingSpriteState.pressedSprite = m_cancelButtonSprites[1];
            m_noButtonCancellingSpriteState.selectedSprite = m_cancelButtonSprites[2];
            m_noButtonCancellingSpriteState.disabledSprite = m_cancelButtonSprites[3];
        }

        /** Show the "Leave" UI to the player.*/
        public void ShowMenu()
        {
            m_menuRoot.gameObject.SetActive(true);
            m_yesButton.interactable = true;
            m_noButton.interactable = true;
            m_canvasCollider.enabled = true;
        }

        /** Hide the "Leave" UI from the player's view, resetting it to its default appearance.*/
        public void HideMenu()
        {
            m_leaveButton.CancelLeaveProcess();
            SetCancelButtonState(false);
            m_yesButton.interactable = false;
            m_noButton.interactable = false;
            m_canvasCollider.enabled = false;
            m_timerText.gameObject.SetActive(false);
            m_menuRoot.gameObject.SetActive(false);
        }

        /** Confirm that the player wants to leave; starts the "leave game" behavior. */
        public void YesButtonPressed()
        {
            m_timerText.gameObject.SetActive(true);
            m_leaveButton.StartLeaveProcess();
            SetCancelButtonState(true);
            m_yesButton.interactable = false;
            m_noButton.interactable = true;
        }

        private void SetCancelButtonState(bool cancel)
        {
            m_noButton.spriteState = cancel ? m_noButtonCancellingSpriteState : m_noButtonDefaultSpriteState;
            m_noButtonImage.sprite = cancel ? m_cancelButtonSprites[2] : m_noButtonSprites[2];
        }

        /** Confirm that the player wants to leave; starts the "leave game" behavior. */
        public void NoButtonPressed() => HideMenu();

        /** Set the text displayed by the timer label on this window to a specified string. */
        public void SetTimerText(string text)
        {
            if (!m_timerText) { return; }
            m_timerText.text = text;
        }

        [Button("Leave Game")]
        public void LeaveGame() => Application.Instance.GoToMainMenu();
    }
}
