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
using UnityEngine;

/// <summary>
/// This sample script demonstrate how to hot reload a local avatar with a given preset
/// </summary>
public class SamplePresetChange : MonoBehaviour
{
    [Tooltip("The avatar entity in which we want to change preset on")]
    [SerializeField]
    private SampleAvatarEntity? _avatarEntity;

    [Tooltip("The preset value we want to change to")]
    [SerializeField]
    private string _presetValue = string.Empty;

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (GUILayout.Button("Change Preset") && _avatarEntity != null)
        {
            _avatarEntity!.ReloadAvatarManually(_presetValue, Oculus.Avatar2.OvrAvatarEntity.AssetSource.Zip);
        }
    }
#endif
}
