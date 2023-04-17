// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.Multiplayer.Networking;
using Meta.Utilities;
using Photon.Voice;
using Photon.Voice.Unity;
using Photon.Voice.Unity.UtilityScripts;
using Unity.Netcode;
using UnityEngine;
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
using UnityEngine.Android;
#endif

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// Controlls the voice over ip setup for Photon Voice.
    /// Includes permission requirement for microphone.
    /// </summary>
    public class VoipController : Singleton<VoipController>
    {
        [SerializeField] private GameObject m_voipSpeakerPrefab;
        [SerializeField] private VoiceConnection m_voipRecorderPrefab;
        [SerializeField] private float m_headHeight = 1.0f;

        public delegate void OnRemoteVoiceAddedFunc(int channelId, NetworkObject player, byte voiceId, VoiceInfo voiceInfo, ref RemoteVoiceOptions options);
        public event OnRemoteVoiceAddedFunc OnRemoteVoiceAdded;

        public VoiceConnection VoiceConnection { get; private set; }

        private new void OnEnable()
        {
            base.OnEnable();
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            // On Start we ask for Microphone permission
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
        }

        public void StartVoip(NetworkObject player)
        {
            VoiceConnection = Instantiate(m_voipRecorderPrefab, player.transform);
            VoiceConnection.SpeakerPrefab = m_voipSpeakerPrefab;
            VoiceConnection.Client.VoiceClient.OnRemoteVoiceInfoAction += OnRemoteVoiceInfo;
            if (player.TryGetComponent<VoipHandler>(out var voipHandler))
            {
                voipHandler.SetRecorder(VoiceConnection);
            }

            // We attach the recorder to the player entity which has the networkObject we want to reference as the id
            _ = VoiceConnection.Client.LocalPlayer.SetCustomProperties(new()
            {
                [nameof(NetworkObject.NetworkObjectId)] = (int)player.NetworkObjectId,
            });
            VoiceConnection.SpeakerLinked += OnSpeakerLinked;

            _ = StartCoroutine(JoinPhotonVoiceRoom());
        }

        private NetworkObject GetPlayer(int playerId)
        {
            var actor = VoiceConnection.Client.LocalPlayer.Get(playerId);
            Debug.Assert(actor != null, $"Could not find voice client for Player #{playerId}");

            _ = actor.CustomProperties.TryGetValue(nameof(NetworkObject.NetworkObjectId), out var networkId);
            Debug.Assert(networkId != null, $"Could not find network object id for Player #{playerId}");

            _ = NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue((ulong)(int)networkId, out var player);
            Debug.Assert(player != null, $"Could not find player instance for Player #{playerId} network id #{networkId}");

            return player;
        }

        private void OnSpeakerLinked(Speaker speaker)
        {
            var player = GetPlayer(speaker);

            if (player.TryGetComponent<VoipHandler>(out var voipHandler))
            {
                voipHandler.SetSpeaker(speaker);
            }

            speaker.transform.parent = player.transform;
            speaker.transform.localPosition = new Vector3(0.0f, m_headHeight, 0.0f);
        }

        public NetworkObject GetPlayer(Speaker speaker) => GetPlayer(speaker.RemoteVoice.PlayerId);

        private IEnumerator JoinPhotonVoiceRoom()
        {
            yield return new WaitUntil(() => NetworkSession.Instance?.PhotonVoiceRoom?.IsNullOrEmpty() == false && VoiceConnection != null);

            // Only join if we can record voice
            if (CanRecordVoice())
            {
                var connectAndJoin = VoiceConnection.GetComponent<ConnectAndJoin>();
                connectAndJoin.RoomName = NetworkSession.Instance.PhotonVoiceRoom;
                connectAndJoin.ConnectNow();
            }
        }

        private bool CanRecordVoice()
        {
            // Only record if permission was accepted
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
            return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
            return true;
#endif
        }

        private void OnRemoteVoiceInfo(int channelId, int playerId, byte voiceId, VoiceInfo voiceInfo, ref RemoteVoiceOptions options)
        {
            var player = GetPlayer(playerId);
            OnRemoteVoiceAdded?.Invoke(channelId, player, voiceId, voiceInfo, ref options);
        }
    }
}
