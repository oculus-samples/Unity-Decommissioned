// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using ScriptableObjectArchitecture;
using UnityEngine;

namespace Meta.Decommissioned.ScriptableObjects
{
    [System.Serializable]
    [CreateAssetMenu(
        fileName = "GamePhaseGameEvent.asset",
        menuName = SOArchitecture_Utility.GAME_EVENT + "Game Phase Event",
        order = 120)]
    public sealed class GamePhaseGameEvent : GameEventBase<GamePhase>
    {
    }
}
