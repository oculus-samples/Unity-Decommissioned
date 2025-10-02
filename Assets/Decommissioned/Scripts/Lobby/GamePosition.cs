// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Decommissioned.Interactables;
using Meta.Decommissioned.Occlusion;
using Meta.Decommissioned.Player;
using Meta.Decommissioned.ScriptableObjects;
using Meta.Multiplayer.Core;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.XR.Samples;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Lobby
{
    /// <summary>
    /// Class representing a specific position in the game that may or may not be currently occupied by a player during the
    /// game. Players are moved between these positions by a <see cref="LocationManager"/>.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class GamePosition : NetworkBehaviour
    {
        [field: Header("Game Location Options")]
        [field: SerializeField]
        public MiniGameRoom MiniGameRoom { get; private set; }

        [field: SerializeField] public int PositionIndex { get; private set; }

        [SerializeField] private PlayerAssignmentInterface m_playerAssignment;

        [SerializeField] private MiniGameAssignmentButton[] m_assignmentButtons = new MiniGameAssignmentButton[5];

        [SerializeField] private Transform m_playerAssignmentPosition;
        public PlayerAssignmentDoor PlayerAssignmentDoor;

        [field: SerializeField] public PlayerColorConfig.GameColor PositionColor { get; private set; }

        [ShowNativeProperty] public bool IsOccupied => OccupyingPlayer != null;

        [ShowNativeProperty]
        public NetworkObject OccupyingPlayer =>
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening &&
            m_occupyingPlayer.Value.TryGet(out var player)
                ? player
                : null;

        public PlayerId? OccupyingPlayerId => OccupyingPlayer.GetOwnerPlayerId();

        private readonly NetworkVariable<NetworkObjectReference> m_occupyingPlayer = new();

        [field: SerializeField] public bool IsInitialSpawnPoint { get; private set; }

        public event Action<NetworkObject, NetworkObject> OnOccupyingPlayerChanged;

        private static LocationManager LocationManager => LocationManager.Instance;

        public void Awake()
        {
            _ = StartCoroutine(StartupRoutine());

            IEnumerator StartupRoutine()
            {
                yield return new WaitUntil(() => NetworkManager.Singleton != null);
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectServerRpc;
                CommanderCandidateManager.Instance.OnNewCommander += OnNewCommander;
            }
        }

        public override void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectServerRpc;
            }

            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            m_occupyingPlayer.OnValueChanged += OnOccupyingPlayerHasChanged;
        }

        /// <summary>
        /// When a new commander is set, we ensure that all related components are in the proper state depending on whether
        /// or not the Commander is currently occupying this position.
        /// </summary>
        private void OnNewCommander(PlayerId player)
        {
            if (!IsOccupied)
            {
                return;
            }

            var positionOwner = OccupyingPlayer.GetOwnerPlayerId();

            if (m_playerAssignment == null || positionOwner != player)
            {
                return;
            }

            m_playerAssignment.transform.SetPositionAndRotation(m_playerAssignmentPosition.position,
                m_playerAssignmentPosition.rotation);

            if (m_playerAssignment.LocationButtons.Count > 0)
            {
                m_playerAssignment.DisableStationButtons();
            }

            m_playerAssignment.LocationButtons.Clear();
            m_playerAssignment.LocationButtons.AddRange(m_assignmentButtons);
            m_playerAssignment.AssignmentDoor = PlayerAssignmentDoor;
            m_playerAssignment.SetInterfaceActive(positionOwner == player, player);
        }

        public void OnOccupyingPlayerHasChanged(NetworkObjectReference previousPlayer, NetworkObjectReference newPlayer)
        {
            _ = previousPlayer.TryGet(out var oldPlayer);
            _ = newPlayer.TryGet(out var player);

            if (!IsOccupied)
            {
                OnOccupyingPlayerChanged?.Invoke(oldPlayer, default);
                return;
            }

            // If this player is the local one, we'll want to set their camera and occlusion based on their new position
            if (player.IsLocalPlayer)
            {
                if (player.TryGetComponent(out ClientNetworkTransform clientNetworkTransform))
                {
                    clientNetworkTransform.Teleport(transform.position, Quaternion.LookRotation(transform.forward, Vector3.up),
                        Vector3.one);
                }

                PlayerCamera.Instance.Refocus();
                RoomOcclusionZoneManager.Instance.ApplyOcclusion(MiniGameRoom);
            }

            OnOccupyingPlayerChanged?.Invoke(oldPlayer, player);
            LocationManager.InvokeOnPlayerJoinedRoom(player, MiniGameRoom);
        }

        [ServerRpc(RequireOwnership = false)]
        private void OnClientDisconnectServerRpc(ulong clientId)
        {
            if (!IsOccupied)
            {
                return;
            }

            if (OccupyingPlayer.OwnerClientId == clientId)
            {
                ClearPosition();
            }
        }

        public void OccupyPosition(NetworkObject playerObject)
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                SubmitMarkOccupiedServerRpc(playerObject);
            }
            else
            {
                m_occupyingPlayer.Value = playerObject;
            }

            if (PositionColor == PlayerColorConfig.GameColor.None)
            {
                return;
            }

            var playerId = playerObject.GetOwnerPlayerId();

            if (playerId == null)
            {
                Debug.LogError($"Could not retrieve PlayerId from a player at position {PositionIndex}", this);
                return;
            }

            PlayerColor.GetByPlayerId(playerId.Value).SetPlayerColor(PositionColor);
        }

        public void ClearPosition()
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                SubmitMarkUnoccupiedServerRpc();
                return;
            }
            m_occupyingPlayer.Value = default;
        }

        [ServerRpc(RequireOwnership = false)]
        private void SubmitMarkOccupiedServerRpc(NetworkObjectReference playerObject)
        {
            m_occupyingPlayer.Value = playerObject;
        }

        [ServerRpc(RequireOwnership = false)]
        private void SubmitMarkUnoccupiedServerRpc()
        {
            m_occupyingPlayer.Value = default;
        }

        [ContextMenu("Occupy with local player")]
        private void Debug__OccupyWithLocalPlayer()
        {
            _ = LocationManager.TeleportPlayer(PlayerManager.LocalPlayerId.Object, MiniGameRoom);
        }
    }
}
