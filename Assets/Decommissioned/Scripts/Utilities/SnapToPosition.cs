// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;
using Quaternion = UnityEngine.Quaternion;

namespace Meta.Decommissioned.Utils
{
    /**
     * Class representing a position we can "snap" a specified object to when within a certain distance.
     */
    [MetaCodeSample("Decommissioned")]
    public class SnapToPosition : MonoBehaviour
    {
        [SerializeField] private GameObject m_snappableObject;
        [SerializeField] private float m_snapDistance = 0.1f;
        [SerializeField] private UnityEvent<GameObject> m_onObjectSnapped;
        private bool m_positionIsOccupied;
        private InteractableGroupView m_interactable;

        private void Awake()
        {
            if (m_snappableObject != null)
            {
                m_interactable = m_snappableObject.GetComponent<InteractableGroupView>();
                if (m_interactable == null)
                {
                    Debug.LogError("The object assigned to a SnapToPosition component did not have an InteractableGroupView component!");
                    enabled = false;
                }
            }
            else
            {
                Debug.LogError("An object with the SnapToPosition component did not have a snappable object assigned!");
                enabled = false;
            }
        }

        private void Update()
        {
            var objectDistance = Vector3.Distance(transform.position, m_snappableObject.transform.position);
            var objectIsGrabbed = m_interactable.State == InteractableState.Select;
            if (objectDistance <= m_snapDistance && !m_positionIsOccupied && !objectIsGrabbed) { SnapOnObject(); }
            else if (objectDistance > m_snapDistance) { m_positionIsOccupied = false; }
        }

        private void SnapOnObject()
        {
            m_snappableObject.transform.position = transform.position;
            m_snappableObject.transform.rotation = Quaternion.identity;
            m_positionIsOccupied = true;
            m_onObjectSnapped.Invoke(m_snappableObject);
        }
    }
}
