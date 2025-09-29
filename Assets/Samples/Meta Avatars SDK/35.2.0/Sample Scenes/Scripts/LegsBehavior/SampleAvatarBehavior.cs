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
    /// Initializes avatar behavior with user defined behavior parameters
    /// </summary>
    public class SampleAvatarBehavior : OvrAvatarBaseBehavior
    {
        [Tooltip("The main behavior to use inside the behavior.zip file")]
        public string MainBehaviorName = "apn_prototype/main";

        [Tooltip("The name for 1st person output pose")]
        public string FirstPersonOutputPoseName = "pose";

        [Tooltip("The name for 3rd person output pose")]
        public string ThirdPersonOutputPoseName = "pose3P";

        [SerializeField]
        [Tooltip("Behavior zip file path inside the streaming asset folder")]
        private string _customBehaviorZipFilePath = string.Empty;

        protected override string MainBehavior { get => MainBehaviorName; set => MainBehaviorName = value; }
        protected override string FirstPersonOutputPose { get => FirstPersonOutputPoseName; set => FirstPersonOutputPoseName = value; }
        protected override string ThirdPersonOutputPose { get => ThirdPersonOutputPoseName; set => ThirdPersonOutputPoseName = value; }
        protected override string? CustomBehaviorZipFilePath { get => _customBehaviorZipFilePath; set => _customBehaviorZipFilePath = value ?? string.Empty; }
    }
}
