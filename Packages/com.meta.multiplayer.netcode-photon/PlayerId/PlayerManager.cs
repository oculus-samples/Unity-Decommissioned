// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Multiplayer.Networking;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Multiplayer.PlayerManagement
{
    public class PlayerManager : NetworkSingleton<PlayerManager>
    {
        public static PlayerId LocalPlayerId { get; private set; }
        public static string LocalUsername { get; private set; }

        public event Action<PlayerId> OnPlayerEnter;
        public event Action<PlayerId> OnPlayerExit;

        // Use this for initialization
        private new void Awake()
        {
            base.Awake();
            StartListening();
        }

        private void StartListening()
        {
            _ = StartCoroutine(ListenToNetwork());

            IEnumerator ListenToNetwork()
            {
                yield return new WaitUntil(() => NetworkManager.Singleton != null);

                StopListening(); // avoid doubling up

                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
                if (NetworkManager.Singleton.IsHost)
                {
                    OnClientConnect(NetworkManager.ServerClientId);
                }
            }
        }

        private void StopListening()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnect;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            }
        }

        public override void OnDestroy()
        {
            StopListening();
            base.OnDestroy();
        }

        private void OnClientConnect(ulong clientId)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                _ = StartCoroutine(Routine());
            }

            IEnumerator Routine()
            {
                yield return new WaitUntil(() => NetworkLayer.Instance.IsInRoom);

                var playerId = GetPlayerIdByClientId(clientId);
                OnClientConnectClientRpc(playerId ?? default);
            }
        }

        private void OnClientDisconnect(ulong clientId)
        {
            var playerId = GetPlayerIdByClientId(clientId);
            OnClientDisconnectClientRpc(playerId ?? default);
            if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.ServerClientId)
            {
                StopListening();
            }
        }

        [ClientRpc]
        private void OnClientConnectClientRpc(PlayerId playerId)
        {
            OnPlayerEnter?.Invoke(playerId);
        }

        [ClientRpc]
        private void OnClientDisconnectClientRpc(PlayerId playerId)
        {
            OnPlayerExit?.Invoke(playerId);
        }

        private static string GetPlayerIdFromStorage()
        {
            var prefKey = GetPlayerIdKey();
            return PlayerPrefs.HasKey(prefKey) ? PlayerPrefs.GetString(prefKey) : string.Empty;
        }

        private static string GetPlayerIdKey()
        {
            var prefKey = "playerId";

            //used so multiple editors dont have the same key
            if (Application.isEditor)
            {
                var s = Application.dataPath.Split('/');
                prefKey = s[^2];
            }

            return prefKey;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod]
        private static void InitializeLocalPlayerId()
        {
            var id = new PlayerId(GetPlayerIdFromStorage());
            if (id.Exists())
            {
                Debug.Log($"LocalPlayerId from storage: {id}");
            }
            else
            {
                id = PlayerId.New();
                SetPlayerId(id);
                Debug.Log($"LocalPlayerId generated: {id}");
            }
            LocalPlayerId = id;
        }

        private static void SetPlayerId(in PlayerId id)
        {
            var prefKey = GetPlayerIdKey();
            PlayerPrefs.SetString(prefKey, id.ToString());
        }

        public PlayerId? GetPlayerIdByClientId(ulong clientId)
        {
            var obj = PlayerObject.GetByClientId(clientId);
            return obj != null ? obj.PlayerId : null;
        }

        public static void SetLocalUsername(string username)
        {
            LocalUsername = username;

            var localObject = PlayerObject.Instances.FirstOrDefault(p => p.IsLocalPlayer);
            if (localObject != null)
            {
                localObject.Username = username;
            }
        }

        public IEnumerable<PlayerId> AllPlayerIds => PlayerObject.Instances.Select(p => p.PlayerId);
        public IEnumerable<NetworkObject> AllPlayerObjects => PlayerObject.Instances.Select(p => p.NetworkObject);
    }
}
