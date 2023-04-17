// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.Decommissioned.Player;
using Meta.Utilities;
using UnityEngine;
using UnityEngine.Audio;

namespace Meta.Decommissioned.Audio
{
    public enum AudioType
    {
        None,
        Master,
        SoundEffects,
        Music,
        Voice
    }

    /**
     * Singleton for managing all audio (sound effects, music, and voice chat) in the game.
     */
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private AudioMixer m_mixer;
        [SerializeField] private AudioMixerGroup m_soundGroup;

        private const string MASTER_VOLUME_PARAM = "Volume_Master";
        private const string MUSIC_VOLUME_PARAM = "Volume_Music";
        private const string SOUND_VOLUME_PARAM = "Volume_Sound";
        private const string VOICE_VOLUME_PARAM = "Volume_Voice";

        private const int MAX_WORLD_AUDIOS = 16;
        private int m_currentWorldAudios;

        private const float MAX_VOLUME = 0f;
        private const float MIN_VOLUME = -80f;

        private const float MAX_MUSIC_VOLUME = -20f;

        private new void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (UserSettings.Instance.GetPlayerSetting(VOICE_VOLUME_PARAM, out float voiceSetting))
            {
                SetAudioVolume(AudioType.Voice, voiceSetting);
            }
            if (UserSettings.Instance.GetPlayerSetting(SOUND_VOLUME_PARAM, out float soundSetting))
            {
                SetAudioVolume(AudioType.SoundEffects, soundSetting);
            }
            if (UserSettings.Instance.GetPlayerSetting(MUSIC_VOLUME_PARAM, out float musicSetting))
            {
                SetAudioVolume(AudioType.Music, musicSetting);
            }
        }

        /**
         * Given a value from a slider, set the volume of a given audio track.
         * <param name="type">The audio track/type (music, sound effects, or voice chat) to be adjusted.</param>
         * <param name="sliderValue">The value (from a slider) that will be converted into the new volume level of the
         * selected type.</param>
         */
        public void SetAudioVolume(AudioType type, float sliderValue)
        {
            if (type == AudioType.None)
            {
                Debug.LogError("Tried to set the volume of an audio mixer group with the type \"None\"!");
                return;
            }

            var maxVolume = type == AudioType.Music ? MAX_MUSIC_VOLUME : MAX_VOLUME;
            var minVolume = MIN_VOLUME;

            var volume = Mathf.Log10(Mathf.Clamp(sliderValue, 0.001f, 1f)) * (maxVolume - minVolume) / 2f + maxVolume;

            if (volume > maxVolume) { volume = maxVolume; }
            else if (volume < minVolume) { volume = minVolume; }

            switch (type)
            {
                case AudioType.Master:
                    _ = m_mixer.SetFloat(MASTER_VOLUME_PARAM, volume);
                    break;
                case AudioType.SoundEffects:
                    _ = m_mixer.SetFloat(SOUND_VOLUME_PARAM, volume);
                    break;
                case AudioType.Music:
                    _ = m_mixer.SetFloat(MUSIC_VOLUME_PARAM, volume);
                    break;
                case AudioType.Voice:
                    _ = m_mixer.SetFloat(VOICE_VOLUME_PARAM, volume);
                    break;
                default:
                    Debug.LogError("Unknown AudioType passed to SetAudioVolume in AudioManager!");
                    break;
            }
        }

        /// <summary>
        /// Plays a sound effect in world space at the given position.
        /// </summary>
        /// <param name="position">The position to play the sound at.</param>
        /// <param name="sound">The sound clip to play.</param>
        /// <returns>True if the sound was successfully played, false otherwise.</returns>
        public bool PlaySoundInSpace(Vector3 position, AudioClip sound)
        {
            if (m_currentWorldAudios >= MAX_WORLD_AUDIOS)
            {
                Debug.LogWarning("Tried to play a sound in world space but we are at the max audio limit!");
                return false;
            }

            m_currentWorldAudios++;
            var newSound = new GameObject(sound.name);
            newSound.transform.position = position;
            var audioSource = newSound.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.outputAudioMixerGroup = m_soundGroup;
            audioSource.clip = sound;
            audioSource.loop = false;
            audioSource.priority = 100 + m_currentWorldAudios;
            audioSource.spatialBlend = 1;
            audioSource.time = 0;
            audioSource.Play();

            _ = StartCoroutine(WaitForAudioSource(audioSource));

            return true;
        }

        private IEnumerator WaitForAudioSource(AudioSource source)
        {
            yield return new WaitUntil(() => !source.isPlaying);

            Destroy(source.gameObject);
            m_currentWorldAudios--;
        }
    }
}
