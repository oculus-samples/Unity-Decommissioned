/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#nullable enable

#if USING_XR_MANAGEMENT && (USING_XR_SDK_OCULUS || USING_XR_SDK_OPENXR) && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using UnityEngine;
using Oculus.Avatar2;
using Oculus.Platform;
using System;
using System.Collections.Generic;

public class OpenAvatarEditor : MonoBehaviour, IUIControllerInterface
{
    private const string logScope = "open_avatar_editor";
    private float lastLaunchAttemptTimestamp;
    // Only attempt to launch the avatar editor once per N seconds
    private float debouncePeriodSeconds = 2.0f;

    void Update()
    {
#if USING_XR_SDK
        // Button Press
        // TODO: Should the user be able to run avatar editor when in UI?
        if (!UIManager.IsPaused && OVRInput.Get(OVRInput.Button.One) && OVRInput.Get(OVRInput.Button.Two))
        {
            if (lastLaunchAttemptTimestamp + debouncePeriodSeconds > Time.time)
            {
                return;
            }

            lastLaunchAttemptTimestamp = Time.time;
            if (OvrPlatformInit.status != OvrPlatformInitStatus.Succeeded)
            {
                OvrAvatarLog.LogError("OvrPlatform not initialized.", logScope);
                return;
            }

            AvatarEditorUtils.LaunchAvatarEditor();

        }
#endif
    }

#if USING_XR_SDK
    public List<UIInputControllerButton> GetControlSchema()
    {
        var openAvatarEditor = new UIInputControllerButton
        {
            combinationButtons = new List<OVRInput.Button>
            {
                OVRInput.Button.One,
                OVRInput.Button.Two
            },
            controller = OVRInput.Controller.Active,
            description = "Opens the Avatar Editor",
            scope = "OpenAvatarEditor"
        };
        var buttons = new List<UIInputControllerButton>
        {
            openAvatarEditor,
        };
        return buttons;
    }
#endif
}
