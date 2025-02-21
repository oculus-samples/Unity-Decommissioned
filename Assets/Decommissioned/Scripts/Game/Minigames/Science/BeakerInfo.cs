// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Multiplayer.Core;
using Meta.Utilities;
using Oculus.Interaction;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    public class BeakerInfo : MonoBehaviour
    {
        [field: SerializeField, AutoSet] public ChemistryBeaker Logic { private set; get; }
        [field: SerializeField, AutoSet] public ClientNetworkTransform NetTransform { private set; get; }
        [field: SerializeField, AutoSet] public NetworkObject NetObject { private set; get; }
        [field: SerializeField, AutoSet] public Grabbable Grabbable { private set; get; }
        [field: SerializeField, AutoSet] public RigidbodyKinematicLocker RigidbodyKinematicLocker { private set; get; }
        [field: SerializeField, AutoSet] public Rigidbody Rigidbody { private set; get; }
        [field: SerializeField, AutoSet] public Collider Collider { private set; get; }
    }
}
