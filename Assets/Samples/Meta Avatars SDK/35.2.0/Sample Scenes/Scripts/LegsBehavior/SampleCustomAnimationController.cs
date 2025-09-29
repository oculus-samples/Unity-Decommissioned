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

using Oculus.Avatar2.Experimental;
using UnityEngine;

namespace Oculus.Avatar2
{
    /// <summary>
    /// A sample script that demonstrates how to trigger transition between default (non-custom) / custom avatar animation
    /// </summary>
    public class SampleCustomAnimationController : MonoBehaviour
    {
        [Tooltip("The default (non-custom)/custom state transition parameter id as defined in the custom animation graph")]
        public string AnimationTransitionParamId = "TransitionToCustom";

        private OvrAvatarAnimationBehavior? _animationBehavior;

        private bool _thumbstickWasInDeadzone = true;
        private const float _thumbstickDeadzone = 0.2f;

        private void Awake()
        {
            if (!TryGetComponent<OvrAvatarAnimationBehavior>(out _animationBehavior))
            {
                OvrAvatarLog.LogError("AnimationBehavior cannot be null. Drop in the avatar aniamtion behavior that is attached to the avatar game object");
            }
        }

        private void Update()
        {
            if (UIManager.IsPaused)
            {
                return;
            }

            // Pressing the alpha 0 key when running in editor, or button one on the left
            // touch controller triggers the state transition in the animation graph
            if (CheckAnimationTransitionInput())
            {
                if (_animationBehavior!.CustomAnimationBlendFactor <= 0.5f)
                {
                    TransitionToCustomAnimation();
                }
                else
                {
                    TransitionToDefaultAnimation();
                }
            }

            if (_animationBehavior != null)
            {
                // Pressing the alpha 9 key when running in editor, or button two on the right
                // touch controller toggles the sitting state
                if (CheckToggleSitInput())
                {
                    // Toggle sitting on and off
                    _animationBehavior.IsSitting = !_animationBehavior.IsSitting;
                }

                // Pressing the alpha 8 key when running in editor, or button two on the left
                // touch controller recalibrates the stand height
                if (CheckRecalibrateStandHeightInput())
                {
                    _animationBehavior.RecalibrateStandingHeight();
                }
            }

            Vector2 thumbstickInput = GetThumbstickInput();
            if (Mathf.Abs(thumbstickInput.x) >= _thumbstickDeadzone)
            {
                if (_thumbstickWasInDeadzone)
                {
                    // Rotate the avatar's GameObject 45 degrees to the left or right, depending on the sign of the thumbstick x-axis input.
                    transform.rotation *= Quaternion.AngleAxis(45.0f * Mathf.Sign(thumbstickInput.x), Vector3.up);
                }

                // We require that the thumbstick must return to the deadzone before allowing the rotation to be
                // applied again. This prevents accidentally over-rotating if you hold the thumbstick down.
                _thumbstickWasInDeadzone = false;
            }
            else
            {
                _thumbstickWasInDeadzone = true;
            }
        }

        private void TransitionToCustomAnimation()
        {
            if (_animationBehavior!.CustomAnimator == null)
            {
                OvrAvatarLog.LogError("Unable to transition to custom animation, custom animator not found");
                return;
            }

            if (Mathf.Approximately(_animationBehavior!.CustomAnimationBlendFactor, 1))
            {
                OvrAvatarLog.LogWarning("Already running custom animation");
                return;
            }

            _animationBehavior!.CustomAnimator!.SetBool(AnimationTransitionParamId, true);
        }

        private void TransitionToDefaultAnimation()
        {
            if (_animationBehavior!.CustomAnimator == null)
            {
                OvrAvatarLog.LogError("Unable to transition to default animation, custom animator not found");
                return;
            }

            if (Mathf.Approximately(_animationBehavior!.CustomAnimationBlendFactor, 0))
            {
                OvrAvatarLog.LogWarning("Already running default (non-custom) animation");
                return;
            }

            _animationBehavior!.CustomAnimator!.SetBool(AnimationTransitionParamId, false);
        }

        private bool CheckAnimationTransitionInput()
        {
#if USING_XR_SDK
            return Input.GetKeyUp(KeyCode.Alpha0) || Input.GetKeyUp(KeyCode.Keypad0) || OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch);
#else
            return Input.GetKeyUp(KeyCode.Alpha0) || Input.GetKeyUp(KeyCode.Keypad0);
#endif
        }

        private bool CheckToggleSitInput()
        {
#if USING_XR_SDK
            return Input.GetKeyUp(KeyCode.Alpha9) || Input.GetKeyUp(KeyCode.Keypad9) || OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch);
#else
            return Input.GetKeyUp(KeyCode.Alpha9) || Input.GetKeyUp(KeyCode.Keypad9);
#endif
        }

        private bool CheckRecalibrateStandHeightInput()
        {
#if USING_XR_SDK
            return Input.GetKeyUp(KeyCode.Alpha8) || Input.GetKeyUp(KeyCode.Keypad8) || OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTouch);
#else
            return Input.GetKeyUp(KeyCode.Alpha8) || Input.GetKeyUp(KeyCode.Keypad8);
#endif
        }

        private Vector2 GetThumbstickInput()
        {
#if USING_XR_SDK
            return OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
#else
            return Vector2.zero;
#endif
        }
    }
}
