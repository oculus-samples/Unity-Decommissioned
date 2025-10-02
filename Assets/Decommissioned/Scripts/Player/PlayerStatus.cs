// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Decommissioned.Lobby;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Player
{
    /// <summary>
    /// PlayerStatus allows us to access the player's current "sub-role" in the game; this determines their role and
    /// unique tasks during the night phase.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class PlayerStatus : NetworkMultiton<PlayerStatus>
    {
        public enum Status
        {
            None,
            Commander
        }

        public bool EnableLogging;

        [Serializable]
        public struct StatusData
        {
            public UnityEvent OnApply;
            public UnityEvent OnRemove;
        }

        public struct NightRoomSpawn : INetworkSerializeByMemcpy
        {
            public MiniGameRoom MiniGameRoom;
            public int SpawnIndex;

            public NightRoomSpawn(MiniGameRoom miniGameRoom, int spawnIndex)
            {
                MiniGameRoom = miniGameRoom;
                SpawnIndex = spawnIndex;
            }
        }

        protected NetworkVariable<Status> m_currentStatus = new();
        protected NetworkVariable<NightRoomSpawn> m_nextNightRoom = new();


        public Status CurrentStatus { get => m_currentStatus.Value; set => m_currentStatus.Value = value; }
        public GamePosition CurrentGamePosition => LocationManager.Instance.GetGamePositionByPlayer(NetworkObject);

        public NightRoomSpawn NextNightRoom
        {
            get => m_nextNightRoom.Value;
            set => m_nextNightRoom.Value = value;
        }

        public static PlayerStatus GetByPlayerId(PlayerId id) => Instances.FirstOrDefault(p => p.NetworkObject.GetOwnerPlayerId().Value == id);
        public static PlayerStatus GetByPlayerObject(NetworkObject player) => Instances.FirstOrDefault(p => p.gameObject == player.gameObject);

        protected new void Awake()
        {
            m_currentStatus.OnValueChanged += OnStatusChanged;
            m_nextNightRoom.OnValueChanged += OnNextNightRoomChanged;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            OnStatusChanged(default, m_currentStatus.Value);
        }

        [SerializeField] private EnumDictionary<Status, StatusData> m_statusDatas = new();

        protected void OnStatusChanged(Status previousValue, Status newValue)
        {
            if (EnableLogging)
            {
                Debug.Log($"[PlayerStatus] {this} status changed to {newValue}", this);
            }
            m_statusDatas[previousValue].OnRemove?.Invoke();
            m_statusDatas[newValue].OnApply?.Invoke();
        }


        private void OnNextNightRoomChanged(NightRoomSpawn previousValue, NightRoomSpawn newValue)
        {
            Debug.Log($"[PlayerStatus] {this} next work room changed to {(newValue.MiniGameRoom, newValue.SpawnIndex)}", this);
            PhaseSpawnManager.Instance.OnNightRoomsChanged();
        }

        /**
         * This method defines behavior for when the game ends -- reset status to default.
         */
        public void OnGameEnd()
        {
            if (this == null || !IsServer) { return; }
            m_currentStatus.Value = default;
        }

#if UNITY_EDITOR
        [ContextMenu("TEST: move to room None")]
        protected void MoveToRoomNone()
        {
            _ = LocationManager.Instance.TeleportPlayer(NetworkObject, MiniGameRoom.None);
        }

        [ContextMenu("TEST: move to room Workbench1")]
        protected void MoveToRoom1()
        {
            _ = LocationManager.Instance.TeleportPlayer(NetworkObject, MiniGameRoom.Science);
        }
#endif
    }
}
