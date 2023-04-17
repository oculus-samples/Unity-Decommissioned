// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Concurrent;
using Meta.Multiplayer.Core;
using Meta.Utilities;
using Photon.Voice;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Multiplayer.Avatar
{
    public class PhotonVoiceAvatarReceiver : MonoBehaviour, IDecoder
    {
        [AutoSet, SerializeField] private PhotonVoiceAvatarNetworking m_networking;
        private bool m_recieving = true;
        private ConcurrentQueue<NativeList<byte>> m_queue = new();

        unsafe void IDecoder.Input(ref FrameBuffer frameBuffer)
        {
            if (frameBuffer.Array?.Length is null or 0)
                return;

            var data = new NativeList<byte>(frameBuffer.Length, Allocator.TempJob);
            data.AddRange(frameBuffer.Ptr.ToPointer(), frameBuffer.Length);
            m_queue.Enqueue(data);
        }

        private unsafe void Update()
        {
            while (m_queue.TryDequeue(out var data))
            {
                if (m_recieving)
                {
                    m_networking.ReceiveAvatarData(data);
                }
                data.Dispose();
            }
        }

        private void OnDestroy()
        {
            while (m_queue.TryDequeue(out var data))
            {
                data.Dispose();
            }
        }

        string IDecoder.Error => null;
        void IDecoder.Open(VoiceInfo info) { }
        void IDisposable.Dispose() { }
    }
}
