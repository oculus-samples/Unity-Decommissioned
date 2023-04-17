// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.Player;
using Meta.Decommissioned.ScriptableObjects;
using Meta.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Audio.Voiceover
{
    /**
     * Pre-gameplay step for calling out the player that won the Commander vote during the VOTE phase.
     */
    public class CommanderResultCallout : PreGameplayStep
    {
        [SerializeField, AutoSet] private AudioSource m_audioSource;
        [SerializeField] private EnumDictionary<PlayerColorConfig.GameColor, AudioClip> m_playerColorCalloutClips;

        public override IEnumerator Run() { yield return PlayCalloutAudio(); }

        public override void End() { }

        private IEnumerator PlayCalloutAudio()
        {
            yield return new WaitUntil(() => CommanderCandidateManager.Instance != null);
            var commander = CommanderCandidateManager.Instance.GetCommander();
            var commanderColor = PlayerColor.GetByPlayerId(commander).GameColor;
            var clip = m_playerColorCalloutClips[commanderColor];
            PlayClipClientRpc(m_playerColorCalloutClips.FirstOrDefault(x => x.Key == commanderColor).Key);
            yield return new WaitForSeconds(clip.length);
        }

        [ClientRpc]
        private void PlayClipClientRpc(PlayerColorConfig.GameColor commanderColor)
        {
            var clip = m_playerColorCalloutClips[commanderColor];
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
