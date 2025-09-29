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

using System;
using Oculus.Platform;
using UnityEngine;

namespace Oculus.Avatar2
{
    // This class is used for backwards compatibility
    public class AvatarEditorDeeplink
    {
        public static void LaunchAvatarEditor()
        {
            AvatarEditorUtils.LaunchAvatarEditor();
        }
    }

    public class AvatarEditorUtils
    {

#if UNITY_ANDROID && !UNITY_EDITOR
        private static string IS_OPENED_BY_SDK2_APP_KEY = "isOpenedBySdk2App";
        private static string VERSION_KEY = "version";
#endif
        public static void LaunchAvatarEditor()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
            AvatarEditorOptions options = new AvatarEditorOptions();
            options.SetSourceOverride("avatar_2_sdk");
            var result = new Request<Oculus.Platform.Models.AvatarEditorResult>(Oculus.Platform.CAPI.ovr_Avatar_LaunchAvatarEditor((IntPtr)options));
#elif UNITY_ANDROID
            try
            {
                AndroidJavaObject activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
                string packageName = currentActivity.Call<string>("getPackageName");

                var intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setPackage", "com.oculus.vrshell");
                intent.Call<AndroidJavaObject>("setAction", "com.oculus.vrshell.intent.action.LAUNCH");
                intent.Call<AndroidJavaObject>(
                    "putExtra",
                    "intent_data",
                    "com.oculus.avatareditor/com.oculus.avatareditor.PanelService"
                );
                intent.Call<AndroidJavaObject>("putExtra", "intent_pkg", packageName);

                var paramsBuilder = new IntentUriParamsBuilder();
                paramsBuilder.AddParam($"returnUrl=apk://{packageName}");
                paramsBuilder.AddParam(IS_OPENED_BY_SDK2_APP_KEY, "true");
                paramsBuilder.AddParam(VERSION_KEY, "V2");

                string uriExtra = paramsBuilder.ToString();
                intent.Call<AndroidJavaObject>(
                    "putExtra",
                    "uri",
                    uriExtra
                );

                // Broadcast instead of starting activity, so that it goes to overlay
                currentActivity.Call("sendBroadcast", intent);
            }
            catch (Exception error)
            {
                OvrAvatarLog.LogError("[AvatarEditorUtils] Launch Avatar Editor error: " + error);
            }
#endif
        }

    }
}
