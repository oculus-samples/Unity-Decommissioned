// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections.Generic;
using System.Linq;
using Meta.Decommissioned.Lobby;
using Meta.Multiplayer.Networking;
using Meta.Utilities;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    /** A simple script controlling the behavior of the visuals
     * inside the beaker.
     * <seealso cref="ChemistryBeaker"/>
     */
    [MetaCodeSample("Decommissioned")]
    public class BeakerFillVisual : NetworkBehaviour
    {
        [Tooltip("The renderer of the fill visual; we use this to change liquid color.")]
        [SerializeField, AutoSetFromChildren] private Renderer m_fillRenderer;
        [Tooltip("The initial scale of the fill visual before player input.")]
        [SerializeField] private Vector3 m_defaultFillMeshScale = new(1, 0, 1);

        [Tooltip("The higher this value is, the faster the color of the fill visual will change as the beaker fills.")]
        [SerializeField] private float m_colorChangeSpeed = 1f;
        private Vector3 m_currentFillMeshScale;
        public Color CurrentFillColor => m_fillRenderer.material.GetColor("_BaseColor");
        private NetworkVariable<Color> m_currentColor = new(Color.clear, writePerm: NetworkVariableWritePermission.Owner);
        private GamePosition m_miniGamePosition;

        private void Awake()
        {
            if (!m_fillRenderer)
            {
                Debug.LogWarning("BeakerFillVisual does not have a valid renderer component!");
                return;
            }

            ToggleFillVisual(false);
            SetFillVisualColor(m_fillRenderer.material.color);
            m_currentFillMeshScale = gameObject.transform.localScale;
            m_currentColor.OnValueChanged += OnCurrentColorChanged;
            m_miniGamePosition = LocationManager.Instance.GetGamePositionByType(MiniGameRoom.Science).FirstOrDefault();
            if (m_miniGamePosition == null) { return; }
            m_miniGamePosition.OnOccupyingPlayerChanged += OnAssignedPlayerChanged;
        }

        private void OnAssignedPlayerChanged(NetworkObject previousPlayer, NetworkObject currentPlayer)
        {
            if (IsServer && currentPlayer != null)
            {
                var newPlayerId = currentPlayer.GetOwnerPlayerId();
                if (newPlayerId.HasValue)
                {
                    NetworkObject.ChangeOwnership(newPlayerId.Value);
                }
            }
        }

        private void OnDisable()
        {
            m_currentColor.OnValueChanged -= OnCurrentColorChanged;
            m_miniGamePosition.OnOccupyingPlayerChanged -= OnAssignedPlayerChanged;
        }

        private void OnCurrentColorChanged(Color previousColor, Color newColor) => m_fillRenderer.material.SetColor("_BaseColor", newColor);

        /**
         * Enable, color, and animate the beaker filling visual.
         * <param name="newColor">The color that the mesh material will change to on update.</param>
         * <param name="fillAmount">The value the fill visual will scale to on the Y axis.</param>
         */
        public void UpdateFillVisual(Color newColor, float fillAmount = 1)
        {
            ToggleFillVisual(true);
            SetFillVisualColor(Color.Lerp(CurrentFillColor, newColor, Time.deltaTime * m_colorChangeSpeed));
            m_currentFillMeshScale = new Vector3(1, fillAmount, 1);
            gameObject.transform.localScale = m_currentFillMeshScale;
        }
        /**
         * Given a set of colors and values, change the fill visual's color to the average color of the set.
         */
        public void SetMixedFillVisualColor(Dictionary<Color, float> colorValues)
        {
            var colorsToMix = new List<Color>();
            colorsToMix.InsertRange(colorsToMix.Count, Enumerable.Repeat(colorValues.ElementAt(0).Key, colorValues.ElementAt(0).Value > 0 ? 1 : 0));
            colorsToMix.InsertRange(colorsToMix.Count, Enumerable.Repeat(colorValues.ElementAt(1).Key, colorValues.ElementAt(1).Value > 0 ? 1 : 0));
            colorsToMix.InsertRange(colorsToMix.Count, Enumerable.Repeat(colorValues.ElementAt(2).Key, colorValues.ElementAt(2).Value > 0 ? 1 : 0));

            var result = new Color(0, 0, 0, 0);
            foreach (var color in colorsToMix) { result += color; }
            SetFillVisualColor(result / colorsToMix.Count);
        }
        /** Disable all renders and set them to a new color; resets the current renderer index.
         * <param name="resetColor">The color all renderers will be reset to.</param>
         */
        public void ResetFillNodes(Color resetColor)
        {
            SetFillVisualColor(resetColor);
            ToggleFillVisual(false);
            gameObject.transform.localScale = m_defaultFillMeshScale;
        }
        private void ToggleFillVisual(bool showVisual)
        {
            if (!m_fillRenderer) { return; }
            m_fillRenderer.enabled = showVisual;
        }

        /** Change all fill nodes to a specific color.
         * <param name="newColor">The color that will be used on the rendered at the current index.</param>
         */
        private void SetFillVisualColor(Color newColor)
        {
            if (!m_fillRenderer) { return; }
            if (IsOwner) { m_currentColor.Value = newColor; }
        }
    }
}
