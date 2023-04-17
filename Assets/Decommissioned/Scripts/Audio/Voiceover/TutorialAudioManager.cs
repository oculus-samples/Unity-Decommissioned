// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.Lobby;
using Meta.Decommissioned.Player;
using Meta.Multiplayer.Networking;
using Meta.Utilities;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Audio.Voiceover
{
    /**
     * Class for managing tutorial audio clips for mini games. Allows us to play them at certain times,
     * and determine if and when they should be repeated.
     */
    public class TutorialAudioManager : MonoBehaviour
    {
        [SerializeField, AutoSet] private AudioSource m_tutorialAudioSource;
        [SerializeField, Required] private GamePosition m_assignedGamePosition;

        [SerializeField] private bool m_tutorialsEnabled = true;
        [SerializeField] private bool m_alwaysPlayInOrder = true;
        [SerializeField] private bool m_resetOnFinish = true;
        [SerializeField] private bool m_repeatTutorialEachRound;
        [SerializeField] private bool m_repeatTutorialStepsAfterWait;
        [SerializeField] private float m_tutorialStepRepeatWaitTime = 15f;

        [SerializeField] private AudioClip m_tutorialCrewIntro;
        [SerializeField] private AudioClip m_tutorialMoleIntro;

        [Tooltip("List of clips that will be played in this tutorial for crew. Indexing starts at 1.")]
        [SerializeField] private AudioClip[] m_tutorialAudioClips;

        [Tooltip("List of clips that will be played in this tutorial for moles. Indexing starts at 1.")]
        [SerializeField] private AudioClip[] m_moleTutorialAudioClips;

        private Dictionary<int, AudioClip> m_crewTutorialSteps = new();
        private Dictionary<int, AudioClip> m_moleTutorialSteps = new();

        private bool PlayerIsAtStation => m_assignedGamePosition.IsOccupied && m_assignedGamePosition.OccupyingPlayer.IsLocalPlayer;

        private Role LocalPlayerRole => m_assignedGamePosition.OccupyingPlayer != null
            ? PlayerRole.GetByPlayerId(m_assignedGamePosition.OccupyingPlayer.GetOwnerPlayerId().Value).CurrentRole
            : Role.Unknown;

        private bool AllClipsPlayed => LocalPlayerRole == Role.Crewmate
            ? m_playedClipIndexes.Count == m_crewTutorialSteps.Count
            : m_playedClipIndexes.Count == m_moleTutorialSteps.Count;

        private Coroutine m_repeatingClipRoutine;
        private List<int> m_playedClipIndexes = new();
        private float m_minClipWaitTime = 0.5f;

        private void OnEnable()
        {
            if (m_assignedGamePosition == null || m_tutorialAudioSource == null)
            {
                Debug.LogError($"{gameObject.name}: TutorialAudioManager hasn't been configured correctly! Tutorial" +
                                             " clips will not play.");
                return;
            }

            if (m_tutorialAudioClips.Length == 0 || m_moleTutorialAudioClips.Length == 0) { return; }
            m_crewTutorialSteps = m_tutorialAudioClips.ToDictionary(clip => m_tutorialAudioClips.IndexOf(clip) + 1 ?? 0);
            m_moleTutorialSteps = m_moleTutorialAudioClips.ToDictionary(clip => m_moleTutorialAudioClips.IndexOf(clip) + 1 ?? 0);
            m_assignedGamePosition.OnOccupyingPlayerChanged += OnPlayerChanged;
        }

        private void OnDisable() => m_assignedGamePosition.OnOccupyingPlayerChanged -= OnPlayerChanged;

        private void OnPlayerChanged(NetworkObject prevPlayer, NetworkObject player)
        {
            if (m_repeatTutorialEachRound) { ResetTutorial(true); }
        }

        /**
         * Set whether or not any of the tutorials clips can be triggered.
         */
        public void ToggleTutorials(bool willEnableTutorials)
        {
            m_tutorialsEnabled = willEnableTutorials;
            if (m_repeatingClipRoutine != null) { StopCoroutine(m_repeatingClipRoutine); }
        }

        /**
         * Play the audio clip corresponding to the given step of the tutorial.
         * <param name="tutorialStep">Determines which tutorial step will be played. Indexing starts at 1.</param>
         */
        public void PlayTutorialStep(int tutorialStep)
        {
            if (!PlayerIsAtStation || !m_tutorialsEnabled) { return; }

            var tutorialClips = LocalPlayerRole == Role.Crewmate ? m_crewTutorialSteps : m_moleTutorialSteps;

            if (tutorialStep < 0 || tutorialStep > tutorialClips.Count)
            {
                Debug.LogError($"{gameObject.name}: no tutorial clip at index {tutorialStep}!");
                return;
            }

            var playingOutOfOrder = tutorialStep != 0 && m_playedClipIndexes.LastOrDefault() != tutorialStep - 1;
            if (m_playedClipIndexes.Contains(tutorialStep) || (m_alwaysPlayInOrder && playingOutOfOrder)) { return; }

            if (m_repeatingClipRoutine != null) { StopCoroutine(m_repeatingClipRoutine); }
            var clipToPlay = tutorialClips[tutorialStep];
            _ = StartCoroutine(WaitForCurrentClipBeforePlaying(clipToPlay));

            m_playedClipIndexes.Add(tutorialStep);

            if (m_repeatTutorialStepsAfterWait && !AllClipsPlayed)
            {
                m_repeatingClipRoutine = StartCoroutine(WaitToRepeatStep(clipToPlay));
            }
        }

        /**
         * Play this tutorial's intro audio clip; this is usually played before the others separately.
         */
        private void PlayTutorialIntro()
        {
            if (!m_tutorialsEnabled || !PlayerIsAtStation) { return; }
            _ = LocalPlayerRole == Role.Crewmate
                ? StartCoroutine(WaitForCurrentClipBeforePlaying(m_tutorialCrewIntro))
                : StartCoroutine(WaitForCurrentClipBeforePlaying(m_tutorialMoleIntro));
        }

        /**
         * Replay the most recently played clip.
         */
        public void ReplayLastClip(bool instructionsToggledOn)
        {
            if (m_tutorialAudioSource.isPlaying || !m_tutorialsEnabled || !PlayerIsAtStation || !instructionsToggledOn) { return; }
            var tutorialClips = LocalPlayerRole == Role.Crewmate ? m_crewTutorialSteps : m_moleTutorialSteps;
            var roleIntro = LocalPlayerRole == Role.Crewmate ? m_tutorialCrewIntro : m_tutorialMoleIntro;
            var clipToPlay = m_playedClipIndexes.LastOrDefault() == default ? roleIntro : tutorialClips[m_playedClipIndexes.LastOrDefault()];
            _ = StartCoroutine(WaitForCurrentClipBeforePlaying(clipToPlay));
        }

        /**
         * Clear out our record of played clips; this allows us to repeat the tutorial again.
         */
        [Button("Reset Tutorials (Reset Clip Queue)")]
        public void ResetTutorial(bool ignoreTutorialProgress = false)
        {
            if (m_resetOnFinish && !AllClipsPlayed && !ignoreTutorialProgress) { return; }
            if (m_repeatingClipRoutine != null) { StopCoroutine(m_repeatingClipRoutine); }
            if (m_resetOnFinish)
            {
                m_tutorialsEnabled = true;
                if (!ignoreTutorialProgress) { PlayTutorialIntro(); }
            }

            m_playedClipIndexes.Clear();
        }

        /**
         * Checks a boolean value for possible audio interrupt.
         * <param name="boardShown">Indicate whether the board is shown or not. If false, stops all audio.</param>
         */
        public void CheckForAudioInterrupt(bool boardShown) { if (!boardShown) { m_tutorialAudioSource.Stop(); } }

        private IEnumerator WaitToRepeatStep(AudioClip clip)
        {
            while (m_repeatTutorialStepsAfterWait && m_tutorialsEnabled)
            {
                yield return new WaitForSeconds(m_tutorialStepRepeatWaitTime);
                yield return WaitForCurrentClipBeforePlaying(clip);
            }
        }

        private IEnumerator WaitForCurrentClipBeforePlaying(AudioClip clip)
        {
            yield return new WaitUntil(() => !m_tutorialAudioSource.isPlaying);
            yield return new WaitForSeconds(m_minClipWaitTime);
            m_tutorialAudioSource.PlayOneShot(clip);
        }
    }
}
