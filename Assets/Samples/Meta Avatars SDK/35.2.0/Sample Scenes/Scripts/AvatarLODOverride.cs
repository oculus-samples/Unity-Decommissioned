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

/**
 *
 * AvatarLODOverride class overrides the Level of Detail (LOD) settings for avatars.
 * It provides input-controlled functions to increase or reduce the LOD level of an avatar in runtime.
 *
 * Usage:
 * - Add this script to any OvrAvatarEntity
 * - Use input controls (or public functions) to increase/reduce the Avatar's LOD level.
 *
 */

#if USING_XR_MANAGEMENT && (USING_XR_SDK_OCULUS || USING_XR_SDK_OPENXR) && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using System;
using Oculus.Avatar2;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(OvrAvatarEntity))]
public class AvatarLODOverride : MonoBehaviour, IUIControllerInterface
{
    [Serializable]
    public struct InputMask
    {
        public OVRInput.Controller controller;
        public OVRInput.Button button;
    }

#if USING_XR_SDK
    [Header("Controller Input")]
    [SerializeField]
    private InputMask increaseLODLevelButton = new InputMask
    { controller = OVRInput.Controller.RTouch, button = OVRInput.Button.PrimaryThumbstick };

    [SerializeField]
    private InputMask decreaseLODLevelButton = new InputMask
    { controller = OVRInput.Controller.LTouch, button = OVRInput.Button.PrimaryThumbstick };
#endif

    [SerializeField]
    [Tooltip("Adds position offset to the AvatarLOD debug Label.\n" +
             "Works when AvatarLODManager::displayLODLabels is enabled.")]
    private Vector3 displayLODLabelOffset = new Vector3(0.5f, 1.0f, 0.0f);

#if UNITY_EDITOR
    [Header("Keyboard Input")]
    [Tooltip("Keyboard Debug controls only work inside Unity Editor.")]
    [SerializeField]
    private bool useKeyboardDebug = false;

    [SerializeField]
    private KeyCode increaseLODLevelKeyboard = KeyCode.G;

    [SerializeField]
    private KeyCode decreaseLODLevelKeyboard = KeyCode.F;
#endif

    private OvrAvatarEntity? _avatarEntity;

    private AvatarLODManager? _avatarLODManager;

    private void Awake()
    {
        if (!TryGetComponent(out _avatarEntity))
        {
            OvrAvatarLog.LogError($"AvatarLODOverride failed to get Avatar entity for {name}");
            return;
        }

        if (_avatarEntity is not null)
        {
            _avatarEntity.AvatarLOD.overrideLOD = true;
        }
        else
        {
            OvrAvatarLog.LogError("No Avatar Entity found");
        }
    }

    private void Start()
    {
        if (AvatarLODManager.hasInstance)
        {
            _avatarLODManager = AvatarLODManager.Instance;
        }
        CheckDebugLabel();
    }

    private void OverrideAvatarLODWithOffset(int offset)
    {
        if (_avatarEntity is not null)
        {
            int currentLODLevel;
            if (IsLODOverrideEnabled())
            {
                currentLODLevel = _avatarEntity.AvatarLOD.overrideLevel;
            }
            else
            {
                currentLODLevel = _avatarEntity.AvatarLOD.Level;
                _avatarEntity.AvatarLOD.overrideLOD = true;
            }

            int overrideLODLevel = Mathf.Clamp(currentLODLevel + offset,
                _avatarEntity.AvatarLOD.minLodLevel,
                _avatarEntity.AvatarLOD.maxLodLevel);

            _avatarEntity.AvatarLOD.overrideLevel = overrideLODLevel;

            if (overrideLODLevel != currentLODLevel)
            {
                OvrAvatarLog.LogInfo(
                    $"AvatarEntity {_avatarEntity.name} LOD Changed from {currentLODLevel} to {overrideLODLevel}");
            }
        }
        else
        {
            OvrAvatarLog.LogError("No Avatar Entity found");
        }

        CheckDebugLabel();
    }

    private void CheckDebugLabel()
    {
        if (_avatarLODManager is not null &&
            _avatarLODManager.debug.displayLODLabels)
        {
            _avatarLODManager.debug.displayLODLabelOffset = displayLODLabelOffset;
            if (_avatarEntity is not null)
            {
                _avatarEntity.AvatarLOD.UpdateDebugLabel();

            }
            else
            {
                OvrAvatarLog.LogError("No Avatar Entity found");
            }
        }
    }

#if UNITY_EDITOR
    public void EnableKeyboardDebug()
    {
        useKeyboardDebug = true;
    }
#endif

    public void IncreaseLODLevel()
    {
        OverrideAvatarLODWithOffset(1);
    }

    public void DecreaseLODLevel()
    {
        OverrideAvatarLODWithOffset(-1);
    }

    public bool IsLODOverrideEnabled()
    {
        if (_avatarEntity is not null)
        {
            return _avatarEntity.AvatarLOD.overrideLOD;
        }
        OvrAvatarLog.LogError("No Avatar Entity found");
        return false;
    }

    private void Update()
    {
#if USING_XR_SDK
        if (!UIManager.IsPaused && OVRInput.GetDown(increaseLODLevelButton.button, increaseLODLevelButton.controller))
        {
            IncreaseLODLevel();
        }

        if (!UIManager.IsPaused && OVRInput.GetDown(decreaseLODLevelButton.button, decreaseLODLevelButton.controller))
        {
            DecreaseLODLevel();
        }
#endif
#if UNITY_EDITOR
        if (useKeyboardDebug)
        {
            if (Input.GetKeyDown(increaseLODLevelKeyboard))
            {
                IncreaseLODLevel();
            }

            if (Input.GetKeyDown(decreaseLODLevelKeyboard))
            {
                DecreaseLODLevel();
            }
        }
#endif
    }

#if USING_XR_SDK
    public List<UIInputControllerButton> GetControlSchema()
    {
        var increaseLOD = new UIInputControllerButton
        {
            button = increaseLODLevelButton.button,
            controller = increaseLODLevelButton.controller,
            description = "Increase the LOD Level of the Avatar.",
            scope = "AvatarLODOverride"
        };
        var decreaseLOD = new UIInputControllerButton
        {
            button = decreaseLODLevelButton.button,
            controller = decreaseLODLevelButton.controller,
            description = "Decrease the LOD Level of the Avatar.",
            scope = "AvatarLODOverride"
        };
        var buttons = new List<UIInputControllerButton>
        {
            increaseLOD,
            decreaseLOD,
        };
        return buttons;
    }
#endif
}
