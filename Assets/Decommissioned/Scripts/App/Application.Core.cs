// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Meta.Decommissioned.Player;
using Meta.Multiplayer.Core;
using Meta.Multiplayer.Networking;
using Meta.Utilities;
using Oculus.Avatar2;
using Oculus.Platform;
using Oculus.Platform.Models;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned
{
    public partial class Application
    {
        public bool IsTestScene = false;
        public NetworkLayer NetworkLayer;
        public SceneLoader SceneLoader = new() { AllowSceneReload = true };
        public Spawner Spawner;
        public VoipController Voip;
        public GroupPresenceState GroupPresenceState { get; protected set; }


        protected LaunchType m_launchType = LaunchType.Unknown;

        public string CurrentRoom => NetworkLayer.CurrentRoom;

        public Task<Message<PlatformInitialize>> OculusPlatformInitialization { get; private set; }

        public bool TryJoinOnInit { get; set; }
        public string RoomNameOverride = "";
        public Vector3 MinimumBoundary = Vector3.one;
        public GameObject GuardianBoundPrefab;
        public bool ShowUserGuardian = false;
        public OVRHand[] OvrHands;

        private ulong m_userId = 0;
        private WaitForSeconds m_disconnectToMainMenuWaitTime = new(1);

        private HashSet<Routine> m_routines = new();

        protected Action<string> m_onErrorLoggedCallbacks = null;
        protected Action m_onErrorClearedCallbacks = null;
        protected Action<string> m_onInfoLoggedCallbacks = null;
        protected Action m_onInfoClearedCallbacks = null;
        public UnityEvent OnConnectToMatchRequested;
        public UnityEvent OnLeavingStartup;

#if UNITY_EDITOR
        [NaughtyAttributes.ShowNativeProperty] public string CurrentlyRunningRoutine0 => m_routines.Skip(0).FirstOrDefault()?.ToString() ?? "";
        [NaughtyAttributes.ShowNativeProperty] public string CurrentlyRunningRoutine1 => m_routines.Skip(1).FirstOrDefault()?.ToString() ?? "";
        [NaughtyAttributes.ShowNativeProperty] public string CurrentlyRunningRoutine2 => m_routines.Skip(2).FirstOrDefault()?.ToString() ?? "";
        [NaughtyAttributes.ShowNativeProperty] public string CurrentlyRunningRoutine3 => m_routines.Skip(3).FirstOrDefault()?.ToString() ?? "";
#endif

        public void RegisterErrorLogCallback(Action<string> callback)
        {
            m_onErrorLoggedCallbacks += callback;
        }

        public void UnregisterErrorLogCallback(Action<string> callback)
        {
            m_onErrorLoggedCallbacks -= callback;
        }

        public void RegisterErrorLogClearCallback(Action callback)
        {
            m_onErrorClearedCallbacks += callback;
        }

        public void UnregisterErrorLogClearCallback(Action callback)
        {
            m_onErrorClearedCallbacks -= callback;
        }

        public void RegisterInfoLogCallback(Action<string> callback)
        {
            m_onInfoLoggedCallbacks += callback;
        }

        public void UnregisterInfoLogCallback(Action<string> callback)
        {
            m_onInfoLoggedCallbacks -= callback;
        }

        public void RegisterInfoLogClearCallback(Action callback)
        {
            m_onInfoClearedCallbacks += callback;
        }

        public void UnregisterInfoLogClearCallback(Action callback)
        {
            m_onInfoClearedCallbacks -= callback;
        }

        protected void Log(string log)
        {
            if (EnableLogging)
            {
                Debug.Log($"[{nameof(Application)}] {log}", this);
            }
        }

        protected new void OnEnable()
        {
            base.OnEnable();
            DontDestroyOnLoad(this);

            if (NetworkLayer == null)
                return;

            _ = StartCoroutine(new WaitUntil(() => NetworkManager.Singleton != null).Then(InitializeNetworkManagerApprovals));
            NetworkLayer.OnClientConnectedCallback += OnClientConnected;
            NetworkLayer.OnClientDisconnectedCallback += OnClientDisconnected;
            NetworkLayer.StartHostCallback += OnHostStarted;
            NetworkLayer.StartClientCallback += OnClientStarted;
            NetworkLayer.RestoreHostCallback += OnHostRestored;
            NetworkLayer.RestoreClientCallback += OnClientRestored;
            NetworkLayer.StartLobbyCallback += OnLobbyStarted;
            NetworkLayer.OnHostExit += OnHostExit;
            NetworkLayer.BeforeSwitchRoomRoutine += BeforeSwitchRoom;
        }

        private void InitializeNetworkManagerApprovals()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += OnPlayerAttemptConnection;
        }


        private void OnPlayerAttemptConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            if (request.ClientNetworkId == NetworkManager.ServerClientId)
            {
                m_pendingConnections.Clear();
                m_playerJoinRequests.Clear();
            }

            response.CreatePlayerObject = false;
            response.Position = Vector3.zero;
            response.Rotation = Quaternion.identity;
            response.Pending = AnyClientCurrentlyJoining();
            m_pendingConnections[request.ClientNetworkId] = response;
            _ = StartCoroutine(JoinQueueTimeout(request.ClientNetworkId));
            response.Approved = true;
            StartPlayerQueueRoutine();
        }

        #region Networking callbacks

        protected void OnClientConnected(ulong clientId)
        {
            QueueNewClient(clientId);
        }

        protected void OnClientDisconnected(ulong clientId)
        {
            if (AnyClientCurrentlyJoining() && m_pendingConnections.FirstOrDefault().Key == clientId)
            {
                ProcessNextPendingConnection();
            }
        }

        protected void OnHostStarted()
        {
            Log("OnHostStarted");

            SceneLoader.LoadScene(LOBBY_SCENE);

            _ = StartCoroutine(Impl());

            IEnumerator Impl()
            {
                yield return new WaitUntil(() => SceneLoader.SceneLoaded && NetworkManager.Singleton.IsListening);

                if (!NetworkManager.Singleton.IsHost)
                {
                    yield break;
                }

                _ = Spawner.SpawnSession();
                yield return new WaitUntil(() => NetworkManager.Singleton.SpawnManager == null || NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null);

                if (NetworkManager.Singleton.SpawnManager == null)
                {
                    yield break;
                }

                StartVoip();
            }
        }

        public void OnHostRestarted()
        {
            Log("OnHostRestarted");

            SceneLoader.LoadScene(LOBBY_SCENE);

            _ = StartCoroutine(Impl());

            IEnumerator Impl()
            {
                yield return new WaitUntil(() => SceneLoader.SceneLoaded);
                yield return new WaitUntil(() => NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null);

                StartVoip();
            }
        }

        protected void OnClientStarted()
        {
            StartVoip();
        }

        protected void OnHostRestored()
        {
            StartVoip();
        }

        protected void OnClientRestored()
        {
            StartVoip();
        }

        protected void OnLobbyStarted()
        {
            GoToMainMenu();
        }

        protected void OnHostExit()
        {
            _ = StartCoroutine(Instance.GoToMainMenuAfterTime());
        }

        #endregion

        protected void StartVoip()
        {
            var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            Voip.StartVoip(player);
        }

        protected void Start()
        {
            _ = StartCoroutine(Init());
        }

        public void RetryStart()
        {
            _ = StartCoroutine(Init());
        }

        protected IEnumerator Init()
        {
            OculusPlatformInitialization = Core.AsyncInitialize().Gen();
            _ = OculusPlatformInitialization.ContinueWith(task => OnOculusPlatformInitialized(task.Result));

            if (IsTestScene)
                yield break;

            yield return OculusPlatformInitialization.ToRoutine();
            var accessToken = Users.GetAccessToken().Gen();
            yield return accessToken.ToRoutine();
            OvrAvatarEntitlement.SetAccessToken(accessToken.Result.Data);
            yield return CheckBoundary();
            yield return new WaitForSecondsRealtime(2);

            yield return CheckUserAvatar();

            Log($"Launch type: {m_launchType}");
            if (m_launchType is LaunchType.Normal or LaunchType.Unknown)
            {
                GroupPresenceState = new GroupPresenceState();

                _ = StartCoroutine(GenerateNewGroupPresence());
            }

            yield return new WaitUntil(() => GroupPresenceState != null && GroupPresenceState.Destination != null);

            var room = m_launchType != LaunchType.Normal ? GroupPresenceState.LobbySessionID : null;
            if (TryJoinOnInit)
            {
                var roomNameOverride = Instance.RoomNameOverride;
                room = roomNameOverride.IfNullOrEmpty(SystemInfo.deviceUniqueIdentifier);
            }
            NetworkLayer.Init(room, null);

            OnLeavingStartup?.Invoke();
        }


        #region Photon and Room management
        protected bool SwitchRoom(string destination, string lobbySessionID, bool isHosting)
        {
            Log($"Switching to room {lobbySessionID} ({destination}) as {(isHosting ? "host" : "client")}");
            CancelRoutines();
            NetworkLayer.SwitchPhotonRealtimeRoom(lobbySessionID, isHosting, null);
            return true;
        }

        private bool GroupPresenceExists() =>
            GroupPresenceState != null;

        public bool ConnectToMatch(bool isHosting)
        {
            OnConnectToMatchRequested.Invoke();
            return SwitchRoom("Game", null, isHosting);
        }

        public void GoToMainMenu()
        {
            Log("GOING TO MAIN MENU");

            NetworkLayer.Leave();

            CancelRoutines();

            SceneLoader.LoadScene(MAIN_MENU_SCENE, false);
        }

        public IEnumerator GoToMainMenuAfterTime()
        {
            yield return m_disconnectToMainMenuWaitTime;

            GoToMainMenu();
        }

        protected string GetPhotonRoomName()
        {
            return GroupPresenceState.MatchSessionID == ""
                ? GroupPresenceState.LobbySessionID
                : GroupPresenceState.MatchSessionID;
        }
        #endregion

        protected new Routine StartCoroutine(IEnumerator routine)
        {
            var wrapped = new Routine { Inner = routine };
            _ = m_routines.Add(wrapped);
            Log($"Starting coroutine {wrapped}");
            _ = base.StartCoroutine(wrapped);
            return wrapped;
        }

        protected void CancelRoutines()
        {
            foreach (var routine in m_routines.ToList())
                routine.Cancel();
        }

        public class Routine : IEnumerator
        {
            public IEnumerator Inner;

            public bool IsStarted { get; private set; }
            public bool IsComplete { get; private set; }
            public bool IsCancelled { get; private set; }
            public bool IsRunning => IsStarted && !IsComplete && !IsCancelled;

            public void Cancel()
            {
                IsCancelled = true;
                _ = Instance.m_routines.Remove(this);
                Instance.Log($"Cancelled coroutine {this}");
            }

            public bool MoveNext()
            {
                if (IsCancelled)
                    return false;

                IsStarted = true;

                var result = Inner.MoveNext();
                if (!result)
                {
                    IsComplete = true;
                    Instance.Log($"Completed coroutine {this}");
                    _ = Instance.m_routines.Remove(this);
                }

                return result;
            }

            public object Current => Inner.Current;
            public void Reset() => Inner.Reset();
            public override string ToString() => Inner.ToString().
                Replace("Meta.Decommissioned.Application", "").
                Replace("+<>c__DisplayClass", "");
        }

        public void UpdateGroupPresence() =>
            StartCoroutine(GenerateNewGroupPresence());
    }
}
