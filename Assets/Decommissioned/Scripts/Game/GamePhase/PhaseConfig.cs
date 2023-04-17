// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using UnityEngine;

namespace Meta.Decommissioned.Game
{
    /// <summary>
    /// Scriptable object encapsulating configuration values for a <see cref="GamePhase"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Decommissioned/Phase Configuration")]
    public class PhaseConfig : ScriptableObject
    {
        public float DurationSeconds;
    }
}
