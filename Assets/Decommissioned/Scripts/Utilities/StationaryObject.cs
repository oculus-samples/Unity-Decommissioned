// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Oculus.Interaction;
using UnityEngine;

namespace Meta.Decommissioned.Utils
{
    [DefaultExecutionOrder(-1)]
    public class StationaryObject : MonoBehaviour, ITransformer
    {
        [SerializeField, Tooltip("Should this object use LateUpdate to lock itself?")] private bool m_useLateUpdate = true;
        [SerializeField, Tooltip("Shoudl this object lock its position?")] private bool m_lockPosition = true;
        [SerializeField, Tooltip("Should this object lock its rotation?")] private bool m_lockRotation = true;
        [SerializeField, Optional, Tooltip("A custom transform to lock this object to. If specified, this object will lock onto this transform.")] private Transform m_lockTransform;
        private Vector3 m_originalPosition = Vector3.zero;
        private Quaternion m_originalRotation = Quaternion.identity;

        private void Start()
        {
            m_originalPosition = transform.position;
            m_originalRotation = transform.rotation;
        }

        private void Update()
        {
            if (!m_useLateUpdate)
            {
                LockObject();
            }
        }
        private void LateUpdate()
        {
            if (m_useLateUpdate)
            {
                LockObject();
            }
        }

        private void LockObject()
        {
            if (m_lockPosition)
            {
                transform.position = m_lockTransform ? m_lockTransform.position : m_originalPosition;
            }
            if (m_lockRotation)
            {
                transform.rotation = m_lockTransform ? m_lockTransform.rotation : m_originalRotation;
            }
        }

        #region Transformer
        private Vector3 m_initialPosition;
        private IGrabbable m_grabbable;

        public void Initialize(IGrabbable grabbable)
        {
            m_grabbable = grabbable;
            m_initialPosition = m_grabbable.Transform.position;
        }

        public void BeginTransform() { }

        public void UpdateTransform()
        {
            if (m_lockPosition)
            {
                m_grabbable.Transform.position = m_initialPosition;
            }
        }

        public void EndTransform() { }
        #endregion
    }
}
