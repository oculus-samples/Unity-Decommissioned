// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Unity.Netcode;

namespace Meta.Multiplayer.Networking
{
    public struct NetworkReference<T> : INetworkSerializable, IEquatable<NetworkBehaviourReference>, IEquatable<NetworkReference<T>>
        where T : NetworkBehaviour
    {
        private NetworkBehaviourReference m_reference;

        public static implicit operator NetworkReference<T>(T behaviour) => new()
        {
            m_reference = behaviour != null ? new(behaviour) : default
        };

        public bool TryGet(out T behaviour, NetworkManager manager = null)
        {
            if (manager == null)
                manager = NetworkManager.Singleton;

            if (manager != null && manager.SpawnManager?.SpawnedObjects != null)
                return m_reference.TryGet(out behaviour, manager);

            behaviour = null;
            return false;
        }

        public static implicit operator T(NetworkReference<T> reference) => reference.TryGet(out var b) ? b : null;
        public T Value => TryGet(out var b) ? b : null;

        public void NetworkSerialize<U>(BufferSerializer<U> serializer) where U : IReaderWriter => m_reference.NetworkSerialize(serializer);
        public bool Equals(NetworkBehaviourReference other) => m_reference.Equals(other);
        public bool Equals(NetworkReference<T> other) => m_reference.Equals(other.m_reference);
    }
}
