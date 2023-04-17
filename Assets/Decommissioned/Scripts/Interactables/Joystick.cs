// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Oculus.Interaction;
using UnityEngine;

namespace Meta.Decommissioned.Interactables
{
    /// <summary>
    /// A class that controls a joystick object. Player interaction with this joystick can be
    /// drive other values with directional input.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class Joystick : MonoBehaviour, ITransformer
    {
        [SerializeField, Tooltip("The max angle that this joystick considers 100% tilted.")]
        private float m_maxJoystickAngle = 60f;

        [SerializeField, Tooltip("Should this joystick auto-center itself?")]
        private bool m_joystickCentering = true;

        [SerializeField, Tooltip("The time the joystick will take to re-center itself.")]
        private float m_joystickCenteringTime = 1f;

        [SerializeField, Tooltip("The joystick itself. This will be used to calculate the axis tilt.")]
        private Transform m_joystick;

        [SerializeField, Tooltip("The grabbable object at the end of the joystick.")]
        private Transform m_grabbable;

        [SerializeField, Tooltip("The point where the grabbable object will attempt to stay at during movement.")]
        private Transform m_grabbablePoint;

        [SerializeField, Tooltip("Should this joystick update using LateUpdate? This is useful if your character grabbing executes later than Update.")]
        private bool m_useLateUpdate;

        private bool m_isBeingGrabbed;
        private Vector3 m_joystickCenterLocation = Vector3.zero;
        private float m_joystickXVelocity;
        private float m_joystickZVelocity;
        private float m_joystickXAxis;
        private float m_joystickZAxis;

        [SerializeField, Tooltip("The grabbable component that the user grabs to control the joystick.")]
        private Grabbable m_grabbableComponent;
        private Vector3 m_grabOffsetInLocalSpace;

        private void Awake()
        {
            m_joystickCenterLocation = m_grabbablePoint.position;
        }
        private void OnEnable()
        {
            m_grabbableComponent.WhenPointerEventRaised += OnGrabbableWhenPointerEventRaised;
        }
        private void OnDisable()
        {
            m_grabbableComponent.WhenPointerEventRaised -= OnGrabbableWhenPointerEventRaised;
        }

        private void OnGrabbableWhenPointerEventRaised(PointerEvent pointerEvent)
        {
            if (pointerEvent.Type == PointerEventType.Select)
            {
                m_isBeingGrabbed = true;
            }
            if (pointerEvent.Type == PointerEventType.Unselect)
            {
                m_isBeingGrabbed = false;
            }
        }
        private void Update()
        {
            if (!m_useLateUpdate)
            {
                ConstrainJoystick();
            }
        }
        private void LateUpdate()
        {
            if (m_useLateUpdate)
            {
                ConstrainJoystick();
            }
        }

        private void ConstrainJoystick()
        {
            m_joystick.rotation = Quaternion.LookRotation(m_grabbable.position - m_joystick.position);
            m_joystick.eulerAngles += new Vector3(90, 0, 0);
            m_joystick.localEulerAngles = new Vector3(m_joystick.localEulerAngles.x >= m_maxJoystickAngle ? m_maxJoystickAngle : m_joystick.localEulerAngles.x,
                m_joystick.localEulerAngles.y,
                0);
            m_grabbable.position = m_grabbablePoint.position;

            var newLocalJoystickAngle = m_joystick.localEulerAngles;
            m_joystickXAxis = newLocalJoystickAngle.x / m_maxJoystickAngle;
            m_joystickXAxis *= CalculateJoystickAngleDelta(newLocalJoystickAngle.y, 90);
            m_joystickZAxis = newLocalJoystickAngle.x / m_maxJoystickAngle;
            m_joystickZAxis *= CalculateJoystickAngleDelta(newLocalJoystickAngle.y, 0);

            if (m_joystickCentering && !m_isBeingGrabbed)
            {
                CenterJoystick();
            }
        }

        private void CenterJoystick()
        {
            var grabPosition = m_grabbable.position;
            var newJoystickX = Mathf.SmoothDamp(grabPosition.x, m_joystickCenterLocation.x, ref m_joystickXVelocity, m_joystickCenteringTime);
            var newJoystickZ = Mathf.SmoothDamp(grabPosition.z, m_joystickCenterLocation.z, ref m_joystickZVelocity, m_joystickCenteringTime);
            m_grabbable.position = new Vector3(newJoystickX, grabPosition.y, newJoystickZ);
        }

        /// <summary>
        /// Gets the axis values of this joystick. Values range from -1 to 1.
        /// </summary>
        /// <returns>A <see cref="Vector2"/> of the X and Y axis tilt values of this joystick.</returns>
        public Vector2 GetJoystickAxisValues() => new(m_joystickXAxis, m_joystickZAxis);

        private static float CalculateJoystickAngleDelta(float angle, float target)
        {
            var angleDelta = Mathf.DeltaAngle(angle, target);
            var angleTo = Mathf.Abs(angleDelta);
            var normalizedDelta = 90 - angleTo;
            normalizedDelta /= 90;
            return Mathf.Clamp(normalizedDelta, -1, 1);
        }

        private Vector3 GrabPoint
        {
            get
            {
                var grabPoints = m_grabbableComponent.GrabPoints;
                var point0 = grabPoints[0].position;
                if (grabPoints.Count == 1)
                    return point0;
                var point1 = grabPoints[1].position;
                return Vector3.LerpUnclamped(point0, point1, 0.5f);
            }
        }

        void ITransformer.Initialize(IGrabbable _) { }

        void ITransformer.BeginTransform()
        {
            var targetTransform = m_grabbableComponent.Transform;
            m_grabOffsetInLocalSpace = targetTransform.InverseTransformVector(GrabPoint - targetTransform.position);
        }
        void ITransformer.UpdateTransform()
        {
            var targetTransform = m_grabbableComponent.Transform;
            var constrainedPosition = GrabPoint - targetTransform.TransformVector(m_grabOffsetInLocalSpace);
            targetTransform.position = constrainedPosition;
            ConstrainJoystick();
        }
        void ITransformer.EndTransform() { }
    }
}
