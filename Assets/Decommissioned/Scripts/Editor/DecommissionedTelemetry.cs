// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using UnityEditor;

namespace Meta.Decommissioned.Editor
{
    /// <summary>
    /// This class helps us track the usage of this showcase
    /// </summary>
    [InitializeOnLoad]
    public static class DecommissionedTelemetry
    {
        private const string ENABLED_KEY = "OculusTelemetryEnabled";
        private const string PRIVACY_POLICY_URL = "https://www.oculus.com/legal/privacy-policy/";

        static DecommissionedTelemetry() => Collect();

        [MenuItem("Oculus/Telemetry Settings")]
        private static void TelemetrySettings()
        {
            Collect(true);
        }

        private static void Collect(bool force = false)
        {
            if (force || EditorPrefs.HasKey(ENABLED_KEY) == false)
            {
                var response = EditorUtility.DisplayDialogComplex(
                    "Enable Meta Telemetry",
                    $"Enabling telemetry will transmit data to Meta about your usage of its samples and tools. This information is used by Meta to improve our products and better serve our developers. For more information, go to this url: {PRIVACY_POLICY_URL}",
                    "Enable",
                    "Opt out",
                    "Open Privacy Policy");

                EditorPrefs.SetBool(ENABLED_KEY, response == 0);

                if (response == 2)
                {
                    EditorPrefs.DeleteKey(ENABLED_KEY);
                    EditorUtility.OpenWithDefaultApp(PRIVACY_POLICY_URL);
                }
            }

            if (EditorPrefs.GetBool(ENABLED_KEY) && SessionState.GetBool("OculusTelemetry-module_loaded-Decommissioned", false) == false)
            {
                _ = OVRPlugin.SetDeveloperMode(OVRPlugin.Bool.True);
                _ = OVRPlugin.SendEvent("module_loaded", "Unity-Decommissioned", "integration");
                SessionState.SetBool("OculusTelemetry-module_loaded-Decommissioned", true);
            }
        }
    }
}
