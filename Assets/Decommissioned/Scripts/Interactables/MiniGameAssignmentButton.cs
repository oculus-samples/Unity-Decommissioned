// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Linq;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Decommissioned.Player;
using Meta.Decommissioned.UI;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Meta.XR.Samples;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Interactables
{
    /// <summary>
    /// Represents a button used to select a station to assign a player to. When a player is assigned, this component
    /// will change the visual state of one or multiple lights indicating which specific players have been assigned to its
    /// associated <see cref="MiniGame"/>.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class MiniGameAssignmentButton : MonoBehaviour
    {
        [Header("Rendering")]
        [SerializeField] private MeshRenderer m_buttonRenderer;
        [SerializeField] private Material m_highlightMaterial;
        [SerializeField] private Material m_highlightMaterialBeginningPhase;
        private MaterialPropertyBlock m_buttonLightProperties;
        private MaterialPropertyBlock m_buttonSelectedProperties;
        private readonly int m_lightColorKeyword = Shader.PropertyToID("_BaseColor");

        [field: Header("Components")]
        [field: SerializeField] public PositionLight[] PositionLights { get; private set; }

        [SerializeField, AutoSetFromParent] private MainRoomHealthGauge m_healthGauge;
        [ShowNativeProperty] public MiniGame AssignedMiniGame => m_healthGauge.m_miniGame;
        [SerializeField] private UnityEvent<MiniGameAssignmentButton> m_onButtonInitialized;
        [SerializeField] private UnityEvent<MiniGameAssignmentButton> m_onButtonPushed;
        private bool m_isPushable;

        public bool IsPushable
        {
            get => m_isPushable;
            set
            {
                if (value != IsPushable) { if (value) { SetIsSelected(false); } }
                m_isPushable = value;
            }
        }

        public void OnButtonPushed() => m_onButtonPushed.Invoke(this);

        public void Start()
        {
            ResetButtonLights();
            m_onButtonInitialized.Invoke(this);
        }

        public void SetIsSelected(bool isSelected)
        {
            m_buttonSelectedProperties ??= new();
            m_buttonSelectedProperties.SetColor("_CenterColor", new(1, 1, 1, isSelected ? 0.25f : 0));
            m_buttonRenderer.SetPropertyBlock(m_buttonSelectedProperties);
        }

        /// <summary>
        /// Sets the lights assigned to this button to the color of the player being assigned to a station.
        /// </summary>
        /// <param name="playerId">ID of the player being assigned.</param>
        public void AssignPlayer(PlayerId playerId)
        {
            foreach (var positionLight in PositionLights)
            {
                if (positionLight.Player != default) { continue; }

                positionLight.Player = playerId;
                var emissionColor = PlayerColor.GetByPlayerId(playerId).MultiplyColorFromGameColor(2);
                SetLightColor(positionLight, emissionColor);
                RemoveHighlightMaterial();
                break;
            }
        }

        /// <summary>
        /// Reset the color of all position lights.
        /// </summary>
        public void ClearAssignments()
        {
            foreach (var positionLight in PositionLights)
            {
                positionLight.Player = default;
                ResetButtonLight(positionLight);
            }
        }

        /// <summary>
        /// Reset the color of all position lights associated with a specific player.
        /// </summary>
        /// <param name="playerId">The ID of the player who's assignments are being cleared.</param>
        public void ClearAssignments(PlayerId playerId)
        {
            foreach (var positionLight in PositionLights)
            {
                if (positionLight.Player != playerId) { continue; }
                positionLight.Player = default;
                ResetButtonLight(positionLight);
            }
        }

        private void ResetButtonLights() { foreach (var positionLight in PositionLights) { ResetButtonLight(positionLight); } }

        private void ResetButtonLight(PositionLight positionLight) => SetLightColor(positionLight, Color.grey);


        /// <summary>
        /// Adds a material that causes this button to "flash".
        /// </summary>
        public void SetHighlightMaterial() => m_buttonRenderer.sharedMaterials =
            m_buttonRenderer.sharedMaterials.Append(m_highlightMaterial).ToArray();


        /// <summary>
        /// Adds a material that causes this button to "flash" at the start of a phase.
        /// </summary>
        public void SetHighlightPhaseMaterial() => m_buttonRenderer.sharedMaterials =
            m_buttonRenderer.sharedMaterials.Append(m_highlightMaterialBeginningPhase).ToArray();

        public void RemoveHighlightMaterial() =>
            m_buttonRenderer.sharedMaterials = m_buttonRenderer.sharedMaterials.Except(m_highlightMaterial).ToArray();

        public void RemoveHighlightPhaseMaterial() => m_buttonRenderer.sharedMaterials =
                m_buttonRenderer.sharedMaterials.Except(m_highlightMaterialBeginningPhase).ToArray();

        private void SetLightColor(PositionLight light, Color color)
        {
            m_buttonLightProperties ??= new();
            m_buttonLightProperties.SetColor(m_lightColorKeyword, color);
            light.LightRenderer?.SetPropertyBlock(m_buttonLightProperties);
        }

        [System.Serializable]
        public class PositionLight
        {
            public MeshRenderer LightRenderer;
            public PlayerId Player;
        }
    }
}
