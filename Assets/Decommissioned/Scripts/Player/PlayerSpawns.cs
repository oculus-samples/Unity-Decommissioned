// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Decommissioned.Lobby;
using Meta.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Player
{

    /// <summary>
    /// Multiton that stores a player's current position, as well as their "Main Spawn", a position they can be returned
    /// to from anywhere.
    /// </summary>
    public class PlayerSpawns : Multiton<PlayerSpawns>
    {
        private int m_mainSpawnIndex = -1;
        private GamePosition m_mainSpawn;

        public bool EnableLogging;

        public GamePosition MainSpawn => m_mainSpawn == null
            ? LocationManager.Instance?.GetGamePositionByIndex(MiniGameRoom.None, m_mainSpawnIndex)
            : m_mainSpawn;

        public static PlayerSpawns GetByPlayerObject(NetworkObject player) =>
            Instances.FirstOrDefault(p => p.gameObject == player.gameObject);

        public void SetMainSpawn(GamePosition position)
        {
            m_mainSpawn = position;
            m_mainSpawnIndex = position.PositionIndex;
            if (EnableLogging)
            {
                Debug.Log("Setting main spawn to " + m_mainSpawnIndex);
            }
        }
    }
}
