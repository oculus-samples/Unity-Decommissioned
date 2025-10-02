// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Linq;
using Meta.Decommissioned.Game;
using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.Lobby
{
    /**
     * Class representing a phase indicator light; on Game state change, this light
     * toggles on or off depending on the states assigned to it.
     **/
    [MetaCodeSample("Decommissioned")]
    public class PhaseLight : MonoBehaviour
    {
        [SerializeField] private MeshRenderer m_lightMeshRenderer;
        [SerializeField] private Color m_activeColor = Color.white;
        [SerializeField] private Color m_inactiveColor = Color.black;
        [SerializeField] private Phase[] m_trackedPhases;

        private void Awake() => m_lightMeshRenderer.material.SetColor("_EmissionColor", Color.black);

        private void Start() => GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;

        public void OnDestroy() => GamePhaseManager.Instance.OnPhaseChanged -= OnPhaseChanged;

        private void OnPhaseChanged(Phase newPhase)
        {
            if (m_trackedPhases.Contains(newPhase))
            {
                m_lightMeshRenderer.material.SetColor("_EmissionColor", m_activeColor);
            }
            else
            {
                m_lightMeshRenderer.material.SetColor("_EmissionColor", m_inactiveColor);
            }
        }
    }
}
