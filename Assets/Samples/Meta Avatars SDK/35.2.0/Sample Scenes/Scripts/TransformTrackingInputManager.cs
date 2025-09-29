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

using Oculus.Avatar2;
using UnityEngine;

public class TrackingTransformsInputControlDelegate : OvrAvatarInputControlDelegate
{
    public CAPI.ovrAvatar2ControllerType controllerType = CAPI.ovrAvatar2ControllerType.Invalid;

    public override bool GetInputControlState(out OvrAvatarInputControlState inputControlState)
    {
        inputControlState = default;
        inputControlState.type = controllerType;

        return true;
    }
}

public class TrackingTransformsInputTrackingDelegate : OvrAvatarInputTrackingDelegate
{
    private TransformTrackingInputManager _transforms;

    public TrackingTransformsInputTrackingDelegate(TransformTrackingInputManager transforms)
    {
        _transforms = transforms;
    }

    public override bool GetRawInputTrackingState(out OvrAvatarInputTrackingState inputTrackingState)
    {
        inputTrackingState = default;
        Quaternion randomRot = Quaternion.Euler(Random.Range(-.1f, .1f), Random.Range(-.1f, .1f), Random.Range(-.1f, .1f));

        if (_transforms.hmd is not null)
        {
            inputTrackingState.headset = (CAPI.ovrAvatar2Transform)_transforms.hmd;
            inputTrackingState.headsetActive = true;
        }


        if (_transforms.leftController is not null)
        {
            inputTrackingState.leftController = (CAPI.ovrAvatar2Transform)_transforms.leftController;
            inputTrackingState.leftController.orientation *= randomRot;
            inputTrackingState.leftControllerActive = true;
            inputTrackingState.leftControllerVisible = _transforms.controllersVisible;
        }
        else
        {
            inputTrackingState.leftControllerActive = false;
        }

        if (_transforms.rightController is not null)
        {
            inputTrackingState.rightController = (CAPI.ovrAvatar2Transform)_transforms.rightController;
            inputTrackingState.rightController.orientation *= randomRot;
            inputTrackingState.rightControllerActive = true;
            inputTrackingState.rightControllerVisible = _transforms.controllersVisible;
        }
        else
        {
            inputTrackingState.rightControllerActive = false;
        }

        return true;
    }
}

// This class assigns Transform data to body tracking system
// so that avatar can be controlled without a headset
public class TransformTrackingInputManager : OvrAvatarInputManager
{
    public Transform? hmd;

    public Transform? leftController;

    public Transform? rightController;

    public bool controllersVisible = false;

    protected override void OnTrackingInitialized()
    {
        var inputTrackingDelegate = new TrackingTransformsInputTrackingDelegate(this);
        var inputControlDelegate = new TrackingTransformsInputControlDelegate();
        _inputTrackingProvider = new OvrAvatarInputTrackingDelegatedProvider(inputTrackingDelegate);
        _inputControlProvider = new OvrAvatarInputControlDelegatedProvider(inputControlDelegate);
    }
}
