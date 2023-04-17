// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game.MiniGames;
using Meta.Decommissioned.Player;
using Unity.Netcode;

namespace Meta.Decommissioned.Game
{
    public class DiscussionPhase : GamePhase
    {
        public override Phase Phase => Phase.Discussion;

#if UNITY_EDITOR
        protected override float DurationSeconds => UnityEditor.EditorPrefs.GetBool("skip discuss phase") ? 0.1f : base.DurationSeconds;
#endif

        protected override void Begin()
        {
            if (IsServer)
            {
                GameManager.Instance.StartNewRound();
                TeleportPlayers();
            }
            base.Begin();
        }

        private void TeleportPlayers()
        {
            foreach (var player in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerSpawn = PlayerSpawns.GetByPlayerObject(player.PlayerObject);
                _ = playerSpawn != null
                    ? LocationManager.Instance.TeleportPlayer(player.PlayerObject, playerSpawn.MainSpawn)
                    : LocationManager.Instance.TeleportPlayer(player.PlayerObject, MiniGameRoom.None);
            }
        }
    }
}
