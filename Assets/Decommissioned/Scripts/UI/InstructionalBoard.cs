// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.Lobby;
using Meta.Decommissioned.Player;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.XR.Samples;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.UI
{
    /// <summary>
    /// A class for handling an automated instructional board for displaying instructions or information to the player.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class InstructionalBoard : NetworkBehaviour
    {
        /// <summary>
        /// The game position that this instructional board is intended for use at.
        /// </summary>
        [Tooltip("The game position that this instructional board is intended for use at.")]
        [SerializeField] private GamePosition m_relevantGamePosition;

        [Tooltip("A reference to the animator that this instructional board uses to animate.")]
        [SerializeField] private Animator m_animator;

        [Tooltip("A reference to the object rendering the instructional board visuals.")]
        [SerializeField] private GameObject m_boardVisualsObject;

        [Tooltip("A reference to the text that is displayed on this instructional board.")]
        [SerializeField] private TextMeshPro m_instructionalText;

        [Tooltip("Determines whether or not the instructions will be shown at the start of the night phase.")]
        [SerializeField] private bool m_activateAtWorkPhaseStart;

        [Tooltip("Determines whether or not instructions will automatically change based on the player occupying its position.")]
        [SerializeField] private bool m_doNotUpdateWithPlayer;

        [Tooltip("A reference to the list of meshes this instructional board has for hiding and showing purposes.")]
        [SerializeField] private MeshRenderer[] m_boardMeshes;

        [Tooltip("The text that will be displayed for players with the CREWMATE role.")]
        [TextArea(2, 10)]
        [SerializeField] private string m_crewInstructions = "";

        [Tooltip("The text that will be displayed for players with the MOLE role.")]
        [TextArea(2, 10)]
        [SerializeField] private string m_moleInstructions = "";

        [SerializeField] private UnityEvent<bool> m_onInstructionsToggled;

        private int m_animatorParameter;
        private NetworkVariable<bool> m_isDisplaying = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private bool m_isDisplayingOffline;

        /// <summary>
        /// Is this instructional board showing or not?
        /// </summary>
        public bool IsDisplaying => m_isDisplaying.Value;

        private void Awake()
        {
            m_animatorParameter = Animator.StringToHash("isDisplaying");
            SetTextForPlayerRole(Role.Crewmate);
            HideBoard();
        }

        private void OnEnable()
        {
            if (GamePhaseManager.Instance == null) { return; }
            GamePhaseManager.Instance.OnPhaseChanged += OnPhaseManagerOnPhaseChanged;
            m_isDisplaying.OnValueChanged += OnDisplayStateChanged;
            if (m_relevantGamePosition == null) { return; }
            m_relevantGamePosition.OnOccupyingPlayerChanged += OnOccupyingPlayerChanged;
        }

        private void OnDisplayStateChanged(bool oldState, bool newState)
        {
            if (newState) { DisplayBoard(); }
            else { HideBoard(); }
        }

        private void OnDisable()
        {
            if (GamePhaseManager.Instance == null) { return; }
            GamePhaseManager.Instance.OnPhaseChanged -= OnPhaseManagerOnPhaseChanged;
            m_isDisplaying.OnValueChanged -= OnDisplayStateChanged;
            if (m_relevantGamePosition == null) { return; }
            m_relevantGamePosition.OnOccupyingPlayerChanged -= OnOccupyingPlayerChanged;
        }

        private void OnPhaseManagerOnPhaseChanged(Phase newPhase)
        {
            if (newPhase != Phase.Night) { return; }
            if (m_activateAtWorkPhaseStart) { DisplayBoard(); }
        }

        private void OnOccupyingPlayerChanged(NetworkObject prevPlayer, NetworkObject player)
        {
            if (IsServer)
            {
                if (!m_relevantGamePosition.IsOccupied) { NetworkObject.ChangeOwnership(PlayerId.ServerPlayerId()); }
                else { NetworkObject.ChangeOwnership(player.GetOwnerPlayerId() ?? PlayerId.ServerPlayerId()); }
            }

            if (!m_relevantGamePosition.IsOccupied || m_doNotUpdateWithPlayer) { return; }
            var playerRole = PlayerRole.GetByPlayerObject(player);
            SetTextForPlayerRole(playerRole.CurrentRole);
        }

        /// <summary>
        /// Toggles the visibility of this instructional board, including animating and hiding/showing itself.
        /// </summary>
        public void ToggleBoardVisibility()
        {
            if (m_relevantGamePosition == null)
            {
                if (m_isDisplaying.Value || m_isDisplayingOffline) { HideBoard(); }
                else { DisplayBoard(); }
                return;
            }

            if (!m_relevantGamePosition.IsOccupied) { return; }
            var assignedPlayer = m_relevantGamePosition.OccupyingPlayer;
            if (assignedPlayer.GetOwnerPlayerId() != PlayerManager.LocalPlayerId) { return; }
            m_onInstructionsToggled.Invoke(!m_isDisplaying.Value);
            if (m_isDisplaying.Value) { HideBoard(); }
            else { DisplayBoard(); }
        }

        /// <summary>
        /// Set the current text of this instructional board.
        /// </summary>
        public void SetInstructionsText(string text) => m_instructionalText.text = text;

        private void DisplayBoard()
        {
            if (IsOwner) { m_isDisplaying.Value = true; }
            m_isDisplayingOffline = true;
            m_boardVisualsObject.SetActive(true);
            m_instructionalText.gameObject.SetActive(true);
            var playerAtMiniGame = GetPlayerAtMiniGameSpawn();
            if (playerAtMiniGame)
            {
                var playerRole = PlayerRole.GetByPlayerObject(playerAtMiniGame);
                SetTextForPlayerRole(playerRole.CurrentRole);
            }
            foreach (var mesh in m_boardMeshes) { mesh.enabled = true; }
            m_animator.SetBool(m_animatorParameter, true);
        }

        private void SetTextForPlayerRole(Role playerRole) => SetInstructionsText(playerRole != Role.Mole ? m_crewInstructions : m_moleInstructions);

        /**
         * Hide this instructional board from view.
         */
        public void HideBoard()
        {
            if (IsOwner) { m_isDisplaying.Value = false; }
            m_isDisplayingOffline = false;
            m_animator.SetBool(m_animatorParameter, false);
            _ = StartCoroutine(WaitToHideText());
        }

        private IEnumerator WaitToHideText()
        {
            var stateInfo = m_animator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(stateInfo.length);

            if (m_isDisplaying.Value) { yield break; }

            foreach (var mesh in m_boardMeshes) { mesh.enabled = false; }
            m_instructionalText.gameObject.SetActive(false);
            m_boardVisualsObject.SetActive(false);
        }

        private NetworkObject GetPlayerAtMiniGameSpawn() => m_relevantGamePosition == null ? null : m_relevantGamePosition.OccupyingPlayer;
    }
}
