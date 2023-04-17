// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

namespace Meta.Decommissioned.Utils
{
    public class PokeRadius : MonoBehaviour
    {
        [SerializeField]
        private PokeInteractable m_interactable;
        [SerializeField]
        private float m_radius = 0.05f;

        private List<PokeInteractor> m_trackedInteractors = new();
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, m_radius);
        }
        private void Awake()
        {
            if (m_interactable)
            {
                m_interactable.WhenInteractorAdded.Action += WhenInteractorAdded_Action;
                m_interactable.WhenInteractorRemoved.Action += WhenInteractorRemoved_Action;
            }
        }

        private void WhenInteractorRemoved_Action(PokeInteractor obj)
        {
            _ = m_trackedInteractors.Remove(obj);
        }

        private void WhenInteractorAdded_Action(PokeInteractor obj)
        {
            m_trackedInteractors.Add(obj);
        }

        private List<PokeInteractor> m_removal = new();
        private void Update()
        {
            foreach (var interactor in m_trackedInteractors)
            {
                if (interactor.HasSelectedInteractable && interactor != m_interactable)
                {
                    m_removal.Add(interactor);
                }
                else
                {
                    var distance = Vector3.Distance(transform.position, interactor.Origin);
                    if (distance > m_radius)
                    {
                        m_removal.Add(interactor);
                    }
                }

            }

            foreach (var interactor in m_removal)
            {
                Disengage(interactor);
            }

            if (m_removal.Count > 0)
            {
                m_removal.Clear();
            }
        }

        private void Disengage(PokeInteractor interactor)
        {
            m_interactable.RemoveInteractor(interactor);
        }
    }
}

