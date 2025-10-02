// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using Meta.Decommissioned.Lobby;
using Meta.XR.Samples;
using ScriptableObjectArchitecture;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.ScriptableObjects
{
    [MetaCodeSample("Decommissioned")]
    [Serializable]
    public class PlayerReadyUpUnityEvent : UnityEvent<ReadyUp.ReadyStatus>
    { }

    [MetaCodeSample("Decommissioned")]
    [CreateAssetMenu(menuName = "Game Events/Decommissioned/Player Ready Up Event")]
    public class PlayerReadyUpEvent : GameEventBase<ReadyUp.ReadyStatus>
    { }
}
