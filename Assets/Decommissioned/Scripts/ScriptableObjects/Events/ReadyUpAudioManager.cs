// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Lobby;
using Meta.Utilities;
using UnityEngine;

namespace Meta.Decommissioned.ScriptableObjects
{
    /**
     * Simple class for playing the appropriate audio based on the readying player's
     * new "Ready" status.
     */
    public class ReadyUpAudioManager : MonoBehaviour
    {
        [SerializeField, AutoSetFromChildren] private AudioSource m_readyUpAudio;
        [SerializeField] private AudioClip m_playerReadyAudioClip;
        [SerializeField] private AudioClip m_playerUnreadyAudioClip;

        public void PlayAudioForReadyStatus(ReadyUp.ReadyStatus readyStatus)
        {
            if (m_readyUpAudio == null || m_playerReadyAudioClip == null | m_playerUnreadyAudioClip == null) { return; }

            if (!readyStatus.IsPlayerReady)
            {
                m_readyUpAudio.PlayOneShot(m_playerUnreadyAudioClip);
                return;
            }

            m_readyUpAudio.PlayOneShot(m_playerReadyAudioClip);
        }
    }
}
