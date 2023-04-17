// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Utilities;
using UnityEngine;

namespace Meta.Decommissioned.Tutorials
{
    public enum InstructionsLibraryKey
    {
        None,
        Main,
        Garage,
        Holodeck,
        Science,
        Habitation,
        Hydroponics,
        Commander,
        Phase,
        Vote,
        Plan,
        Discuss,
        Work
    }


    /// <summary>
    /// A scriptable object storing a set of editable instructions, which can be retrieved and shown
    /// to the player. This asset contains numerous instructions and allows us to retrieve them using
    /// an enum.
    /// <seealso cref="InstructionsLibraryKey"/>
    /// <seealso cref="MiniGameInstructions"/>
    /// </summary>
    [CreateAssetMenu(menuName = "Decommissioned/Instructions Library")]
    public class InstructionsLibrary : ScriptableObject
    {
        [SerializeField]
        protected EnumDictionary<InstructionsLibraryKey, MiniGameInstructions> m_instructionsLibrary = new();

        public EnumDictionary<InstructionsLibraryKey, MiniGameInstructions> Library => m_instructionsLibrary;
    }
}
