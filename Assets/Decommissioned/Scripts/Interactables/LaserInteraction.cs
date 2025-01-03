// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Input;
using Meta.Utilities.Input;
using Oculus.Interaction.Input;
using Oculus.Interaction.PoseDetection;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Interactables
{
    /// <summary>
    /// Encapsulates configuration and values for a laser fired from the player's hand. Determines whether or
    /// not a laser is fired based on the hand's current pose.
    /// </summary>
    public class LaserInteraction : MonoBehaviour
    {
        public bool LeftHandInLaserPose { get; private set; }
        public bool RightHandInLaserPose { get; private set; }

        [SerializeField] private HandRef m_leftHandPaperPoseRef;
        [SerializeField] private HandRef m_rightHandPaperPoseRef;

        [SerializeField] private ShapeRecognizerActiveState m_leftShapeRecognizer;
        [SerializeField] private ShapeRecognizerActiveState m_rightShapeRecognizer;
        [SerializeField] private TransformRecognizerActiveState m_leftTransformRecognizer;
        [SerializeField] private TransformRecognizerActiveState m_rightTransformRecognizer;

        private Hand m_leftHand;
        private Hand m_rightHand;
        private bool m_watchLaserPose;
        private bool m_handsInitialized;
        private bool m_leftControllerIsFiring;
        private bool m_rightControllerIsFiring;

        private XRInputManager m_inputManager;

        public UnityEvent<bool> OnLaserPoseEntered;
        public UnityEvent<bool> OnLaserPoseExited;

        private void Awake() => XRInputProvider.WhenInstantiated(xrInput => m_inputManager = xrInput.InputManager);

        private void InitializeHands()
        {
            m_leftHand = HandRefHelper.Instance.LeftHandRef;
            m_rightHand = HandRefHelper.Instance.RightHandRef;

            m_leftHandPaperPoseRef.InjectHand(m_leftHand);
            m_rightHandPaperPoseRef.InjectHand(m_rightHand);

            m_leftShapeRecognizer.InjectFingerFeatureStateProvider(HandRefHelper.Instance.LeftFingerFeatSP);
            m_rightShapeRecognizer.InjectFingerFeatureStateProvider(HandRefHelper.Instance.RightFingerFeatSP);

            m_leftTransformRecognizer.InjectTransformFeatureStateProvider(HandRefHelper.Instance.LeftTransformFeatSP);
            m_rightTransformRecognizer.InjectTransformFeatureStateProvider(HandRefHelper.Instance.RightTransformFeatSP);

            m_handsInitialized = true;
        }

        public void StartWatchingPose()
        {
            if (!m_handsInitialized)
            {
                InitializeHands();
            }

            m_watchLaserPose = true;
            m_leftHandPaperPoseRef.gameObject.SetActive(true);
            m_rightHandPaperPoseRef.gameObject.SetActive(true);
        }

        public void StopWatchingPose()
        {
            m_watchLaserPose = false;
            m_leftHandPaperPoseRef.gameObject.SetActive(false);
            m_rightHandPaperPoseRef.gameObject.SetActive(false);
        }

        public void OnLaserHandPoseEntered(bool isLeft)
        {
            if (!m_watchLaserPose)
            {
                return;
            }

            if (isLeft)
            {
                LeftHandInLaserPose = true;
            }
            else
            {
                RightHandInLaserPose = true;
            }

            OnLaserPoseEntered?.Invoke(isLeft);
        }

        public void OnLaserHandPoseExited(bool isLeft)
        {
            if (!m_watchLaserPose)
            {
                return;
            }

            if (isLeft)
            {
                LeftHandInLaserPose = false;
            }
            else
            {
                RightHandInLaserPose = false;
            }

            OnLaserPoseExited?.Invoke(isLeft);
        }

        private void Update()
        {
            if (!m_watchLaserPose || m_inputManager == null || OVRPlugin.GetHandTrackingEnabled())
            {
                return;
            }

            var left = m_inputManager.GetActions(true);
            var leftButtonPressed = left.ButtonOne.action.ReadValue<float>() + left.ButtonTwo.action.ReadValue<float>();

            var right = m_inputManager.GetActions(false);
            var rightButtonPressed = right.ButtonOne.action.ReadValue<float>() + right.ButtonTwo.action.ReadValue<float>();

            if (leftButtonPressed > 0.5f)
            {
                if (!m_leftControllerIsFiring)
                {
                    OnLaserHandPoseEntered(true);
                    m_leftControllerIsFiring = true;
                }
            }
            else
            {
                if (m_leftControllerIsFiring)
                {
                    OnLaserHandPoseExited(true);
                }
                m_leftControllerIsFiring = false;
            }

            if (rightButtonPressed > 0.5f)
            {
                if (!m_rightControllerIsFiring)
                {
                    OnLaserHandPoseEntered(false);
                    m_rightControllerIsFiring = true;
                }
            }
            else
            {
                if (m_rightControllerIsFiring)
                {
                    OnLaserHandPoseExited(false);
                }

                m_rightControllerIsFiring = false;
            }
        }
    }
}
