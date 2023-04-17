// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Linq;
using Meta.Multiplayer.Avatar;
using Meta.Multiplayer.Core;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using NaughtyAttributes;
using Oculus.Avatar2;
using Oculus.Platform;
using Oculus.Platform.Models;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Oculus.Avatar2.OvrAvatarEntity;

namespace Meta.Decommissioned
{
    public partial class Application
    {
        private const string GUARDIAN_MISSING_MESSAGE = "<color=#FF0000>WARNING: <color=#FFFFFF>No Guardian detected. It is advised to play <i>Decommissioned</i> with Guardian Enabled.\nPlease enable Guardian in the Meta Quest settings, or you may press your trigger or pinch to continue anyways.";
        private const string GUARDIAN_TOO_SMALL_MESSAGE = "<color=#FF0000>WARNING: <color=#FFFFFF>Your current Guardian does not meet the minimum size requirements for <i>Decommissioned.</i>\nPlease adjust your Guardian to the displayed size, or you may press your trigger or pinch to continue anyways.";
        private const string GUARDIAN_SKIP_MESSAGE = "<color=#FF0000>Skipping Guardian Requirements...";
        private const string USER_AVATAR_MISSING_MESSAGE = "<color=#FF0000>WARNING: <color=#FFFFFF>No Meta Avatar Detected. It is advised to play <i>Decommissioned</i> with a custom Meta Avatar. If you would like to create a Meta Avatar now, please press your right trigger or pinch your right fingers. Otherwise, press the left trigger or pinch your left hand to continue without an avatar.";
        private const string USER_AVATAR_ERROR_MESSAGE = "<color=#FF0000>Unable to detect user avatar!";
        private const string OPENING_USER_AVATAR_UI = "<color=#FF0000>Opening Meta Avatar Creator...";
        private const string SKIPPING_USER_AVATAR_UI = "<color=#FF0000>Skipping Meta Avatar Creation...";
        private const string CONTINUING_MESSAGE = "<color=#FF0000>Continuing...";

        public bool IsAvatarEntityReady(AvatarEntity avatarEntity)
        {
            return avatarEntity.IsLocal ? ((avatarEntity.HasUserAvatar && avatarEntity.CurrentState == AvatarState.UserAvatar) || (!avatarEntity.HasUserAvatar && avatarEntity.CurrentState is not (AvatarState.Created or AvatarState.None)))
                :
            avatarEntity.CurrentState is not (AvatarState.Created or AvatarState.None) &&
            (avatarEntity.IsLocal || avatarEntity.GetStreamingPlaybackState()?.numSamples > 0);
        }

        protected IEnumerator CheckBoundary()
        {
            yield return new WaitForSecondsRealtime(1);

            var boundary = OVRManager.boundary;

            if (boundary == null || !boundary.GetConfigured())
            {
                m_onInfoLoggedCallbacks?.Invoke(GUARDIAN_MISSING_MESSAGE);

                yield return new WaitUntil(ShouldSkipGuardianCheck);
                DisplayGuardianSkipMessage();
                yield break;
            }

            var boundaryDimensions = boundary.GetDimensions(OVRBoundary.BoundaryType.PlayArea);

            if (boundaryDimensions.x < MinimumBoundary.x || boundaryDimensions.z < MinimumBoundary.z)
            {
                m_onInfoLoggedCallbacks?.Invoke(GUARDIAN_TOO_SMALL_MESSAGE);

                GameObject recommendedGuardianArea;
                var userGuardian = new GameObject("UserGuardian");
                if (GuardianBoundPrefab)
                {
                    recommendedGuardianArea = Instantiate(GuardianBoundPrefab);
                    recommendedGuardianArea.transform.localScale = MinimumBoundary;
                }
                else
                {
                    recommendedGuardianArea = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    recommendedGuardianArea.transform.localScale = MinimumBoundary;
                    recommendedGuardianArea.transform.eulerAngles = new Vector3(90, 0, 0);
                }

                if (ShowUserGuardian)
                {
                    recommendedGuardianArea.transform.position = new Vector3(0, -.005f, 0);

                    var boundaryPoints = boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
                    var lastBoundaryPoint = new Vector3[1] { boundaryPoints[0] };
                    var allBoundaryPoints = new Vector3[boundaryPoints.Length + 1];
                    boundaryPoints.CopyTo(allBoundaryPoints, 0);
                    lastBoundaryPoint.CopyTo(allBoundaryPoints, boundaryPoints.Length);
                    userGuardian.transform.eulerAngles = new Vector3(90, 0, 0);
                    var lineComponent = userGuardian.AddComponent<LineRenderer>();
                    var lineShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

                    lineComponent.material = new Material(lineShader);
                    lineComponent.startColor = Color.red;
                    lineComponent.endColor = Color.red;
                    lineComponent.startWidth = .1f;
                    lineComponent.endWidth = .1f;
                    lineComponent.alignment = LineAlignment.TransformZ;
                    lineComponent.numCornerVertices = 6;
                    lineComponent.numCapVertices = 3;
                    lineComponent.positionCount = allBoundaryPoints.Length;

                    lineComponent.SetPositions(allBoundaryPoints);
                }

                yield return new WaitUntil(ShouldSkipGuardianCheck);

                Destroy(recommendedGuardianArea);
                Destroy(userGuardian);
                DisplayGuardianSkipMessage();
            }
        }

