// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Linq;
using UnityEngine;

namespace Meta.Decommissioned.Audio.Voiceover
{
    /**
     * Scriptable Object containing a unique audio clips for each role in the game: allows us to more easily retrieve
     * specific clips based on the player's role when necessary.
     */
    [CreateAssetMenu(menuName = "Decommissioned/Audio/Role Audio Clips")]
    public class RoleAudioClips : ScriptableObject
    {
        [Tooltip("The index of each clip should correspond to the position the local player takes at the mini game.")]
        [field: SerializeField] public AudioClip[] CrewAudioClips { get; private set; }

        [Tooltip("The index of each clip should correspond to the position the local player takes at the mini game.")]
        [field: SerializeField] public AudioClip[] MoleAudioClips { get; private set; }

        public AudioClip[] GetAllAudioClips => CrewAudioClips.Concat(MoleAudioClips).ToArray();

        public AudioClip GetLongestClip => GetAllAudioClips.OrderByDescending(clip => clip.length).First();
    }
}
