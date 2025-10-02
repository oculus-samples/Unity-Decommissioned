// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Decommissioned.Lobby;
using Meta.Decommissioned.Player;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game
{
    /// <summary>
    /// This singleton manages the movement of players from place to place during the game; from here, we can access what
    /// players are in which rooms and move them between specific <see cref="GamePosition"/>s.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class LocationManager : NetworkSingleton<LocationManager>
    {
        private GamePosition[] m_gamePositions = Array.Empty<GamePosition>();
        public event Action<NetworkObject, MiniGameRoom> OnPlayerJoinedRoom;
        public bool EnableLogging;

        private new void Awake()
        {
            base.Awake();
            m_gamePositions = FindObjectsByType<GamePosition>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        public void InvokeOnPlayerJoinedRoom(NetworkObject playerObject, MiniGameRoom room) =>
            OnPlayerJoinedRoom?.Invoke(playerObject, room);

        public void UpdateLocalPosition()
        {
            foreach (var pos in m_gamePositions)
            {
                // if the occupying player is rejoining, they'll need to teleport immediately
                var occupyingPlayer = pos.OccupyingPlayer;
                if (occupyingPlayer != null && occupyingPlayer.NetworkObjectId != default)
                {
                    _ = StartCoroutine(
                        new WaitUntil(() =>
                                pos.NetworkManager.SpawnManager.GetLocalPlayerObject() != null &&
                                pos.OccupyingPlayer != null)
                            .Then(() => pos.OnOccupyingPlayerHasChanged(default, pos.OccupyingPlayer)));
                }
            }
        }

        #region GamePosition Retrieval
        public GamePosition[] GetAllGamePositions() => m_gamePositions;

        private GamePosition GetNextUnOccupiedGamePosition(MiniGameRoom room) => m_gamePositions
            .Where(x => x.MiniGameRoom == room).OrderBy(x => x.PositionIndex).FirstOrDefault(x => !x.IsOccupied);

        public GamePosition GetGamePositionByIndex(MiniGameRoom room, int index) => m_gamePositions
            .FirstOrDefault(x => x.MiniGameRoom == room && x.PositionIndex == index);

        public GamePosition[] GetGamePositionByType(MiniGameRoom room) =>
            m_gamePositions.Where(x => x.MiniGameRoom == room).ToArray();

        public GamePosition GetGamePositionByPlayer(NetworkObject player) => player != null
            ? m_gamePositions
                .FirstOrDefault(x => x.IsOccupied && x.OccupyingPlayer.NetworkObjectId == player.NetworkObjectId)
            : null;

        public GamePosition GetGamePositionByPlayerId(PlayerId playerId) => m_gamePositions.FirstOrDefault(x =>
            x.IsOccupied && x.OccupyingPlayerId is { } id && id == playerId);

        public IEnumerable<NetworkObject> GetPlayersInRoom(MiniGameRoom room) =>
            m_gamePositions.Where(x => x.MiniGameRoom == room && x.OccupyingPlayer != null)
                .Select(location => location.OccupyingPlayer);

        public GamePosition[] GetAssignedMiniGamePositions() =>
                m_gamePositions.Where(x => x.MiniGameRoom != MiniGameRoom.None && x.IsOccupied).ToArray();

        public IEnumerable<NetworkObject> GetPlayersNotInRoom(MiniGameRoom room) =>
            m_gamePositions.Where(x => x.MiniGameRoom != room && x.OccupyingPlayer != null)
                .Select(location => location.OccupyingPlayer);

        public string GetFriendlyMiniGameRoomName(MiniGameRoom room) => room.ToString();
        #endregion

        #region Teleportation methods
#if UNITY_EDITOR
        [ServerRpc(RequireOwnership = false)]
        public void TeleportPlayer_ServerRpc(NetworkObjectReference playerObject, MiniGameRoom room)
        {
            _ = TeleportPlayer(playerObject, room);
        }
#endif

        public void TeleportToMainRoom(NetworkObject playerObject)
        {
            var playerSpawns = PlayerSpawns.GetByPlayerObject(playerObject);
            if (playerSpawns != null)
            {
                if (playerSpawns.MainSpawn == null)
                {
                    var nextOpenPosition = GetNextUnOccupiedGamePosition(MiniGameRoom.None);
                    playerSpawns.SetMainSpawn(nextOpenPosition);
                }
                if (EnableLogging)
                {
                    Debug.Log($"Teleporting {playerObject.name} to spawn {playerSpawns.MainSpawn.name}");
                }
                _ = TeleportPlayer(playerObject, playerSpawns.MainSpawn);
            }
            else
            {
                if (EnableLogging)
                {
                    Debug.Log($"Teleporting {playerObject.name} to random spawn");
                }
                _ = TeleportPlayer(playerObject, MiniGameRoom.None);
            }
        }

        public GamePosition TeleportPlayer(NetworkObject playerObject, MiniGameRoom room)
        {
            foreach (var location in m_gamePositions.Where(x => x.OccupyingPlayer == playerObject))
            {
                location.ClearPosition();
            }

            var target = m_gamePositions.FirstOrDefault(x => x.MiniGameRoom == room && x.IsOccupied is false);
            if (target != null)
            {
                if (EnableLogging)
                {
                    Debug.Log($"Teleporting {playerObject} to {room} location {target}", playerObject);
                }
                target.OccupyPosition(playerObject);
            }
            else
            {
                Debug.LogError($"Could not find {room} to teleport {playerObject}.", playerObject);
            }

            return target;
        }

        public GamePosition TeleportPlayer(NetworkObject playerObject, MiniGameRoom room, int spawnIndex)
        {
            foreach (var location in m_gamePositions.Where(x => x.OccupyingPlayer == playerObject))
            {
                location.ClearPosition();
            }

            var target = m_gamePositions.FirstOrDefault(x =>
                x.MiniGameRoom == room && x.IsOccupied is false && x.PositionIndex == spawnIndex);

            if (target != null)
            {
                if (EnableLogging)
                {
                    Debug.Log($"Teleporting {playerObject} to {room} location {target}", playerObject);
                }
                target.OccupyPosition(playerObject);
            }
            else
            {
                Debug.LogError($"Could not find {room} with spawn number {spawnIndex} to teleport {playerObject} " +
                               "to. Trying with generic spawning.", playerObject);
                return TeleportPlayer(playerObject, room);
            }

            return target;
        }

        public GamePosition TeleportPlayer(NetworkObject playerObject, GamePosition position)
        {
            foreach (var location in m_gamePositions.Where(x => x.OccupyingPlayer == playerObject))
            {
                location.ClearPosition();
            }

            var target = position;
            if (EnableLogging)
            {
                Debug.Log($"Teleporting {playerObject} to {target.MiniGameRoom} location {target}", playerObject);
            }
            target.OccupyPosition(playerObject);
            return target;
        }

        #endregion

    }
}
