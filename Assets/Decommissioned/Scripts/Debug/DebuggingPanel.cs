// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.XR.Samples;
using TMPro;
using UnityEngine;

namespace Meta.Decommissioned.Logging
{
    [MetaCodeSample("Decommissioned")]
    public class DebuggingPanel : MonoBehaviour
    {
        public TMP_Text ConsoleLog;

        private void Awake() => ApplicationLog.WhenInstantiated(log => log.SetDebuggingPanel(this));
    }
}
