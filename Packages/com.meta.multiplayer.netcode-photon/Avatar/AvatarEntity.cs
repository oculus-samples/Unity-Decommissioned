// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.Multiplayer.Core;
using Meta.Utilities;
using Oculus.Avatar2;
using Oculus.Platform;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static Oculus.Avatar2.CAPI;

namespace Meta.Multiplayer.Avatar
{
    /// <summary>
    /// The AvatarEntity handles the setup of the Avatar, adds functionalities to the base class OvrAvatarEntity.
    /// On Joint loaded callbacks
    /// Set up lipsync and body tracking.
    /// Paired with the AvatarNetworking it initializes it to support networking in a multiplayer project.
    /// In a not networked setup, it will track the camera rig to keep the position in sync.
    /// </summary>
    [DefaultExecutionOrder(50)] // after GpuSkinningConfiguration initializes
    public class AvatarEntity : OvrAvatarEntity
    {
        [Serializable]
        public struct OnJointLoadedPair
        {
            public ovrAvatar2JointType Joint;
            public Transform TargetToSetAsChild;
            public UnityEvent<Transform> OnLoaded;
        }

        [SerializeField, AutoSet] private NetworkObject m_networkObject;

        public UnityEvent OnNetworkingInit;
        public UnityEvent<ulong> OnNetworkingUserIdSet;

        [SerializeField, AutoSetFromChildren(IncludeInactive = true)]
        private OvrAvatarLipSyncBehavior m_lipSync;

        [SerializeField] private bool m_isLocalIfNotNetworked;
        [SerializeField] private bool m_enableCameraTracking;

        public List<OnJointLoadedPair> OnJointLoadedEvents = new();

        public Transform GetJointTransform(ovrAvatar2JointType jointType) => GetSkeletonTransformByType(jointType);
        public bool HasUserAvatar { get; private set; } = false;
        public bool HasDoneAvatarCheck { get; private set; }

        public ovrAvatar2EntityId EntityId => entityId;

        [Header("Face Pose Input")]
        [SerializeField, AutoSet]
        private OvrAvatarFacePoseBehavior m_facePoseProvider;
        [SerializeField, AutoSet]
        private OvrAvatarEyePoseBehavior m_eyePoseProvider;

        private Task m_initializationTask;
        private Task m_setUpAccessTokenTask;

        protected override void Awake()
        {
            m_setUpAccessTokenTask = SetUpAccessTokenAsync();

            base.Awake();

            _ = OVRPlugin.StartFaceTracking();
            _ = OVRPlugin.StartEyeTracking();
        }


        private void Start()
        {
            if ((m_networkObject == null || m_networkObject.NetworkManager == null || !m_networkObject.NetworkManager.IsListening) && m_isLocalIfNotNetworked)
            {
                Initialize();
            }
        }

        private async Task SetUpAccessTokenAsync()
        {
            var accessToken = await Users.GetAccessToken().Gen();
            OvrAvatarEntitlement.SetAccessToken(accessToken.Data);
        }

