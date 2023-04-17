// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Unity.Netcode;

namespace Meta.Multiplayer.Networking
{
    public static class NetcodeHelpers
    {
        public static ClientRpcParams CreateSendRpcParams(ulong targetClientId) =>
            new()
            {
                Send = new()
                {
                    // don't use NativeArray because of Unity Issue #UUM-17174
                    TargetClientIds = new[] { targetClientId },
                }
            };

        public static ClientRpcParams CreateSendRpcParams(PlayerId playerId) =>
            CreateSendRpcParams(playerId.ClientId.Value);

        public static ClientRpcParams CreateSendRpcParams(PlayerId[] playerIds) =>
            new()
            {
                Send = new()
                {
                    TargetClientIds = playerIds.Select(playerID => playerID.ClientId).WhereNonNull().ToList(),
                }
            };

        public static IEnumerable<T> AsEnumerable<T>(this NetworkList<T> list) where T : unmanaged, IEquatable<T>
        {
            var enumerator = list.GetEnumerator();
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }
    }
}
