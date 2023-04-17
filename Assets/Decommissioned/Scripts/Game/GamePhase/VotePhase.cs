// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections.Generic;
using System.Linq;
using Meta.Decommissioned.Player;
using Meta.Decommissioned.Voting;
using Meta.Multiplayer.PlayerManagement;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game
{
    public class VotePhase : GamePhase
    {
        public override Phase Phase => Phase.Voting;
        private NetworkObject m_oldCommander;


#if UNITY_EDITOR
        protected override float DurationSeconds => UnityEditor.EditorPrefs.GetBool("skip vote phase") ? 0.1f : base.DurationSeconds;
#endif

        protected override void Begin()
        {
            if (IsServer)
            {
                m_oldCommander = GetCurrentCommander();
                CommanderCandidateManager.Instance.SetUpNewCommanderCandidates();
            }
            base.Begin();
        }

        public override void End()
        {
            VoteForNewCommander();
            base.End();
        }

        private void VoteForNewCommander()
        {
            var allVotes = VotingManager.Instance.TallyVotes();
            var noVotesRecorded = allVotes.Length == 0;
            var newCommanderId = noVotesRecorded ?
                ChoosePlayerFromCandidates() :
                ChooseNewCommander(allVotes);

            var (commanderCandidate1, commanderCandidate2) = CommanderCandidateManager.Instance.GetCommanderCandidates();
            var noCandidatesLeft = commanderCandidate1.Object == null && commanderCandidate2.Object == null;

            if ((!newCommanderId.HasValue || newCommanderId.Value.Object == null) && !noCandidatesLeft)
            {
                if (!newCommanderId.HasValue || newCommanderId.Value.Object == null)
                {
                    Debug.LogWarning("The new commander did not exist, setting new commander as the remaining candidate");
                    newCommanderId = newCommanderId == commanderCandidate1 ? commanderCandidate2 : commanderCandidate1;
                }
                else
                {
                    Debug.LogError("The new commander did not have a PlayerId and no suitable new commander was found! Unable to progress...");
                    return;
                }
            }
            else if (noCandidatesLeft)
            {
                Debug.LogWarning("No commander candidates left to pick! Selecting a random player...");
                var allPlayers = PlayerManager.Instance.AllPlayerIds.ToList();
                var newCommanderIndex = Random.Range(0, allPlayers.Count);
                newCommanderId = allPlayers[newCommanderIndex];
            }

            if (m_oldCommander != null)
            {
                RemoveCommanderStatus(m_oldCommander);
            }
            CommanderCandidateManager.Instance.SetNewCommander(newCommanderId.Value);
        }

        private static PlayerId? ChoosePlayerFromCandidates()
        {
            var (candidateA, candidateB) = CommanderCandidateManager.Instance.GetCommanderCandidates();
            return Random.Range(0, 1) == 1 ? candidateA : candidateB;
        }

        private static NetworkObject GetCurrentCommander() => CommanderCandidateManager.Instance.GetCommander().Object;

        private PlayerId ChooseNewCommander(IEnumerable<(PlayerId id, int count)> tempVotes)
        {
            var votes = tempVotes.ToArray();
            var mostVotes = VotingManager.Instance.GetIdWithMostVotes(votes);
            var isVoteTied = mostVotes == null;
            return isVoteTied ? BreakCommanderVoteTie(votes) : mostVotes.Value;
        }

        private void RemoveCommanderStatus(NetworkObject commander)
        {
            var playerStatus = PlayerStatus.GetByPlayerObject(commander);
            if (playerStatus == null) { return; }
            playerStatus.CurrentStatus = PlayerStatus.Status.None;
        }

        private PlayerId BreakCommanderVoteTie((PlayerId id, int count)[] votes)
        {
            var possibleWinners = TieWinners(votes);
            var (id, _) = RandomChoice(possibleWinners);
            return id;
        }

        private static T RandomChoice<T>(IReadOnlyList<T> options)
        {
            var rng = new System.Random();
            var index = rng.Next(options.Count);
            return options[index];
        }

        private static (PlayerId id, int count)[] TieWinners((PlayerId id, int count)[] votes)
        {
            if (votes.Length == 0) { return votes; }
            var mostVotes = votes[0].count;
            return votes.Where(v => v.count == mostVotes).ToArray();
        }
    }
}
