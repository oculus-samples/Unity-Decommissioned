// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.XR.Samples;
using NaughtyAttributes.Editor;
using Unity.Netcode;
using UnityEditor;

// Need to override Netcode's editor with this so NaughtyAttributes will work
// https://github.com/dbrizov/NaughtyAttributes/issues/254

namespace Meta.Decommissioned.Editor
{
    [MetaCodeSample("Decommissioned")]
    [CustomEditor(typeof(NetworkBehaviour), true)]
    [CanEditMultipleObjects]
    public class NetworkBehaviourEditor : NaughtyInspector
    {
    }
}
