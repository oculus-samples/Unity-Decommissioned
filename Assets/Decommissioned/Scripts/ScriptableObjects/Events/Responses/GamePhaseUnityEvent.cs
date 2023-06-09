// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using UnityEngine.Events;

namespace Meta.Decommissioned.ScriptableObjects
{
    [System.Serializable]
    public sealed class GamePhaseUnityEvent : UnityEvent<GamePhase>
    {
    }
}
