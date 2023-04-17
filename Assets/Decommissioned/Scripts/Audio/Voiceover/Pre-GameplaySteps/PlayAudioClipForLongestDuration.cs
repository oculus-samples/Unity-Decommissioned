// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Utilities;
using ScriptableObjectArchitecture;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Audio.Voiceover
{
    public class PlayAudioClipForLongestDuration : PlayAudioClip
    {
        [SerializeField] private AudioClipSelector m_clipSelector;
        [SerializeField] private List<AudioClip> m_clips;
        [SerializeField] private GameEvent m_clipEndedEvent;

        public override IEnumerator Run()
        {
            yield return base.Run();
            PlayClipClientRpc();
            yield return WaitForLongestClip();
            IsComplete = true;
        }

        public override void End() { }

        [ClientRpc]
        private void PlayClipClientRpc() { _ = StartCoroutine(PlayLocalClip()); }

        private IEnumerator PlayLocalClip()
        {
            if (!m_clipSelector.TrySelectClip(out var clientSelectionIndex))
            {
                IsComplete = true;
                if (m_clipEndedEvent != null) { m_clipEndedEvent.Raise(); }
                yield break;
            }


            var selectedClip = m_clips[clientSelectionIndex];
            yield return StartCoroutine(PlayClip(selectedClip));
            if (m_clipEndedEvent != null) { m_clipEndedEvent.Raise(); }
        }

        private IEnumerator WaitForLongestClip()
        {
            var longestClip = m_clips
                .WhereNonNull()
                .Max(clip => clip.length);
            yield return new WaitForSeconds(longestClip);
        }
    }
}
