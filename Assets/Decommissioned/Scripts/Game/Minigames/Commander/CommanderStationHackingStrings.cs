// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using Meta.Utilities;
using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    [MetaCodeSample("Decommissioned")]
    [CreateAssetMenu(menuName = "Decommissioned/Commander Hacking String")]
    public class CommanderStationHackingStrings : ScriptableObject
    {
        [field: SerializeField]
        public EnumDictionary<MiniGameRoom, CommanderHackingString> PositiveStrings { get; private set; } = new();

        [field: SerializeField]
        public EnumDictionary<MiniGameRoom, CommanderHackingString> NegativeStrings { get; private set; } = new();
    }

    [Serializable]
    public struct CommanderHackingString
    {
        [Multiline(6)]
        [SerializeField]
        public string[] HackingStrings;

        public CommanderHackingString(string[] hackingStrings) => HackingStrings = hackingStrings;
    }
}
