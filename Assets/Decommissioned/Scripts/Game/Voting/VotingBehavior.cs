// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections;
using Meta.Decommissioned.Game;
using Meta.Multiplayer.Avatar;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static Meta.Decommissioned.Voting.VotingManager;

namespace Meta.Decommissioned.Voting
{
    /// <summary>
    /// Class for managing the flow for player voting; controls when a vote is initiated,
    /// how long it will take before the vote is actually confirmed, and vote cancelling.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class VotingBehavior : NetworkMultiton<VotingBehavior>
    {
        public struct VoterState : INetworkSerializeByMemcpy
        {
            public PlayerId CurrentVote;
            public PlayerId Targeting;

            public VoteStatus Status => Targeting == CurrentVote ?
                        CurrentVote == default ? VoteStatus.None : VoteStatus.Voted :
                        Targeting == default ? VoteStatus.Unvoting : VoteStatus.Voting;

            public VoterState WithCurrentVote(PlayerId playerId)
            {
                var s = this;
                s.CurrentVote = playerId;
                return s;
            }
            public VoterState WithTargeting(PlayerId playerId)
            {
                var s = this;
                s.Targeting = playerId;
                return s;
            }
        }

        [SerializeField, AutoSet] private NetworkObject m_networkedPlayerEntity;
        [SerializeField] private float m_votingConfirmationWaitTime = 1.5f;
        private Coroutine m_voteConfirmationRoutine;
        private bool m_isConfirmingVote;
        [SerializeField, AutoSet] private AvatarEntity m_avatarEntity;
        private Transform m_headJoint;
        private Camera m_mainCamera;

        [SerializeField] private UnityEvent<PlayerId> m_onVoteStarted;
        [SerializeField] private UnityEvent<PlayerId> m_onVoteConfirmed;
        [SerializeField] private UnityEvent<PlayerId> m_onVoteCancelled;
        [SerializeField] private UnityEvent<PlayerId> m_onUnvoteStarted;
        [SerializeField] private UnityEvent<PlayerId> m_onUnvoteConfirmed;
        [SerializeField] private UnityEvent<PlayerId> m_onUnvoteCanceled;

        private NetworkVariable<VoterState> m_state = new(writePerm: NetworkVariableWritePermission.Owner);
        public PlayerId CurrentVote => m_state.Value.CurrentVote;
        public VoteStatus VoteStatus => m_state.Value.Status;
        public PlayerId Targeting => m_state.Value.Targeting;
        public PlayerId ArrowTargeting => VoteStatus is VoteStatus.Unvoting ? CurrentVote : Targeting;

        public event Action<VotingBehavior> OnVoteChanged;

        private new void Awake()
        {
            base.Awake();
            m_mainCamera = Camera.main;
        }
        private new void OnEnable()
        {
            base.OnEnable();
            GamePhaseManager.WhenInstantiated(phaseMan => phaseMan.OnPhaseChanged += OnPhaseChanged);
        }

        private new void OnDisable()
        {
            base.OnDisable();
            if (GamePhaseManager.Instance != null)
            {
                GamePhaseManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            m_state.OnValueChanged += OnStateChanged;
        }

        private void OnPhaseChanged(Phase newPhase)
        {
            if (newPhase == Phase.Voting)
            {
                ConfirmUnvote();
                CancelVote();
            }
        }
        private void OnStateChanged(VoterState previousValue, VoterState newValue)
        {
            OnVoteChanged(this);
        }

        /// <summary>
        /// Start the process of setting and confirming the player's vote. To confirm their vote,
        /// the player must maintain the voting gesture for a specified number of seconds.
        /// </summary>
        public void InitiateVote()
        {
            if (m_avatarEntity != null)
            {
                var planes = GeometryUtility.CalculateFrustumPlanes(m_mainCamera);
                m_headJoint = m_avatarEntity.GetJointTransform(Oculus.Avatar2.CAPI.ovrAvatar2JointType.Head);
                var playerOnScreen = GeometryUtility.TestPlanesAABB(planes, new Bounds(m_headJoint.position, new Vector3(0.1f, 0.1f, 0.1f)));

                if (!playerOnScreen)
                {
                    return;
                }
            }

            if (m_isConfirmingVote || GamePhaseManager.CurrentPhase != Phase.Voting) { return; }
            m_isConfirmingVote = true;
            var voter = VotingManager.Instance.GetLocalVoter();
            //If the player is voting for the first time or voting for a new person it starts the confirmation process
            var targetVoteId = NetworkObject.GetOwnerPlayerId();
            if (!targetVoteId.HasValue)
            {
                Debug.LogError("Unable to get the PlayerID of a voting player!");
                return;
            }

            if (voter == default || voter.CurrentVote != targetVoteId)
            {
                m_voteConfirmationRoutine = StartCoroutine(WaitToConfirmVote());
                voter.m_state.Value = voter.m_state.Value.
                    WithTargeting(targetVoteId ?? default);
                voter.m_onVoteStarted.Invoke(Targeting);
            }
            //if the player voting for the same person they have already voted for, it starts the unvoting process
            else
            {
                m_voteConfirmationRoutine = StartCoroutine(WaitToUnvote());
                voter.m_state.Value = voter.m_state.Value.
                    WithTargeting(default);
                voter.m_onUnvoteStarted.Invoke(CurrentVote);
            }
        }

        /// <summary>
        /// Cancels the process of voting for a player
        /// </summary>
        public void CancelVote()
        {
            m_isConfirmingVote = false;
            if (m_voteConfirmationRoutine != null) { StopCoroutine(m_voteConfirmationRoutine); }
            var voter = VotingManager.Instance.GetLocalVoter();
            if (voter != null)
            {
                voter.m_state.Value = voter.m_state.Value.
                    WithTargeting(voter.CurrentVote);
                voter.m_onVoteCancelled.Invoke(voter.CurrentVote);
            }
        }

        /// <summary>
        /// Locks in the players current vote
        /// </summary>
        public void ConfirmVote()
        {
            m_isConfirmingVote = false;
            var playerIsCandidate = VotingManager.Instance.IsPlayerACandidate(m_networkedPlayerEntity);
            if (playerIsCandidate)
            {
                var voter = VotingManager.Instance.GetLocalVoter();
                voter.m_state.Value = voter.m_state.Value.
                    WithCurrentVote(voter.m_state.Value.Targeting);
                voter.m_onVoteConfirmed.Invoke(VotingManager.Instance.GetLocalVoter().CurrentVote);
            }
        }


        /// <summary>
        /// Finalizes the unvoting process
        /// </summary>
        public void ConfirmUnvote()
        {
            var voter = VotingManager.Instance.GetLocalVoter();
            if (voter != null)
            {
                voter.m_state.Value = voter.m_state.Value.
                    WithCurrentVote(default);
                voter.m_onUnvoteConfirmed.Invoke(default);
            }
        }

        /// <summary>
        /// Cancels the unvoting process
        /// </summary>
        public void CancelUnvote()
        {
            m_isConfirmingVote = false;
            if (m_voteConfirmationRoutine != null) { StopCoroutine(m_voteConfirmationRoutine); }
            var voter = VotingManager.Instance.GetLocalVoter();
            if (voter != null)
            {
                voter.m_state.Value = voter.m_state.Value.
                    WithTargeting(voter.CurrentVote);
                voter.m_onUnvoteCanceled.Invoke(voter.CurrentVote);
            }
        }

        private IEnumerator WaitToConfirmVote()
        {
            yield return new WaitForSeconds(m_votingConfirmationWaitTime);

            if (!m_isConfirmingVote)
            {
                CancelVote();
                yield break;
            }

            ConfirmVote();
        }

        private IEnumerator WaitToUnvote()
        {
            yield return new WaitForSeconds(m_votingConfirmationWaitTime);

            if (!m_isConfirmingVote)
            {
                CancelUnvote();
                yield break;
            }

            ConfirmUnvote();
        }
    }
}
