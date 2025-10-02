// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.XR.Samples;
using UnityEditor;

namespace Meta.Decommissioned.Editor
{
    /// <summary>
    /// This class helps us track the usage of this showcase
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    [InitializeOnLoad]
    public static class DecommissionedTelemetry
    {
        static DecommissionedTelemetry() => Collect();

        private static void Collect(bool force = false)
        {
            if (SessionState.GetBool("OculusTelemetry-module_loaded-Decommissioned", false) == false)
            {
                _ = OVRPlugin.SetDeveloperMode(OVRPlugin.Bool.True);
                _ = OVRPlugin.SendEvent("module_loaded", "Unity-Decommissioned", "integration");
                SessionState.SetBool("OculusTelemetry-module_loaded-Decommissioned", true);
            }
        }
    }
}
