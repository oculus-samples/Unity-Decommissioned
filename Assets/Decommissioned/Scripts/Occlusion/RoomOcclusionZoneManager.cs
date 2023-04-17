// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Linq;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Utilities;
using UnityEngine;

namespace Meta.Decommissioned.Occlusion
{
    /// <summary>
    /// Manages all <see cref="RoomOcclusionZone"/>s in the scene to occlude and un-occlude them when a player enters a new room.
    /// </summary>
    public class RoomOcclusionZoneManager : Singleton<RoomOcclusionZoneManager>
    {
        private const MiniGameRoom DEFAULT_ROOM = MiniGameRoom.Commander;

        /// <summary>
        /// Invoked when occlusion has been applied in the scene.
        /// </summary>
        public event Action OnOcclusionApplied;

        private RoomOcclusionZone[] m_roomZones;

        private void Start()
        {
            m_roomZones = RoomOcclusionZone.Instances.ToArray();
        }

        /// <summary>
        /// Applies occlusion in the scene based on which room is entered.
        /// </summary>
        /// <param name="room">The room that is being entered.</param>
        /// <param name="roomExclusions">A list of rooms to exclude from the occlusion calculation. These rooms will not be occluded at all.</param>
        public void ApplyOcclusion(MiniGameRoom room, MiniGameRoom[] roomExclusions = null)
        {
            if (m_roomZones.Length == 0)
            {
                Debug.LogWarning("No RoomOcclusionZones available to apply occlusion with. No occlusion will be applied.");
                return;
            }

            if (room == MiniGameRoom.None) { room = DEFAULT_ROOM; }

            var occupiedRoomZone = GetRoomZone(room);
            var unoccupiedRoomZones = GetUnrelatedRoomZones(room);

            if (occupiedRoomZone != null)
            {
                occupiedRoomZone.UnOccludeZone();
            }

            if (roomExclusions != null)
            {
                foreach (var excludedRoom in roomExclusions)
                {
                    var excludedZone = GetRoomZone(excludedRoom);
                    excludedZone.UnOccludeZone();
                }
            }

            foreach (var zone in unoccupiedRoomZones)
            {
                if (roomExclusions != null && roomExclusions.Contains(zone.ZoneRoom))
                {
                    continue;
                }

                zone.OccludeZone();
            }

            OnOcclusionApplied?.Invoke();
        }

        /// <summary>
        /// Gets a <see cref="RoomOcclusionZone"/> from the scene.
        /// </summary>
        /// <param name="room">The room to get the <see cref="RoomOcclusionZone"/> from.</param>
        /// <returns>The <see cref="RoomOcclusionZone"/> that is assigned to the given room.</returns>
        public RoomOcclusionZone GetRoomZone(MiniGameRoom room)
        {
            if (m_roomZones.Length == 0)
            {
                Debug.LogError("Unable to find any RoomOcclusionZones in the scene! Please make sure there is at least 1 room zone in the scene!");
                return null;
            }
            var roomZone = m_roomZones.FirstOrDefault(zone => zone.ZoneRoom == room);
            if (roomZone == null)
            {
                var roomName = room.ToString();
                Debug.LogError($"No RoomOcclusionZones found with the room {roomName}!");
                return null;
            }

            return roomZone;
        }
        /// <summary>
        /// Gets all <see cref="RoomOcclusionZone"/>s from the scene that are not the given room.
        /// </summary>
        /// <param name="room">The room to ignore when getting all <see cref="RoomOcclusionZone"/>s from the scene.</param>
        /// <returns>An array of <see cref="RoomOcclusionZone"/>s that are not assigned to the given room.</returns>
        public RoomOcclusionZone[] GetUnrelatedRoomZones(MiniGameRoom room)
        {
            if (m_roomZones.Length == 0)
            {
                Debug.LogError("Unable to find any RoomOcclusionZones in the scene! Please make sure there is at least 1 room zone in the scene!");
                return null;
            }
            var roomZones = m_roomZones.Where(zone => zone.ZoneRoom != room).ToArray();
            if (roomZones.Length == 0)
            {
                var roomName = room.ToString();
                Debug.LogError($"No RoomOcclusionZones found outside of the room {roomName}!");
                return null;
            }

            return roomZones;
        }
    }
}
