// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

namespace Meta.Decommissioned.Tutorials
{
    /// <summary>
    /// Manages the sequence of animations used for tutorials in-game.
    /// </summary>
    public class TutorialQueueingManager : MonoBehaviour
    {
        [SerializeField] private List<ManagedAnimation> m_managedAnimations;

        public void Play(string animationName)
        {
            var animation = m_managedAnimations.First(x => x.AnimationName == animationName);
            if (animation != default) { animation.PlayAnimation(); }
        }

        public void ActivateAnimations()
        {
            foreach (var anim in m_managedAnimations) { anim.ActivateAnimation(); }
        }

        public void DeactivateAnimations()
        {
            foreach (var anim in m_managedAnimations) { anim.DeactivateAnimation(); }
        }

        /// <summary>
        /// An animation managed by a <see cref="TutorialQueueingManager"/> for tutorials in-game.
        /// </summary>
        [Serializable]
        public class ManagedAnimation
        {
            [SerializeField] private Animator[] m_tutorialAnimators;
            [field: SerializeField] public bool IsQueued { get; private set; } = true;

            [field: HideIf(nameof(IsQueued))]
            [field: SerializeField]
            public string AnimationName { get; private set; } = "";

            private int m_animationStartId = Animator.StringToHash("Play");

            private int m_animationStopId = Animator.StringToHash("Stop");

            public void PlayAnimation()
            {
                foreach (var animator in m_tutorialAnimators)
                {
                    animator.SetTrigger(m_animationStartId);
                }
            }

            public void StopAnimation()
            {
                foreach (var animator in m_tutorialAnimators)
                {
                    animator.SetTrigger(m_animationStopId);
                }
            }

            public void DeactivateAnimation()
            {
                foreach (var animator in m_tutorialAnimators)
                {
                    animator.enabled = false;
                }
            }

            public void ActivateAnimation()
            {
                foreach (var animator in m_tutorialAnimators)
                {
                    animator.enabled = true;
                }
            }
        }
    }
}
