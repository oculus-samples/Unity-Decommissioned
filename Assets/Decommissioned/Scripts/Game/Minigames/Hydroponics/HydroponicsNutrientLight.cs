// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Lobby;
using Meta.Utilities;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    /**
     * Object representing a light indicating what nutrient the assigned plant needs.
     */
    public class HydroponicsNutrientLight : MonoBehaviour
    {
        [SerializeField, Required] private MeshRenderer m_lightMeshRenderer;
        [SerializeField, Required] private GamePosition m_gamePosition;
        [SerializeField] private EnumDictionary<Nutrient, Color> m_nutrientLightColors;
        private NetworkObject m_assignedPlayer;

        private void OnEnable()
        {
            if (m_gamePosition == null) { return; }
            m_gamePosition.OnOccupyingPlayerChanged += ShowLightToAssignedPlayer;
        }

        /**
         * Change the color of the light based on the given nutrient.
         * <param name="nutrient">The nutrient that determines the chosen color.</param>
         */
        public void ChangeLightColor(Nutrient nutrient) => m_lightMeshRenderer.material.SetColor("_EmissionColor", m_nutrientLightColors[nutrient]);

        private void ShowLightToAssignedPlayer(NetworkObject oldObject, NetworkObject networkObject)
        {
            if (!m_gamePosition.IsOccupied)
            {
                m_lightMeshRenderer.material.SetColor("_EmissionColor", Color.black);
                return;
            }

            m_assignedPlayer = m_gamePosition.OccupyingPlayer;
            var color = m_assignedPlayer.IsLocalPlayer ? Color.white : Color.black;
            m_lightMeshRenderer.material.SetColor("_EmissionColor", color);
        }
    }
}
