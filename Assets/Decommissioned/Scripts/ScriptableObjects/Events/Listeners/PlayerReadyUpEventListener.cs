// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Lobby;
using ScriptableObjectArchitecture;

namespace Meta.Decommissioned.ScriptableObjects
{
    public class PlayerReadyUpEventListener :
        BaseGameEventListener<ReadyUp.ReadyStatus, PlayerReadyUpEvent, PlayerReadyUpUnityEvent>
    { }
}
