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

#if USING_XR_SDK

using System;
using Oculus.Avatar2;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;

public enum OvrPlatformInitStatus
{
    NotStarted = 0,
    Initializing,
    Succeeded,
    Failed
}

public static class OvrPlatformInit
{
    private const string logScope = "OvrPlatformInit";

    public static OvrPlatformInitStatus status { get; private set; } = OvrPlatformInitStatus.NotStarted;

    public static void InitializeOvrPlatform()
    {
        if (status == OvrPlatformInitStatus.Succeeded)
        {
            OvrAvatarLog.LogWarning("OvrPlatform is already initialized.", logScope);
            return;
        }

        try
        {
            status = OvrPlatformInitStatus.Initializing;
            Core.AsyncInitialize().OnComplete(InitializeComplete);

            void InitializeComplete(Message<PlatformInitialize> msg)
            {
                if (msg.Data.Result != PlatformInitializeResult.Success)
                {
                    status = OvrPlatformInitStatus.Failed;
                    OvrAvatarLog.LogError("Failed to initialize OvrPlatform", logScope);
                }
                else
                {
                    Entitlements.IsUserEntitledToApplication().OnComplete(CheckEntitlement);
                }
            }

            void CheckEntitlement(Message msg)
            {
                if (msg.IsError == false)
                {
                    Users.GetAccessToken().OnComplete(GetAccessTokenComplete);
                }
                else
                {
                    status = OvrPlatformInitStatus.Failed;
                    var e = msg.GetError();
                    OvrAvatarLog.LogError($"Failed entitlement check: {e.Code} - {e.Message}", logScope);
                }
            }

            void GetAccessTokenComplete(Message<string> msg)
            {
                if (String.IsNullOrEmpty(msg.Data))
                {
                    string output = "Token is null or empty.";
                    if (msg.IsError)
                    {
                        var e = msg.GetError();
                        output = $"{e.Code} - {e.Message}";
                    }

                    status = OvrPlatformInitStatus.Failed;
                    OvrAvatarLog.LogError($"Failed to retrieve access token: {output}", logScope);
                }
                else
                {
                    OvrAvatarLog.LogDebug($"Successfully retrieved access token.", logScope);
                    OvrAvatarEntitlement.SetAccessToken(msg.Data);
                    status = OvrPlatformInitStatus.Succeeded;
                }
            }
        }
        catch (Exception e)
        {
            status = OvrPlatformInitStatus.Failed;
            OvrAvatarLog.LogError($"{e.Message}\n{e.StackTrace}", logScope);
        }
    }

    public static void ResetOvrPlatformInitState()
    {
        status = OvrPlatformInitStatus.NotStarted;
    }
}

#endif
