// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE



#pragma warning disable IDE1006

using Oculus.Avatar2;
using Meta.XR.Samples;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Oculus.Interaction.AvatarIntegration
{
    [MetaCodeSample("Decommissioned")]
    public class HandTrackingInputTrackingDelegate : OvrAvatarInputTrackingDelegate
    {
        private Transform _root;
        private IHand _leftHand;
        private IHand _rightHand;
        private IHmd _hmd;

        public HandTrackingInputTrackingDelegate(Transform root, IHand leftHand, IHand rightHand, IHmd hmd)
        {
            _root = root;
            _leftHand = leftHand;
            _rightHand = rightHand;
            _hmd = hmd;
        }

        public override bool GetRawInputTrackingState(
            out OvrAvatarInputTrackingState inputTrackingState)
        {
            inputTrackingState = default;

            var worldToRootPose = _root.GetPose();
            PoseUtils.Invert(ref worldToRootPose);

            var hasData = false;
            if (_hmd.TryGetRootPose(out var headPose))
            {
                inputTrackingState.headsetActive = true;
                inputTrackingState.headset =
                    InteractionAvatarConversions.PoseToAvatarTransform(headPose.GetTransformedBy(worldToRootPose));
                hasData = true;
            }

            if (_leftHand.GetRootPose(out var leftHandRootPose))
            {
                inputTrackingState.leftControllerActive = true;
                inputTrackingState.leftController =
                    InteractionAvatarConversions.PoseToAvatarTransform(leftHandRootPose.GetTransformedBy(worldToRootPose));
                hasData = true;
            }

            if (_rightHand.GetRootPose(out var rightHandRootPose))
            {
                inputTrackingState.rightControllerActive = true;
                inputTrackingState.rightController =
                    InteractionAvatarConversions.PoseToAvatarTransform(rightHandRootPose.GetTransformedBy(worldToRootPose));
                hasData = true;
            }

            return hasData;
        }
    }
}
