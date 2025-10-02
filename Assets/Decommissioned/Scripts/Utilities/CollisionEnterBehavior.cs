// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.XR.Samples;
using ScriptableObjectArchitecture;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Utils
{
    /// <summary>
    /// Allows us to set additional behavior upon an object entering a collision (i.e. playing sound effects).
    /// Can be configured to execute this behavior only when certain tags are detected.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class CollisionEnterBehavior : MonoBehaviour
    {
        [SerializeField] protected bool m_onlyInvokeOnTag;
        [SerializeField] protected StringVariable m_triggerTag;
        [SerializeField] protected UnityEvent<GameObject> m_onCollisionEnter;
        [SerializeField] protected UnityEvent<GameObject> m_onCollisionExit;

        private void OnCollisionEnter(Collision collision) => OnCollision(collision.gameObject);
        private void OnCollisionExit(Collision collision) => OnExitCollision(collision.gameObject);

        protected virtual void OnCollision(GameObject collidedObject)
        {
            if (m_onlyInvokeOnTag && !collidedObject.CompareTag(m_triggerTag)) { return; }

            m_onCollisionEnter.Invoke(collidedObject);
        }

        protected virtual void OnExitCollision(GameObject collidedObject)
        {
            if (m_onlyInvokeOnTag && !collidedObject.CompareTag(m_triggerTag)) { return; }

            m_onCollisionExit.Invoke(collidedObject);
        }
    }
}
