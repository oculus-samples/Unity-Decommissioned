// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Multiplayer.Avatar;
using Meta.Multiplayer.Core;
using Meta.Utilities;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned
{
    public partial class Application : Singleton<Application>
    {
        public const string STARTUP_SCENE = "Startup";
        public const string MAIN_MENU_SCENE = "MainMenu";
        public const string LOBBY_SCENE = "Lobby";

        [SerializeField] private UnityEvent m_onAutoStart;
        public TextMeshProUGUI ErrorText;
        public TextMeshProUGUI InfoText;
        public bool EnableLogging;

        protected new void Awake()
        {
            EnableLogging = true;
            base.Awake();

            var frequencies = OVRPlugin.systemDisplayFrequenciesAvailable.Where(f => f < 91);
            if (frequencies.Any())
            {
                OVRPlugin.systemDisplayFrequency = frequencies.Max();
                if (EnableLogging)
                {
                    Debug.Log($"Set display refresh rate to {OVRPlugin.systemDisplayFrequency}");
                }
            }
            OVRManager.SetSpaceWarp(true);

            if (UnityEngine.Application.isEditor)
            {
                RoomNameOverride = NetworkSettings.UseDeviceRoom ? SystemInfo.deviceUniqueIdentifier : NetworkSettings.RoomName;
            }

            var room = AndroidHelpers.GetStringIntentExtra("autoJoinRoom");
            if (room != null)
            {
                RoomNameOverride = room;
            }

            if (UnityEngine.Application.isEditor && NetworkSettings.Autostart)
            {
                m_onAutoStart?.Invoke();
            }

            RegisterErrorLogCallback(OnErrorLogged);
            RegisterInfoLogCallback(OnInfoLogged);
            RegisterErrorLogClearCallback(ClearErrorLog);
            RegisterInfoLogClearCallback(ClearInfoLog);
        }

        private void OnInfoLogged(string message)
        {
            if (InfoText)
            {
                InfoText.gameObject.SetActive(true);
                InfoText.text += $"\n{message}";
            }
        }

        private void ClearInfoLog()
        {
            if (InfoText)
            {
                InfoText.text = "";
            }
        }

        private void OnErrorLogged(string message)
        {
            if (ErrorText)
            {
                if (InfoText)
                {
                    InfoText.gameObject.SetActive(false);
                }
                ErrorText.gameObject.SetActive(true);
                ErrorText.text += $"\n{message}";
            }
        }

        private void ClearErrorLog()
        {
            if (ErrorText)
            {
                ErrorText.text = "";
            }
        }

        //called after the playerObject is spawned on the server
        public void OnSpawnClient(NetworkObject playerObject)
        {
            if (LocationManager.Instance)
            {
                _ = StartCoroutine(TeleportWhenLoaded());

                IEnumerator TeleportWhenLoaded()
                {

                    yield return new WaitUntil(() => playerObject.IsSpawned);

                    if (playerObject.TryGetComponent<AvatarEntity>(out var avatarEntity))
                    {
                        yield return new WaitUntil(() => IsAvatarEntityReady(avatarEntity));
                    }
                    else
                    {
                        Debug.LogWarning("Unable to get the AvatarEntity from a joining player! They will appear before their tracking is ready.");
                    }
                    LocationManager.Instance.TeleportToMainRoom(playerObject);
                }
            }
        }

        public IEnumerator GenerateNewGroupPresence(string roomName = null)
        {
            GroupPresenceState ??= new GroupPresenceState();

            var game = GameManager.Instance;
            var inMainMenu = game == null;
            var gameStarted = inMainMenu || game.State is not GameState.ReadyUp;

            yield return new WaitUntil(() => inMainMenu || NetworkLayer.CurrentRoom != null);
            roomName ??= NetworkLayer.CurrentRoom;

            var destination = inMainMenu ? "main-menu" :
                gameStarted ? "in-game" :
                LOBBY_SCENE;
            var joinable = destination == LOBBY_SCENE;
            yield return GroupPresenceState.Set(destination, roomName, roomName, joinable);
        }
    }
}
