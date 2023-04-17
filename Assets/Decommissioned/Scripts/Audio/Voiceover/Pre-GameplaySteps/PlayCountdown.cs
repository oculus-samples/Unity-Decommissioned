// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Collections.Generic;
using Meta.Decommissioned.Game;
using Meta.Utilities;
using ScriptableObjectArchitecture;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Audio.Voiceover
{
    /**
     * Pre-gameplay step for playing a countdown before starting a round.
     */
    public class PlayCountdown : PreGameplayStep
    {
        [SerializeField, AutoSet] private AudioSource m_audioSource;
        [SerializeField] private float m_delayBeforeCountdownStart = 0.5f;
        [SerializeField] private float m_countDelay = 1f;
        [SerializeField] private List<AudioClip> m_countdownClips;
        [SerializeField] private GameEvent m_countdownStartedEvent;
        [SerializeField] private GameEvent m_countdownEndedEvent;

        public override IEnumerator Run()
        {
            yield return new WaitForSeconds(m_delayBeforeCountdownStart);

            OnCountdownStartedClientRpc();
            foreach (var clip in m_countdownClips)
            {
                if (clip != null)
                {
                    PlayClipClientRpc(m_countdownClips.IndexOf(clip));
                    yield return new WaitForSeconds(m_countDelay);
                }
            }
            OnCountdownEndedClientRpc();
        }

        public override void End() { }

        [ClientRpc]
        private void PlayClipClientRpc(int clipIndex) => _ = StartCoroutine(PlayClip(m_countdownClips[clipIndex]));

        [ClientRpc]
        private void OnCountdownStartedClientRpc() => m_countdownStartedEvent.Raise();

        [ClientRpc]
        private void OnCountdownEndedClientRpc() => m_countdownEndedEvent.Raise();

        protected IEnumerator PlayClip(AudioClip clip)
        {
            if (GamePhaseManager.Instance.DebugSkipAudio || clip == null)
            {
                yield break;
            }
            m_audioSource.PlayOneShot(clip);
            yield return new WaitUntil(() => !m_audioSource.isPlaying);
        }
    }
}
