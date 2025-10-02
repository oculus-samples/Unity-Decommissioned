// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Utilities;
using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.Tutorials
{
    /// <summary>
    /// Class for accessing and displaying the contents of MiniGameInstructions assets. This is a subclass
    /// of InstructionsLibraryBrowser that allows us to retrieve and interate through a special set of instructions
    /// using indexes.
    /// <seealso cref="MiniGameInstructions"/>
    /// <seealso cref="MiniGameInstructions"/>
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class IndexedInstructionsBrowser : InstructionsLibraryBrowser
    {
        [SerializeField] private MiniGameInstructions[] m_indexedInstructions;
        private int m_currentPage = 1;

        private void Awake() => GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;

        private void OnPhaseChanged(Phase newPhase)
        {
            if (m_instructionalBoard.IsDisplaying) { ShowInstructionPageAtIndex(m_currentPage); }
        }

        public void ShowInstructionPageAtIndex(int pageIndex)
        {
            if (!m_instructionalBoard.IsOwner) { return; }
            if (pageIndex >= m_indexedInstructions.Length || pageIndex < 0) { return; }

            var currentInstructions = m_indexedInstructions[pageIndex];
            var instructions = GetMiniGameInstructions(currentInstructions.InstructionsKey);

            var currentPhase = GamePhaseManager.CurrentPhase;

            if (currentPhase == Phase.Voting && currentInstructions.InstructionsKey == InstructionsLibraryKey.Vote)
            {
                instructions = GetPlayerVotingInstructions(currentInstructions);
            }

            if (currentPhase == Phase.Planning && currentInstructions.InstructionsKey == InstructionsLibraryKey.Plan)
            {
                instructions = GetPlayerStatusInstructions(currentInstructions);
            }

            m_instructionalBoard.SetInstructionsText(instructions);
            m_currentPage = pageIndex;

            if (!m_willPlayVoiceover || (!m_instructionalBoard.IsDisplaying)) { return; }

            PlayVoiceoverOnPage(m_currentPage);
        }

        public void ShowPageForCurrentPhase()
        {
            if (!m_instructionalBoard.IsOwner) { return; }

            var phasePageIndex = GamePhaseManager.CurrentPhase switch
            {
                Phase.Voting => m_indexedInstructions.IndexOf(m_indexedInstructions.SingleOrDefault(instructions =>
                    instructions.InstructionsKey == InstructionsLibraryKey.Vote)) ?? 0,
                Phase.Planning => m_indexedInstructions.IndexOf(m_indexedInstructions.SingleOrDefault(instructions =>
                    instructions.InstructionsKey == InstructionsLibraryKey.Plan)) ?? 0,
                Phase.Night => m_indexedInstructions.IndexOf(m_indexedInstructions.SingleOrDefault(instructions =>
                    instructions.InstructionsKey == InstructionsLibraryKey.Work)) ?? 0,
                Phase.Discussion => m_indexedInstructions.IndexOf(m_indexedInstructions.SingleOrDefault(instructions =>
                    instructions.InstructionsKey == InstructionsLibraryKey.Discuss)) ?? 0,
                _ => 0,
            };

            m_currentPage = phasePageIndex;
            ShowInstructionPageAtIndex(m_currentPage);
        }


        [ContextMenu("Show Next Page")]
        public void ShowNextInstructionPage()
        {
            m_currentPage++;
            if (m_currentPage >= m_indexedInstructions.Length) { m_currentPage = 0; }
            ShowInstructionPageAtIndex(m_currentPage);
        }


        [ContextMenu("Show Previous Page")]
        public void ShowPreviousInstructionPage()
        {
            m_currentPage--;
            if (m_currentPage <= 0) { m_currentPage = 0; }
            ShowInstructionPageAtIndex(m_currentPage);
        }

        /// <summary>
        /// If ClosedBoardStopsAudio is true, turns off the audio source when the instructional board is being hidden.
        /// </summary>
        /// <param name="boardShowing">If false, stop the currently playing voiceover.</param>
        public void CheckBoardStateForAudioInterrupt(bool boardShowing)
        {
            if (m_audioSource == null || !m_closedBoardStopsAudio || boardShowing) { return; }
            m_audioSource.Stop();
        }

        private void PlayVoiceoverOnPage(int pageIndex)
        {
            if (!m_instructionalBoard.IsOwner) { return; }

            if (pageIndex >= m_indexedInstructions.Length || pageIndex < 0)
            {
                Debug.LogWarning($"{gameObject.name}: InstructionsBrowser has no clip at index {pageIndex}! Clip will not play.",
                    this);
                return;
            }

            var currentPage = m_indexedInstructions[pageIndex];
            var currentPhase = GamePhaseManager.CurrentPhase;

            var needsAltVotingPage = currentPhase == Phase.Voting && LocalPlayerIsCandidate &&
                                     currentPage.InstructionsKey == InstructionsLibraryKey.Vote;

            var needsAltPlanningPage = currentPhase == Phase.Planning && LocalPlayerIsCommander &&
                                       currentPage.InstructionsKey == InstructionsLibraryKey.Plan;

            PlayVoiceover(currentPage, needsAltVotingPage || needsAltPlanningPage);
        }
    }
}
