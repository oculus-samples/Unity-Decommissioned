// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Multiplayer.Core;
using Meta.Utilities;
using Photon.Voice.Unity;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Player
{
    /// <summary>
    /// Manages user voice chat; allows us to retrieve and set the state (mute / unmute) of voice for
    /// the receiving player(s).
    /// </summary>
    public class PlayerVoip : Multiton<PlayerVoip>
    {
        [SerializeField, AutoSet] protected Speaker m_voipSpeaker;
        [SerializeField, AutoSet] protected AudioSource m_voipAudio;

        public NetworkObject NetworkObject => VoipController.Instance.GetPlayer(m_voipSpeaker);

        public void SetMuted(bool mute)
        {
            if (m_voipAudio != null)
            {
                m_voipAudio.mute = mute;
            }
        }
    }
}
