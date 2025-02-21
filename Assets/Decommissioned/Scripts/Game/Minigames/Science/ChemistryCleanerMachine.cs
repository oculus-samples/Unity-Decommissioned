// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.Utilities;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Game.MiniGames
{
    public class ChemistryCleanerMachine : MonoBehaviour
    {

        [SerializeField] private Transform m_vialInsertPoint;
        [SerializeField] private Transform[] m_vialPositions = new Transform[5];
        [SerializeField] private UnityEvent m_onCleanVial;
        private int m_currentStep;
        private bool m_newVialReady;
        private bool m_canInsertVial;

        [SerializeField, AutoSet]
        private Animator m_animator;
        private int m_currentStepId;
        [SerializeField]
        private BeakerInfo m_vialInfo;
        [SerializeField]
        private Transform m_vialTransform;

        private void Awake()
        {
            if (!m_animator)
            {
                Debug.LogError("The Chemistry object set did not contain an animator! Please make sure the root of the Chemistry objects contains an Animator component!");
            }

            m_currentStepId = Animator.StringToHash("CurrentStep");

            if (m_vialInfo.Grabbable != null)
            {
                m_vialInfo.Grabbable.WhenPointerEventRaised += OnVialPointerEventRaised;
            }
            else
            {
                Debug.LogError("The Chemistry vial components were not assigned to the Cleaner Machine! Please make sure the Vial components are assigned to the Cleaner Machine logic component.");
            }

            HideVial(0);
        }

        private void OnVialPointerEventRaised(PointerEvent pointerEvent)
        {
            if (pointerEvent.Type != PointerEventType.Select) { return; }
            m_newVialReady = false;
            m_canInsertVial = true;
        }

        private void Update()
        {
            if (m_newVialReady)
            {
                m_vialTransform.SetPositionAndRotation(m_vialPositions[m_currentStep].position, m_vialPositions[m_currentStep].rotation);
            }
        }

        public void OnVialInserted()
        {
            if (!m_vialInfo.NetObject.IsOwner)
            {
                return;
            }

            if (!m_animator || !m_canInsertVial) { return; }
            if (m_vialInfo.Grabbable.SelectingPointsCount > 0) { return; }
            m_canInsertVial = false;
            m_vialInfo.Grabbable.enabled = false;
            m_vialInfo.RigidbodyKinematicLocker.enabled = false;
            m_vialInfo.Collider.enabled = false;
            m_vialInfo.Rigidbody.isKinematic = true;
            _ = StartCoroutine(InsertVial());
        }

        public void OnNewVialReady() => _ = StartCoroutine(PlaceNewVial());

        private IEnumerator PlaceNewVial()
        {
            var waitForFrame = new WaitForEndOfFrame();
            yield return waitForFrame;
            var step = m_currentStep;
            HideVial(step);

            var vialPosition = m_vialPositions[step];
            m_vialInfo.NetTransform.Teleport(vialPosition.position, vialPosition.rotation, m_vialInfo.NetTransform.transform.lossyScale);

            m_vialInfo.Grabbable.enabled = true;
            m_vialInfo.RigidbodyKinematicLocker.enabled = true;
            m_vialInfo.Collider.enabled = true;
            m_vialInfo.Rigidbody.isKinematic = false; // must be not kinematic so that the PhysicsGrabbable will drop it
            m_newVialReady = true;
        }

        private IEnumerator InsertVial()
        {
            var lastVial = GetLastStep();
            ShowVial(lastVial);

            var toInsertPoint = m_vialInsertPoint.position - m_vialTransform.position;
            m_vialTransform.position += Vector3.ProjectOnPlane(toInsertPoint, m_vialInsertPoint.up);

            while (m_vialTransform.position.IsCloseTo(m_vialInsertPoint.position) is false)
            {
                m_vialInfo.Collider.enabled = false;
                m_vialInfo.Rigidbody.isKinematic = true;
                m_vialTransform.rotation = m_vialInsertPoint.rotation;
                m_vialTransform.position = Vector3.MoveTowards(m_vialTransform.position, m_vialInsertPoint.position, Time.deltaTime * 0.1f);
                yield return null;
            }

            _ = StartCoroutine(CleanVial());
        }

        private IEnumerator CleanVial()
        {
            m_onCleanVial?.Invoke();
            m_vialInfo.Logic.ClearBeakerValues();
            AdvanceStep();
            m_animator.SetInteger(m_currentStepId, m_currentStep);
            yield break;
        }

        private void HideVial(int vialIndex) => m_vialPositions[vialIndex].gameObject.SetActive(false);

        private void ShowVial(int vialIndex) => m_vialPositions[vialIndex].gameObject.SetActive(true);

        private void AdvanceStep()
        {
            m_currentStep++;
            if (m_currentStep >= m_vialPositions.Length) { m_currentStep = 0; }
        }

        private int GetLastStep()
        {
            var lastStep = m_currentStep - 1;
            if (lastStep < 0) { lastStep = 4; }
            return lastStep;
        }
    }
}
