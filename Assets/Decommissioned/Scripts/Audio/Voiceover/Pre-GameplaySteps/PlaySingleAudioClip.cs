// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Audio.Voiceover
{
    [MetaCodeSample("Decommissioned")]
    public class PlaySingleAudioClip : PlayAudioClip
    {
        [SerializeField] private AudioClip m_clip;

        public override IEnumerator Run()
        {
            yield return base.Run();
            PlayAudioClientRpc();
            yield return PlayClip(m_clip);
        }

        public override void End() { }

        [ClientRpc]
        private void PlayAudioClientRpc()
        {
            if (IsServer)
            {
                return;  // Server will play its own audio & wait for completion
            }
            _ = StartCoroutine(PlayClip(m_clip));
        }
    }
}
