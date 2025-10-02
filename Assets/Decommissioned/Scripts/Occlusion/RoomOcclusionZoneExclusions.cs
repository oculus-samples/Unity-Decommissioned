// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.Occlusion
{
    /// <summary>
    /// Defines renderer exclusions from a <see cref="RoomOcclusionZone"/> to ignore when applying occlusion to the room.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    [CreateAssetMenu(menuName = "Decommissioned/Room Occlusion Zone Exclusion")]
    public class RoomOcclusionZoneExclusions : ScriptableObject
    {
        [Tooltip("The prefabs or game objects to be ignored by an occlusion zone.")]
        [field: SerializeField] public GameObject[] OcclusionExclusions { get; private set; }
    }
}
