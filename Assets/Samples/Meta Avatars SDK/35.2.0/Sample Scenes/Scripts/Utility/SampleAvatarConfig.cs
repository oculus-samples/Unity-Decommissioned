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
using System.Collections.Generic;
using UnityEngine;
using Oculus.Avatar2;


public class SampleAvatarConfig
{
    [Serializable]
    public struct AssetData
    {
        public OvrAvatarEntity.AssetSource source;
        public string path;
    }

    // OvrAvatarEntity
    public CAPI.ovrAvatar2EntityCreateInfo CreationInfo;
    public CAPI.ovrAvatar2EntityViewFlags ActiveView;
    public CAPI.ovrAvatar2EntityManifestationFlags ActiveManifestation;
    // Sample Avatar Entity
    public bool LoadUserFromCdn = true;
    public List<AssetData>? Assets;

    public override string ToString()
    {
        return $"\tCreationInfo:\n" +
               $"\t\tFeatures: {CreationInfo.features.ToString()}\n" +
               $"\t\trenderFilters: {CreationInfo.renderFilters.ToString()}\n" +
               $"\t\tRender Filters:\n" +
               $"\t\t\tLOD Flags: {CreationInfo.renderFilters.lodFlags}\n" +
               $"\t\t\tManifestation Flags: {CreationInfo.renderFilters.manifestationFlags}\n" +
               $"\t\t\tView Flags: {CreationInfo.renderFilters.viewFlags}\n" +
               $"\t\t\tSub Mesh Inclusion Flags: {CreationInfo.renderFilters.subMeshInclusionFlags}\n" +
               $"\t\t\tQuality: {CreationInfo.renderFilters.quality}\n" +
               $"\t\t\tLoad Rig Zip From GLB: {CreationInfo.renderFilters.loadRigZipFromGlb}\n" +
               $"\t\tlodFlags: {CreationInfo.lodFlags.ToString()}\n" +
               $"\t\tIsValid: {CreationInfo.IsValid.ToString()}\n" +
               $"\tActiveView: {ActiveView.ToString()}\n" +
               $"\tActiveManifestation: {ActiveManifestation.ToString()}\n" +
               $"\tLoadUserFromCdn: {LoadUserFromCdn}\n";
    }
}
