// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections.Generic;
using System.Linq;
using Meta.Utilities;

namespace Meta.Decommissioned.Game.MiniGames
{
    /// <summary>
    ///   Handles all minigames communications between the client and the server, and manages the minigames system to update minigames
    ///   completions and report info to other parts of the game.
    /// </summary>
    public class MiniGameManager : Singleton<MiniGameManager>
    {
        /// <summary>
        ///   Gets all minigames in the current scene, excluding inactive behaviours. Use sparingly!
        /// </summary>
        /// <returns>An array of all minigames found in the scene.</returns>
        public IEnumerable<MiniGame> GetAllMiniGamesInScene() =>
            MiniGame.Instances.Where(g => g.isActiveAndEnabled && g.Config.CanBeAssigned);

        /// <summary>
        ///   Gets all minigames in a specific room.
        /// </summary>
        /// <param name="room">The room to get all minigames from.</param>
        /// <returns>An array of all minigames found in the given room.</returns>
        public IEnumerable<MiniGame> GetAllMiniGamesInRoom(MiniGameRoom room) =>
            GetAllMiniGamesInScene().Where(miniGame => miniGame.Config.Room == room);

        /// <summary>
        /// Gets the number of minigames that have reached 0 health.
        /// </summary>
        /// <returns>Returns the number of minigames that have reached 0 health.</returns>
        public int GetNumberOfMiniGamesDead() =>
            GetAllMiniGamesInScene().GroupBy(game => game.Config.Room).Select(game => game.First()).Count(game => game.CurrentHealth <= 0);


        public static string GetRoomName(MiniGameRoom room) => room is MiniGameRoom.None ? "None" :
            MiniGame.Instances.FirstOrDefault(t => t.Config.Room == room)?.Config.RoomName;
    }
}
