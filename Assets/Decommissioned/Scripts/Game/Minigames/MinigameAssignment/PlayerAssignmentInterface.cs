// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Decommissioned.Interactables;
using Meta.Decommissioned.Lobby;
using Meta.Decommissioned.Player;
using Meta.Decommissioned.UI;
using Meta.Decommissioned.Utils;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    /// <summary>
    /// Class representing an object for controlling player assignments. Through this component, we provide a user interface
    /// through which the Commander can assign players to <see cref="MiniGame"/>s.
    /// </summary>
    public class PlayerAssignmentInterface : MonoBehaviour
    {
        public Animator Animator;

        public List<PlayerSelectButton> PlayerButtons = new();
        public List<MiniGameAssignmentButton> LocationButtons = new();
        [SerializeField] private GameObject m_helpButton;
        [SerializeField] private InstructionalBoard m_helpBoard;
        public PlayerAssignmentDoor AssignmentDoor;

        private MiniGameAssignmentButton m_miniGameAssignmentButtonSelected;
        private PlayerId m_commanderID;
        private YieldInstruction m_deployWaitTime = new WaitForSeconds(1);

        private void Awake()
        {
            _ = StartCoroutine(LoadPlayersOnSessionManager());

            IEnumerator LoadPlayersOnSessionManager()
            {
                yield return new WaitUntil(() => PlayerManager.Instance != null);
                PlayerManager.Instance.OnPlayerEnter += OnPlayerJoin;
                PlayerManager.Instance.OnPlayerExit += OnPlayerQuit;
                yield return new WaitUntil(() => GameManager.Instance != null);
                CommanderCandidateManager.Instance.OnNewCommander += OnNewCommander;
                GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChange;
            }
        }

        private void OnEnable()
        {
            PhaseSpawnManager.Instance.OnAssignmentsUpdated += OnAssignmentsUpdated;
            DisableStationButtons();
        }

        private void OnDisable() => PhaseSpawnManager.Instance.OnAssignmentsUpdated -= OnAssignmentsUpdated;

        private void OnAssignmentsUpdated()
        {
            foreach (var button in LocationButtons)
            {
                var miniGameRoom = button.AssignedMiniGame.SpawnPoint.MiniGameRoom;
                var players = PhaseSpawnManager.Instance.GetPlayersAssignedToRoom(miniGameRoom).
                    Select(p => p.NetworkObject.GetOwnerPlayerId()).
                    WhereNonNull();
                using var buttonPlayers = button.PositionLights.Select(l => l.Player).WhereNonDefault()
                    .ToTempArray(button.PositionLights.Length);

                if (buttonPlayers.Except(players).Any())
                {
                    // if the buttons have any players selected that aren't in the list, just reset
                    button.ClearAssignments();
                    foreach (var player in players) { button.AssignPlayer(player); }
                }
                else
                {
                    // otherwise, just assign the new players
                    foreach (var player in players.Except(buttonPlayers)) { button.AssignPlayer(player); }
                }
            }

            SetPlayerNameBorderStates();
        }

        private void OnPlayerJoin(PlayerId playerId)
        {
            //Using a coroutine here ensures that the player's nametag loads in time
            _ = StartCoroutine(LoadPlayersAfterJoin());

            IEnumerator LoadPlayersAfterJoin()
            {
                yield return new WaitUntil(() => PlayerManager.Instance.AllPlayerIds.Contains(playerId));
                LoadPlayers();
            }
        }

        private void OnPlayerQuit(PlayerId playerId)
        {
            var localPlayerId = PlayerManager.LocalPlayerId;
            if (localPlayerId == m_commanderID)
            {
                _ = StartCoroutine(LoadPlayersAfterQuit());

                IEnumerator LoadPlayersAfterQuit()
                {
                    // allows their nametag time to unload
                    yield return new WaitUntil(() => !PlayerManager.Instance.AllPlayerIds.Contains(playerId));
                    LoadPlayers(localPlayerId);
                }
            }
        }

        private void OnNewCommander(PlayerId playerId)
        {
            m_commanderID = playerId;
            if (playerId != PlayerManager.LocalPlayerId)
            {
                foreach (var button in PlayerButtons) { button.transform.parent.gameObject.SetActive(false); }

                DisableHelpMenu();

                SetPlayerNameBorderStates();
            }
        }

        private void OnPhaseChange(Phase phase)
        {
            switch (phase)
            {
                case Phase.Voting:
                    PhaseSpawnManager.Instance.ClearAssignments_ServerRpc();
                    return;

                case Phase.Night:
                    {
                        ClearHighlightPhaseMaterial();
                        if (NetworkManager.Singleton.IsServer) { PhaseSpawnManager.Instance.FillInAssignments(); }
                        foreach (var button in LocationButtons) { button.ClearAssignments(); }
                        RetractInterface();
                        Animator.gameObject.SetActive(false);

                        DisableHelpMenu();

                        foreach (var locationButton in LocationButtons) { locationButton.IsPushable = false; }

                        return;
                    }

                case Phase.Planning:
                    return;
                case Phase.Invalid:
                    break;
                case Phase.Discussion:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Show the assignment interface and prepare it for player interaction.
        /// </summary>
        /// <param name="active">Determines whether or not the interface will be activated.</param>
        /// <param name="commander">The PlayerId of the player assigned as Commander.</param>
        private void DisableHelpMenu()
        {
            if (m_helpBoard && m_helpButton)
            {
                if (m_helpBoard.IsDisplaying) { m_helpBoard.ToggleBoardVisibility(); }

                m_helpButton.SetActive(false);
            }
        }


        /// <summary>
        /// Show the assignment interface and prepare it for player interaction.
        /// </summary>
        /// <param name="active">Determines whether or not the interface will be activated.</param>
        /// <param name="commander">The PlayerId of the player assigned as Commander.</param>
        public void SetInterfaceActive(bool active, PlayerId commander)
        {
            Animator.gameObject.SetActive(active);
            if (active)
            {
                DeployInterface();
                if (commander == PlayerManager.LocalPlayerId)
                {
                    LoadPlayers(commander);
                    if (m_helpButton) { m_helpButton.SetActive(true); }

                    PhaseSpawnManager.Instance.SetNightSpawnRoom_ServerRpc(PlayerStatus.GetByPlayerId(commander), MiniGameRoom.Commander);
                    EnableStationButtons();
                    return;
                }
            }

            DisableStationButtons();
        }

        private void LoadPlayers(PlayerId commander = default)
        {
            foreach (var button in PlayerButtons)
            {
                button.PlayerId = default;
                button.transform.parent.gameObject.SetActive(false);
            }

            var index = 0;

            foreach (var player in PlayerManager.Instance.AllPlayerObjects)
            {
                var ownerId = player.GetOwnerPlayerId();
                if (ownerId.HasValue && ownerId.Value != commander)
                {
                    PlayerButtons[index].transform.parent.gameObject.SetActive(true);
                    var playerName = player.GetOwnerPlayerId().Value.PlayerObject.Username;
                    PlayerButtons[index].TextRenderer.text = NameTruncator.TruncateName(playerName);
                    PlayerButtons[index].PlayerId = ownerId.Value;
                    var playerGameColor = PlayerColor.GetByPlayerId(ownerId.Value);
                    var emissionColor = playerGameColor.MultiplyColorFromGameColor(1.5f);
                    PlayerButtons[index].SetButtonColor(emissionColor);
                }

                index++;
            }
        }

        private void DeployInterface()
        {
            if (AssignmentDoor != null)
            {
                _ = StartCoroutine(DeployInterfaceDelay());
                AssignmentDoor.OpenDoor();
            }
            else { Animator.Play("Deploy"); }
        }

        private IEnumerator DeployInterfaceDelay()
        {
            yield return m_deployWaitTime;
            Animator.Play("Deploy");
        }

        private void RetractInterface()
        {
            Animator.Play("Retract");
            _ = StartCoroutine(RetractInterfaceDelay());
        }

        private IEnumerator RetractInterfaceDelay()
        {
            yield return m_deployWaitTime;
            if (AssignmentDoor != null) { AssignmentDoor.CloseDoor(); }
        }

        /// <summary>
        /// Execute behavior upon Commander pressing one of the buttons associated with a MiniGame.
        /// </summary>
        /// <param name="selectedButton">The button the Commander just pressed associated with a specific Mini Game Room.</param>
        public void OnAssignmentButtonSelect(MiniGameAssignmentButton selectedButton)
        {
            if (!selectedButton.IsPushable)
            {
                SetButtonHighlights();
                return;
            }

            if (m_miniGameAssignmentButtonSelected != null)
            {
                m_miniGameAssignmentButtonSelected.SetIsSelected(false);
                m_miniGameAssignmentButtonSelected.SetHighlightPhaseMaterial();
            }

            var miniGameSelected = selectedButton.AssignedMiniGame;
            if (miniGameSelected.CurrentHealth == 0) { return; }

            m_miniGameAssignmentButtonSelected = selectedButton;
            SetButtonHighlights(m_miniGameAssignmentButtonSelected);
            selectedButton.IsPushable = true;
        }


        /// <summary>
        /// Execute behavior upon Commander pressing one of the buttons associated with a player.
        /// </summary>
        /// <param name="selectedButton">The button the Commander just pressed associated with a specific player.</param>
        public void OnPlayerSelected(PlayerSelectButton selectedButton)
        {
            if (m_miniGameAssignmentButtonSelected == null) { return; }

            //Unassign players from old buttons if re-assigned through this method.
            foreach (var locationButton in LocationButtons)
            {
                locationButton.ClearAssignments(selectedButton.PlayerId);
            }

            m_miniGameAssignmentButtonSelected.AssignPlayer(selectedButton.PlayerId);

            PhaseSpawnManager.Instance.SetNightSpawnRoom_ServerRpc(PlayerStatus.GetByPlayerId(selectedButton.PlayerId),
                m_miniGameAssignmentButtonSelected.AssignedMiniGame.SpawnPoint.MiniGameRoom);

            var anyPlayerPositionsLeft = m_miniGameAssignmentButtonSelected.PositionLights.Any(x => x.Player == default);
            SetPlayerNameBorderStates();

            if (anyPlayerPositionsLeft)
            {
                m_miniGameAssignmentButtonSelected.IsPushable = true;
                m_miniGameAssignmentButtonSelected.SetIsSelected(true);
                SetButtonHighlights(m_miniGameAssignmentButtonSelected);
                return;
            }

            EnableStationButtons();
            SetButtonHighlights();
            m_miniGameAssignmentButtonSelected = null;
        }

        private void SetPlayerNameBorderStates()
        {
            foreach (var button in PlayerButtons)
            {
                button.SetBorderHighlightState(PhaseSpawnManager.Instance.AssignedPlayers.Contains(PlayerStatus.GetByPlayerId(button.PlayerId)));
            }
        }

        /// <summary>
        /// Set which buttons will be "flashing" at the moment or not.
        /// </summary>
        /// <param name="selectedButton">The button the player has currently selected.</param>
        private void SetButtonHighlights(MiniGameAssignmentButton selectedButton = null)
        {
            foreach (var button in LocationButtons)
            {
                if (selectedButton != null && selectedButton == button)
                {
                    button.SetHighlightMaterial();
                    button.RemoveHighlightPhaseMaterial();
                }
                else { button.RemoveHighlightMaterial(); }
            }
        }

        private void EnableStationButtons()
        {
            foreach (var locationButton in LocationButtons)
            {
                var deadButton = locationButton.AssignedMiniGame != null &&
                    locationButton.AssignedMiniGame.CurrentHealth == 0;
                locationButton.IsPushable = !deadButton;
                locationButton.SetIsSelected(false);
                locationButton.SetHighlightPhaseMaterial();
            }
        }

        private void ClearHighlightPhaseMaterial()
        {
            foreach (var locationButton in LocationButtons)
            {
                locationButton.RemoveHighlightPhaseMaterial();
            }
        }

        public void DisableStationButtons()
        {
            foreach (var locationButton in LocationButtons)
            {
                locationButton.IsPushable = false;
                locationButton.SetIsSelected(false);
                locationButton.RemoveHighlightPhaseMaterial();
            }
        }
    }
}
