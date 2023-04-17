// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Decommissioned.Player;
using Meta.Multiplayer.Networking;
using Meta.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Lobby
{
    /// <summary>
    /// Manages player spawns based on the different phases of the game.
    /// </summary>
    public class PhaseSpawnManager : NetworkSingleton<PhaseSpawnManager>
    {
        private GamePosition[] m_allNightSpawnPoints;

        public event Action OnAssignmentsUpdated;

        public bool EnableLogging;

        [SerializeField] private MiniGameRoom m_debugSpawnOverride = MiniGameRoom.None;

        public IEnumerable<PlayerStatus> AssignedPlayers => PlayerStatus.Instances.
            Where(p => p.NextNightRoom.MiniGameRoom != MiniGameRoom.None).
            Append(PlayerStatus.GetByPlayerId(CommanderCandidateManager.Instance.GetCommander())).
            Distinct();

        private static IEnumerable<PlayerStatus> AllPlayers => PlayerStatus.Instances;

        public IEnumerable<PlayerStatus> UnassignedPlayers => AllPlayers.Except(AssignedPlayers);

        public IEnumerable<PlayerStatus> PlayersInSingles => AssignedPlayers.Where(p =>
        {
            var room = p.NextNightRoom.MiniGameRoom;
            return room is not (MiniGameRoom.None or MiniGameRoom.Commander) &&
                NumSpawnPointsInRoom(room) == 1;
        });

        private int NumSpawnPointsInRoom(MiniGameRoom room) => GetAllNightSpawnPointsInRoom(room).Count();

        private int NumRemainingSpawnPointsInRoom(MiniGameRoom room) =>
            NumSpawnPointsInRoom(room) - NumPlayersAssignedToRoom(room);

        private int? GetNextRemainingSpawnInRoom(MiniGameRoom room) => GetAllNightSpawnPointsInRoom(room).
            Where(x => !x.IsOccupied && AllPlayers.All(p => p.NextNightRoom.MiniGameRoom != room || p.NextNightRoom.SpawnIndex != x.PositionIndex)).
            Select(x => x.PositionIndex).
            FirstOrDefault();

        public IEnumerable<MiniGameRoom> IncompleteRooms => GetAllNightSpawnPoints().Select(p => p.MiniGameRoom).Distinct().
            Where(r => NumSpawnPointsInRoom(r) > GetPlayersAssignedToRoom(r).Count());

        public IEnumerable<MiniGameRoom> PartiallyCompleteRooms =>
            IncompleteRooms.Where(r => GetPlayersAssignedToRoom(r).Any());

        public void OnNightRoomsChanged() => OnAssignmentsUpdated?.Invoke();

        private void Start()
        {
            m_allNightSpawnPoints = FindObjectsOfType<GamePosition>().Where(spawn => !spawn.IsInitialSpawnPoint).ToArray();

            var roomName = AndroidHelpers.GetStringIntentExtra("miniGameRoom");
            if (roomName != null)
            {
                var wasParsed = Enum.TryParse(roomName, out m_debugSpawnOverride);
                if (wasParsed)
                    Debug.Log($"[{nameof(PhaseSpawnManager)}] {nameof(m_debugSpawnOverride)} set to: {m_debugSpawnOverride}", this);
                else
                    Debug.LogError($"[{nameof(PhaseSpawnManager)}] Failed to parse {nameof(MiniGameRoom)}: {roomName}", this);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetNightSpawnRoom_ServerRpc(NetworkReference<PlayerStatus> player, MiniGameRoom room) => SetNightSpawnRoom(player, room);

        private void SetNightSpawnRoom(PlayerStatus player, MiniGameRoom room)
        {
            if (EnableLogging)
            {
                Debug.Log($"Assigning client {player} to spawn {room}", this);
            }
            var spawnPointsInRoom = NumRemainingSpawnPointsInRoom(room);
            if (spawnPointsInRoom == 0)
            {
                if (EnableLogging)
                {
                    Debug.Log($"Clearing {room} assignments to make room for {player}", this);
                }
                foreach (var oldPlayer in GetPlayersAssignedToRoom(room))
                    oldPlayer.NextNightRoom = new(MiniGameRoom.None, 0);
            }

            var nextAvailableSpawn = GetNextRemainingSpawnInRoom(room);
            if (nextAvailableSpawn.HasValue)
            {
                if (EnableLogging)
                {
                    Debug.Log($"Setting night spawn to {nextAvailableSpawn.Value}");
                }
                player.NextNightRoom = new(room, nextAvailableSpawn.Value);
            }
            else
            {
                Debug.LogError($"No night spawns available in room {room}!");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClearAssignments_ServerRpc()
        {
            foreach (var player in AllPlayers)
                player.NextNightRoom = new(MiniGameRoom.None, 0);
        }

        /// <summary>
        /// If any rooms have not been completely filled or any players have not been assigned to a room, this method
        /// will automatically assign them to rooms based on the number of players that are needed.
        /// </summary>
        public void FillInAssignments()
        {
            while (PartiallyCompleteRooms.Any())
            {
                FillPartiallyFilledRooms();
                BackFillMultiplayerRooms();

                // if there are still partially assigned rooms left, unassign players from one of them and try again
                var leastFilledRoom = PartiallyCompleteRooms.OrderByDescending(NumRemainingSpawnPointsInRoom).FirstOrDefault();

                if (leastFilledRoom is not MiniGameRoom.None)
                {
                    if (EnableLogging)
                    {
                        Debug.LogWarning($"{gameObject.name}: Can't finish filling room {leastFilledRoom}, clearing it instead.", this);
                    }
                    foreach (var player in GetPlayersAssignedToRoom(leastFilledRoom))
                        player.NextNightRoom = new(MiniGameRoom.None, 0);
                }
            }

            // if there are any unassigned players left, put them in the "most empty" locations that can be filled
            var availableRooms = IncompleteRooms.OrderBy(NumRemainingSpawnPointsInRoom);
            foreach (var room in availableRooms)
            {
                while (NumRemainingSpawnPointsInRoom(room) != 0 && UnassignedPlayers.Any())
                {
                    var player = UnassignedPlayers.First();
                    SetNightSpawnRoom(player, room);
                }

                if (!UnassignedPlayers.Any()) { break; }
            }

            if (NetworkManager.Singleton.ConnectedClientsList.Count > 2)
            {
                BackFillMultiplayerRooms();
            }
            else
            {
                if (EnableLogging)
                {
                    Debug.LogWarning("Not enough players to back-fill multiplayer rooms!");
                }
            }
        }

        /// <summary>
        /// Fill rooms that have not yet been fully occupied with unassigned players. 
        /// </summary>
        private void FillPartiallyFilledRooms()
        {
            foreach (var room in PartiallyCompleteRooms)
            {
                var player = UnassignedPlayers.FirstOrDefault();
                if (player != null)
                {
                    SetNightSpawnRoom(player, room);
                }
            }
        }

        private void BackFillMultiplayerRooms()
        {
            foreach (var room in PartiallyCompleteRooms)
            {
                var player = PlayersInSingles.FirstOrDefault();
                if (player != null)
                {
                    SetNightSpawnRoom(player, room);
                }
            }
        }

        public IEnumerable<PlayerStatus> GetPlayersAssignedToRoom(MiniGameRoom targetRoom)
        {
            foreach (var player in AllPlayers)
                if (player.NextNightRoom.MiniGameRoom == targetRoom)
                    yield return player;
        }

        public int NumPlayersAssignedToRoom(MiniGameRoom room) => GetPlayersAssignedToRoom(room).Count();

        public void SendPlayerToNightPosition(PlayerStatus player)
        {
            if (player.NextNightRoom.MiniGameRoom != MiniGameRoom.None)
            {
                var spawn = player.NextNightRoom;
                if (m_debugSpawnOverride is not MiniGameRoom.None)
                    spawn.MiniGameRoom = m_debugSpawnOverride;
                _ = LocationManager.Instance.TeleportPlayer(player.NetworkObject, spawn.MiniGameRoom, spawn.SpawnIndex);
            }
        }

        public GamePosition[] GetAllNightSpawnPoints() => m_allNightSpawnPoints;

        public IEnumerable<GamePosition> GetAllNightSpawnPointsInRoom(MiniGameRoom room) =>
            m_allNightSpawnPoints.Where(spawn => spawn.MiniGameRoom == room);
    }
}
