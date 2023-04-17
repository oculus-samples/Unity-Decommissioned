// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Lobby;
using Meta.Utilities;
using NaughtyAttributes;
using Oculus.Interaction;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Interactables
{
    /// <summary>
    /// Class for managing the behavior of a poke-able button.
    /// </summary>
    public class PokeButton : MonoBehaviour
    {
        [SerializeField, AutoSetFromChildren] private PokeInteractable m_buttonObject;
        [SerializeField, Required] private MeshRenderer m_buttonMesh;
        [SerializeField] private GamePosition m_assignedGamePosition;
        [SerializeField] private bool m_initializeOnAwake;

        private readonly int m_buttonEmissionProperty = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock m_buttonProperties;
        private bool m_isActive;
        public bool EnableLogging;

        private void Awake()
        {
            if (m_initializeOnAwake) { InitializeButton(); }
            if (!m_buttonMesh) { return; }
            m_buttonProperties = new();
            m_buttonMesh.SetPropertyBlock(m_buttonProperties);
        }

        public void InitializeButton()
        {
            if (m_buttonObject == null)
            {
                if (EnableLogging)
                {
                    Debug.LogWarning($"{this} did not have a PokeInteractable assigned to it! It will not change active states this session.", this);
                }
                return;
            }

            if (m_assignedGamePosition == null)
            {
                if (EnableLogging)
                {
                    Debug.LogWarning($"{this} did not have a GamePosition assigned to it! It will not change active states this session.", this);
                }
                return;
            }

            m_buttonObject.enabled = false;
            m_assignedGamePosition.OnOccupyingPlayerChanged += OnOccupyingPlayerChanged;
        }

        public void ToggleButtonActive()
        {
            m_isActive = !m_isActive;
            SetButtonLight(m_isActive);
        }

        public void SetButtonActive(bool setActive)
        {
            m_isActive = setActive;
            SetButtonLight(m_isActive);
        }

        private void SetButtonLight(bool turnLightOn)
        {
            if (m_buttonMesh == null)
            {
                return;
            }

            var lightColor = turnLightOn ? Color.green : Color.black;
            m_buttonProperties.SetColor(m_buttonEmissionProperty, lightColor);
            m_buttonMesh.SetPropertyBlock(m_buttonProperties);
        }

        private void OnOccupyingPlayerChanged(NetworkObject oldPlayer, NetworkObject newPlayer) =>
            m_buttonObject.enabled = newPlayer != null && newPlayer.IsLocalPlayer;
    }
}
