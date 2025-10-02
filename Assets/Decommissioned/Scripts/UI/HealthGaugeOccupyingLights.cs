// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using Meta.Decommissioned.Lobby;
using Meta.Decommissioned.Player;
using Meta.Multiplayer.Networking;
using Meta.Utilities;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.UI
{
    [MetaCodeSample("Decommissioned")]
    public class HealthGaugeOccupyingLights : MonoBehaviour
    {
        [SerializeField] private MeshRenderer[] m_meshRenderers;
        private GamePosition[] m_relevantPositions;
        [SerializeField, AutoSet] private MainRoomHealthGauge m_healthGauge;
        private readonly Color m_inactiveColor = Color.grey;
        private readonly int m_lightColorKeyword = Shader.PropertyToID("_BaseColor");

        private void Start()
        {
            SetLightColor(0, m_inactiveColor);
            if (m_meshRenderers.Length > 1) { SetLightColor(1, m_inactiveColor); }
        }

        private void OnEnable()
        {
            PopulatePositions();

            if (m_relevantPositions[0])
            {
                m_relevantPositions[0].OnOccupyingPlayerChanged += OnPositionOneOccupantChanged;
            }
            if (m_relevantPositions.Length > 1 && m_relevantPositions[1])
            {
                m_relevantPositions[1].OnOccupyingPlayerChanged += OnPositionTwoOccupantChanged;
            }

            GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (m_relevantPositions[0])
            {
                m_relevantPositions[0].OnOccupyingPlayerChanged -= OnPositionOneOccupantChanged;
            }
            if (m_relevantPositions.Length > 1 && m_relevantPositions[1])
            {
                m_relevantPositions[1].OnOccupyingPlayerChanged -= OnPositionTwoOccupantChanged;
            }

            GamePhaseManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(Phase newPhase)
        {
            if (newPhase == Phase.Night)
            {
                if (!m_relevantPositions[0].IsOccupied) { SetLightColor(0, m_inactiveColor); }
                if (m_relevantPositions.Length > 1 && !m_relevantPositions[1].IsOccupied) { SetLightColor(1, m_inactiveColor); }
            }
        }

        private void PopulatePositions()
        {
            var positionsInMiniGame = LocationManager.Instance.GetGamePositionByType(m_healthGauge.MiniGameRoom);
            m_relevantPositions = positionsInMiniGame;
        }

        private void OnPositionOneOccupantChanged(NetworkObject oldPlayer, NetworkObject newPlayer)
        {
            if (newPlayer == null || !m_relevantPositions[0].IsOccupied)
            {
                SetLightColor(0, m_inactiveColor);
                return;
            }

            var playerId = newPlayer.GetOwnerPlayerId();

            if (playerId != null)
            {
                var playerColor = PlayerColor.GetByPlayerId(playerId.Value);
                var emissionColor = playerColor.MultiplyColorFromGameColor(2f);
                SetLightColor(0, emissionColor);
            }
            else { SetLightColor(0, m_inactiveColor); }
        }

        private void OnPositionTwoOccupantChanged(NetworkObject oldPlayer, NetworkObject newPlayer)
        {
            if (newPlayer == null || !m_relevantPositions[1].IsOccupied)
            {
                SetLightColor(0, m_inactiveColor);
                return;
            }

            var playerId = newPlayer.GetOwnerPlayerId();

            if (playerId != null)
            {
                var playerColor = PlayerColor.GetByPlayerId(playerId.Value);
                var emissionColor = playerColor.MultiplyColorFromGameColor(2f);
                SetLightColor(1, emissionColor);
            }
            else { SetLightColor(1, m_inactiveColor); }
        }

        private void SetLightColor(int lightIndex, Color color)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor(m_lightColorKeyword, color);
            m_meshRenderers[lightIndex].SetPropertyBlock(block);
        }
    }
}
