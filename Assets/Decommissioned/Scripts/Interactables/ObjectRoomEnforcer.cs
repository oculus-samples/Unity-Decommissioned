// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using Meta.Utilities;
using Oculus.Interaction;
using UnityEngine;

namespace Meta.Decommissioned.Interactables
{
    /// <summary>
    /// A script that will enforce objects to their original rooms when phases are transitioned.
    /// </summary>
    public class ObjectRoomEnforcer : MonoBehaviour
    {
        private Vector3 m_originalPosition;
        private Quaternion m_originalRotation;
        [SerializeField, AutoSet] private Grabbable m_grabbable;
        private PointerEvent? m_currentPointerEvent;
        private void Awake()
        {
            m_originalPosition = transform.position;
            m_originalRotation = transform.rotation;
        }
        private void OnEnable()
        {
            GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
            GameManager.OnGameStateChanged += OnGameStateChanged;
            m_grabbable.WhenPointerEventRaised += OnPointerEventRaised;
        }
        private void OnDisable()
        {
            GamePhaseManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            GameManager.OnGameStateChanged -= OnGameStateChanged;
            if (m_grabbable != null) { m_grabbable.WhenPointerEventRaised -= OnPointerEventRaised; }
        }

        private void OnPointerEventRaised(PointerEvent pointerEvent)
        {
            switch (pointerEvent.Type)
            {
                case PointerEventType.Hover:
                    break;
                case PointerEventType.Unhover:
                    break;
                case PointerEventType.Select:
                    m_currentPointerEvent = pointerEvent;
                    break;
                case PointerEventType.Unselect:
                    m_currentPointerEvent = null;
                    break;
                case PointerEventType.Move:
                    break;
                case PointerEventType.Cancel:
                    m_currentPointerEvent = null;
                    break;
                default:
                    break;
            }
        }

        private void OnPhaseChanged(Phase newPhase)
        {
            if (newPhase == Phase.Discussion)
            {
                ResetObjectPosition();
            }
        }
        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.GameEnd)
            {
                ResetObjectPosition();
            }
        }

        private void ResetObjectPosition()
        {
            if (m_currentPointerEvent == null)
            {
                return;
            }

            //Tells the Oculus SDK we have dropped this object
            var dropPointerEvent = new PointerEvent(m_currentPointerEvent.Value.Identifier, PointerEventType.Cancel, m_currentPointerEvent.Value.Pose);
            m_grabbable.ProcessPointerEvent(dropPointerEvent);

            transform.position = m_originalPosition;
            transform.rotation = m_originalRotation;
        }
    }
}
