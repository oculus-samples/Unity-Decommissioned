// Copyright (c) Meta Platforms, Inc. and affiliates.

#if HAS_META_INTERACTION

using Meta.Utilities;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Meta.Utilities.Input
{
    public class XRHandRefChooser : MonoBehaviour
    {
        [SerializeField] private HandRef[] m_targetRefs = new HandRef[0];
        [Interface(typeof(IHand))]
        [SerializeField] private MonoBehaviour[] m_handTrackingHands = new MonoBehaviour[0];
        [Interface(typeof(IHand))]
        [SerializeField] private MonoBehaviour[] m_virtualHands = new MonoBehaviour[0];

        [SerializeField] private GameObject[] m_setActiveForHandTracking = new GameObject[0];
        [SerializeField] private GameObject[] m_setActiveForVirtualHands = new GameObject[0];

        // TODO: Optimize away the Update
        private bool? m_wasHandTracking;
        private void Update()
        {
            var isHandTracking = (OVRInput.GetConnectedControllers() & OVRInput.Controller.Hands) != 0;
            if (m_wasHandTracking == isHandTracking)
                return;
            m_wasHandTracking = isHandTracking;

            // Disable previous before updating the hand ref, this will unregister current WhenHandUpdated callbacks
            // in the hierarchy
            var activeSet = isHandTracking ? m_setActiveForVirtualHands : m_setActiveForHandTracking;
            foreach (var obj in activeSet)
            {
                obj.SetActive(false);
            }

            var sources = isHandTracking ? m_handTrackingHands : m_virtualHands;
            foreach (var (source, target) in sources.Zip(m_targetRefs))
            {
                target.InjectAllHandRef(source as IHand);
            }

            foreach (var obj in m_setActiveForHandTracking)
            {
                obj.SetActive(isHandTracking);
            }
            foreach (var obj in m_setActiveForVirtualHands)
            {
                obj.SetActive(!isHandTracking);
            }
        }
    }
}

#endif
