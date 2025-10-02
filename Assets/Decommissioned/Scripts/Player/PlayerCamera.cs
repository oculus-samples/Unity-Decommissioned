// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Linq;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Multiplayer.Core
{
    [MetaCodeSample("Decommissioned")]
    public class PlayerCamera : Singleton<PlayerCamera>
    {
        protected void Start()
        {
            DontDestroyOnLoad(this);
            Refocus();
        }

        [ContextMenu("Refocus")]
        public void Refocus()
        {
            var target = PlayerObject.Instances.FirstOrDefault(p => p.IsLocalPlayer)?.transform;
            if (target != null)
            {
                var rotation = Quaternion.Euler(0, target.eulerAngles.y, 0);
                transform.SetPositionAndRotation(target.position, rotation);
            }
        }
    }
}
