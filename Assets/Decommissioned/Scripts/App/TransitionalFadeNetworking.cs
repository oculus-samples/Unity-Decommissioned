// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Multiplayer.Avatar;
using Meta.Multiplayer.Networking;
using Meta.Utilities;
using Netcode.Transports.PhotonRealtime;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.Decommissioned
{
    [RequireComponent(typeof(TransitionalFade))]
    public class TransitionalFadeNetworking : MonoBehaviour
    {
        private const string HOST_LEFT_REASON = "The host has left the game.\nReturning to main menu...";
        private const string CLIENT_TIMEOUT_REASON = "The host has left the game.\nReturning to main menu...";

        [SerializeField] private PhotonRealtimeTransport m_photonTransport;
        [SerializeField] private NetworkLayer m_networkLayer;
        [SerializeField, AutoSet] private TransitionalFade m_transitionalFade;

        private void OnEnable()
        {
            if (m_networkLayer != null)
            {
                m_networkLayer.StartHostCallback += OnStartConnection;
                m_networkLayer.StartClientCallback += OnStartConnection;
                m_networkLayer.OnHostExit += OnHostLeft;
            }
            if (m_photonTransport != null)
            {
                m_photonTransport.OnTransportEvent += OnPhotonTransportEvent;
            }
            m_transitionalFade.OnSceneLoadTrigger += OnSceneLoadTrigger;
        }

        private void OnDisable()
        {
            if (m_networkLayer != null)
            {
                m_networkLayer.StartHostCallback -= OnStartConnection;
                m_networkLayer.StartClientCallback -= OnStartConnection;
                m_networkLayer.OnHostExit -= OnHostLeft;
            }
            if (m_photonTransport != null)
            {
                m_photonTransport.OnTransportEvent -= OnPhotonTransportEvent;
            }
        }

        private bool OnSceneLoadTrigger(Scene scene)
        {
            if (scene.name == Application.LOBBY_SCENE)
            {
                LocationManager.WhenInstantiated(
                    lm => lm.OnPlayerJoinedRoom += OnPlayerTeleportFinished);
                LocationManager.WhenDestroyed(
                    lm => lm.OnPlayerJoinedRoom -= OnPlayerTeleportFinished);

                FadeOnHostJoin();
                return true;
            }

            if (scene.name == Application.MAIN_MENU_SCENE)
            {
                _ = StartCoroutine(FadeOnPlayerInitialized());
                return true;
            }

            return false;
        }

        private void OnPlayerTeleportFinished(NetworkObject player, MiniGameRoom room)
        {
            if (IsLocalPlayer(player))
            {
                m_transitionalFade.FadeFromBlack(.25f);
            }
        }


        private void OnStartConnection() => m_transitionalFade.FadeToBlack(true, TransitionalFade.LOADING_TEXT);

        private void OnPhotonTransportEvent(NetworkEvent eventType, ulong clientId, ArraySegment<byte> payload, float receiveTime)
        {
            if ((eventType == NetworkEvent.Disconnect || eventType == NetworkEvent.Connect) && SceneManager.GetActiveScene().name != Application.LOBBY_SCENE)
            {
                OnStartConnection();
            }
        }

        private void OnHostLeft()
        {
            m_transitionalFade.FadeToBlack(true, HOST_LEFT_REASON);
        }

        private IEnumerator FadeInOnPlayerLoad(NetworkClient localPlayer)
        {
            yield return new WaitUntil(() => localPlayer.PlayerObject != null);

            var playerObject = localPlayer.PlayerObject;
            if (playerObject == null)
            {
                Debug.LogWarning("Unable to get the local player object to monitor fade out! Fading out normally...");
                m_transitionalFade.FadeFromBlack(0.5f);
                yield break;
            }

            yield return new WaitUntil(() => localPlayer.PlayerObject != null);

            if (!playerObject.TryGetComponent<AvatarEntity>(out var playerEntity))
            {
                Debug.LogWarning("Unable to get the local player entity to monitor fade out! Fading out normally...");
                m_transitionalFade.FadeFromBlack(0.5f);
                yield break;
            }

            if (Application.Instance == null)
            {
                Debug.LogWarning("Application is not ready to check for avatars! Fading out normally...");
                m_transitionalFade.FadeFromBlack(0.5f);
                yield break;
            }

            yield return new WaitUntil(() => Application.Instance.IsAvatarEntityReady(playerEntity));

            m_transitionalFade.FadeFromBlack(0.5f);
        }

        public void FadeOnHostJoin()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            {
                var localPlayer = NetworkManager.Singleton.LocalClient;
                if (localPlayer == null)
                {
                    Debug.LogWarning("Unable to get the local player to monitor fade out! Fading out normally...");
                    m_transitionalFade.FadeFromBlack(0.5f);
                    return;
                }

                _ = StartCoroutine(FadeInOnPlayerLoad(localPlayer));
            }
        }
        internal IEnumerator FadeOnPlayerInitialized()
        {
            yield return new WaitUntil(() => FindObjectOfType<AvatarEntity>() != null);

            var playerEntity = FindObjectOfType<AvatarEntity>();
            if (playerEntity == null)
            {
                Debug.LogWarning("Unable to get the local player entity to monitor fade out! Fading out normally...");
                m_transitionalFade.FadeFromBlack(0.5f);
                yield break;
            }

            if (Application.Instance == null)
            {
                Debug.LogWarning("Application is not ready to check for avatars! Fading out normally...");
                m_transitionalFade.FadeFromBlack(0.5f);
                yield break;
            }

            yield return new WaitUntil(() => Application.Instance.IsAvatarEntityReady(playerEntity));

            m_transitionalFade.FadeFromBlack(0.5f);
        }
        public bool IsLocalPlayer(object player) =>
            player is NetworkObject netPlayer && netPlayer.IsLocalPlayer;
    }
}
