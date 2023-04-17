// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using ScriptableObjectArchitecture;
using UnityEngine;

namespace Meta.Decommissioned.ScriptableObjects
{
    [AddComponentMenu(SOArchitecture_Utility.EVENT_LISTENER_SUBMENU + "GamePhase")]
    public sealed class GamePhaseGameEventListener : BaseGameEventListener<GamePhase, GamePhaseGameEvent, GamePhaseUnityEvent>
    {
    }
}
