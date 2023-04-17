// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Utilities;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.Decommissioned.Audio
{
    /// <summary>
    /// Contains all of the logic for the dynamic in-game music player 
    /// </summary>
    public class MusicTrackManager : Singleton<MusicTrackManager>
    {
        /// <summary>
        /// Contains all of the supported clips to be used by the Jukebox.
        /// </summary>
        public enum JukeboxClip
        {
            None,
            Menu_Percussion,
            Menu_Choir_Female,
            Menu_Choir_Male,
            Menu_DistantSynth,
            Menu_Galactic_Choir,
            Menu_SquareRattle,
            Transition_ArpPad,
            Transition_BassPad,
            Transition_FadingMelody,
            Transition_LoomingPad,
            Transition_OrchestralHits,
            Transition_SpacePad,
            Gameplay_PercussionLoop1,
            Gameplay_PercussionLoop2,
            Gameplay_PercussionLoop3,
            Gameplay_PercussionLoop3_Cont,
            Gameplay_PercussionLoop3_ContEnd,
            Gameplay_AltPianoHold,
            Gameplay_AltPianoPad,
            Gameplay_AltPianoStabs,
            Gameplay_AltPianoStabs_Funkadeliks,
            Gameplay_BassSlideMelody,
            Gameplay_MainMelody,
            Gameplay_PhatBass,
            Gameplay_PhatBass_NoTail,
            Gameplay_SynthBassHarmony,
            Gameplay_UnderlyingPadMelody
        }

        [SerializeField]
        [Tooltip("The prefab for the speaker that will be created to play each audio channel.")]
        private GameObject m_speakerPrefab;

        [SerializeField]
        [Tooltip("How many channels the jukebox can play at one time. This option can impact performance.")]
        private int m_instrumentalChannelCount = 4;

        [SerializeField]
        [Tooltip("The list of all audio clips the jukebox can play.")]
        private EnumDictionary<JukeboxClip, AudioClip> m_audioBank = new();

#if !UNITY_EDITOR
    private string m_lastUnloadedSceneName = "";
#endif

        private List<AudioSource> m_speakers = new();
        private JukeboxClip[] m_clipQueue = new JukeboxClip[8];
        private JukeboxClip[] m_clipRemovalQueue = new JukeboxClip[8];

        private int m_lastBarCount;
        private int m_barCount;
        private int m_totalBarCount;
        private int m_barInterval = 32;

        private float m_currentMusicTime;
        private float m_maxMusicTime = 16.696f;
        private bool m_canQueueClips = true;
        private bool m_restartingGameLoop;

        protected new void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);

            for (var i = 0; i < m_clipQueue.Length; i++)
            {
                m_clipQueue[i] = JukeboxClip.None;
                m_clipRemovalQueue[i] = JukeboxClip.None;
            }
            InitializeInstrumentalChannels();
        }

        protected new void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        protected void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            var sceneName = scene.name;

#if !UNITY_EDITOR
            if (m_lastUnloadedSceneName == Application.STARTUP_SCENE && sceneName == Application.MAIN_MENU_SCENE)
            {
                return;
            }
