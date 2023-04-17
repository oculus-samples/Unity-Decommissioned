// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Linq;
using Meta.Multiplayer.Networking;
using Unity.Collections;
using Unity.Netcode;

namespace Meta.Multiplayer.PlayerManagement
{
    public class PlayerObject : NetworkMultiton<PlayerObject>
    {
        private NetworkVariable<ForceNetworkSerializeByMemcpy<FixedString128Bytes>> m_username =
            new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<PlayerId> m_playerId =
            new(writePerm: NetworkVariableWritePermission.Owner);

        public string Username
        {
            get => m_username.Value.Value.ToString();
            set => m_username.Value = new(value);
        }

        public PlayerId PlayerId
        {
            get => m_playerId.Value;
            set => m_playerId.Value = value;
        }

        public static PlayerObject GetByPlayerId(PlayerId id) => Instances.FirstOrDefault(p => p.PlayerId == id);
        public static PlayerObject GetByClientId(ulong id) => Instances.FirstOrDefault(p => p.OwnerClientId == id);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                PlayerId = PlayerManager.LocalPlayerId;
                Username = PlayerManager.LocalUsername;
            }
        }
    }
}
