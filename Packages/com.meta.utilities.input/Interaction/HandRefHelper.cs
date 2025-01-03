// Copyright (c) Meta Platforms, Inc. and affiliates.

#if HAS_META_INTERACTION

using Oculus.Interaction.Input;
using Oculus.Interaction.PoseDetection;

namespace Meta.Utilities.Input
{
    public class HandRefHelper : Singleton<HandRefHelper>
    {
        [UnityEngine.Serialization.FormerlySerializedAs("m_leftHandRef")]
        public Hand LeftHandRef;
        [UnityEngine.Serialization.FormerlySerializedAs("m_rightHandRef")]
        public Hand RightHandRef;

        [UnityEngine.Serialization.FormerlySerializedAs("m_leftHandAnchor")]
        public HandRef LeftHandAnchor;
        [UnityEngine.Serialization.FormerlySerializedAs("m_rightHandAnchor")]
        public HandRef RightHandAnchor;

        public FingerFeatureStateProvider LeftFingerFeatSP;
        public TransformFeatureStateProvider LeftTransformFeatSP;
        public JointDeltaProvider LeftJointDeltaProvider;

        public FingerFeatureStateProvider RightFingerFeatSP;
        public TransformFeatureStateProvider RightTransformFeatSP;
        public JointDeltaProvider RightJointDeltaProvider;
    }
}

#endif
