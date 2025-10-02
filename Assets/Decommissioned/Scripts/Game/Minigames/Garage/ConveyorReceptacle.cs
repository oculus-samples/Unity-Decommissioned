// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Game.MiniGames
{
    public enum Receptacles
    {
        ReceptacleA,
        ReceptacleB,
        NoReceptacle
    }

    [MetaCodeSample("Decommissioned")]
    public class ConveyorReceptacle : MonoBehaviour
    {
        public Receptacles ReceptacleType
        {
            get => m_receptacleType;
            private set => m_receptacleType = value;
        }

        [SerializeField] private Receptacles m_receptacleType = Receptacles.ReceptacleA;
        [SerializeField] private UnityEvent m_onObjectCorrectDestination;
        [SerializeField] private UnityEvent m_onObjectWrongDestination;

        /**
         * Execute behavior upon receiving a Conveyor Object.
         */
        public void OnObjectReceived(ConveyorObject conveyorObject)
        {
            var eventToInvoke = conveyorObject.RequiredReceptacle == m_receptacleType
                ? m_onObjectCorrectDestination
                : m_onObjectWrongDestination;

            eventToInvoke.Invoke();
        }

    }
}
