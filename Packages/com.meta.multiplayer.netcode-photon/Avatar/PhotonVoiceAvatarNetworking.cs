// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Linq;
using Meta.Multiplayer.Core;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Photon.Voice;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using static Oculus.Avatar2.OvrAvatarEntity;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Meta.Multiplayer.Avatar
{
    /// <summary>
    /// Handles the networking of the Avatar using Photon Voice Transport.
    /// Local avatars will send their state to other users through Photon Voice.
    /// For remote avatars we receive the state and apply it to the avatar entity.
    /// </summary>
    [RequireComponent(typeof(NetworkObject)), RequireComponent(typeof(PhotonVoiceAvatarSender)), RequireComponent(typeof(PhotonVoiceAvatarReceiver))]
    public class PhotonVoiceAvatarNetworking : NetworkBehaviour
    {
        public const string AVATAR_DATA_FLAG = "AvatarData";
        private const float PLAYBACK_SMOOTH_FACTOR = 0.25f;

        [AutoSet, SerializeField] private PhotonVoiceAvatarSender m_sender;
        [AutoSet, SerializeField] private ClientNetworkTransform m_networkTransform;

        [SerializeField, AutoSet] private AvatarEntity m_entity;

        [SerializeField] private float m_streamDelayMultiplier = 0.5f;

        private NetworkVariable<ulong> m_userId = new(0, writePerm: NetworkVariableWritePermission.Owner);
        private Stopwatch m_streamDelayWatch = new();
        private float m_currentStreamDelay;

        public void Awake()
        {
            m_userId.OnValueChanged += OnUserIdChanged;
        }

        private void OnEnable()
        {
            m_entity.OnNetworkingInit.AddListener(Init);
            m_entity.OnNetworkingUserIdSet.AddListener(SetUserId);
            VoipController.WhenInstantiated(voipController => voipController.OnRemoteVoiceAdded += OnRemoteVoiceAdded);
        }

        private void OnDisable()
        {
            m_entity.OnNetworkingInit.RemoveListener(Init);
            m_entity.OnNetworkingUserIdSet.RemoveListener(SetUserId);

            if (VoipController.Instance != null)
                VoipController.Instance.OnRemoteVoiceAdded -= OnRemoteVoiceAdded;
        }

        private void OnRemoteVoiceAdded(int channelId, NetworkObject player, byte voiceId, VoiceInfo voiceInfo, ref RemoteVoiceOptions options)
        {
            if ((voiceInfo.UserData as string) == AVATAR_DATA_FLAG &&
                player.TryGetComponent(out PhotonVoiceAvatarReceiver receiver))
            {
                options.Decoder = receiver;
            }
        }

        public void Init()
        {
            if (m_entity.IsLocal)
            {
                _ = StartCoroutine(UpdateDataStream());
                m_sender.Init();
            }
            else
            {
                if (m_userId.Value != 0)
                    OnUserIdChanged(0, m_userId.Value);
            }
        }

        public void SetUserId(ulong newId) => m_userId.Value = newId;

        private void OnUserIdChanged(ulong previousValue, ulong newValue)
        {
            if (newValue != 0/* && m_entity.IsCreated*/)
                m_entity.LoadUser(newValue);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_userId.OnValueChanged?.Invoke(ulong.MaxValue, m_userId.Value);
            m_entity.Initialize();

            NetworkLayer.Instance.RestoreHostCallback += OnHostRestored;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            m_entity.Teardown();

            NetworkLayer.Instance.RestoreHostCallback -= OnHostRestored;
        }

        public override void OnLostOwnership()
        {
            base.OnLostOwnership();

            // m_entity.Teardown();
            m_entity.Initialize();
        }

        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();

            // m_entity.Teardown();
            m_entity.Initialize();
        }

        private void OnHostRestored()
        {
            if (m_entity.IsLocal)
            {
                _ = StartCoroutine(UpdateDataStream());
            }
        }

        private IEnumerator UpdateDataStream()
        {
            if (isActiveAndEnabled && m_entity.IsLocal)
            {
                if (m_networkTransform != null)
                {
                    m_networkTransform.ForceSync();
                }
            }

            var lastUpdateTime = new EnumDictionary<StreamLOD, double>();
            while (isActiveAndEnabled && m_entity.IsLocal)
            {
                transform.SetPositionAndRotation(
                    CameraRigRef.Instance.transform.position,
                    CameraRigRef.Instance.transform.rotation);

                if (m_entity.IsCreated && m_entity.HasJoints && NetworkObject?.IsSpawned is true)
                {
                    var now = Time.unscaledTimeAsDouble;
                    var (lod, timeSinceLastUpdate) = lastUpdateTime.
                        Select(pair => (pair.Key, now - pair.Value)).
                        Where(pair => m_updateFrequencySecondsByLod[pair.Key].Value is { } frequency && pair.Item2 > frequency).
                        OrderByDescending(pair => pair.Item2).
                        FirstOrDefault();
                    if (timeSinceLastUpdate != default)
                    {
                        // act like every lower frequency lod got updated too
                        var lodFrequency = m_updateFrequencySecondsByLod[lod].Value;
                        foreach (var (key, _) in m_updateFrequencySecondsByLod.Where(pair => pair.Value.Value <= lodFrequency))
                            lastUpdateTime[key] = now;

                        SendAvatarData(lod);
                    }
                }

                yield return null;
            }
        }

        [SerializeField] private EnumDictionary<StreamLOD, NullableFloat> m_updateFrequencySecondsByLod;

        public delegate bool IsVisibleTo(AvatarNetworking targetAvatar);

        private byte[] m_avatarDataBuffer = new byte[2048];

        private void SendAvatarData(StreamLOD lod)
        {
            var numBytes = m_entity.RecordStreamData_AutoBuffer(lod, ref m_avatarDataBuffer);
            m_sender.SendAvatarData(new(m_avatarDataBuffer, 0, (int)numBytes));
        }

        public void ReceiveAvatarData(NativeList<byte> data)
        {
            if (m_entity != null && !m_entity.IsLocal)
            {
                _ = m_entity.ApplyStreamData(data);

                var latency = (float)m_streamDelayWatch.Elapsed.TotalSeconds;
                var delay = Mathf.Clamp01(latency * m_streamDelayMultiplier);
                m_currentStreamDelay = Mathf.LerpUnclamped(m_currentStreamDelay, delay, PLAYBACK_SMOOTH_FACTOR);
                m_entity.SetPlaybackTimeDelay(m_currentStreamDelay);
                m_streamDelayWatch.Restart();
            }
        }

        public PlayerId? GetOwnerPlayerId()
        {
            var instance = PlayerManager.Instance;
            return instance != null ? instance.GetPlayerIdByClientId(OwnerClientId) : null;
        }
    }
}
