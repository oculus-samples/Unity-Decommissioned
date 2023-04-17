// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Lobby;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    /**
     * Class for managing ownership of an object based on the player currently occupying a specific position.
     * */
    public class MiniGameObjectOwnershipManager : NetworkBehaviour
    {
        [Tooltip("The position that will be tracked. When the player at this position changes, so does the ownership of this object.")]
        [SerializeField] private GamePosition m_trackedGamePosition;

        // Start is called before the first frame update
        private void Awake()
        {
            if (m_trackedGamePosition == null)
            {
                Debug.LogWarning($"{gameObject.name}: Tracked game position is null! Cannot manage ownership.", this);
                return;
            }

            m_trackedGamePosition.OnOccupyingPlayerChanged += OnPositionPlayerChanged;
        }

        private void OnPositionPlayerChanged(NetworkObject previousPlayer, NetworkObject player)
        {
            if (!IsServer || m_trackedGamePosition == null) { return; }

            if (player == null)
            {
                NetworkObject.ChangeOwnership(PlayerId.ServerPlayerId());
                return;
            }

            NetworkObject.ChangeOwnership(player.GetOwnerPlayerId() ?? PlayerId.ServerPlayerId());

        }
    }
}
