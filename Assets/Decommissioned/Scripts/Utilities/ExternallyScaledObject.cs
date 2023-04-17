// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using UnityEngine;

namespace Meta.Decommissioned.Utils
{
    public class ExternallyScaledObject : MonoBehaviour
    {
        [SerializeField] private Transform m_externalPoint;
        private Vector3 m_currentScale = Vector3.one;
        private float m_currentDistance;
        private float m_originalDistance;
        [SerializeField] private bool m_updateInRealTime;

        private void Awake()
        {
            m_currentDistance = Vector3.Distance(m_externalPoint.localPosition, transform.localPosition);
            m_originalDistance = m_currentDistance;
            ScaleObject();
        }

        private void Update()
        {
            if (m_updateInRealTime)
            {
                ScaleObject();
            }
        }

        public void UpdateObjectScale()
        {
            ScaleObject();
        }

        private void ScaleObject()
        {
            m_currentDistance = Vector3.Distance(m_externalPoint.localPosition, transform.localPosition);
            m_currentScale.x = m_currentDistance / m_originalDistance;
            m_currentScale.y = m_currentDistance / m_originalDistance;
            m_currentScale.z = m_currentDistance / m_originalDistance;
            transform.localScale = m_currentScale;
        }
    }
}
