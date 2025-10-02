// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Decommissioned.Player;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Audio.Voiceover
{
    [MetaCodeSample("Decommissioned")]
    public class PlayMiniGameVoiceover : PlayAudioClip
    {
        [SerializeField] private EnumDictionary<MiniGameRoom, RoleAudioClips> m_voiceOverClips = new();

        public override IEnumerator Run()
        {
            yield return base.Run();
            yield return WaitForLocalMiniGameRoomIntro();
        }

        public override void End() { }

        private IEnumerator WaitForLocalMiniGameRoomIntro()
        {
            var playerCount = NetworkManager.ConnectedClients.Count;

            // Wait until all players have been assigned a mini game.
            yield return new WaitUntil(() => LocationManager.Instance.GetAssignedMiniGamePositions().Length >= playerCount);

            PlayLocalClipClientRpc();
            yield return WaitForLongestClip();
            IsComplete = true;
        }

        [ClientRpc]
        private void PlayLocalClipClientRpc()
        {
            var player = NetworkManager.Singleton.LocalClient.PlayerObject;
            var playerId = PlayerManager.LocalPlayerId;
            var playerRole = PlayerRole.GetByPlayerId(playerId).CurrentRole;
            var playerPosition = LocationManager.Instance.GetGamePositionByPlayer(player);
            var room = LocationManager.Instance.GetGamePositionByPlayer(player).MiniGameRoom;

            var clip = playerRole != Role.Mole ? m_voiceOverClips[room].CrewAudioClips[playerPosition.PositionIndex]
                : m_voiceOverClips[room].MoleAudioClips[playerPosition.PositionIndex];

            _ = StartCoroutine(PlayClip(clip));  // Don't wait for playing to finish; we do this next
        }

        private IEnumerator WaitForLongestClip()
        {
            var assignedMiniGames = NetworkManager.Singleton.ConnectedClientsList
                .Select(c => c.PlayerObject)
                .Select(p => LocationManager.Instance.GetGamePositionByPlayer(p).MiniGameRoom)
                .ToList();

            var longestClip = m_voiceOverClips[assignedMiniGames.FirstOrDefault()].CrewAudioClips[0];

            // For every mini game, look at its associated RoleAudioClips object; compare until we have the longest clip.
            foreach (var miniGame in assignedMiniGames)
            {
                var currentClip = m_voiceOverClips[miniGame].GetLongestClip;
                longestClip = currentClip.length > longestClip.length ? currentClip : longestClip;
            }

            yield return new WaitForSeconds(longestClip.length);
        }
    }
}
