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

namespace Oculus.Avatar2
{
    public static class LODGalleryUtils
    {

        public enum LODGalleryAvatarType
        {
            UltraLight = 0,
            Light = 1,
            Standard = 2,
        }

        public static CAPI.ovrAvatar2EntityCreateInfo GetCreationInfoUltraLightQuality(CAPI.ovrAvatar2EntityFeatures features)
        {
            return new CAPI.ovrAvatar2EntityCreateInfo
            {
                features = features,
                renderFilters = new CAPI.ovrAvatar2EntityFilters
                {
                    lodFlags = CAPI.ovrAvatar2EntityLODFlags.All,
                    manifestationFlags = CAPI.ovrAvatar2EntityManifestationFlags.Half,
                    viewFlags = CAPI.ovrAvatar2EntityViewFlags.ThirdPerson,
                    subMeshInclusionFlags = CAPI.ovrAvatar2EntitySubMeshInclusionFlags.All,
                    quality = CAPI.ovrAvatar2EntityQuality.Ultralight,
                    loadRigZipFromGlb = false,
                }
            };
        }

        public static CAPI.ovrAvatar2EntityCreateInfo GetCreationInfoLightQuality(CAPI.ovrAvatar2EntityFeatures features)
        {
            return new CAPI.ovrAvatar2EntityCreateInfo
            {
                features = features,
                renderFilters = new CAPI.ovrAvatar2EntityFilters
                {
                    lodFlags = CAPI.ovrAvatar2EntityLODFlags.All,
                    manifestationFlags = CAPI.ovrAvatar2EntityManifestationFlags.Half,
                    viewFlags = CAPI.ovrAvatar2EntityViewFlags.ThirdPerson,
                    subMeshInclusionFlags = CAPI.ovrAvatar2EntitySubMeshInclusionFlags.All,
                    quality = CAPI.ovrAvatar2EntityQuality.Light,
                    loadRigZipFromGlb = false,
                }
            };
        }

        public static CAPI.ovrAvatar2EntityCreateInfo GetCreationInfoStandardQuality(CAPI.ovrAvatar2EntityFeatures features)
        {
            return new CAPI.ovrAvatar2EntityCreateInfo
            {
                features = features,
                renderFilters = new CAPI.ovrAvatar2EntityFilters
                {
                    lodFlags = CAPI.ovrAvatar2EntityLODFlags.All,
                    manifestationFlags = CAPI.ovrAvatar2EntityManifestationFlags.Half,
                    viewFlags = CAPI.ovrAvatar2EntityViewFlags.ThirdPerson,
                    subMeshInclusionFlags = CAPI.ovrAvatar2EntitySubMeshInclusionFlags.All,
                    quality = CAPI.ovrAvatar2EntityQuality.Standard,
                    loadRigZipFromGlb = false,
                },

            };
        }
    }
}
