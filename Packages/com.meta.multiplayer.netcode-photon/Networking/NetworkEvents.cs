// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Multiplayer.Networking
{
    public class NetworkEvents : NetworkBehaviour
    {
        [SerializeField] private UnityEvent m_onHostNetworkSpawn;
        [SerializeField] private UnityEvent m_onClientNetworkSpawn;
        [SerializeField] private UnityEvent<ulong> m_onClientConnected;

        private void OnEnable()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsHost)
                m_onHostNetworkSpawn?.Invoke();
            else
                m_onClientNetworkSpawn?.Invoke();
        }

        private void OnClientConnected(ulong id) => m_onClientConnected.Invoke(id);
    }
}
