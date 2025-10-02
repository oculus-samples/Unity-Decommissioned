// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Decommissioned.Player;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Audio
{
    [MetaCodeSample("Decommissioned")]
    public class PlayerAudioManager : MonoBehaviour
    {
        public bool EnableLogging;

        protected void Awake()
        {
            LocationManager.WhenInstantiated(man => man.OnPlayerJoinedRoom += OnPlayerJoinedRoom);
        }

        private void OnPlayerJoinedRoom(NetworkObject player, MiniGameRoom room)
        {
            UpdateAllPlayerMute();
        }

        private void UpdateAllPlayerMute()
        {
            var localRoom = GetRoomByPlayer(PlayerManager.LocalPlayerId.Object);
            foreach (var voip in PlayerVoip.Instances)
            {
                var player = voip.NetworkObject;
                if (player.IsLocalPlayer is false)
                {
                    var mute = localRoom != GetRoomByPlayer(player);
                    if (EnableLogging)
                    {
                        Debug.Log($"{player.GetOwnerPlayerId()} mute set to {mute}", voip);
                    }
                    voip.SetMuted(mute);
                }
            }
        }

        private static MiniGameRoom? GetRoomByPlayer(NetworkObject player)
        {
            var localPosition = LocationManager.Instance.GetGamePositionByPlayer(player);
            return localPosition != null ? localPosition.MiniGameRoom : null;
        }
    }
}
