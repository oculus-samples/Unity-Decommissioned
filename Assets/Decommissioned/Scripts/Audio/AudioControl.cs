// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.Audio
{
    /// <summary>
    /// Provides programmatic control over a specific audio mixer group.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class AudioControl : MonoBehaviour
    {
        [SerializeField] private AudioType m_audioType = AudioType.None;
        public void SetAudioVolume(float volume)
        {
            if (m_audioType == AudioType.None)
            {
                Debug.LogError("Tried to set the volume of an audio mixer group with the type \"None\"!");
                return;
            }

            AudioManager.Instance.SetAudioVolume(m_audioType, volume);
        }
    }
}
