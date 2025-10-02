// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.Utils
{
    [MetaCodeSample("Decommissioned")]
    public class WristFollowing : MonoBehaviour
    {
        [SerializeField] private Vector3 m_offset;
        [SerializeField] private Vector3 m_rot_offset;

        private GameObject m_wrist;

        private void Start() => m_wrist = GameObject.Find("LeftHandAnchor");

        private void LateUpdate()
        {
            var wristPosition = m_wrist.transform;
            transform.position = wristPosition.position + m_offset;
            transform.rotation = wristPosition.rotation * Quaternion.Euler(m_rot_offset);
        }
    }
}
