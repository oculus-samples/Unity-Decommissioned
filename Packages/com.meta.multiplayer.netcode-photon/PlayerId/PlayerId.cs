// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Unity.Collections;
using Unity.Netcode;

namespace Meta.Multiplayer.PlayerManagement
{
    [Serializable]
    public struct PlayerId : INetworkSerializeByMemcpy, IEquatable<PlayerId>
    {
        private FixedString64Bytes m_playerIdSerializable;

        public PlayerId(string id) => m_playerIdSerializable = id is null ? default : new(id);

        public static PlayerId New() => new(Guid.NewGuid().ToString());

        public static PlayerId ServerPlayerId() =>
            PlayerManager.Instance.GetPlayerIdByClientId(NetworkManager.ServerClientId) ?? New();

        public NetworkObject Object => PlayerObject != null ? PlayerObject.NetworkObject : null;

        public PlayerObject PlayerObject => PlayerObject.GetByPlayerId(this);
        public ulong? ClientId => Object != null ? Object.OwnerClientId : null;

        public bool Equals(PlayerId other) => other.m_playerIdSerializable == m_playerIdSerializable;
        public static bool operator ==(PlayerId a, PlayerId b) => a.m_playerIdSerializable == b.m_playerIdSerializable;
        public static bool operator !=(PlayerId a, PlayerId b) => a.m_playerIdSerializable != b.m_playerIdSerializable;

        public bool Exists() => this != default;

        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => m_playerIdSerializable.GetHashCode();
        public override string ToString() => m_playerIdSerializable.ToString();
    }
}
