// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.XR.Samples;
using ScriptableObjectArchitecture;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Utils
{
    [MetaCodeSample("Decommissioned")]
    public class TriggerEnterBehavior : MonoBehaviour
    {
        [SerializeField] private bool m_onlyInvokeOnTag;
        [SerializeField] private StringVariable m_triggerTag;
        [SerializeField] private bool m_enforceTriggerExitTime;
        [SerializeField] private float m_exitTime = 0.5f;
        [SerializeField] private UnityEvent<GameObject> m_onTriggerEnter;
        [SerializeField] private UnityEvent<GameObject> m_onTriggerStay;
        [SerializeField] private UnityEvent<GameObject> m_onTriggerExit;

        private bool m_isInsideTrigger;

        private void OnTriggerEnter(Collider other)
        {
            if (m_onlyInvokeOnTag && !other.CompareTag(m_triggerTag)) { return; }
            m_isInsideTrigger = true;
            m_onTriggerEnter.Invoke(other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            if (m_onlyInvokeOnTag && !other.CompareTag(m_triggerTag)) { return; }

            m_isInsideTrigger = false;

            if (m_enforceTriggerExitTime)
            {
                _ = StartCoroutine(WaitForExitTime(other.gameObject));
                return;
            }

            m_onTriggerExit.Invoke(other.gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            if (m_onlyInvokeOnTag && !other.CompareTag(m_triggerTag)) { return; }
            m_onTriggerStay.Invoke(other.gameObject);
        }

        private IEnumerator WaitForExitTime(GameObject triggerObject)
        {
            yield return new WaitForSeconds(m_exitTime);
            if (m_isInsideTrigger) { yield break; }
            m_onTriggerExit.Invoke(triggerObject);
        }
    }
}
