// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Player
{
    /// <summary>
    /// Class for managing a specific player's role; this allows us to determine what "team" players are on
    /// during the game and change the behavior or appearance of other components accordingly.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class PlayerRole : NetworkMultiton<PlayerRole>
    {
        [SerializeField] private NetworkVariable<Role> m_currentRole = new(Role.Unknown);

        public event Action<Role> OnCurrentRoleChanged;

        public Role CurrentRole { get => m_currentRole.Value; set => m_currentRole.Value = value; }

        public static PlayerRole GetByPlayerId(PlayerId id) => Instances.FirstOrDefault(p => p.NetworkObject.GetOwnerPlayerId() == id);
        public static PlayerRole GetByPlayerObject(NetworkObject player) => Instances.FirstOrDefault(p => p.NetworkObject == player);

        private new void Awake()
        {
            base.Awake();
            m_currentRole.OnValueChanged += OnRoleChanged;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            OnRoleChanged(default, m_currentRole.Value);
        }

        public override void OnNetworkDespawn()
        {
            m_currentRole.OnValueChanged -= OnRoleChanged;
            base.OnNetworkDespawn();
        }

        private void OnRoleChanged(Role previousRole, Role newRole)
        {
            if (!IsLocalPlayer) { return; }
            OnCurrentRoleChanged?.Invoke(newRole);
        }
    }
}
