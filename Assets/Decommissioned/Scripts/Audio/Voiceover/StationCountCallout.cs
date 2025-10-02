// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Utilities;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Audio.Voiceover
{
    [MetaCodeSample("Decommissioned")]
    public class StationCountCallout : PreGameplayStep
    {
        private enum StationsRemainingCounts
        {
            OneLeft,
            TwoLeft,
            ThreeLeft,
            FourLeft,
            FiveLeft
        }

        [SerializeField, AutoSet] private AudioSource m_audioSource;
        [SerializeField] private EnumDictionary<StationsRemainingCounts, AudioClip> m_countClips;

        public override IEnumerator Run() { yield return StartCoroutine(PlayCalloutAudio()); }

        public override void End() { }

        private AudioClip GetClipForStationsRemaining()
        {
            return GameManager.Instance.StationsRemaining switch
            {
                1 => m_countClips[StationsRemainingCounts.OneLeft],
                2 => m_countClips[StationsRemainingCounts.TwoLeft],
                3 => m_countClips[StationsRemainingCounts.ThreeLeft],
                4 => m_countClips[StationsRemainingCounts.FourLeft],
                5 => m_countClips[StationsRemainingCounts.FiveLeft],
                _ => m_countClips[StationsRemainingCounts.FiveLeft],
            };
        }

        private IEnumerator PlayCalloutAudio()
        {
            _ = GetClipForStationsRemaining();
            var clip = GetClipForStationsRemaining();
            PlayClipAtIndexClientRpc(m_countClips.FirstOrDefault(clipAtCounts => clipAtCounts.Value == clip).Key);
            yield return new WaitForSeconds(clip.length);
        }

        [ClientRpc]
        private void PlayClipAtIndexClientRpc(StationsRemainingCounts clipIndex)
        {
            var clip = m_countClips[clipIndex];
            if (clip == null) { return; }
            _ = StartCoroutine(PlayClip(clip));
        }

        private IEnumerator PlayClip(AudioClip clip)
        {
            if (GamePhaseManager.Instance.DebugSkipAudio || clip == null) { yield break; }
            m_audioSource.PlayOneShot(clip);
            yield return new WaitUntil(() => !m_audioSource.isPlaying);
        }
    }
}
