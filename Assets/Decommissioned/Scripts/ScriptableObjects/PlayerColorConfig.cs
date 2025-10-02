// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Utilities;
using Meta.XR.Samples;
using ScriptableObjectArchitecture;
using UnityEngine;

namespace Meta.Decommissioned.ScriptableObjects
{
    [MetaCodeSample("Decommissioned")]
    [CreateAssetMenu(menuName = "Decommissioned/Player Color Config")]
    public class PlayerColorConfig : ScriptableObject
    {
        public enum GameColor
        {
            None,
            Pink,
            Orange,
            Yellow,
            Purple,
            Green,
            Blue,
            Cyan,
            Brown
        }

        [SerializeField] private EnumDictionary<GameColor, ColorVariable> m_playerColorLibrary;

        public Color GetColorFromGameColor(GameColor color) => m_playerColorLibrary[color];
    }
}
