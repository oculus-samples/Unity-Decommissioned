// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Unity.Netcode;

namespace Meta.Decommissioned.Game
{
    /// <summary>
    /// A network behavior for a component with behavior that is executed before a <see cref="GamePhase"/> begins.
    /// </summary>
    public abstract class PreGameplayStep : NetworkBehaviour
    {
        protected bool IsComplete { get; set; }
        public abstract IEnumerator Run();

        public abstract void End();
    }
}
