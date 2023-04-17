// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Utilities;
using Netcode.Transports.PhotonRealtime;
using Photon.Realtime;
using UnityEngine;

namespace Meta.Decommissioned
{
    /// <summary>
    /// Implements functions used on Photon connection. Setting the right room options based on the application state.
    /// Exposes room properties for player slots open and spectator slots open.
    /// </summary>
    public class PhotonConnectionHandler : MonoBehaviour
    {
        [SerializeField, AutoSet] private PhotonRealtimeTransport m_photonRealtimeTransport;

        private void Start()
        {
            m_photonRealtimeTransport.GetHostRoomOptionsFunc += GetHostRoomOptions;
        }

        private void OnDestroy()
        {
            m_photonRealtimeTransport.GetHostRoomOptionsFunc -= GetHostRoomOptions;
        }

        private RoomOptions GetHostRoomOptions(bool usePrivateRoom, byte maxPlayers) => new()
        {
            MaxPlayers = maxPlayers,
            IsVisible = !usePrivateRoom,
        };
    }
}
