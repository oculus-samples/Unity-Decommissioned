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
using UnityEngine;

// This class is used to assign all children avatar entites to the input and
// behavioral controllers, which are assumed to be loaded and present already.
// The normal order of operations is:
// 1. FindManagers(): Load your managers like OvrAvatarManager and all input mechanisms.
// 2. FindAvatarEntities(): Load you showcase avatars.
// 3. ApplyAssignnments(): Assign the managers to the avatarEntities.
// The steps can either be called seperately as needed or as a group with FindAndAssign().
public class DynamicInputAndBehaviorAssignment : MonoBehaviour
{
    public bool AssignBodyTracking = false;
    public bool AssignFacePose = true;
    public bool AssignEyePose = true;
    public bool AssignLipSync = true;

    private OvrAvatarInputManagerBehavior? bodyTrackingBehavior = null;
    private OvrAvatarFacePoseBehavior? facePoseBehavior = null;
    private OvrAvatarEyePoseBehavior? eyePoseBehavior = null;
    private OvrAvatarLipSyncBehavior? lipSyncBehavior = null;

    private OvrAvatarEntity[]? avatarEntities;

    void Start()
    {
        FindAndAssign();
    }

    public void FindAndAssign()
    {
        FindManagers();
        FindAvatarEntities();
        ApplyAssignnments();
    }

    public void FindManagers()
    {
        OvrAvatarInputManagerBehavior[] bodyTrackingBehaviors = FindObjectsOfType<OvrAvatarInputManagerBehavior>();
        foreach (var behavior in bodyTrackingBehaviors)
        {
            if (behavior.gameObject.activeInHierarchy)
            {
                bodyTrackingBehavior = behavior;
                break;
            }
        }
        OvrAvatarFacePoseBehavior[] facePoseBehaviors = GameObject.FindObjectsOfType<OvrAvatarFacePoseBehavior>();
        foreach (var behavior in facePoseBehaviors)
        {
            if (behavior.gameObject.activeInHierarchy)
            {
                facePoseBehavior = behavior;
                break;
            }
        }
        OvrAvatarEyePoseBehavior[] eyePoseBehaviors = GameObject.FindObjectsOfType<OvrAvatarEyePoseBehavior>();
        foreach (var behavior in eyePoseBehaviors)
        {
            if (behavior.gameObject.activeInHierarchy)
            {
                eyePoseBehavior = behavior;
                break;
            }
        }
        OvrAvatarLipSyncBehavior[] lipSyncBehaviors = GameObject.FindObjectsOfType<OvrAvatarLipSyncBehavior>();
        foreach (var behavior in lipSyncBehaviors)
        {
            if (behavior.gameObject.activeInHierarchy)
            {
                lipSyncBehavior = behavior;
                break;
            }
        }
    }

    public void FindAvatarEntities()
    {
        avatarEntities = GetComponentsInChildren<OvrAvatarEntity>();
    }

    public void ApplyAssignnments()
    {
        if (avatarEntities is not null)
        {
            foreach (OvrAvatarEntity entity in avatarEntities)
            {
                if (AssignBodyTracking && bodyTrackingBehavior != null)
                {
                    entity.SetInputManager(bodyTrackingBehavior);
                }
                if (AssignFacePose && facePoseBehavior != null)
                {
                    entity.SetFacePoseProvider(facePoseBehavior);
                }
                if (AssignEyePose && eyePoseBehavior != null)
                {
                    entity.SetEyePoseProvider(eyePoseBehavior);
                }
                if (AssignLipSync && lipSyncBehavior != null)
                {
                    entity.SetLipSync(lipSyncBehavior);
                }
            }
        }
        else
        {
            OvrAvatarLog.LogError("No Avatar Entities found");
        }
    }

}
