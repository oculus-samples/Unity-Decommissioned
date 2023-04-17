// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Linq;
using Meta.Utilities;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Interactables
{
    /// <summary>
    /// A component that, when attached to an object, allows us to detect if the user is shaking it.
    /// </summary>
    [RequireComponent(typeof(InteractableGroupView))]
    public class ShakeableObject : MonoBehaviour
    {
        [SerializeField] private float m_shakeVelocityThreshold = 5f;
        [SerializeField] private UnityEvent m_onObjectShake;
        [SerializeField, AutoSet] private InteractableGroupView m_interactableGroupView;

        private void FixedUpdate()
        {
            var interactor = m_interactableGroupView.SelectingInteractorViews.OfType<HandGrabInteractor>().FirstOrDefault();

            if (interactor == null)
            {
                return;
            }

            var linearVelocity = interactor.VelocityCalculator.CalculateThrowVelocity(interactor.Interactable.transform).LinearVelocity.magnitude;
            var angularVelocity = interactor.VelocityCalculator.CalculateThrowVelocity(interactor.Interactable.transform).AngularVelocity.magnitude;

            if (linearVelocity >= m_shakeVelocityThreshold && angularVelocity >= m_shakeVelocityThreshold && m_interactableGroupView.State == InteractableState.Select)
            {
                m_onObjectShake.Invoke();
            }
        }
    }
}
