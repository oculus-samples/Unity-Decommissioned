// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game.MiniGames;
using Meta.Decommissioned.Lobby;
using Meta.Decommissioned.Player;
using Meta.Multiplayer.Core;
using Unity.Netcode;

namespace Meta.Decommissioned.Game
{
    public class NightPhase : GamePhase
    {
        public override Phase Phase => Phase.Night;

#if UNITY_EDITOR
        protected override float DurationSeconds => UnityEditor.EditorPrefs.GetBool("skip work phase") ? 0.1f : base.DurationSeconds;
#endif

        protected override void Begin()
        {
            base.Begin();
            if (IsServer is false) { return; }

            foreach (var player in PlayerStatus.Instances)
            {
                PhaseSpawnManager.Instance.SendPlayerToNightPosition(player);
            }

            UpdateNightPositionClientRpc();
        }

        protected override void Execute()
        {
            var allMiniGames = MiniGameManager.Instance.GetAllMiniGamesInScene();
            if (IsServer)
            {
                foreach (var miniGame in allMiniGames)
                {
                    miniGame.StartHealthDrain();
                    miniGame.RaiseMiniGameStartEvent();
                }
            }

            base.Execute();
        }

        // Updates player to move from DayPhase room to NightPhase room w/ their Header Menu
        [ClientRpc]
        private void UpdateNightPositionClientRpc() => PlayerCamera.Instance.Refocus();
    }
}
