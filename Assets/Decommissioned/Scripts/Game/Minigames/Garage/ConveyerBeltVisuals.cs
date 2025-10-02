// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    /// <summary>
    /// Defines the effects logic for running the conveyer belts in the garage MiniGame.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class ConveyerBeltVisuals : MonoBehaviour
    {
        /// <summary>
        /// The max speed that the conveyor belt will run at.
        /// </summary>
        [Tooltip("The max speed that the conveyor belt will run at.")]
        [SerializeField] private float m_conveyerSpeed = 1f;

        /// <summary>
        /// The speed that the conveyor belt will speed up and slow down.
        /// </summary>
        [Tooltip("The speed that the conveyor belt will speed up and slow down.")]
        [SerializeField] private float m_conveyerVelocitySpeed = 0.001f;

        /// <summary>
        /// Should this conveyor belt run in reverse?
        /// </summary>
        [Tooltip("Should this conveyor belt run in reverse?")]
        [SerializeField] private bool m_reverse;

        [SerializeField, Tooltip("A reference to any conveyor belt meshes to be moved.")]
        private MeshRenderer[] m_conveyerMeshes;

        private MaterialPropertyBlock m_conveyerMaterialProperty;
        private readonly int m_materialOffsetProperty = Shader.PropertyToID("_BaseMap_ST");
        private bool m_isRunning;
        private bool m_speedUp;
        private Vector4 m_conveyerCurrentOffset = new(1, 1, 0, 0);
        private float m_currentConveyerVelocity;

        private void Start() => m_conveyerMaterialProperty = new();

        private void Update()
        {
            if (!m_isRunning)
            {
                return;
            }

            if (m_speedUp && m_currentConveyerVelocity < m_conveyerSpeed)
            {
                m_currentConveyerVelocity += m_conveyerVelocitySpeed * Time.deltaTime;
            }
            else if (!m_speedUp && m_currentConveyerVelocity > 0)
            {
                m_currentConveyerVelocity -= m_conveyerVelocitySpeed * Time.deltaTime;
            }

            m_conveyerCurrentOffset.w += m_reverse ? -m_currentConveyerVelocity * Time.deltaTime : m_currentConveyerVelocity * Time.deltaTime;
            if (m_conveyerCurrentOffset.w > 1)
            {
                m_conveyerCurrentOffset.w = 0;
            }
            else if (m_conveyerCurrentOffset.w < 0)
            {
                m_conveyerCurrentOffset.w = 1;
            }

            if (m_currentConveyerVelocity < 0)
            {
                m_currentConveyerVelocity = 0;
            }

            m_conveyerMaterialProperty.SetVector(m_materialOffsetProperty, m_conveyerCurrentOffset);

            foreach (var mesh in m_conveyerMeshes)
            {
                if (mesh == null)
                {
                    continue;
                }

                mesh.SetPropertyBlock(m_conveyerMaterialProperty, 1);
            }

            if (Mathf.Approximately(m_currentConveyerVelocity, 0f))
            {
                m_isRunning = false;
            }
        }

        /// <summary>
        /// Informs the conveyer belts to start moving
        /// </summary>
        public void StartConveyorBelts()
        {
            m_isRunning = true;
            m_speedUp = true;
        }

        /// <summary>
        /// Informs the conveyer belts to stop moving
        /// </summary>
        public void StopConveyorBelts() => m_speedUp = false;
    }
}
