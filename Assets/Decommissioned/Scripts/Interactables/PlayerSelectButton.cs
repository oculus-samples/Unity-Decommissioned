// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Linq;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Interactables
{
    /// <summary>
    /// Represents a button used to select a player to assign to a specific <see cref="MiniGame"/>, chosen
    /// by first pressing an <see cref="MiniGameAssignmentButton"/>.
    /// </summary>
    public class PlayerSelectButton : MonoBehaviour
    {
        [Header("Rendering")]
        [SerializeField] private MeshRenderer m_buttonRenderer;

        [SerializeField] private Renderer m_borderHighlightMesh;
        public TMPro.TMP_Text TextRenderer;

        [SerializeField] private EnumDictionary<ButtonBorderStates, Texture> m_borderStateTextures;

        [Header("Components")]
        [HideInInspector] public PlayerId PlayerId;

        [SerializeField] private UnityEvent<PlayerSelectButton> m_onButtonPushed;

        private readonly int m_baseMapProperty = Shader.PropertyToID("_BaseMap");
        private readonly int m_buttonColorProperty = Shader.PropertyToID("_BaseColor");
        private readonly int m_buttonEmissionProperty = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock m_buttonProperties;

        private void Awake()
        {
            m_buttonProperties = new();
            m_buttonRenderer.SetPropertyBlock(m_buttonProperties);
        }

        public void SetButtonColor(Color color)
        {
            m_buttonProperties.SetColor(m_buttonColorProperty, color);
            m_buttonProperties.SetColor(m_buttonEmissionProperty, color);
            m_buttonRenderer.SetPropertyBlock(m_buttonProperties);
        }

        /// <summary>
        /// Set the current appearance of the border surrounding the name beside this button.
        /// </summary>
        /// <param name="isActive">If true, set the name box's appearance to active state;
        /// otherwise, set it to its inactive appearance.</param>
        public void SetBorderHighlightState(bool isActive)
        {
            if (m_borderHighlightMesh == null || !m_borderStateTextures.Any()) { return; }
            var newBorderProperties = new MaterialPropertyBlock();
            m_borderHighlightMesh.GetPropertyBlock(newBorderProperties);

            if (!isActive) { newBorderProperties.SetTexture(m_baseMapProperty, m_borderStateTextures[ButtonBorderStates.Inactive]); }
            else { newBorderProperties.SetTexture(m_baseMapProperty, m_borderStateTextures[ButtonBorderStates.Active]); }

            m_borderHighlightMesh.SetPropertyBlock(newBorderProperties);
        }

        public void OnPlayerButtonPushed() => m_onButtonPushed.Invoke(this);

        private enum ButtonBorderStates
        {
            Inactive,
            Active
        }
    }
}