        protected IEnumerator CheckUserAvatar()
        {
#if UNITY_EDITOR
            if (NetworkSettings.Autostart)
            {
                m_onInfoClearedCallbacks?.Invoke();
                m_onInfoLoggedCallbacks?.Invoke("Auto-Start enabled, skipping avatar checks...");

                yield return new WaitForSecondsRealtime(1);

                yield break;
            }
#endif
            if (EnableLogging)
            {
                Debug.Log("Before User Logged");
            }
            Users.GetLoggedInUser().OnComplete(OnGetLoggedInUserComplete);
            yield return new WaitUntil(() => m_userId != 0 && OvrAvatarManager.Instance != null);
            if (EnableLogging)
            {
                Debug.Log("User Logged");
            }

            var checkForAvatarTask = OvrAvatarManager.Instance.UserHasAvatarAsync(m_userId);

            if (checkForAvatarTask == null)
            {
                m_onInfoClearedCallbacks?.Invoke();
                m_onInfoLoggedCallbacks?.Invoke(USER_AVATAR_ERROR_MESSAGE);
                yield break;
            }

            yield return new WaitUntil(() => checkForAvatarTask.IsCompleted);

            var hasUserAvatarResult = checkForAvatarTask.Result;

            switch (hasUserAvatarResult)
            {
                case OvrAvatarManager.HasAvatarRequestResultCode.HasAvatar:
                    break;
                case OvrAvatarManager.HasAvatarRequestResultCode.HasNoAvatar:
                    m_onInfoClearedCallbacks?.Invoke();
                    m_onInfoLoggedCallbacks?.Invoke(USER_AVATAR_MISSING_MESSAGE);
                    break;
                case OvrAvatarManager.HasAvatarRequestResultCode.BadParameter:
                case OvrAvatarManager.HasAvatarRequestResultCode.RequestCancelled:
                case OvrAvatarManager.HasAvatarRequestResultCode.RequestFailed:
                case OvrAvatarManager.HasAvatarRequestResultCode.SendFailed:
                case OvrAvatarManager.HasAvatarRequestResultCode.UnknownError:
                    m_onInfoClearedCallbacks?.Invoke();
                    m_onInfoLoggedCallbacks?.Invoke(USER_AVATAR_ERROR_MESSAGE);
                    break;
            }

            if (hasUserAvatarResult != OvrAvatarManager.HasAvatarRequestResultCode.HasAvatar)
            {
                yield return new WaitForSecondsRealtime(1);

                if (hasUserAvatarResult == OvrAvatarManager.HasAvatarRequestResultCode.HasNoAvatar)
                {
                    var hasPressedTrigger = false;
                    while (!hasPressedTrigger)
                    {
                        if (IsPlayerPressingRightTrigger())
                        {
                            m_onInfoLoggedCallbacks?.Invoke(OPENING_USER_AVATAR_UI);
                            AvatarEditorDeeplink.LaunchAvatarEditor();
                            hasPressedTrigger = true;
                        }
                        else if (IsPlayerPressingLeftTrigger()
#if UNITY_EDITOR
                        || UnityEditor.EditorPrefs.GetBool("NetworkSettingsToolbar.Autostart", false)
#endif
                        )
                        {
                            m_onInfoLoggedCallbacks?.Invoke(SKIPPING_USER_AVATAR_UI);
                            hasPressedTrigger = true;
                        }

                        yield return null;
                    }
                }
                else
                {
                    yield return new WaitUntil(IsPlayerPressingEitherTrigger);
                    m_onInfoLoggedCallbacks?.Invoke(CONTINUING_MESSAGE);
                }

                yield return new WaitForSecondsRealtime(1);
            }
        }

