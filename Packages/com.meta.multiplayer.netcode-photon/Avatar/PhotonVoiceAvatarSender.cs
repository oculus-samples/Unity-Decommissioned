// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using Meta.Multiplayer.Core;
using Photon.Voice;
using Photon.Voice.Unity;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Multiplayer.Avatar
{
    public class PhotonVoiceAvatarSender : MonoBehaviour, IEncoder
    {
        private LocalVoice m_voice;
        private VoiceConnection m_voiceConnection;
        private VoiceClient m_client;

        public void Init()
        {
            _ = StartCoroutine(Routine());

            IEnumerator Routine()
            {
                yield return new WaitUntil(() => VoipController.Instance?.VoiceConnection?.Client?.IsConnectedAndReady is true);

                if (m_voice != null)
                {
                    m_voice.Dispose();
                }

                m_voiceConnection = VoipController.Instance?.VoiceConnection;
                m_client = m_voiceConnection.VoiceClient;

                var voiceInfo = VoiceInfo.CreateAudio(Codec.Raw, 0, 1, 1000, PhotonVoiceAvatarNetworking.AVATAR_DATA_FLAG);
                m_voice = m_client.CreateLocalVoice(voiceInfo, 0, this);

                m_voice.InterestGroup = 0;
                m_voice.DebugEchoMode = false;
                m_voice.Encrypt = false;
                m_voice.Reliable = false;
                m_voice.TransmitEnabled = true;
            }
        }

        public unsafe void SendAvatarData(ArraySegment<byte> bytes)
        {
            if (m_voice is null)
                return;

            Output(bytes, 0);
        }

        public Action<ArraySegment<byte>, FrameFlags> Output { get; set; }

        ArraySegment<byte> IEncoder.DequeueOutput(out FrameFlags flags)
        {
            flags = 0;
            return ArraySegment<byte>.Empty;
        }

        string IEncoder.Error => null;
        void IEncoder.EndOfStream() { }
        I IEncoder.GetPlatformAPI<I>() => null;
        void IDisposable.Dispose() { }
    }
}
