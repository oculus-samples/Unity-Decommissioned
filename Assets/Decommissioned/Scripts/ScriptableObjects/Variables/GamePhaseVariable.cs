// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using Meta.XR.Samples;
using ScriptableObjectArchitecture;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.ScriptableObjects
{
    [MetaCodeSample("Decommissioned")]
    [System.Serializable]
    public class GamePhaseEvent : UnityEvent<GamePhase> { }

    [MetaCodeSample("Decommissioned")]
    [CreateAssetMenu(
        fileName = "GamePhaseVariable.asset",
        menuName = SOArchitecture_Utility.VARIABLE_SUBMENU + "Game Phase Event",
        order = 120)]
    public class GamePhaseVariable : BaseVariable<GamePhase, GamePhaseEvent>
    {
    }
}
