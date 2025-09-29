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

using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Avatar2;
using UnityEngine;

public class UICanvasController : MonoBehaviour, IUIControllerInterface
{
    private const string logScope = "UICanvasController";
    [SerializeField] private Vector3 worldSpaceUIOffset;
    private Camera? _camera;
    private Canvas? _canvas;
    private RenderMode _renderMode = RenderMode.WorldSpace;
#if USING_XR_SDK
    private readonly UIInputControllerButton _resetCanvasPositionButton = new()
    {
        button = OVRInput.Button.PrimaryThumbstick,
        controller = OVRInput.Controller.RTouch,
        description = "Sets the position of the UI to face user's current view",
        scope = logScope
    };
#endif

    private const float INITIAL_ADJUST_DELAY = 0.5f;

    private void Awake()
    {
        _canvas = GetComponentInChildren<Canvas>();
        if (_canvas == null)
        {
            OvrAvatarLog.LogError("UICanvasController::Awake : Null Canvas.", logScope);
            return;
        }
#if UNITY_EDITOR
        _renderMode = OvrAvatarUtility.IsHeadsetActive() ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
#else
        _renderMode = RenderMode.WorldSpace;
#endif
        _canvas.renderMode = _renderMode;

        // Rotate canvas to face the player in WorldSpace
        if (_renderMode == RenderMode.WorldSpace)
        {
            _canvas.transform.Rotate(Vector3.up, 180);
        }
    }

    private void Start()
    {
        if (Camera.main == null)
        {
            return;
        }

        _camera = Camera.main;
        StartCoroutine(DelayedAdjustCanvas());
    }

    private IEnumerator DelayedAdjustCanvas()
    {
        yield return new WaitForSeconds(INITIAL_ADJUST_DELAY);
        AdjustCanvasPosition();
    }

    private void AdjustCanvasPosition()
    {
        if (_camera == null)
        {
            if (Camera.main == null)
            {
                return;
            }

            _camera = Camera.main;
        }

        if (_renderMode == RenderMode.WorldSpace)
        {
            var camPos = _camera.transform.position;
            transform.position = camPos + worldSpaceUIOffset.magnitude * _camera.transform.forward;
            transform.LookAt(camPos);
        }
    }

    private void Update()
    {
        if (_renderMode != RenderMode.WorldSpace)
        {
            return;
        }
#if USING_XR_SDK
        if (OVRInput.GetUp(_resetCanvasPositionButton.button, _resetCanvasPositionButton.controller))
        {
            AdjustCanvasPosition();
        }
#endif
    }

#if USING_XR_SDK
    public List<UIInputControllerButton> GetControlSchema()
    {
        var buttons = new List<UIInputControllerButton>
        {
            _resetCanvasPositionButton,
        };

        return buttons;
    }
#endif
}
