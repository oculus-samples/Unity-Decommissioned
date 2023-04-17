// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Lobby;
using Meta.Utilities;
using Oculus.Interaction;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Interactables
{
    /// <summary>
    /// An interactable "pump" object; players can push and pull this object up and down to invoke behaviors
    /// specified in the associated Unity Event.
    ///
    /// <p>This component is one of two used to create a pump object; for implementation of an interactable handle, see
    /// the <see cref="PumpTransformer"/> script.</p>
    /// </summary>
    public class PumpControl : MonoBehaviour
    {
        [SerializeField, Tooltip("A reference to the handle that this pump uses.")]
        private GameObject m_pumpHandle;

        [SerializeField, Tooltip("The amount of distance that the pump handle can be pushed in before being stopped.")]
        private float m_rodPushDistance = .1f;

        [Range(0, 1), SerializeField, Tooltip("The percentage that this pump has to be pushed down in order to activate.")]
        private float m_activationPercentage = 1f;

        [SerializeField, Tooltip("Should this pump reset it's handle back to resting position when it has stopped being grabbed?")]
        private bool m_resetWhenUngrabbed;

        [SerializeField] private float m_resetSpeed = .001f;

        [SerializeField, Tooltip("If specified, this pump will not be functional for any players except for the one at this position.")]
        private GamePosition m_pumpPosition;

        [SerializeField, Tooltip("Should this pump update using LateUpdate? This is useful if your character grabbing executes later than Update.")]
        private bool m_useLateUpdate;

        [SerializeField, AutoSetFromChildren]
        private Grabbable m_handleGrabbable;

        private bool m_active;
        private Transform m_handleTransform;

        private Vector3 m_currentHandlePosition;
        private float m_minHandleY;
        private float m_maxHandleY;
        private bool m_hasBeenActivated;

        [SerializeField, Tooltip("Occurs when this pump has been pushed all the way down.")]
        private UnityEvent m_onPumpActivated;

        private void Awake()
        {
            m_handleTransform = m_pumpHandle.transform;
            m_currentHandlePosition = m_handleTransform.localPosition;
            m_maxHandleY = m_currentHandlePosition.y;
            m_minHandleY = m_maxHandleY - m_rodPushDistance;
            if (m_pumpPosition != null)
            {
                m_pumpPosition.OnOccupyingPlayerChanged += OnPumpPositionOccupantChanged;
            }
        }

        private void OnPumpPositionOccupantChanged(NetworkObject previousPlayer, NetworkObject newPlayer)
        {
            m_active = false;
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (m_pumpPosition != null && localPlayer != null && newPlayer == localPlayer)
            {
                m_active = true;
            }
        }
        private void Update()
        {
            if (!m_useLateUpdate && m_active)
            {
                ConstrainRod();
                WatchPumpHeight();
            }
        }

        private void LateUpdate()
        {
            if (m_useLateUpdate && m_active)
            {
                ConstrainRod();
                WatchPumpHeight();
            }
        }

        private void ConstrainRod()
        {
            m_currentHandlePosition.y = m_handleTransform.localPosition.y;
            if (m_currentHandlePosition.y > m_maxHandleY)
            {
                m_currentHandlePosition.y = m_maxHandleY;
            }
            else if (m_currentHandlePosition.y < m_minHandleY)
            {
                m_currentHandlePosition.y = m_minHandleY;
            }

            if (m_resetWhenUngrabbed && m_handleGrabbable.SelectingPointsCount == 0)
            {
                m_currentHandlePosition.y = Mathf.MoveTowards(m_currentHandlePosition.y, m_maxHandleY, m_resetSpeed);
            }

            m_handleTransform.localPosition = m_currentHandlePosition;
            m_handleTransform.localEulerAngles = Vector3.zero;
        }

        private void WatchPumpHeight()
        {
            var isInGoal = m_currentHandlePosition.y <= (m_minHandleY + m_maxHandleY) * m_activationPercentage;
            if (!m_hasBeenActivated && isInGoal)
            {
                m_hasBeenActivated = true;
                m_onPumpActivated?.Invoke();
            }
            else if (m_hasBeenActivated && !isInGoal)
            {
                m_hasBeenActivated = false;
            }
        }
    }
}
