// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Multiplayer.PlayerManagement;
using Unity.Netcode;

namespace Meta.Multiplayer.Networking
{
    public static class NetworkObjectExtension
    {
        public static PlayerId? GetOwnerPlayerId(this NetworkObject networkObject)
        {
            var instance = PlayerManager.Instance;
            return instance != null ? instance.GetPlayerIdByClientId(networkObject.OwnerClientId) : null;
        }

        public static void ChangeOwnership(this NetworkObject networkObject, PlayerId playerId)
        {
            networkObject.ChangeOwnership(playerId.Object.OwnerClientId);
        }
    }
}
