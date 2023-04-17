// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

namespace Meta.Decommissioned.Game
{
    public partial class GameEnd
    {
        /// <summary>
        /// Enum encapsulating all possible reasons for a match to end.
        /// </summary>
        public enum GameEndReason
        {
            Unknown,
            AllPlayersQuit,
            MiniGamesCompleted,
            CrewmatesOutnumbered,
            MiniGamesFailed,
            MiniGameDied,
            CrewmatesLeft,
            MolesLeft,
            MaxRoundsReached,
        }
    }
}
