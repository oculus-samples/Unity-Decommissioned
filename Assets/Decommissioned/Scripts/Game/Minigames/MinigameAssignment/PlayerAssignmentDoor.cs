// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    /// <summary>
    /// Defines the assignment door for the <see cref="PlayerAssignmentInterface"/> that animates open and closed.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class PlayerAssignmentDoor : MonoBehaviour
    {
        [SerializeField] private AnimationCurve m_doorRotationCurve = new();
        private Transform m_doorTransform;
        private Vector3 m_doorCurrentRotation;
        private bool m_animateDoor;
        private bool m_doorOpening;
        private float m_animTime;

        private void Start()
        {
            m_doorTransform = transform;
            m_doorCurrentRotation = m_doorTransform.eulerAngles;
        }

        private void Update()
        {
            if (m_animateDoor)
            {
                if (m_doorOpening)
                {
                    m_animTime += Time.deltaTime;
                }
                else
                {
                    m_animTime -= Time.deltaTime;
                }
                var keyCurveValue = m_doorRotationCurve.Evaluate(m_animTime);
                m_doorCurrentRotation.x = -90f + keyCurveValue;
                m_doorTransform.eulerAngles = m_doorCurrentRotation;

                if ((m_doorOpening && m_animTime > m_doorRotationCurve.keys[^1].time) || (!m_doorOpening && m_animTime <= 0))
                {
                    m_animateDoor = false;
                }
            }
        }

        /// <summary>
        /// Opens this assignment door.
        /// </summary>
        [ContextMenu("Open")]
        public void OpenDoor()
        {
            m_doorOpening = true;
            m_animTime = 0;
            m_animateDoor = true;
        }
        /// <summary>
        /// Closes this assignment door.
        /// </summary>
        [ContextMenu("Close")]
        public void CloseDoor()
        {
            m_doorOpening = false;
            m_animTime = m_doorRotationCurve.keys[^1].time;
            m_animateDoor = true;
        }
    }
}