        private IEnumerator EnforcePlayerJoinPosition()
        {
            var networkManager = NetworkManager.Singleton;
            yield return new WaitUntil(() => !networkManager.IsHost);

            yield return new WaitUntil(() => networkManager.LocalClient != null && networkManager.LocalClient.PlayerObject != null);

            //Until the player teleports, enforce their position at 0
            var playerObject = networkManager.LocalClient?.PlayerObject;
            if (playerObject == null)
                yield break;

            var clientNetworkTransform = playerObject.GetComponent<ClientNetworkTransform>();
            while (Game.LocationManager.Instance && Game.LocationManager.Instance.GetGamePositionByPlayerId(PlayerManager.LocalPlayerId) == default)
            {
                clientNetworkTransform.Teleport(transform.position, Quaternion.LookRotation(transform.forward, Vector3.up), Vector3.one);

                yield return null;
            }
        }

        protected void OnOculusPlatformInitialized(Message<PlatformInitialize> message)
        {
            if (message.IsError)
            {
                LogError("Failed to initialize Oculus Platform SDK", message.GetError());
                return;
            }

            Log("Oculus Platform SDK initialized successfully");

            _ = Entitlements.IsUserEntitledToApplication().OnComplete(msg =>
            {
                if (msg.IsError)
                {
                    LogError("You are not entitled to use this app", msg.GetError());
                    return;
                }

                m_launchType = ApplicationLifecycle.GetLaunchDetails().LaunchType;

                GroupPresence.SetJoinIntentReceivedNotificationCallback(OnJoinIntentReceived);
                GroupPresence.SetInvitationsSentNotificationCallback(OnInvitationsSent);

                _ = Users.GetLoggedInUser().OnComplete(OnLoggedInUser);

            });
        }

        protected void OnLoggedInUser(Message<User> message)
        {
            if (message.IsError)
            {
                LogError("Cannot get user info", message.GetError());
                return;
            }

            SetUpUser(message.Data.ID);
        }

        protected async void SetUpUser(ulong id)
        {
            // Workaround.
            // At the moment, Platform.Users.GetLoggedInUser() seems to only be returning the user ID.
            // Display name is blank.
            // Platform.Users.Get(ulong userID) returns the display name.
            var user = await Users.Get(id).Gen();
            PlayerManager.SetLocalUsername(user.Data.DisplayName);

            SetUpOvrAvatar();
        }

        protected async void SetUpOvrAvatar()
        {
            var accessToken = await Users.GetAccessToken().Gen();
            OvrAvatarEntitlement.SetAccessToken(accessToken.Data);
        }

        [Button]
        public void TestJoinIntentCoordinated()
        {
            m_launchType = LaunchType.Coordinated;
            OnJoinIntentReceived(LOBBY_SCENE, "TEST-JOIN-INTENT", "TEST-JOIN-INTENT", "TEST-JOIN-INTENT");
        }

        [Button]
        public void TestJoinIntentDeeplink()
        {
            m_launchType = LaunchType.Deeplink;
            OnJoinIntentReceived(LOBBY_SCENE, "TEST-JOIN-INTENT", "TEST-JOIN-INTENT", "TEST-JOIN-INTENT");
        }

        [Button]
        public void TestJoinIntentInvite()
        {
            m_launchType = LaunchType.Invite;
            OnJoinIntentReceived(LOBBY_SCENE, "TEST-JOIN-INTENT", "TEST-JOIN-INTENT", "TEST-JOIN-INTENT");
        }

        [Button]
        public void TestDisconnect()
        {
            GoToMainMenu();
        }

        protected void OnJoinIntentReceived(Message<GroupPresenceJoinIntent> message) =>
            OnJoinIntentReceived(
                message.Data.DestinationApiName,
                message.Data.LobbySessionId,
                message.Data.MatchSessionId,
                message.Data.DeeplinkMessage);

