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

namespace Oculus.Avatar2
{
    /// <summary>
    /// A class that listens to state change event for the default animation state.
    ///
    /// A DEFAULT state is a state inside a custom animation graph that represents all
    /// the default animations. An avatar is said to be in default state when it's playing
    /// default animation.
    ///
    /// </summary>
    public class OvrAvatarDefaultStateListener : StateMachineBehaviour
    {
        public delegate void AnimationStateChangeDelegate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex);
        public event AnimationStateChangeDelegate? OnEnterState;
        public event AnimationStateChangeDelegate? OnUpdateState;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            OnEnterState?.Invoke(animator, stateInfo, layerIndex);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            OnUpdateState?.Invoke(animator, stateInfo, layerIndex);
        }
    }
}
