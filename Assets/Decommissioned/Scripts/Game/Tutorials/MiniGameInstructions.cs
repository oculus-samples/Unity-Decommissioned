// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.Tutorials
{
    /// <summary>
    ///  A scriptable object storing editable instructions, which can be retrieved and shown to the player.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    [CreateAssetMenu(menuName = "Decommissioned/MiniGame Instructions")]
    public class MiniGameInstructions : ScriptableObject
    {
        public InstructionsLibraryKey InstructionsKey => m_instructionsKey;

        [SerializeField] private InstructionsLibraryKey m_instructionsKey;

        [TextArea(2, 10)][SerializeField] protected string m_instructions;
        [TextArea(2, 10)][SerializeField] protected string m_alternateInstructions;
        [TextArea(2, 10)][SerializeField] protected string m_moleInstructions;
        [TextArea(2, 10)][SerializeField] protected string m_alternateMoleInstructions;

        [SerializeField] private AudioClip m_instructionsAudio;
        [SerializeField] private AudioClip m_alternateInstructionsAudio;
        [SerializeField] private AudioClip m_moleInstructionsAudio;
        [SerializeField] private AudioClip m_alternateMoleInstructionsAudio;

        public string[] MiniGameCrewInstructions => new[] { m_instructions, m_alternateInstructions };

        public AudioClip[] CrewInstructionsAudioClips => new[] { m_instructionsAudio, m_alternateInstructionsAudio };

        public AudioClip[] MoleInstructionsAudioClips => new[] { m_moleInstructionsAudio, m_alternateMoleInstructionsAudio };

        public string[] MiniGameMoleInstructions => new[] { m_moleInstructions, m_alternateMoleInstructions };
    }
}
