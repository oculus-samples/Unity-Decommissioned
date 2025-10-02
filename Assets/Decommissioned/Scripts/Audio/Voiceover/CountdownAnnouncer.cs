// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using Meta.Utilities;
using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.Audio.Voiceover
{
    [MetaCodeSample("Decommissioned")]
    public class CountdownAnnouncer : MonoBehaviour
    {
        private int? m_lastCount;
        [SerializeField, AutoSet] private AudioSource m_source;
        [SerializeField] private AudioClip[] m_countdownClips;

        private void Update()
        {
            var timer = GamePhaseManager.Instance.PhaseTimer;
            if (timer == null) { return; }

            var count = Mathf.CeilToInt((float)timer.TimeRemaining);
            if (count == m_lastCount) { return; }

            m_lastCount = count;

            if (count < 0 || !(count < m_countdownClips?.Length)) { return; }

            var clip = m_countdownClips[count];
            if (clip != null) { m_source.PlayOneShot(clip); }
        }
    }
}
