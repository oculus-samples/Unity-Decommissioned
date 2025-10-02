// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game.MiniGames;
using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.ScriptableObjects
{
    /// <summary>
    /// A scriptable object containing values determining the attributes and behavior of a MiniGame.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    [CreateAssetMenu(menuName = "Decommissioned/Mini-Game Configuration Object")]
    public class MiniGameConfig : ScriptableObject
    {
        [Tooltip("The name of this MiniGame.")]
        [field: SerializeField] public string MiniGameName { get; private set; } = "Misc MiniGame";

        [Tooltip("The current room this MiniGame resides in.")]
        [field: SerializeField] public MiniGameRoom Room { get; private set; } = MiniGameRoom.None;

        public string RoomName => Room.ToString();

        [field: Header("Stats and Values")]
        [Tooltip("The maximum health of this MiniGame.")]
        [field: SerializeField] public int MaxHealth { get; private set; } = 100;

        [Tooltip("How much health this MiniGame will lose or gain by default.")]
        [field: SerializeField] public int HealthChangeOnAction { get; private set; } = 10;

        [Tooltip("The maximum amount of health this mini game can lose each night.")]
        [field: SerializeField] public int MaxHealthDecreasePerRound { get; private set; } = 35;

        [Tooltip("The amount added to the health decrease cap when the station is occupied.")]
        [field: SerializeField] public int HealthDecreasePlayerBonus { get; private set; } = 10;

        [field: Header("Flags")]
        [Tooltip("Can this MiniGame be assigned as an active MiniGame?")]
        [field: SerializeField] public bool CanBeAssigned { get; set; } = true;

        [Tooltip("Will this MiniGame's health increase on completion?")]
        [field: SerializeField] public bool HealthIncreasesOnSuccess { get; private set; } = false;

        [Tooltip("Will this MiniGame's health drain during the round?")]
        [field: SerializeField] public bool HealthDrainsOverTime { get; private set; } = false;
    }
}
