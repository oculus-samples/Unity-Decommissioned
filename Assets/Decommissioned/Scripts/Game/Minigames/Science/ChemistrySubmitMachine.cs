// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.Utilities;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    [MetaCodeSample("Decommissioned")]
    public class ChemistrySubmitMachine : MonoBehaviour
    {
        [SerializeField] private ChemistryMiniGame m_miniGameLogic;
        [SerializeField] private Transform m_vialInsertPosition;
        [SerializeField] private BeakerInfo m_vialInfo;
        private YieldInstruction m_vialReleaseWaitTime = new WaitForSeconds(2);
        [SerializeField, AutoSet] private Animator m_animator;
        private int m_submitVialTrigger = Animator.StringToHash("SubmitVial");
        private int m_submitSolutionTrigger = Animator.StringToHash("SubmitSolution");
        private bool m_isVialInSubmission;

        private void Awake()
        {
            if (!m_animator)
            {
                Debug.LogError("The Chemistry object set did not contain an animator! Please make sure the " +
                               "root of the Chemistry objects contains an Animator component!");
            }

            m_vialInfo.Grabbable.WhenPointerEventRaised += OnVialPointerEventRaised;
        }

        private void OnVialPointerEventRaised(PointerEvent pointerEvent)
        {
            if (pointerEvent.Type == PointerEventType.Select) { m_isVialInSubmission = false; }
        }

        private void Update()
        {
            if (m_isVialInSubmission && m_vialInfo.NetTransform.IsOwner)
            {
                m_vialInfo.NetTransform.Teleport(m_vialInsertPosition.position, m_vialInsertPosition.rotation, m_vialInfo.NetTransform.transform.lossyScale);
            }
        }

        public void OnVialInserted()
        {
            if (!m_animator || m_isVialInSubmission) { return; }
            if (m_vialInfo.Grabbable.SelectingPointsCount > 0) { return; }

            m_isVialInSubmission = true;
            m_vialInfo.Grabbable.enabled = false;
            m_vialInfo.RigidbodyKinematicLocker.enabled = false;
            m_vialInfo.Collider.enabled = false;
            m_animator.SetTrigger(m_submitVialTrigger);
            _ = StartCoroutine(SubmitSolutionAfterTime());
        }

        private void OnSolutionSubmitted()
        {
            m_miniGameLogic.CheckSolutionAccuracy();
            m_animator.SetTrigger(m_submitSolutionTrigger);
            _ = StartCoroutine(ReleaseVialAfterTime());
        }

        private IEnumerator SubmitSolutionAfterTime()
        {
            yield return m_vialReleaseWaitTime;
            OnSolutionSubmitted();
        }

        private IEnumerator ReleaseVialAfterTime()
        {
            yield return m_vialReleaseWaitTime;
            m_vialInfo.Grabbable.enabled = true;
            m_vialInfo.RigidbodyKinematicLocker.enabled = true;
            m_vialInfo.Collider.enabled = true;
        }
    }
}
