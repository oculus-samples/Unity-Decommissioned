// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    /**
     * Class containing information about a game object representing a conveyer belt.
     * Determines the movement speed and destination of any object that touches it.
     * <seealso cref="ConveyorObject"/>
     */
    [MetaCodeSample("Decommissioned")]
    public class ConveyorBelt : NetworkBehaviour
    {
        [SerializeField] private float m_objectMovementSpeed = 0.5f;
        [SerializeField] private Transform m_beltDestination;
        public Transform ConveyorBeltDestination => m_beltDestination;
        public float ObjectMovementSpeed => m_objectMovementSpeed;
    }
}
