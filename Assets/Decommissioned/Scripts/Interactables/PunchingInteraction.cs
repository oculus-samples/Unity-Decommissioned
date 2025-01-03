// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.Decommissioned.Input;
using Meta.Utilities.Input;
using Oculus.Interaction.Input;
using Oculus.Interaction.PoseDetection;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Interactables
{
    /// <summary>
    /// Encapsulates configuration and values for a "punch" interaction; when the user makes a fist (via controller
    /// or hand tracking) and moves it at a certain velocity, we can detect it as a punching motion and have
    /// objects respond accordingly.
    /// </summary>
    public class PunchingInteraction : MonoBehaviour
    {
        public bool LeftHandIsInPunchPose { get; private set; }
        public bool RightHandIsInPunchPose { get; private set; }
        public bool LeftHandAtTargetVelocity { get; private set; }
        public bool RightHandAtTargetVelocity { get; private set; }
        public float LeftHandVelocity { get; private set; }
        public float RightHandVelocity { get; private set; }

        [field: SerializeField] public float PunchForceRequired { get; private set; } = 0.0055f;

        private bool m_watchPunching;
        private bool m_handsInitialized;
        private Coroutine m_handVelocityCoroutine;

        [SerializeField] private HandRef m_leftHandRockPoseRef;
        [SerializeField] private HandRef m_rightHandRockPoseRef;

        [SerializeField] private ShapeRecognizerActiveState m_leftShapeRecognizer;
        [SerializeField] private ShapeRecognizerActiveState m_rightShapeRecognizer;
        [SerializeField] private TransformRecognizerActiveState m_leftTransformRecognizer;
        [SerializeField] private TransformRecognizerActiveState m_rightTransformRecognizer;

        private Hand m_leftHand;
        private Hand m_rightHand;
        private Transform m_activeLeftHand;
        private Transform m_activeRightHand;

        private YieldInstruction m_handVelocityWaitTime = new WaitForEndOfFrame();
        private XRInputManager m_inputManager;

        public UnityEvent<bool> OnPunchPoseEntered;
        public UnityEvent<bool> OnPunchPoseExited;

        private void Awake()
        {
            XRInputProvider.WhenInstantiated(xrInput => m_inputManager = xrInput.InputManager);
        }

        private void InitializeHands()
        {
            m_leftHand = HandRefHelper.Instance.LeftHandRef;
            m_rightHand = HandRefHelper.Instance.RightHandRef;

            m_leftHandRockPoseRef.InjectHand(m_leftHand);
            m_rightHandRockPoseRef.InjectHand(m_rightHand);

            m_leftShapeRecognizer.InjectFingerFeatureStateProvider(HandRefHelper.Instance.LeftFingerFeatSP);
            m_rightShapeRecognizer.InjectFingerFeatureStateProvider(HandRefHelper.Instance.RightFingerFeatSP);

            m_leftTransformRecognizer.InjectTransformFeatureStateProvider(HandRefHelper.Instance.LeftTransformFeatSP);
            m_rightTransformRecognizer.InjectTransformFeatureStateProvider(HandRefHelper.Instance.RightTransformFeatSP);

            m_activeLeftHand = m_leftHand.transform;
            m_activeRightHand = m_rightHand.transform;

            m_handsInitialized = true;
        }

        public void StartWatchingPose(Transform leftHand = null, Transform rightHand = null)
        {
            if (!m_handsInitialized)
            {
                InitializeHands();
            }

            m_activeLeftHand = leftHand != null ? leftHand : m_leftHand.transform;
            m_activeRightHand = rightHand != null ? rightHand : m_rightHand.transform;

            m_leftHandRockPoseRef.gameObject.SetActive(true);
            m_rightHandRockPoseRef.gameObject.SetActive(true);

            m_watchPunching = true;
            m_handVelocityCoroutine ??= StartCoroutine(WatchHandVelocity());
        }

        public void StopWatchingPose()
        {
            m_leftHandRockPoseRef.gameObject.SetActive(false);
            m_rightHandRockPoseRef.gameObject.SetActive(false);

            m_watchPunching = false;

            if (m_handVelocityCoroutine != null)
            {
                StopCoroutine(m_handVelocityCoroutine);
                m_handVelocityCoroutine = null;
            }
        }

        public void OnPunchHandPoseEntered(bool isLeft)
        {
            if (isLeft)
            {
                LeftHandIsInPunchPose = true;
            }
            else
            {
                RightHandIsInPunchPose = true;
            }

            OnPunchPoseEntered?.Invoke(isLeft);
        }

        public void OnPunchHandPoseExited(bool isLeft)
        {
            if (isLeft)
            {
                LeftHandIsInPunchPose = false;
            }
            else
            {
                RightHandIsInPunchPose = false;
            }

            OnPunchPoseExited?.Invoke(isLeft);
        }

        private IEnumerator WatchHandVelocity()
        {
            while (m_watchPunching)
            {
                if (m_activeLeftHand == null || m_activeRightHand == null)
                {
                    yield break;
                }

                if (!LeftHandIsInPunchPose && !RightHandIsInPunchPose)
                {
                    yield return null;
                    continue;
                }

                var currentLeftFistLocation = m_activeLeftHand.position;
                var currentRightFistLocation = m_activeRightHand.position;

                yield return m_handVelocityWaitTime;

                if (LeftHandIsInPunchPose)
                {
                    var newLeftFistLocation = m_activeLeftHand.position;
                    var leftFistDistance = Vector3.Distance(currentLeftFistLocation, newLeftFistLocation);

                    LeftHandVelocity = leftFistDistance;
                    LeftHandAtTargetVelocity = leftFistDistance >= PunchForceRequired;
                }

                if (RightHandIsInPunchPose)
                {
                    var newRightFistLocation = m_activeRightHand.position;
                    var rightFistDistance = Vector3.Distance(currentRightFistLocation, newRightFistLocation);

                    RightHandVelocity = rightFistDistance;
                    RightHandAtTargetVelocity = rightFistDistance >= PunchForceRequired;
                }
            }
        }

        private void Update()
        {
            if (!m_watchPunching || m_inputManager == null || OVRPlugin.GetHandTrackingEnabled())
            {
                return;
            }

            var leftIndexTriggerPressed = m_inputManager.GetActions(true).AxisIndexTrigger.action.ReadValue<float>();
            var leftHandTriggerPressed = m_inputManager.GetActions(true).AxisHandTrigger.action.ReadValue<float>();
            var leftHandClosedAmount = leftIndexTriggerPressed + leftHandTriggerPressed;

            var rightIndexTriggerPressed = m_inputManager.GetActions(false).AxisIndexTrigger.action.ReadValue<float>();
            var rightHandTriggerPressed = m_inputManager.GetActions(false).AxisHandTrigger.action.ReadValue<float>();
            var rightHandClosedAmount = rightIndexTriggerPressed + rightHandTriggerPressed;

            if (leftHandClosedAmount > 1.5f)
            {
                OnPunchHandPoseEntered(true);
            }
            else
            {
                OnPunchHandPoseExited(true);
            }

            if (rightHandClosedAmount > 1.5f)
            {
                OnPunchHandPoseEntered(false);
            }
            else
            {
                OnPunchHandPoseExited(false);
            }
        }
    }
}
