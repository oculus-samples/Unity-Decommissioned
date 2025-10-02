// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.Decommissioned.Game;
using Meta.XR.Samples;
using ScriptableObjectArchitecture;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.ScriptableObjects
{
    [MetaCodeSample("Decommissioned")]
    public class RaiseScriptableObjectEvent : PreGameplayStep
    {
        [SerializeField] private GameEvent m_gameEvent;

        public override IEnumerator Run()
        {
            RaiseEventClientRpc();
            IsComplete = true;
            yield break;
        }

        public override void End() { }

        [ClientRpc]
        private void RaiseEventClientRpc() => m_gameEvent.Raise();
    }
}
