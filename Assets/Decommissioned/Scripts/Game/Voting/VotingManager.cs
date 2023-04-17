// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Voting
{
    /// <summary>
    /// Manages the status of votes for each player during a <see cref="VotePhase"/>. From here, we can initiate or cancel
    /// votes, access which players voted, who they voted for, and which candidate has the most votes in their favor.
    /// </summary>
    public class VotingManager : Singleton<VotingManager>
    {
        public enum VoteStatus
        {
            None,
            Voting,
            Voted,
            Unvoting
        }
        public Transform CenterPosition;

        /// <summary>
        /// Gets all votes cast for a specific player.
        /// </summary>
        /// <param name="player">PlayerId of player you want to get votes of</param>
        /// <returns>VoteStatus Array</returns>
        public VotingBehavior[] GetVotesForPlayer(PlayerId player) => VotingBehavior.Instances.
            Where(b => b.CurrentVote == player).
            ToArray();

        /// <summary>
        /// Gets all the voting arrows for a specific player
        /// </summary>
        /// <param name="player">PlayerId of player you want to get voting arrows of</param>
        /// <returns>VoteStatus Array</returns>
        public VotingBehavior[] GetArrowsForPlayer(PlayerId player) => VotingBehavior.Instances.
            Where(b => b.ArrowTargeting == player).
            ToArray();

        public VotingBehavior[] GetAllVotes() => VotingBehavior.Instances.ToArray();

        public (PlayerId id, int count)[] TallyVotes() => GetAllVotes().
            Select(i => i.CurrentVote).
            Where(i => i != default).
            GroupBy(i => i).
            Select(g => (g.Key, count: g.Count())).
            OrderByDescending(t => t.count).
            ToArray();

        public PlayerId? GetIdWithMostVotes((PlayerId id, int count)[] votes) => votes switch
        {
            { Length: 0 } => null,
            { Length: 1 } tally => tally[0].id,
            { } tally when tally[0].count == tally[1].count => null,
            { } tally => tally[0].id,
            _ => null,
        };

        /// <summary>
        /// Gets local VoteStatus
        /// </summary>
        /// <returns>VoteStatus</returns>
        public VotingBehavior GetLocalVoter() => GetPlayerVote(PlayerManager.LocalPlayerId);

        public VotingBehavior GetPlayerVote(PlayerId player) => VotingBehavior.Instances.FirstOrDefault(v => v.OwnerClientId == player.ClientId);

        public bool IsPlayerACandidate(NetworkObject obj)
        {
            var (candidateA, candidateB) = CommanderCandidateManager.Instance.GetCommanderCandidates();
            return candidateA == obj.GetOwnerPlayerId().Value || candidateB == obj.GetOwnerPlayerId().Value;
        }
    }
}
