// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Lobby;
using Meta.XR.Samples;
using ScriptableObjectArchitecture;

namespace Meta.Decommissioned.ScriptableObjects
{
    [MetaCodeSample("Decommissioned")]
    public class PlayerReadyUpEventListener :
        BaseGameEventListener<ReadyUp.ReadyStatus, PlayerReadyUpEvent, PlayerReadyUpUnityEvent>
    { }
}
