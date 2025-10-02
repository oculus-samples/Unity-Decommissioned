// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Audio.Voiceover
{
    [MetaCodeSample("Decommissioned")]
    public class PlaySelectedAudioClip : PlayAudioClip
    {
        [SerializeField] private AudioClipSelector m_clipSelector;
        [SerializeField] private List<AudioClip> m_clips;

        public override IEnumerator Run()
        {
            yield return base.Run();

            if (!m_clipSelector.TrySelectClip(out var selectionIndex))
            {
                IsComplete = true;
                yield break;
            }
            var selectedClip = m_clips[selectionIndex];
            PlayClipClientRpc();
            yield return PlayClip(selectedClip);
            IsComplete = true;
        }

        public override void End() { }

        [ClientRpc]
        private void PlayClipClientRpc()
        {
            // Don't execute on server; it will play by itself and wait for completion
            if (IsServer) { return; }

            _ = m_clipSelector.TrySelectClip(out var clientSelectionIndex);
            var selectedClip = m_clips[clientSelectionIndex];
            _ = StartCoroutine(PlayClip(selectedClip));
        }
    }
}
