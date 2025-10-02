// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace Meta.Decommissioned.Interactables
{
    /// <summary>
    /// Special <see cref="ITransformer"/> object specifically for interaction with the handle of a pump. Configures
    /// and manages multiple grab points and handle movement.
    /// 
    /// <p>This component is one of two used to create a pump object; for implementation of a constrained rod, see
    /// the <see cref="PumpControl"/> script.</p>
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class PumpTransformer : MonoBehaviour, ITransformer
    {
        [SerializeField] private float m_maxPushDistance = 0.5f;

        private Vector3 m_initialPosition;
        private Vector3 m_grabOffsetInLocalSpace;

        private IGrabbable m_grabbable;

        private Vector3 GrabPoint
        {
            get
            {
                var grabPoints = m_grabbable.GrabPoints;
                var point0 = grabPoints[0].position;

                if (grabPoints.Count == 1)
                {
                    return point0;
                }

                var point1 = grabPoints[1].position;
                return Vector3.LerpUnclamped(point0, point1, 0.5f);
            }
        }

        public void Initialize(IGrabbable grabbable)
        {
            m_grabbable = grabbable;
            m_initialPosition = m_grabbable.Transform.localPosition;
        }

        public void BeginTransform()
        {
            var targetTransform = m_grabbable.Transform;
            m_grabOffsetInLocalSpace = targetTransform.InverseTransformVector(GrabPoint - targetTransform.position);
        }

        public void UpdateTransform()
        {
            var targetTransform = m_grabbable.Transform;
            var constrainedPosition = GrabPoint - targetTransform.TransformVector(m_grabOffsetInLocalSpace);

            // the translation constraints occur in parent space
            if (targetTransform.parent != null)
            {
                constrainedPosition = targetTransform.parent.InverseTransformPoint(constrainedPosition);
            }

            constrainedPosition.x = m_initialPosition.x;
            constrainedPosition.y = Math.Clamp(constrainedPosition.y, m_initialPosition.y - m_maxPushDistance, m_initialPosition.y);
            constrainedPosition.z = m_initialPosition.z;

            // Convert the constrained position back to world space
            if (targetTransform.parent != null)
            {
                constrainedPosition = targetTransform.parent.TransformPoint(constrainedPosition);
            }

            targetTransform.position = constrainedPosition;
        }

        public void EndTransform() { }
    }
}
