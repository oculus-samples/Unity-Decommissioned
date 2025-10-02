// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Linq;
using Meta.Decommissioned.Game;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Utils
{
    /**
     * Class for managing a game object that shows or hide based on specified criteria.
     */
    [MetaCodeSample("Decommissioned")]
    public class ContextualObject : NetworkBehaviour
    {
        [SerializeField] private bool m_onlyShowForHost;
        [SerializeField] private bool m_hideAfterGameStart;
        [SerializeField] private bool m_showOnGameEnd;
        [SerializeField] private bool m_showOnTrackedPhases;
        [SerializeField] private Phase[] m_trackedPhases;

        private void Awake()
        {
            GameManager.OnGameStateChanged += OnGameStateChanged;
            if (GameManager.Instance != null) { GamePhaseManager.Instance.OnPhaseChanged += OnGamePhaseChanged; }
        }

        private void Start()
        {
            if (m_onlyShowForHost) { gameObject.SetActive(IsHost); }
        }

        private new void OnDestroy()
        {
            if (GameManager.Instance) { GameManager.OnGameStateChanged -= OnGameStateChanged; }
            if (GamePhaseManager.Instance) { GamePhaseManager.Instance.OnPhaseChanged -= OnGamePhaseChanged; }
            base.OnDestroy();
        }

        private void OnGamePhaseChanged(Phase newPhase)
        {
            if (!m_showOnTrackedPhases) { return; }

            if (m_trackedPhases.Contains(newPhase))
            {
                gameObject?.SetActive(true);
                return;
            }

            gameObject?.SetActive(false);
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (m_onlyShowForHost && !IsHost)
            {
                gameObject?.SetActive(false);
                return;
            }

            switch (newState)
            {
                case GameState.ReadyUp:
                    if (m_hideAfterGameStart) { gameObject?.SetActive(true); }
                    break;
                case GameState.Gameplay:
                    if (m_hideAfterGameStart) { gameObject?.SetActive(false); }
                    break;
                case GameState.GameEnd when m_showOnGameEnd:
                    gameObject?.SetActive(true);
                    break;
                default:
                    break;
            }
        }
    }
}
