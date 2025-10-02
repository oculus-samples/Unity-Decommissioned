// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections;
using Meta.Decommissioned.Interactables;
using Meta.Decommissioned.Occlusion;
using Meta.Decommissioned.Player;
using Meta.Decommissioned.Surveillance;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.XR.Samples;
using ScriptableObjectArchitecture;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Game.MiniGames
{
    /**
     * Class for managing data and input/output for the Commander station.
     */
    [MetaCodeSample("Decommissioned")]
    public class CommanderStation : NetworkBehaviour
    {
        private NetworkVariable<bool> m_isHelping = new(true);
        private NetworkVariable<MiniGameRoom> m_currentMiniGameRoom = new(MiniGameRoom.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public bool IsHelping => m_isHelping.Value;
        public MiniGameRoom CurrentlySelectedRoom => m_currentMiniGameRoom.Value;

        [SerializeField] private RoomSelectButton[] m_roomSelectButtons = Array.Empty<RoomSelectButton>();
        [SerializeField] private int m_healthDecreaseAmount = 5;
        [SerializeField] private int m_healthIncreaseAmount = 5;
        [SerializeField] private MiniGame m_commanderMiniGame;
        [SerializeField] private IntGameEvent m_roomUpdatedEvent;
        [SerializeField] private UnityEvent<int> m_onHelpMiniGame;
        [SerializeField] private UnityEvent<int> m_onHurtMiniGame;
        [SerializeField] private UnityEvent<bool> m_onMiniGameHealthCapReached;

        private NetworkVariable<bool> m_interactionEnabled = new();


        private void OnEnable()
        {
            LocationManager.Instance.OnPlayerJoinedRoom += OnPlayerJoinedRoom;
            m_currentMiniGameRoom.OnValueChanged += OnMiniGameRoomChanged;
        }

        private void OnDisable() => LocationManager.Instance.OnPlayerJoinedRoom -= OnPlayerJoinedRoom;

        private void OnMiniGameRoomChanged(MiniGameRoom previousvalue, MiniGameRoom newvalue)
        {
            m_roomUpdatedEvent.Raise((int)newvalue);

            foreach (var button in m_roomSelectButtons)
                button.SetActive(newvalue == button.MiniGameRoom);
        }

        private void OnPlayerJoinedRoom(NetworkObject player, MiniGameRoom room)
        {
            if (room != MiniGameRoom.Commander) { return; }

            if (IsServer)
            {
                NetworkObject.ChangeOwnership(player.GetOwnerPlayerId() ?? PlayerId.ServerPlayerId());
                var playerRole = PlayerRole.GetByPlayerObject(player);
                var playerIsCrew = playerRole.CurrentRole == Role.Crewmate;
                SetCompletionAction(playerIsCrew);
            }

            if (player.IsLocalPlayer) { PopulateMiniGameRoomList(); }
        }

        private void PopulateMiniGameRoomList()
        {
            foreach (var button in m_roomSelectButtons)
            {
                button.gameObject.SetActive(true);
                button.OnPress = () =>
                {
                    if (this != null)
                        m_currentMiniGameRoom.Value = button.MiniGameRoom;
                };

                button.SetActive(m_currentMiniGameRoom.Value == button.MiniGameRoom);
            }

            _ = StartCoroutine(ActivateButton());

            IEnumerator ActivateButton()
            {
                yield return new WaitUntil(() => NetworkObject.IsOwner);
                m_currentMiniGameRoom.Value = m_roomSelectButtons[0].MiniGameRoom;
                SurveillanceSystem.Instance.SwitchToRoom_ServerRpc(m_roomSelectButtons[0].MiniGameRoom);
                m_roomSelectButtons[0].SetActive(true);
                UnoccludeCurrentlyViewedRoom();
            }
        }

        public void OnCommanderMiniGameCompleted(MiniGame miniGame)
        {
            if (miniGame != m_commanderMiniGame) { return; }

            if (!m_isHelping.Value) { HurtSelectedMiniGameServerRpc(); }
            else { HelpSelectedMiniGameRoomServerRpc(); }
        }

        /**
         * Toggle the player's ability to heal or hurt other stations using the Commander station's keyboard.
         */
        public void SetHackingEnabled(bool hackingEnabled)
        {
            if (!IsHost) { return; }
            m_interactionEnabled.Value = hackingEnabled;
        }

        [ContextMenu("Damage Selected MiniGame")]
        [ServerRpc(RequireOwnership = false)]
        public void HurtSelectedMiniGameServerRpc()
        {
            if (!m_interactionEnabled.Value) { return; }
            var currentRoom = GetCurrentMiniGameRoom();
            var miniGamesInRoom = MiniGameManager.Instance.GetAllMiniGamesInRoom(currentRoom);
            foreach (var miniGame in miniGamesInRoom)
            {
                miniGame.DecreaseHealth(m_healthDecreaseAmount);
                if (miniGame.GetHealthChange() >= miniGame.HealthDecreaseCap) { OnMiniGameCapReachedClientRpc(false); }
                else { OnMiniGameHurtClientRpc(); }
            }
        }

        [ContextMenu("Heal Selected MiniGame")]
        [ServerRpc(RequireOwnership = false)]
        public void HelpSelectedMiniGameRoomServerRpc()
        {
            if (!m_interactionEnabled.Value) { return; }
            var currentRoom = GetCurrentMiniGameRoom();
            var miniGamesInRoom = MiniGameManager.Instance.GetAllMiniGamesInRoom(currentRoom);
            foreach (var miniGame in miniGamesInRoom)
            {
                miniGame.IncreaseHealth(m_healthIncreaseAmount);
                if (miniGame.CurrentHealth >= miniGame.HealthAtRoundStart) { OnMiniGameCapReachedClientRpc(); }
                else { OnMiniGameHelpedClientRpc(); }
            }
        }

        [ClientRpc]
        private void OnMiniGameHelpedClientRpc() => m_onHelpMiniGame.Invoke(m_healthIncreaseAmount);

        [ClientRpc]
        private void OnMiniGameHurtClientRpc() => m_onHurtMiniGame.Invoke(-m_healthDecreaseAmount);

        [ClientRpc]
        private void OnMiniGameCapReachedClientRpc(bool isHealing = true) => m_onMiniGameHealthCapReached.Invoke(isHealing);

        private void SetCompletionAction(bool isHelping)
        {
            if (!IsServer) { return; }
            m_isHelping.Value = isHelping;
        }

        private void UnoccludeCurrentlyViewedRoom()
        {
            var currentRoom = RoomOcclusionZoneManager.Instance.GetRoomZone(m_currentMiniGameRoom.Value);
            currentRoom.UnOccludeZone();
        }

        private MiniGameRoom GetCurrentMiniGameRoom() => m_currentMiniGameRoom.Value;
    }
}
