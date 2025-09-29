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

using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Avatar2.Experimental
{
    /// <summary>
    /// </summary>
    public abstract class OvrAvatarBaseBehavior : MonoBehaviour
    {
        protected abstract string MainBehavior { get; set; }
        protected abstract string FirstPersonOutputPose { get; set; }
        protected abstract string ThirdPersonOutputPose { get; set; }
        protected virtual string? CustomBehaviorZipFilePath { get; set; }

        protected OvrAvatarEntity? Entity { get; private set; }

        protected virtual void OnUserAvatarLoaded(OvrAvatarEntity _)
        {
            InitializeBehaviorSystem(MainBehavior, FirstPersonOutputPose, ThirdPersonOutputPose, CustomBehaviorZipFilePath);
        }

        protected virtual void OnEntityPreTeardown(OvrAvatarEntity _)
        {
        }

        protected virtual void InitializeBehaviorSystem(
            string mainBehavior,
            string firstPersonOutputPose,
            string thirdPersonOutputPose,
            string? customBehaviorZipPath = null)
        {
            if (Entity == null)
            {
                OvrAvatarLog.LogError("No valid entity found");
                return;
            }

            if (!string.IsNullOrEmpty(customBehaviorZipPath))
            {
                var behaviorZipPath = Path.Combine(Application.streamingAssetsPath, customBehaviorZipPath!);
                if (!Entity.LoadBehaviorZip(behaviorZipPath))
                {
                    OvrAvatarLog.LogError($"Failed to load custom behavior zip from path {behaviorZipPath}");
                    return;
                }
            }

            if (!Entity.EnableBehaviorSystem(true))
            {
                OvrAvatarLog.LogError("Failed to enable behavior system");
                return;
            }

            if (!Entity.SetMainBehavior(mainBehavior))
            {
                OvrAvatarLog.LogError($"Failed to set main behavior to {mainBehavior}");
                return;
            }

            if (!Entity.SetOutputPose(firstPersonOutputPose,
                    Avatar2.CAPI.ovrAvatar2EntityViewFlags.FirstPerson))
            {
                OvrAvatarLog.LogError($"Failed to set behavior output pose to {firstPersonOutputPose}");
                return;
            }

            if (!Entity.SetOutputPose(thirdPersonOutputPose,
                    Avatar2.CAPI.ovrAvatar2EntityViewFlags.ThirdPerson))
            {
                OvrAvatarLog.LogError($"Failed to set behavior output pose to {thirdPersonOutputPose}");
                return;
            }
        }

        private void Start()
        {
            Entity = GetComponentInParent<OvrAvatarEntity>();
            Assert.IsNotNull(Entity);

            if (!Entity.IsLocal)
            {
                return;
            }

            Entity.PreTeardownEvent.AddListener(OnEntityPreTeardown);

            // If an avatar hasn't already been loaded, we can listen for loaded
            // events, and then trigger initialization once one has loaded.
            Entity.OnUserAvatarLoadedEvent.AddListener(OnUserAvatarLoaded);
            Entity.OnDefaultAvatarLoadedEvent.AddListener(OnUserAvatarLoaded);

            if (Entity.CurrentState == OvrAvatarEntity.AvatarState.UserAvatar || Entity.CurrentState == OvrAvatarEntity.AvatarState.DefaultAvatar)
            {
                // If an avatar is already loaded, we can immediately start initializing the behavior.
                OnUserAvatarLoaded(Entity);
            }
        }
    }
}
