// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.Player;
using Meta.Decommissioned.UI;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Tutorials
{
    /// <summary>
    ///  Class for accessing and displaying the contents of MiniGameInstructions assets.
    /// <seealso cref="MiniGameInstructions"/>
    /// <seealso cref="InstructionsLibrary"/>
    /// </summary>
    public class InstructionsLibraryBrowser : MonoBehaviour
    {
        [SerializeField, AutoSet] protected InstructionalBoard m_instructionalBoard;
        [SerializeField] protected InstructionsLibrary m_instructionsLibrary;
        [SerializeField] protected bool m_willPlayVoiceover = true;
        [SerializeField] protected bool m_muteAtPhaseStart = true;
        [SerializeField] protected bool m_closedBoardStopsAudio = true;

        protected PlayerStatus LocalPlayerStatus =>
            PlayerStatus.GetByPlayerId(PlayerManager.LocalPlayerId);

        protected Role LocalPlayerRole => PlayerRole.GetByPlayerId(PlayerManager.LocalPlayerId).CurrentRole;

        protected bool LocalPlayerIsCandidate => CommanderCandidateManager.Instance
            .CommanderCandidates
            .Contains(NetworkManager.Singleton.SpawnManager
                .GetLocalPlayerObject().GetOwnerPlayerId() ?? PlayerId.New());

        protected bool LocalPlayerIsCommander => LocalPlayerStatus.CurrentStatus == PlayerStatus.Status.Commander;

        [SerializeField, AutoSet] protected AudioSource m_audioSource;

        private void Start()
        {
            _ = StartCoroutine(WaitForPlayerConnection());

            if (!m_willPlayVoiceover) { return; }

            GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;

            if (m_audioSource == null)
            {
                Debug.LogWarning($"{gameObject.name}: InstructionsBrowser could not find audio source! Tutorial audio will" +
                          " not play.");
                m_willPlayVoiceover = false;
            }

            IEnumerator WaitForPlayerConnection()
            {
                yield return new WaitUntil(() => NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject() != null);
                ShowMainInstructions();
            }
        }

        private void OnPhaseChanged(Phase phase)
        {
            m_audioSource.Stop();
            m_audioSource.mute = m_muteAtPhaseStart;
        }

        private void ShowInstructions(InstructionsLibraryKey key) =>
            m_instructionalBoard.SetInstructionsText(GetMiniGameInstructions(key));

        protected string GetMiniGameInstructions(InstructionsLibraryKey key, bool showAlternateInstructions = false) =>
            LocalPlayerRole != Role.Mole
                ? m_instructionsLibrary.Library[key].MiniGameCrewInstructions[showAlternateInstructions ? 1 : 0]
                : m_instructionsLibrary.Library[key].MiniGameMoleInstructions[showAlternateInstructions ? 1 : 0];

        protected void PlayVoiceover(MiniGameInstructions instructions, bool playAlternateInstructions = false)
        {
            if (m_audioSource == null || !m_willPlayVoiceover) { return; }

            if (m_audioSource.isPlaying) { m_audioSource.Stop(); }

            var clip = LocalPlayerRole != Role.Mole
                ? instructions.CrewInstructionsAudioClips[playAlternateInstructions ? 1 : 0]
                : instructions.MoleInstructionsAudioClips[playAlternateInstructions ? 1 : 0];

            if (clip == null)
            {
                Debug.LogWarning($"{gameObject.name}: MiniGameInstructions have no clip! Audio will not play.", this);
                return;
            }

            m_audioSource.PlayOneShot(clip);
        }

        public void SetVoiceoverMuted(bool mute)
        {
            if (m_audioSource == null) { return; }
            m_audioSource.Stop();
            m_audioSource.mute = mute;
        }

        protected string GetPlayerStatusInstructions(MiniGameInstructions instructions) =>
            GetMiniGameInstructions(instructions.InstructionsKey, LocalPlayerIsCommander);

        protected string GetPlayerVotingInstructions(MiniGameInstructions instructions) =>
            GetMiniGameInstructions(instructions.InstructionsKey, LocalPlayerIsCandidate);

        /// <summary>
        /// Shows the default "How To Play" instructions on the instructional board.
        /// </summary>
        [ContextMenu("Show Main Instructions (Debug)")]
        public void ShowMainInstructions() => ShowInstructions(InstructionsLibraryKey.Main);
    }
}
