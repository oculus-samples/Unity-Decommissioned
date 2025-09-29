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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class allows the avatar to move forward/backward and left/right based on input from a thumbstick (XR SDK) or keyboard (Unity Editor).
// Horizontal/Vertical movement can be inverted if needed.
public class SampleAvatarLocomotion : MonoBehaviour, IUIControllerInterface
{
    [SerializeField]
    [Tooltip("Controls the speed of movement")]
    public float movementSpeed = 1.0f;

    [SerializeField]
    [Tooltip("Invert the horizontal movement direction. Useful for avatar mirroring")]
    public bool invertHorizontalMovement = false;

    [SerializeField]
    [Tooltip("Invert the vertical movement direction. Useful for avatar mirroring")]
    public bool invertVerticalMovement = false;

#if UNITY_EDITOR
    [SerializeField]
    [Tooltip("Use keyboard buttons in Editor/PCVR to move avatars.")]
    private bool _useKeyboardDebug = false;
#endif


    void Update()
    {
        if (UIManager.IsPaused)
        {
            return;
        }
        Vector2 inputVector;
        Vector3 translationVector;
        float movementDelta = movementSpeed * Time.deltaTime;
#if USING_XR_SDK
        // Moves the avatar forward/back and left/right based on primary input
        inputVector = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        translationVector = new Vector3(invertHorizontalMovement ? -inputVector.x : inputVector.x, 0.0f, invertVerticalMovement ? -inputVector.y : inputVector.y);
        transform.Translate(movementDelta * translationVector);
#endif
#if UNITY_EDITOR
        if (_useKeyboardDebug)
        {
            inputVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            translationVector = new Vector3(invertHorizontalMovement ? -inputVector.x : inputVector.x, 0.0f, invertVerticalMovement ? -inputVector.y : inputVector.y);
            transform.Translate(movementDelta * translationVector);
        }
#endif
    }

#if USING_XR_SDK
    public List<UIInputControllerButton> GetControlSchema()
    {
        var primaryAxis2D = new UIInputControllerButton
        {
            axis2d = OVRInput.Axis2D.PrimaryThumbstick,
            controller = OVRInput.Controller.All,
            description = "Move in XZ plane",
            scope = "SampleAvatarLocomotion"
        };
        var buttons = new List<UIInputControllerButton>
        {
            primaryAxis2D
        };
        return buttons;
    }
#endif
}
