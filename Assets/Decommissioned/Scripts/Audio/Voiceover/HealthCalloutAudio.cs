// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Decommissioned.UI;
using Meta.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Audio.Voiceover
{
    /**
     * A PreGameplayStep object that plays clips in sequence depending on the current condition of all active
     * minigames in the game.
     */
    public class HealthCalloutAudio : PreGameplayStep
    {
        [SerializeField, AutoSet] private AudioSource m_audioSource;
        [SerializeField] private AudioClip m_vunerableStationListIntro;
        [SerializeField] private AudioClip m_criticalStationListIntro;
        [SerializeField] private int m_vunerableHealthThreshold = 65;
        [SerializeField] private int m_criticalHealthThreshold = 35;
        [SerializeField] private float m_delayBetweenClips = 0.5f;
        [SerializeField] private EnumDictionary<MiniGameRoom, AudioClip> m_stationCalloutClips;
        private MiniGameManager m_miniGameManager;
        private List<AudioClip> m_allAudioClips;
        private EnumDictionary<MiniGameRoom, List<MainRoomHealthGauge>> m_stationHealthGauges;

        private void OnEnable()
        {
            m_allAudioClips = m_stationCalloutClips.Select(x => x.Value).ToList();
            m_allAudioClips.Add(m_vunerableStationListIntro);
            m_allAudioClips.Add(m_criticalStationListIntro);

            m_stationHealthGauges = new EnumDictionary<MiniGameRoom, List<MainRoomHealthGauge>>();
            foreach (var miniGameRoom in m_stationHealthGauges.Keys)
            {
                m_stationHealthGauges[miniGameRoom] = new List<MainRoomHealthGauge>();
            }
            var allHealthGauges = MainRoomHealthGauge.Instances;
            foreach (var healthGauge in allHealthGauges)
            {
                if (healthGauge.MiniGameRoom is MiniGameRoom.None or MiniGameRoom.Commander || healthGauge == null || !healthGauge.ShouldFlashForCallout)
                {
                    continue;
                }

                m_stationHealthGauges[healthGauge.MiniGameRoom].Add(healthGauge);
            }
        }

        public override IEnumerator Run()
        {
            yield return PlayCalloutAudio();
        }

        public override void End()
        {
            EndGaugeCalloutsClientRpc();
        }

        private IEnumerator PlayCalloutAudio()
        {
            yield return new WaitUntil(() => MiniGameManager.Instance != null);
            m_miniGameManager = MiniGameManager.Instance;
            yield return CalloutStations(GetStationsAtThreshold(1, m_criticalHealthThreshold),
                GetStationsAtThreshold(m_criticalHealthThreshold, m_vunerableHealthThreshold));
        }

        private IEnumerator CalloutStations(MiniGame[] criticalStations, MiniGame[] vulnerableStations)
        {
            var inputIsValid = criticalStations != null && vulnerableStations != null;
            if (!inputIsValid || (criticalStations.Length == 0 && vulnerableStations.Length == 0)) { yield break; }

            var clipsToPlay = new List<AudioClip>();
            var calloutClips = m_stationCalloutClips.Where(entry => entry.Value != null)
                .ToDictionary(e => e.Key, e => e.Value);

            if (vulnerableStations.Length > 0) { clipsToPlay.Add(m_vunerableStationListIntro); }

            foreach (var miniGameRoom in calloutClips)
            {
                var matchingMiniGame = vulnerableStations.FirstOrDefault(miniGame => miniGame.Config.Room == miniGameRoom.Key);
                if (matchingMiniGame != null) { clipsToPlay.Add(miniGameRoom.Value); }
            }

            if (criticalStations.Length > 0) { clipsToPlay.Add(m_criticalStationListIntro); }

            foreach (var miniGameRoom in calloutClips)
            {
                var matchingMiniGame = criticalStations.FirstOrDefault(miniGame => miniGame.Config.Room == miniGameRoom.Key);
                if (matchingMiniGame != null) { clipsToPlay.Add(miniGameRoom.Value); }
            }

            var roomsToCallOut = new MiniGameRoom[clipsToPlay.Count];
            int? currentClip;
            MiniGameRoom currentRoom;
            for (var i = 0; i < roomsToCallOut.Length; i++)
            {
                currentClip = m_stationCalloutClips.Values.IndexOf(clipsToPlay[i]);
                if (currentClip != null)
                {
                    currentRoom = m_stationCalloutClips.Keys.ElementAt(currentClip.Value);
                    roomsToCallOut[i] = currentRoom;
                }
            }

            yield return StartCoroutine(PlayClipsInSequence(clipsToPlay, roomsToCallOut));
        }

        private IEnumerator PlayClipsInSequence(List<AudioClip> audioClips, MiniGameRoom[] miniGameRooms)
        {
            for (var i = 0; i < audioClips.Count; i++)
            {
                PlayClipAtIndexClientRpc(m_allAudioClips.IndexOf(audioClips[i]), miniGameRooms[i]);
                yield return new WaitForSeconds(audioClips[i].length + m_delayBetweenClips);
            }
        }

        [ClientRpc]
        private void PlayClipAtIndexClientRpc(int clipIndex, MiniGameRoom miniGameRoom)
        {
            var clip = m_allAudioClips[clipIndex];
            if (clip == null) { return; }
            _ = StartCoroutine(PlayClip(clip, miniGameRoom));
        }

        [ClientRpc]
        public void EndGaugeCalloutsClientRpc()
        {
            foreach (var miniGameRoom in m_stationHealthGauges.Keys)
            {
                foreach (var gauge in m_stationHealthGauges[miniGameRoom])
                {
                    gauge.SetGaugeDangerFlash(false);
                }
            }
        }

        private IEnumerator PlayClip(AudioClip clip, MiniGameRoom miniGameRoom)
        {
            if (GamePhaseManager.Instance.DebugSkipAudio || clip == null) { yield break; }
            m_audioSource.PlayOneShot(clip);
            foreach (var gauge in m_stationHealthGauges[miniGameRoom])
            {
                gauge.SetGaugeDangerFlash(true);
            }


            yield return new WaitUntil(() => !m_audioSource.isPlaying);

            foreach (var gauge in m_stationHealthGauges[miniGameRoom])
            {
                gauge.SetGaugeDangerFlash(false);
            }
        }

        private MiniGame[] GetStationsAtThreshold(int lowerHealthThreshold, int upperHealthThreshold)
        {
            if (m_miniGameManager == null) { return null; }

            var stations = m_miniGameManager.GetAllMiniGamesInScene()
                .Where(miniGame => miniGame.Config.Room is not MiniGameRoom.Commander and not MiniGameRoom.None)
                .ToArray();

            var validStations = stations.GroupBy(miniGame => miniGame.Config.RoomName)
                .Select(x => x.First())
                .Where(station => station.CurrentHealth <= upperHealthThreshold && station.CurrentHealth > lowerHealthThreshold)
                .ToArray();

            return validStations;
        }
    }
}
