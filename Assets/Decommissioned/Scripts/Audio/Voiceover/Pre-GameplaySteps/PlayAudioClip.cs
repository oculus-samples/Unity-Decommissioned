// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.Decommissioned.Game;
using Meta.Utilities;
using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.Audio.Voiceover
{
    [MetaCodeSample("Decommissioned")]
    [RequireComponent(typeof(AudioSource))]
    public abstract class PlayAudioClip : PreGameplayStep
    {
        [SerializeField, AutoSet]
        protected AudioSource m_audioSource;

        public override IEnumerator Run()
        {
            if (!IsServer)
            {
                IsComplete = true;
                yield break;
            }
        }

        public override void End() { }

        protected IEnumerator PlayClip(AudioClip clip)
        {
            if (GamePhaseManager.Instance.DebugSkipAudio || clip == null) { yield break; }
            m_audioSource.PlayOneShot(clip);
            yield return new WaitUntil(() => !m_audioSource.isPlaying);
        }
    }
}
