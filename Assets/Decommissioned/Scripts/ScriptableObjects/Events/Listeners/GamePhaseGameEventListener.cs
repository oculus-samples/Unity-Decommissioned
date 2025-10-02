// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using Meta.XR.Samples;
using ScriptableObjectArchitecture;
using UnityEngine;

namespace Meta.Decommissioned.ScriptableObjects
{
    [MetaCodeSample("Decommissioned")]
    [AddComponentMenu(SOArchitecture_Utility.EVENT_LISTENER_SUBMENU + "GamePhase")]
    public sealed class GamePhaseGameEventListener : BaseGameEventListener<GamePhase, GamePhaseGameEvent, GamePhaseUnityEvent>
    {
    }
}
