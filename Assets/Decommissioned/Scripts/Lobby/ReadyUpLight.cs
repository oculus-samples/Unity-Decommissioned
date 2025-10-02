// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.Lobby
{
    /**
     * Simple class for managing a light that indicates whether a player spawned at a certain posiiton
     * has readied up or not.
     */
    [MetaCodeSample("Decommissioned")]
    public class ReadyUpLight : MonoBehaviour
    {
        [SerializeField] private GamePosition m_associatedPlayerPosition;
        [SerializeField] private MeshRenderer m_buttonMesh;
        private readonly int m_buttonEmissionProperty = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock m_buttonProperties;

        private void Awake()
        {
            m_buttonProperties = new();
            m_buttonMesh.SetPropertyBlock(m_buttonProperties);
        }

        private void Start() =>
            GamePhaseManager.Instance.OnPhaseChanged += ToggleLightOnPhaseChange;

        /**
         * Behavior upon receiving a ready up event. If the player in this light's
         * assigned position is the one readying up, toggle the light.
         * <param name="readyStatus">Structure containing the data of the player sending the "ready up" event.</param>
         */
        public void OnReadyUpEvent(ReadyUp.ReadyStatus readyStatus)
        {
            if (m_associatedPlayerPosition == null || m_associatedPlayerPosition.OccupyingPlayer == null)
            { return; }

            var positionPlayerId = m_associatedPlayerPosition.OccupyingPlayerId;
            if (!m_associatedPlayerPosition.OccupyingPlayer || !positionPlayerId.HasValue || readyStatus.PlayerId != positionPlayerId.Value) { return; }

            ToggleLight(readyStatus.IsPlayerReady);
        }

        private void ToggleLight(bool turnLightOn)
        {
            var lightColor = turnLightOn ? Color.green : Color.black;
            m_buttonProperties.SetColor(m_buttonEmissionProperty, lightColor);
            m_buttonMesh.SetPropertyBlock(m_buttonProperties);
        }

        private void ToggleLightOnPhaseChange(Phase phase) => ToggleLight(false);
    }
}
