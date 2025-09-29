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

using Oculus.Avatar2;
using UnityEngine;
using Node = UnityEngine.XR.XRNode;

/*
 *
 */
public class SampleInputTrackingDelegate : OvrAvatarInputTrackingDelegate
{

#if USING_XR_SDK
    private OVRCameraRig? _ovrCameraRig = null;
#endif

#if USING_XR_SDK
    public SampleInputTrackingDelegate(OVRCameraRig? ovrCameraRig)
    {
        _ovrCameraRig = ovrCameraRig;
    }
#endif
    public override bool GetRawInputTrackingState(out OvrAvatarInputTrackingState inputTrackingState)
    {
        inputTrackingState = default;
#if USING_XR_SDK
        bool leftControllerActive = false;
        bool rightControllerActive = false;
        if (OVRInput.GetActiveController() != OVRInput.Controller.Hands)
        {
            leftControllerActive = OVRInput.GetControllerOrientationTracked(OVRInput.Controller.LTouch);
            rightControllerActive = OVRInput.GetControllerOrientationTracked(OVRInput.Controller.RTouch);
        }

        if (_ovrCameraRig is not null)
        {
            inputTrackingState.headsetActive = true;
            inputTrackingState.leftControllerActive = leftControllerActive;
            inputTrackingState.rightControllerActive = rightControllerActive;
            inputTrackingState.leftControllerVisible = false;
            inputTrackingState.rightControllerVisible = false;
            inputTrackingState.headset = (CAPI.ovrAvatar2Transform)_ovrCameraRig.centerEyeAnchor;
            inputTrackingState.leftController = (CAPI.ovrAvatar2Transform)_ovrCameraRig.leftHandAnchor;
            inputTrackingState.rightController = (CAPI.ovrAvatar2Transform)_ovrCameraRig.rightHandAnchor;
            return true;
        }
        else if (OVRNodeStateProperties.IsHmdPresent())
        {
            inputTrackingState.headsetActive = true;
            inputTrackingState.leftControllerActive = leftControllerActive;
            inputTrackingState.rightControllerActive = rightControllerActive;
            inputTrackingState.leftControllerVisible = true;
            inputTrackingState.rightControllerVisible = true;

            if (OVRNodeStateProperties.GetNodeStatePropertyVector3(Node.CenterEye, NodeStatePropertyType.Position,
                OVRPlugin.Node.EyeCenter, OVRPlugin.Step.Render, out var headPos))
            {
                inputTrackingState.headset.position = headPos;
            }
            else
            {
                inputTrackingState.headset.position = Vector3.zero;
            }

            if (OVRNodeStateProperties.GetNodeStatePropertyQuaternion(Node.CenterEye, NodeStatePropertyType.Orientation,
                OVRPlugin.Node.EyeCenter, OVRPlugin.Step.Render, out var headRot))
            {
                inputTrackingState.headset.orientation = headRot;
            }
            else
            {
                inputTrackingState.headset.orientation = Quaternion.identity;
            }

            inputTrackingState.headset.scale = Vector3.one;

            inputTrackingState.leftController.position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
            inputTrackingState.rightController.position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
            inputTrackingState.leftController.orientation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);
            inputTrackingState.rightController.orientation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
            inputTrackingState.leftController.scale = Vector3.one;
            inputTrackingState.rightController.scale = Vector3.one;
            return true;
        }
#endif

        return false;
    }
}