        public void Initialize()
        {
            var prevInit = m_initializationTask;
            m_initializationTask = Impl();

            async Task Impl()
            {
                try
                {
                    if (prevInit != null)
                        await prevInit;
                    await InitializeImpl();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private async Task InitializeImpl()
        {
            Teardown();

            var isOwner = m_networkObject == null || (m_networkObject != null && !m_networkObject.NetworkManager.IsClient) ? m_isLocalIfNotNetworked : m_networkObject.IsOwner;

            SetIsLocal(isOwner);
            if (isOwner)
            {
                _creationInfo.features |= ovrAvatar2EntityFeatures.Animation;

                var body = CameraRigRef.Instance.AvatarInputManager;
                SetInputManager(body);

                m_lipSync.gameObject.SetActive(true);
                SetLipSync(m_lipSync);

                SetFacePoseProvider(m_facePoseProvider);
                SetEyePoseProvider(m_eyePoseProvider);

                AvatarLODManager.Instance.firstPersonAvatarLod = AvatarLOD;
            }
            else
            {
                _creationInfo.features &= ~ovrAvatar2EntityFeatures.Animation;

                SetInputManager(null);
                SetFacePoseProvider(null);
                SetEyePoseProvider(null);
                SetLipSync(null);
            }

            await m_setUpAccessTokenTask;

            if (this == null)
                return;

            OnNetworkingInit?.Invoke();

            var activeView = isOwner ? ovrAvatar2EntityViewFlags.FirstPerson : ovrAvatar2EntityViewFlags.ThirdPerson;

            if (IsLocal)
            {
                var user = await Users.GetLoggedInUser().Gen();
                _userId = user.Data.ID;

                if (_userId != 0)
                {
                    await CheckForUserAvatar();

                    if (this == null)
                        return;
                }

                OnNetworkingUserIdSet?.Invoke(_userId);
                CreateEntity();
                SetActiveView(activeView);
                LoadUser();

                if (m_enableCameraTracking)
                {
                    UpdatePositionToCamera();
                    _ = StartCoroutine(TrackCamera());
                }
            }
            else if (_userId != 0)
            {
                await CheckForUserAvatar();

                if (this == null)
                    return;

                CreateEntity();
                SetActiveView(activeView);
                LoadUser();
            }
        }

        protected override ovrAvatar2EntityCreateInfo? ConfigureCreationInfo()
        {
            var info = base.ConfigureCreationInfo() ?? _creationInfo;

            //This checks if the user has a custom avatar. If so, this will disable the default avatar features from this entity. This is done to fix a bug in PlayerWristband.
            //Avatar Bone name mappings are failing when default avatar features are enabled.
            if (HasDoneAvatarCheck)
            {
                var defaultModelFeatures = ovrAvatar2EntityFeatures.UseDefaultAnimHierarchy | ovrAvatar2EntityFeatures.UseDefaultModel;
                if (HasUserAvatar)
                    info.features &= ~defaultModelFeatures;
                else
                    info.features |= defaultModelFeatures;
            }

            if (IsLocal)
                info.features |= ovrAvatar2EntityFeatures.Animation;
            else
                info.features &= ~ovrAvatar2EntityFeatures.Animation;

            return info;
        }

        public void LoadUser(ulong userId)
        {
            if (_userId != userId)
            {
                _userId = userId;
                // LoadUser();
                Initialize();
            }
        }

        private async Task CheckForUserAvatar()
        {
            var hasAvatarCheck = await OvrAvatarManager.Instance.UserHasAvatarAsync(_userId);
            if (hasAvatarCheck == OvrAvatarManager.HasAvatarRequestResultCode.HasAvatar)
            {
                HasUserAvatar = true;
            }
            else if (hasAvatarCheck == OvrAvatarManager.HasAvatarRequestResultCode.HasNoAvatar)
            {
                Debug.Log("Failed to load user avatar: user has no avatar!");
                HasUserAvatar = false;
            }
            else
            {
                Debug.Log("Avatar check failed, retrying...");
                await Task.Delay(4000);
                await CheckForUserAvatar();
            }

            HasDoneAvatarCheck = true;
        }

        public void Show()
        {
            SetActiveView(!m_networkObject.IsOwner
                ? ovrAvatar2EntityViewFlags.ThirdPerson
                : ovrAvatar2EntityViewFlags.FirstPerson);
        }

        public void Hide()
        {
            SetActiveView(ovrAvatar2EntityViewFlags.None);
        }

        protected override void OnSkeletonLoaded()
        {
            base.OnSkeletonLoaded();

            foreach (var evt in OnJointLoadedEvents)
            {
                var jointTransform = GetJointTransform(evt.Joint);
                if (evt.TargetToSetAsChild != null)
                {
                    evt.TargetToSetAsChild.SetParent(jointTransform, false);
                }

                evt.OnLoaded?.Invoke(jointTransform);
            }
        }

        private IEnumerator TrackCamera()
        {
            while (true)
            {
                UpdatePositionToCamera();
                yield return null;
            }
        }

        private void UpdatePositionToCamera()
        {
            var cameraTransform = CameraRigRef.Instance.transform;
            transform.SetPositionAndRotation(
                cameraTransform.position,
                cameraTransform.rotation);
        }
    }
}
