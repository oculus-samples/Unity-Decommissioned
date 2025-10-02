// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using Meta.Decommissioned.Player;
using Meta.Multiplayer.Avatar;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Meta.XR.Samples;
using Oculus.Avatar2;
using Unity.Mathematics;
using UnityEngine;
using static Meta.Decommissioned.Voting.VotingManager;

namespace Meta.Decommissioned.Voting
{
    /// <summary>
    /// VotingArrow manages and updates a visual that indicates who each player is voting for and whether or
    /// not their vote is locked in.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class VotingArrow : MonoBehaviour
    {
        [SerializeField, AutoSetFromParent] private VotingBehavior m_votingBehavior;
        [SerializeField, AutoSetFromParent] private PlayerColor m_playerColor;
        [SerializeField, AutoSetFromChildren] private Renderer[] m_renderers;
        [SerializeField] private float m_arrowHeight = 1;
        [SerializeField] private float m_arrowSpacing = 0.1f;


        [Header("Hovering")]
        [SerializeField] private float m_hoverSpeed = 1.0f;
        [SerializeField] private float m_hoverOffset = 1.0f;
        [SerializeField] private float m_hoverDistance = 1.0f;

        [Header("Arrow Animation")]
        [SerializeField] private float m_arrowFillTime = 1.5f;
        [SerializeField] private float m_arrowMovementSpeed = 1f;
        private float m_arrowFillAmount;

        private float m_arrowOffset;
        private Transform m_targetTransform;

        private readonly int m_fillColorKeyword = Shader.PropertyToID("_FillColor");
        private readonly int m_fillAmountKeyword = Shader.PropertyToID("_FillAmount");
        private MaterialPropertyBlock m_materialProperties;

        private void Awake()
        {
            m_materialProperties = new();
            VotingManager.WhenInstantiated(_ => m_votingBehavior.OnVoteChanged += OnVoteChanged);
            GamePhaseManager.WhenInstantiated(phaseMan => phaseMan.OnPhaseChanged += OnPhaseChanged);
        }

        private void OnPhaseChanged(Phase newPhase)
        {
            if (newPhase == Phase.Planning)
            {
                ResetFilling();
                transform.position = Vector3.zero;
            }
        }

        private void OnDestroy()
        {
            if (VotingManager.Instance == null || GamePhaseManager.Instance == null)
            {
                return;
            }

            m_targetTransform = null;
            m_votingBehavior.OnVoteChanged -= OnVoteChanged;
            GamePhaseManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }

        private int ArrowCount => VotingManager.Instance.GetArrowsForPlayer(m_votingBehavior.ArrowTargeting).Length;
        private int ArrowIndex => VotingManager.Instance.GetArrowsForPlayer(m_votingBehavior.ArrowTargeting).IndexOf(m_votingBehavior);
        private void OnVoteChanged(VotingBehavior voter)
        {
            if (m_playerColor) { SetColor(m_playerColor.Color); }
            if (m_votingBehavior == null)
            {
                return;
            }

            //update filling state based on new vote information
            switch (m_votingBehavior.VoteStatus)
            {
                case VoteStatus.None:
                    //set fill state to empty, set the fill amount to empty, hide arrow, and disable flashing
                    ResetFilling();
                    HideArrow();
                    break;
                case VoteStatus.Voting:
                    //set fill state to filling, set the fill amount to empty, show the arrow, and make sure flashing is disabled
                    ResetFilling();
                    ShowArrow();
                    break;
                case VoteStatus.Voted:
                    //set fill state to filled, set the fill amount to full, and make sure flashing is disabled
                    ResetFilling(true);
                    break;
                case VoteStatus.Unvoting:
                    //set fill state to unfilling and make sure arrow is flashing to indicate that you are unvoting for someone
                    break;
            }

            //get/set transform that will be used as the root location for the arrow
            var target = m_votingBehavior.Targeting.Object;

            if (target == null)
            {
                return;
            }
            var targetAvatarEntity = target.GetComponent<AvatarEntity>();
            m_targetTransform = targetAvatarEntity ?
                targetAvatarEntity.GetJointTransform(CAPI.ovrAvatar2JointType.Head).transform :
                target.transform;
        }

        private void Update()
        {
            if (GamePhaseManager.CurrentPhase != Phase.Voting)
            {
                return;
            }

            if (m_votingBehavior.VoteStatus is VoteStatus.Voting or VoteStatus.Unvoting)
            {
                UpdateFill();
            }
            UpdateHover();
        }

        private void UpdateFill()
        {
            if (m_votingBehavior.VoteStatus == VoteStatus.Voting)
            {
                m_arrowFillAmount += Time.deltaTime / m_arrowFillTime;
            }
            else if (m_votingBehavior.VoteStatus == VoteStatus.Unvoting)
            {
                m_arrowFillAmount -= Time.deltaTime / m_arrowFillTime;
            }
            m_arrowFillAmount = Mathf.Clamp(m_arrowFillAmount, 0f, 1f);
            UpdateFillLevel(m_arrowFillAmount);
        }

        /// <summary>
        /// Sets the fill amount of the visual to either 0 (empty) or 1 (full).
        /// </summary>
        /// <param name="willSetToFull">If true, set the fill amount to 1 -- otherwise, set it to 0.</param>
        public void ResetFilling(bool willSetToFull = false)
        {
            m_arrowFillAmount = willSetToFull ? 1f : 0f;
            UpdateFillLevel(m_arrowFillAmount);
        }

        private void HideArrow()
        {
            foreach (var renderer in m_renderers)
            {
                renderer.enabled = false;
            }
        }

        private void ShowArrow()
        {
            foreach (var renderer in m_renderers)
            {
                renderer.enabled = true;
            }
        }

        private void UpdateHover()
        {
            if (m_votingBehavior == null || m_targetTransform == null)
            {
                transform.position = (PlayerManager.LocalPlayerId.Object != null ? PlayerManager.LocalPlayerId.Object.transform.position : m_targetTransform != null ? m_targetTransform.position : Vector3.zero) + Vector3.up * 2;
                return;
            }

            var dir = VotingManager.Instance.CenterPosition.position - m_targetTransform.position;

            dir.y = 0;
            dir.Normalize();

            m_arrowOffset = (ArrowIndex - ArrowCount - 1) / 2.0f * m_arrowSpacing;
            var newPosition = m_targetTransform.position + Vector3.up * m_arrowHeight + Vector3.Cross(Vector3.up, dir) * m_arrowOffset;
            transform.forward = Vector3.down;

            newPosition.y += (float)math.sin(Time.timeAsDouble * m_hoverSpeed + ArrowIndex * m_hoverOffset) * m_hoverDistance;

            transform.position = Vector3.Lerp(transform.position, newPosition, m_arrowMovementSpeed - Mathf.Pow(0.0001f, Time.deltaTime));
        }


        /// <summary>
        /// Sets the fill amount of the arrow to a specific level on the shader.
        /// </summary>
        /// <param name="fillAmount">Indicates the current "fill" state of the arrow; should be a value between 0 and 1,
        /// with 0 being completely empty and 1 being completely full.</param>
        private void UpdateFillLevel(float fillAmount)
        {
            m_materialProperties.SetFloat(m_fillAmountKeyword, fillAmount);
            foreach (var renderer in m_renderers)
            {
                renderer.SetPropertyBlock(m_materialProperties);
            }
        }

        /// <summary>
        /// Sets the color of the arrow
        /// </summary>
        /// <param name="color">Color of the arrow</param>
        public void SetColor(Color color)
        {
            m_materialProperties.SetColor(m_fillColorKeyword, color);
            foreach (var renderer in m_renderers)
            {
                renderer.SetPropertyBlock(m_materialProperties);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Make Red")]
        private void MakeRed() => SetColor(Color.red);
#endif
    }
}