        protected void OnJoinIntentReceived(string destinationApiName, string lobbySessionId, string matchSessionId, string deeplinkMessage)
        {
            Log(@$"------JOIN INTENT RECEIVED------
                Destination:       {destinationApiName}
                Lobby Session ID:  {lobbySessionId}
                Match Session ID:  {matchSessionId}
                Deep Link Message: {deeplinkMessage}
                --------------------------------");

            // no Group Presence yet:
            // app is being launched by this join intent, either
            // through an in-app direct invite, or through a deeplink
            if (GroupPresenceState == null)
            {
                var lobbySessionID = lobbySessionId;

                GroupPresenceState = new();

                _ = StartCoroutine(
                    GroupPresenceState.Set(
                        destinationApiName,
                        lobbySessionID,
                        lobbySessionId,
                        true
                    )
                );
            }
            // game was already running, meaning the user already has a Group Presence, and
            // is already either hosting or a client of another host.
            else
            {
                OnConnectToMatchRequested.Invoke();
                _ = SwitchRoom(destinationApiName, lobbySessionId, false);
                _ = StartCoroutine(EnforcePlayerJoinPosition());
            }
        }

        private IEnumerator BeforeSwitchRoom()
        {
            yield return new WaitUntil(() => NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening);

            // NetworkManager doesn't automatically unload the scene, so some objects still around.
            // To unload it, we need a different scene to be the active scene.
            const string EMPTY_SCENE_NAME = "EMPTY_SCENE_FOR_TRANSITIONS";
            var emptyScene = SceneManager.GetSceneByName(EMPTY_SCENE_NAME);
            if (!emptyScene.IsValid())
            {
                emptyScene = SceneManager.CreateScene(EMPTY_SCENE_NAME);
            }
            _ = SceneManager.SetActiveScene(emptyScene);

            TransitionalFade.Instance.FadeToBlack();

            yield return new WaitForSecondsRealtime(1.0f);
            yield return SceneManager.UnloadSceneAsync(LOBBY_SCENE);
        }

        protected void OnGetLoggedInUserComplete(Message<User> message)
        {
            var userId = message.Data.ID;
            if (userId == 0)
            {
                Debug.LogError("Unable to get the user's ID!");
                m_onInfoClearedCallbacks?.Invoke();
                m_onErrorClearedCallbacks?.Invoke();
                m_onErrorLoggedCallbacks?.Invoke(USER_AVATAR_ERROR_MESSAGE);
                return;
            }

            m_userId = userId;
        }
        protected void OnInvitationsSent(Message<LaunchInvitePanelFlowResult> message)
        {
            var invitedUsers = message.Data.InvitedUsers.Select(user => new
            {
                Username = user.DisplayName,
                UserID = user.ID
            }).ListToString("\n");
            Log(@$"-------INVITED USERS LIST-------
Size: {message.Data.InvitedUsers.Count}
{invitedUsers}
--------------------------------");
        }

        protected void LogError(string message, Error error)
        {
            var err = new string[] {
                message,
                $"ERROR MESSAGE:   {error.Message}",
                $"ERROR CODE:      {error.Code}",
                $"ERROR HTTP CODE: {error.HttpCode}"
            };
            Debug.LogError(string.Join('\n', err));
            m_onErrorLoggedCallbacks?.Invoke(message);
        }

        protected bool IsPlayerPressingEitherTrigger() =>
            IsPlayerPressingLeftTrigger() || IsPlayerPressingRightTrigger();

        protected bool IsPlayerPressingLeftTrigger() =>
        OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > 0.9f
        || OvrHands[0].GetFingerIsPinching(OVRHand.HandFinger.Index)
#if UNITY_EDITOR
            || UnityEngine.InputSystem.Mouse.current.leftButton.isPressed
#endif
            ;
        protected bool IsPlayerPressingRightTrigger() =>
    OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > 0.9f
    || OvrHands[1].GetFingerIsPinching(OVRHand.HandFinger.Index)
#if UNITY_EDITOR
            || UnityEngine.InputSystem.Mouse.current.rightButton.isPressed
#endif
            ;

        protected bool ShouldSkipGuardianCheck() =>
            IsPlayerPressingEitherTrigger()
#if UNITY_EDITOR
            || NetworkSettings.Autostart
#endif
            ;

        protected void DisplayGuardianSkipMessage() =>
            m_onInfoLoggedCallbacks?.Invoke(GUARDIAN_SKIP_MESSAGE);
    }
}
