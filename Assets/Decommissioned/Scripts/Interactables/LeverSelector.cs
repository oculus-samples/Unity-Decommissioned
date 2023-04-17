// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Interactables
{
    /// <summary>
    /// A class that controls an interactable lever object. By moving this lever up or down, the player can use it
    /// to choose between two different behaviours/options specified within the associated Unity Events.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class LeverSelector : MonoBehaviour
    {
        private enum LeverAxis
        {
            X,
            Y,
            Z,
        }

        [SerializeField, Tooltip("The percentage threshold that the lever will activate the positive side.")]
        private float m_positiveThreshold = 1f;

        [SerializeField, Tooltip("The percentage threshold that the lever will activate the negative side.")]
        private float m_negativeThreshold = 1f;

        [SerializeField, Tooltip("The max angle that this lever considers 100% activated.")]
        private float m_maxLeverAngle = 85f;

        [SerializeField, Tooltip("The axis that this lever swings on.")]
        private LeverAxis m_leverAxis = LeverAxis.X;

        [SerializeField, Tooltip("A reference to the Transform of the lever object.")]
        private Transform m_lever;

        [SerializeField, Tooltip("A reference to the Transform of the Grabbable object.")]
        private Transform m_grabbable;

        [SerializeField, Tooltip("A reference to the Transform that the Grabbable object should follow.")]
        private Transform m_grabbablePoint;

        [SerializeField, Tooltip("Should this lever update using LateUpdate? This is useful if your character grabbing executes later than Update.")]
        private bool m_useLateUpdate;

        private float m_leverPercentage;
        private bool m_hasReachedGoal;
        private Quaternion m_originalGrabbableRotation;

        [SerializeField] private UnityEvent m_onLeverPositive;
        [SerializeField] private UnityEvent m_onLeverNegative;

        private void Awake()
        {
            m_originalGrabbableRotation = m_grabbable.rotation;
        }

        private void Update()
        {
            if (!m_useLateUpdate)
            {
                ConstrainLever();
            }
        }
        private void LateUpdate()
        {
            if (m_useLateUpdate)
            {
                ConstrainLever();
            }
        }

        private void ConstrainLever()
        {
            m_lever.LookAt(m_grabbable);

            var xGuide = m_leverAxis == LeverAxis.X ? m_lever.localEulerAngles.x : 0;
            var yGuide = m_leverAxis == LeverAxis.Y ? m_lever.localEulerAngles.y : -90;
            var zGuide = m_leverAxis == LeverAxis.Z ? m_lever.localEulerAngles.z : 0;
            m_lever.localEulerAngles = new Vector3(xGuide, yGuide, zGuide);

            //Place the grabbable at the correct position
            var xPos = m_leverAxis == LeverAxis.X ? m_grabbable.position.x : m_grabbablePoint.position.x;
            var yPos = m_leverAxis == LeverAxis.Y ? m_grabbable.position.y : m_grabbablePoint.position.y;
            var zPos = m_leverAxis == LeverAxis.Z ? m_grabbable.position.z : m_grabbablePoint.position.z;
            m_grabbable.position = new Vector3(xPos, yPos, zPos);

            var checkedValue = m_leverAxis switch
            {
                LeverAxis.X => m_lever.localEulerAngles.x,
                LeverAxis.Y => m_lever.localEulerAngles.y,
                LeverAxis.Z => m_lever.localEulerAngles.z,
                _ => 0f,
            };

            var isAtMax = checkedValue > m_maxLeverAngle && checkedValue < 180;
            var isAtMin = checkedValue < 360 - m_maxLeverAngle && checkedValue > 180;

            if (isAtMax || isAtMin)
            {
                var resetAngle = isAtMax ? m_maxLeverAngle : -m_maxLeverAngle;
                var xAngle = m_leverAxis == LeverAxis.X ? resetAngle : m_lever.localEulerAngles.x;
                var yAngle = m_leverAxis == LeverAxis.Y ? resetAngle : m_lever.localEulerAngles.y;
                var zAngle = m_leverAxis == LeverAxis.Z ? resetAngle : m_lever.localEulerAngles.z;
                m_lever.localEulerAngles = new Vector3(xAngle, yAngle, zAngle);
                m_grabbable.position = m_grabbablePoint.position;
            }

            m_grabbable.rotation = m_originalGrabbableRotation;

            if (CalculateLeverAngleDelta(checkedValue, 0) > 0 && checkedValue < 180)
            {
                m_leverPercentage = checkedValue / (m_negativeThreshold * m_maxLeverAngle);
                if (m_leverPercentage > m_negativeThreshold && !m_hasReachedGoal)
                {
                    m_hasReachedGoal = true;
                    m_onLeverNegative?.Invoke();
                }
            }
            else
            {
                m_leverPercentage = Mathf.Abs(checkedValue - 360) / (m_positiveThreshold * m_maxLeverAngle);
                if (m_leverPercentage > m_positiveThreshold && !m_hasReachedGoal)
                {
                    m_hasReachedGoal = true;
                    m_onLeverPositive?.Invoke();
                }
            }

            if (m_hasReachedGoal && (m_leverPercentage < m_positiveThreshold || m_leverPercentage < m_negativeThreshold))
            {
                m_hasReachedGoal = false;
            }
        }

        private float CalculateLeverAngleDelta(float angle, float target)
        {
            var angleDelta = Mathf.DeltaAngle(angle, target);
            var angleTo = Mathf.Abs(angleDelta);
            var normalizedDelta = 90 - angleTo;
            normalizedDelta /= 90;

            return Mathf.Clamp(normalizedDelta, -1, 1);
        }
    }
}
