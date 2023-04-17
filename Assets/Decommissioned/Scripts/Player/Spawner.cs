// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Player
{
    public class Spawner : MonoBehaviour
    {
        public NetworkObject PlayerPrefab;
        public NetworkObject SessionPrefab;

        private void OnEnable()
        {
            DontDestroyOnLoad(this);
        }

        public NetworkObject SpawnPlayer(ulong clientId, Vector3 position, Quaternion rotation)
        {
            var player = Instantiate(PlayerPrefab, position, rotation);
            if (NetworkManager.Singleton.IsListening)
                player.SpawnAsPlayerObject(clientId);
            return player;
        }

        public NetworkObject SpawnSession()
        {
            var session = Instantiate(SessionPrefab);
            session.Spawn();
            return session;
        }
    }
}
