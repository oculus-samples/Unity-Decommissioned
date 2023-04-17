// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using TMPro;
using UnityEngine;

namespace Meta.Decommissioned.Logging
{
    public class DebuggingPanel : MonoBehaviour
    {
        public TMP_Text ConsoleLog;

        private void Awake() => ApplicationLog.WhenInstantiated(log => log.SetDebuggingPanel(this));
    }
}