#endif

            if (IsValidSceneForJukebox(sceneName))
            {
                //Clear the queue in case some clips were queued over time and we switched scenes before that time.
                CancelAllQueuedClips();
                //Stop all currently playing speakers to fade to new music
                StopAllSpeakers();
            }

            if (sceneName is Application.STARTUP_SCENE or Application.MAIN_MENU_SCENE)
            {
                if (!IsClipPlaying(JukeboxClip.Menu_Galactic_Choir))
                {
                    QueueAudioClip(JukeboxClip.Menu_Galactic_Choir);
                }
                if (!IsClipPlaying(JukeboxClip.Menu_DistantSynth))
                {
                    QueueAudioClipOnBar(JukeboxClip.Menu_DistantSynth, 8);
                }
                if (!IsClipPlaying(JukeboxClip.Menu_Choir_Female))
                {
                    QueueAudioClipOnBar(JukeboxClip.Menu_Choir_Female, 16);
                }
                if (!IsClipPlaying(JukeboxClip.Menu_Choir_Male))
                {
                    QueueAudioClipOnBar(JukeboxClip.Menu_Choir_Male, 16);
                }
            }
            else if (sceneName == Application.LOBBY_SCENE)
            {
                //Subscribe to gameplay listeners
                GameManager.OnGameStateChanged += OnGameManagerOnStateChanged;
                GamePhaseManager.Instance.OnPhaseChanged += OnGamePhaseManagerOnPhaseChanged;
                CancelAllQueuedClips();
                _ = StopAudioClip(JukeboxClip.Menu_Galactic_Choir);
                _ = StopAudioClip(JukeboxClip.Menu_DistantSynth);
                _ = StopAudioClip(JukeboxClip.Menu_Choir_Female);
                _ = StopAudioClip(JukeboxClip.Menu_Choir_Male);
                //Gameplay tracks when game has been reset
                _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_SynthBassHarmony);
                _ = AddClipToRemovalQueue(JukeboxClip.Transition_FadingMelody);
                _ = AddClipToRemovalQueue(JukeboxClip.Transition_BassPad);
                _ = AddClipToRemovalQueue(JukeboxClip.Transition_LoomingPad);
                _ = AddClipToRemovalQueue(JukeboxClip.Transition_SpacePad);
                _ = AddClipToRemovalQueue(JukeboxClip.Transition_OrchestralHits);
                _ = AddClipToRemovalQueue(JukeboxClip.Transition_ArpPad);

                QueueAudioClip(JukeboxClip.Gameplay_MainMelody);
                QueueAudioClip(JukeboxClip.Gameplay_AltPianoPad);
            }
        }
        private void OnSceneUnloaded(Scene scene)
        {
            var sceneName = scene.name;
#if !UNITY_EDITOR
        m_lastUnloadedSceneName = sceneName;
#endif
            if (sceneName == Application.MAIN_MENU_SCENE)
            {
                //Unsubscribe gameplay listeners
                GameManager.OnGameStateChanged -= OnGameManagerOnStateChanged;
                if (GamePhaseManager.Instance)
                {
                    GamePhaseManager.Instance.OnPhaseChanged -= OnGamePhaseManagerOnPhaseChanged;
                }
            }
        }

        private void OnGameManagerOnStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.ReadyUp:
                    break;

                case GameState.Gameplay:
                    if (!m_restartingGameLoop) { break; }
                    // Game state and game phase may change in a different order for clients; we ensure this is called after the game state change here
                    if (!NetworkManager.Singleton.IsHost) { OnGamePhaseManagerOnPhaseChanged(Phase.Voting); }
                    break;

                case GameState.GameEnd:
                    CancelAllQueuedClips();
                    StopAllSpeakers();
                    QueueAudioClip(JukeboxClip.Gameplay_SynthBassHarmony);
                    StopAudioClipOnBar(JukeboxClip.Gameplay_SynthBassHarmony, 8);
                    QueueAudioClip(JukeboxClip.Transition_FadingMelody);
                    QueueAudioClip(JukeboxClip.Transition_BassPad);
                    StopAudioClipOnBar(JukeboxClip.Transition_BassPad, 32);
                    QueueAudioClip(JukeboxClip.Transition_LoomingPad);
                    StopAudioClipOnBar(JukeboxClip.Transition_LoomingPad, 32);
                    QueueAudioClipOnBar(JukeboxClip.Transition_SpacePad, 8);
                    QueueAudioClipOnBar(JukeboxClip.Transition_OrchestralHits, 16);
                    QueueAudioClipOnBar(JukeboxClip.Transition_ArpPad, 24);
                    m_restartingGameLoop = true;
                    break;

                default:
                    break;
            }
        }

        private void OnGamePhaseManagerOnPhaseChanged(Phase newPhase)
        {
            if (GameManager.Instance.State == GameState.GameEnd) { return; }
            CancelAllQueuedClips();

            var currentRound = GameManager.Instance.CurrentRoundCount;
            switch (newPhase)
            {
                case Phase.Invalid:
                    ClearQueue();
                    StopAllSpeakers();
                    break;

                case Phase.Planning:
                    //Planning and discussion have the same audio cues, but are separate here to stop the appropriate clips
                    _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_PhatBass_NoTail);
                    _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_AltPianoStabs);
                    _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_AltPianoStabs_Funkadeliks);

                    QueueAudioClip(JukeboxClip.Gameplay_MainMelody);
                    QueueAudioClip(JukeboxClip.Gameplay_AltPianoPad);
                    QueueAudioClip(JukeboxClip.Gameplay_AltPianoStabs);
                    QueueAudioClip(JukeboxClip.Gameplay_BassSlideMelody);
                    QueueAudioClip(JukeboxClip.Gameplay_PercussionLoop3);
                    break;

                case Phase.Discussion:
                    //Planning and discussion have the same audio cues, but are separate here to stop the appropriate clips
                    _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_PhatBass_NoTail);
                    _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_AltPianoHold);
                    _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_AltPianoStabs_Funkadeliks);
                    _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_UnderlyingPadMelody);
                    _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_SynthBassHarmony);
                    _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_PercussionLoop1);
                    _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_PercussionLoop3);

                    QueueAudioClip(JukeboxClip.Gameplay_MainMelody);
                    QueueAudioClip(JukeboxClip.Gameplay_AltPianoPad);
                    QueueAudioClip(JukeboxClip.Gameplay_AltPianoStabs);
                    QueueAudioClip(JukeboxClip.Gameplay_BassSlideMelody);
                    QueueAudioClip(JukeboxClip.Gameplay_PercussionLoop3);
                    break;

                case Phase.Night:
                    _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_AltPianoPad);
                    _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_AltPianoStabs);
                    _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_BassSlideMelody);

                    if (currentRound == 1)
                    {
                        _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_PercussionLoop3);

                        QueueAudioClip(JukeboxClip.Gameplay_MainMelody);
                        QueueAudioClip(JukeboxClip.Gameplay_PhatBass_NoTail);
                        QueueAudioClip(JukeboxClip.Gameplay_AltPianoStabs_Funkadeliks);
                        QueueAudioClip(JukeboxClip.Gameplay_UnderlyingPadMelody);
                        QueueAudioClip(JukeboxClip.Gameplay_PercussionLoop1);
                    }
                    else if (currentRound == 2)
                    {
                        _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_MainMelody);
                        QueueAudioClip(JukeboxClip.Gameplay_AltPianoHold);
                        QueueAudioClip(JukeboxClip.Gameplay_PhatBass_NoTail);
                        QueueAudioClip(JukeboxClip.Gameplay_AltPianoStabs_Funkadeliks);
                        QueueAudioClip(JukeboxClip.Gameplay_UnderlyingPadMelody);
                        QueueAudioClip(JukeboxClip.Gameplay_PercussionLoop3);
                    }
                    else if (currentRound >= 3)
                    {
                        _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_MainMelody);
                        QueueAudioClip(JukeboxClip.Gameplay_AltPianoHold);
                        QueueAudioClip(JukeboxClip.Gameplay_SynthBassHarmony);
                        QueueAudioClip(JukeboxClip.Gameplay_PhatBass_NoTail);
                        QueueAudioClip(JukeboxClip.Gameplay_AltPianoStabs_Funkadeliks);
                        QueueAudioClip(JukeboxClip.Gameplay_UnderlyingPadMelody);
                        QueueAudioClip(JukeboxClip.Gameplay_PercussionLoop3);
                    }
                    break;
                case Phase.Voting:

                    // Ensuring the end game music is stopped properly when starting a new game for all players
                    if (m_restartingGameLoop)
                    {
                        CancelAllQueuedClips();
                        StopAllSpeakers();
                        m_restartingGameLoop = false;
                    }

                    if (currentRound <= 1)
                    {
                        _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_MainMelody);
                        _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_AltPianoPad);

                        QueueAudioClip(JukeboxClip.Gameplay_PhatBass_NoTail);
                        QueueAudioClip(JukeboxClip.Gameplay_AltPianoStabs);
                    }
                    else
                    {
                        _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_MainMelody);
                        _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_AltPianoPad);
                        _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_BassSlideMelody);
                        _ = AddClipToRemovalQueue(JukeboxClip.Gameplay_PercussionLoop3);

                        QueueAudioClip(JukeboxClip.Gameplay_PhatBass_NoTail);
                        QueueAudioClip(JukeboxClip.Gameplay_AltPianoStabs_Funkadeliks);
                    }

                    break;

                default:
                    break;
            }
        }

        private void Update() => ProcessClipQueue();

        private void ProcessClipQueue()
        {
            UpdateSync();

            while (GetClipQueueLength() > 0 && CanQueueClips() && IsAnySpeakerUnoccupied())
            {
                var nextAvailableSpeaker = GetNextUnoccupiedSpeaker();
                if (nextAvailableSpeaker == -1)
                {
                    Debug.LogError("Error processing the Jukebox clip queue! No speakers were deemed unoccupied but we were told there was one.");
                    break;
                }

                var nextInQueue = GetNextClipInQueue();
                if (nextInQueue != JukeboxClip.None)
                {
                    PlayAudioClip(nextInQueue, nextAvailableSpeaker);
                    RemoveClipFromQueue(nextInQueue);
                }
            }
            if (GetClipRemovalQueueLength() > 0 && CanQueueClips())
            {
                RemoveClipsInRemovalQueue();
            }
        }

        private void RemoveClipsInRemovalQueue()
        {
            for (var i = 0; i < m_clipRemovalQueue.Length; i++)
            {
                if (m_clipRemovalQueue[i] == JukeboxClip.None) { continue; }
                _ = StopAudioClip(m_clipRemovalQueue[i]);
                m_clipRemovalQueue[i] = JukeboxClip.None;
            }
        }

        private void InitializeInstrumentalChannels()
        {
            for (var i = 0; i < m_instrumentalChannelCount; i++)
            {
                var channel = Instantiate(m_speakerPrefab, transform);
                if (channel && channel.TryGetComponent(out AudioSource speaker)) { AddSpeaker(speaker); }
                else
                {
                    Debug.LogError("Error initializing speakers for the MusicManager!");
                    break;
                }
            }
        }

        private void UpdateSync()
        {
            m_canQueueClips = false;
            m_currentMusicTime += Time.unscaledDeltaTime;

            if (m_currentMusicTime >= m_maxMusicTime)
            {
                m_currentMusicTime -= m_maxMusicTime;
            }
            m_barCount = (int)(m_currentMusicTime * 114 / 60) % m_barInterval;

            if (m_lastBarCount != m_barCount)
            {
                m_lastBarCount = m_barCount;
                m_totalBarCount++;
                m_canQueueClips = true;
            }

            if (AreAllSpeakersUnoccupied())
            {
                m_barCount = 0;
                m_currentMusicTime = 0;
                m_canQueueClips = true;
            }
        }

        private void AddSpeaker(AudioSource speaker)
        {
            if (m_speakers.Contains(speaker))
            {
                Debug.LogWarning("Tried to add a speaker to the jukebox that was already added!");
                return;
            }

            m_speakers.Add(speaker);
        }

        private void RemoveSpeaker(AudioSource speaker)
        {
            if (!m_speakers.Remove(speaker))
            {
                Debug.LogWarning("Tried to remove a speaker from the jukebox that was not in the speakers list!");
            }
        }

        /// <summary>
        /// Queues a <see cref="JukeboxClip"/> for the jukebox to play on the next available bar.
        /// </summary>
        /// <param name="clip">The <see cref="JukeboxClip"/> to queue up.</param>
        /// <param name="time">A delay before the <see cref="JukeboxClip"/> is actually queued up.</param>
        public void QueueAudioClip(JukeboxClip clip, float time = 0)
        {
            if (IsClipPlaying(clip))
            {
                Debug.LogWarning("Tried to queue a clip that was already playing! Ignoring...");
                return;
            }

            if (time > 0)
            {
                _ = StartCoroutine(QueueAudioClipAfterTime(clip, time));
                return;
            }

            AddClipToQueue(clip);
        }

        private IEnumerator QueueAudioClipAfterTime(JukeboxClip clip, float time)
        {
            yield return new WaitForSeconds(time);
            AddClipToQueue(clip);
        }

        /// <summary>
        /// Queues a <see cref="JukeboxClip"/> for the jukebox to play after the amount of bars specified.
        /// </summary>
        /// <param name="clip">The <see cref="JukeboxClip"/> to queue up.</param>
        /// <param name="time">The amount of bars to wait before the <see cref="JukeboxClip"/> is actually queued up.</param>
        public void QueueAudioClipOnBar(JukeboxClip clip, int barCount = 0)
        {
            if (barCount > 0)
            {
                _ = StartCoroutine(QueueAudioClipAfterBar(clip, barCount));
                return;
            }

            AddClipToQueue(clip);
        }

        private IEnumerator QueueAudioClipAfterBar(JukeboxClip clip, int barCount)
        {
            var queuedBar = m_totalBarCount;
            yield return new WaitUntil(() => m_totalBarCount >= queuedBar + barCount);

            AddClipToQueue(clip);
        }

        /// <summary>
        /// Removes a <see cref="JukeboxClip"/> from the queue of clips.
        /// </summary>
        /// <param name="clip">The <see cref="JukeboxClip"/> to remove from the queue.</param>
        public void UnQueueAudioClip(JukeboxClip clip) => RemoveClipFromQueue(clip);

        private void AddClipToQueue(JukeboxClip clip)
        {
            if (m_clipQueue.Contains(clip))
            {
                Debug.LogWarning("Tried to queue a JukeboxClip that was already in the queue! You must wait for the previous one to exit queue.");
                return;
            }

            var nextAvailableSlot = GetNextOpenClipQueueSpot();
            if (nextAvailableSlot == -1)
            {
                Debug.LogWarning($"The jukebox clip queue is full! Unable to queue clip {clip}");
                return;
            }
            m_clipQueue[nextAvailableSlot] = clip;
        }

        private bool AddClipToRemovalQueue(JukeboxClip clip)
        {
            if (!IsClipPlaying(clip)) { return false; }

            if (m_clipRemovalQueue.Contains(clip))
            {
                Debug.LogWarning("Tried to stop a JukeboxClip that was already in the removal queue! You must wait for the previous one to exit queue.");
                return false;
            }

            var nextAvailableSlot = GetNextOpenClipRemovalQueueSpot();
            if (nextAvailableSlot == -1)
            {
                Debug.LogWarning($"The jukebox clip removal queue is full! Unable to queue clip {clip} for removal. Stopping without queue instead...");
                _ = StopAudioClip(clip);
                return false;
            }
            m_clipRemovalQueue[nextAvailableSlot] = clip;
            return true;
        }

        private void RemoveClipFromQueue(JukeboxClip clip)
        {
            var clipIndex = GetIndexOfClipInQueue(clip);
            if (clipIndex == -1)
            {
                Debug.LogWarning("Tried to remove a JukeboxClip from the queue that wasn't in the queue!");
            }
            else { m_clipQueue[clipIndex] = JukeboxClip.None; }
        }

        private void ClearQueue()
        {
            for (var i = 0; i < m_clipQueue.Length; i++) { m_clipQueue[i] = JukeboxClip.None; }
        }

        private void PlayAudioClip(JukeboxClip clip, int speaker, bool fadeIn = true, float fadeSpeed = .005f)
        {
            var currentPlayTime = m_currentMusicTime;
            if (currentPlayTime >= m_audioBank[clip].length)
            {
                currentPlayTime = Mathf.Repeat(m_currentMusicTime, m_audioBank[clip].length);
            }


            if (fadeIn)
            {
                m_speakers[speaker].clip = m_audioBank[clip];
                m_speakers[speaker].volume = 0;
                m_speakers[speaker].time = 0;
                m_speakers[speaker].Play();
                m_speakers[speaker].time = currentPlayTime;
                _ = StartCoroutine(FadeInSpeaker(speaker, fadeSpeed));
            }
            else
            {
                m_speakers[speaker].clip = m_audioBank[clip];
                m_speakers[speaker].volume = 1;
                m_speakers[speaker].time = 0;
                m_speakers[speaker].Play();
                m_speakers[speaker].time = currentPlayTime;
            }
        }

        /// <summary>
        /// Stops a <see cref="JukeboxClip"/> that is currently playing in the jukebox.
        /// </summary>
        /// <param name="clip">The <see cref="JukeboxClip"/> to stop playing.</param>
        /// <param name="time">A delay before the <see cref="JukeboxClip"/> is actually stopped.</param>
        /// <param name="fadeOut">Should the jukebox fade out this <see cref="JukeboxClip"/>?</param>
        /// <param name="fadeSpeed">How fast the jukebox should fade out this <see cref="JukeboxClip"/>.</param>
        public bool StopAudioClip(JukeboxClip clip, float time = 0f, bool fadeOut = true, float fadeSpeed = .005f)
        {
            if (time > 0)
            {
                _ = StartCoroutine(StopAudioClipAfterTime(clip, time, fadeOut, fadeSpeed));
                return true;
            }

            return TryStopAudioClip(clip, fadeOut, fadeSpeed);
        }

        /// <summary>
        /// Stops a <see cref="JukeboxClip"/> that is currently playing in the jukebox after a specified bar count.
        /// </summary>
        /// <param name="clip">The <see cref="JukeboxClip"/> to stop playing.</param>
        /// <param name="barCount">The amount of bars to wait before the <see cref="JukeboxClip"/> is actually stopped.</param>
        /// <param name="fadeOut">Should the jukebox fade out this <see cref="JukeboxClip"/>?</param>
        /// <param name="fadeSpeed">How fast the jukebox should fade out this <see cref="JukeboxClip"/>.</param>
        private void StopAudioClipOnBar(JukeboxClip clip, int barCount = 0, bool fadeOut = true, float fadeSpeed = .005f)
        {
            if (barCount > 0)
            {
                _ = StartCoroutine(StopAudioClipAfterBar(clip, barCount, fadeOut, fadeSpeed));
                return;
            }

            _ = TryStopAudioClip(clip, fadeOut, fadeSpeed);
        }

        private bool TryStopAudioClip(JukeboxClip clip, bool fadeOut, float fadeSpeed)
        {
            if (!IsClipPlaying(clip))
            {
                Debug.LogWarning("Tried to stop a JukeboxClip that wasn't playing anywhere!");
                return false;
            }

            var targetSpeaker = m_speakers.First(speaker => speaker.clip == m_audioBank[clip]);
            if (targetSpeaker == null)
            {
                Debug.LogWarning("Tried to stop a JukeboxClip that wasn't playing anywhere!");
                return false;
            }

            StopSpeaker(m_speakers.IndexOf(targetSpeaker), fadeOut, fadeSpeed);
            return true;
        }

        private void StopSpeaker(int speaker, bool fadeOut = true, float fadeSpeed = .01f)
        {
            if (fadeOut)
            {
                _ = StartCoroutine(FadeOutSpeaker(speaker, fadeSpeed));
                return;
            }

            m_speakers[speaker].Stop();
            m_speakers[speaker].clip = null;
        }

        private void StopAllSpeakers(bool instant = false)
        {
            for (var i = 0; i < m_speakers.Count; i++)
            {
                if (m_speakers[i].isPlaying) { StopSpeaker(i, !instant); }
            }

            m_currentMusicTime = 0;
        }

        private IEnumerator StopAudioClipAfterTime(JukeboxClip clip, float time, bool fadeOut = true, float fadeSpeed = .005f)
        {
            yield return new WaitForSeconds(time);

            _ = TryStopAudioClip(clip, fadeOut, fadeSpeed);
        }

        private IEnumerator StopAudioClipAfterBar(JukeboxClip clip, int barCount, bool fadeOut = true, float fadeSpeed = .005f)
        {
            yield return new WaitUntil(CanQueueClips);

            var queuedBar = m_totalBarCount;
            yield return new WaitUntil(() => m_totalBarCount >= queuedBar + barCount);

            _ = TryStopAudioClip(clip, fadeOut, fadeSpeed);
        }

        private IEnumerator FadeOutSpeaker(int speaker, float fadeSpeed)
        {
            var thisSpeaker = m_speakers[speaker];
            var volume = thisSpeaker.volume;
            var wait = new WaitForFixedUpdate();
            while (volume > 0)
            {
                volume -= fadeSpeed;
                thisSpeaker.volume = volume;

                yield return wait;
            }

            thisSpeaker.Stop();
            thisSpeaker.clip = null;
        }

        private IEnumerator FadeInSpeaker(int speaker, float fadeSpeed)
        {
            var thisSpeaker = m_speakers[speaker];
            var volume = 0f;
            var wait = new WaitForFixedUpdate();
            while (volume < 1)
            {
                volume += fadeSpeed;
                thisSpeaker.volume = volume;

                yield return wait;
            }
        }

        private void CancelAllQueuedClips()
        {
            StopAllCoroutines();
            ClearQueue();
        }

        private bool IsAnySpeakerUnoccupied() => m_speakers.Any(speaker => !speaker.isPlaying);

        private bool AreAllSpeakersUnoccupied() => m_speakers.All(speaker => !speaker.isPlaying);

        private int GetNextUnoccupiedSpeaker()
        {
            if (!IsAnySpeakerUnoccupied()) { return -1; }
            var firstUnoccupiedSpeaker = m_speakers.Where(speaker => !speaker.isPlaying).FirstOrDefault();
            var speakerIndex = m_speakers.IndexOf(firstUnoccupiedSpeaker);
            return speakerIndex;
        }

        private int GetAnyActiveSpeaker(bool prioritizeMain = false)
        {
            return prioritizeMain && IsClipPlaying(JukeboxClip.Gameplay_MainMelody)
                ? m_speakers.IndexOf(m_speakers.First(speaker => speaker.clip == m_audioBank[JukeboxClip.Gameplay_MainMelody]))
                : AreAllSpeakersUnoccupied() ? -1 : m_speakers.IndexOf(m_speakers.First(speaker => speaker.isPlaying));
        }

        private int GetNextOpenClipQueueSpot()
        {
            var firstAvailable = m_clipQueue.Where(clip => clip == JukeboxClip.None).FirstOrDefault();

            return firstAvailable == JukeboxClip.None ? System.Array.IndexOf(m_clipQueue, firstAvailable) : -1;
        }

        private int GetNextOpenClipRemovalQueueSpot()
        {
            var firstAvailable = m_clipRemovalQueue.Where(clip => clip == JukeboxClip.None).FirstOrDefault();

            return firstAvailable == JukeboxClip.None ? System.Array.IndexOf(m_clipRemovalQueue, firstAvailable) : -1;
        }

        private JukeboxClip GetNextClipInQueue() => m_clipQueue.Where(clip => clip != JukeboxClip.None).FirstOrDefault();

        /// <summary>
        /// Is a specific <see cref="JukeboxClip"/> currently playing in the jukebox?
        /// </summary>
        /// <param name="clip">The <see cref="JukeboxClip"/> to check if the jukebox is playing.</param>
        /// <returns></returns>
        public bool IsClipPlaying(JukeboxClip clip) => m_speakers.Any(speaker => speaker.clip == m_audioBank[clip]);

        private int GetIndexOfClipInQueue(JukeboxClip clip) => m_clipQueue.Contains(clip) ? System.Array.IndexOf(m_clipQueue, clip) : -1;

        /// <summary>
        /// Gets the amount of clips currently in the clip queue.
        /// </summary>
        /// <returns>The number of clips currently in the clip queue.</returns>
        public int GetClipQueueLength() => m_clipQueue.Where(clip => clip != JukeboxClip.None).Count();

        /// <summary>
        /// Gets the amount of clips currently in the clip removal queue.
        /// </summary>
        /// <returns>The number of clips currently in the clip removal queue.</returns>
        public int GetClipRemovalQueueLength() => m_clipRemovalQueue.Where(clip => clip != JukeboxClip.None).Count();

        private bool CanQueueClips() => m_canQueueClips;

        private bool IsValidSceneForJukebox(string sceneName) => sceneName is Application.STARTUP_SCENE or Application.MAIN_MENU_SCENE or Application.LOBBY_SCENE;
    }
}
