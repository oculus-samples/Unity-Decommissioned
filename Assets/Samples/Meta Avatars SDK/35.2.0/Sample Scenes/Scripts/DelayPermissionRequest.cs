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

using System.Collections.Generic;
#if USING_XR_SDK
using UnityEngine;

namespace Oculus.Avatar2
{
    public class DelayPermissionRequest : MonoBehaviour, IUIControllerInterface
    {
        void Start()
        {
            OvrAvatarManager.Instance.automaticallyRequestPermissions = false;
        }

        void Update()
        {
            if (!UIManager.IsPaused && OVRInput.Get(OVRInput.Button.Two))
            {
                OvrAvatarManager.Instance.EnablePermissionRequests();
            }
        }

        public List<UIInputControllerButton> GetControlSchema()
        {
            var delayPermissionReq = new UIInputControllerButton
            {
                button = OVRInput.Button.Two,
                controller = OVRInput.Controller.Active,
                description = "Enables permission requests in OvrAvatarManager",
                scope = "DelayPermissionRequest"
            };
            var buttons = new List<UIInputControllerButton>
            {
                delayPermissionReq,
            };
            return buttons;
        }
    }
}
#endif
