// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using TMPro;
using UnityEngine;

namespace Meta.Decommissioned.UI
{
    /**
     * Class representing a progress bar indicating the amount of rounds ("days")
     * passed since the start of the game.
     */
    public class GameRoundCounter : MonoBehaviour
    {
        [SerializeField] private Transform m_currentRoundSlider;
        [SerializeField] private TMP_Text m_sliderLabel;
        private Vector3 m_currentRoundSliderScale = Vector3.one;

        private void Start()
        {
            GameManager.OnGameStateChanged += OnGameStateChanged;
            GameManager.Instance.OnRoundChanged += OnRoundChanged;
            m_currentRoundSliderScale.x = GetSliderScale();
            m_currentRoundSlider.localScale = m_currentRoundSliderScale;
        }

        private void OnRoundChanged(int round) => UpdateRoundCounterVisuals(round);

        public void OnDestroy()
        {
            GameManager.OnGameStateChanged -= OnGameStateChanged;
            GameManager.Instance.OnRoundChanged -= OnRoundChanged;
        }

        private void UpdateRoundCounterVisuals(int currentRound)
        {
            currentRound = currentRound >= GameManager.Instance.MaxRounds
                ? GameManager.Instance.MaxRounds
                : currentRound;

            m_currentRoundSliderScale.x = GetSliderScale();
            UpdateRoundCountSlider();

            if (GameManager.Instance.CurrentRoundCount >= GameManager.Instance.MaxRounds)
            {
                m_sliderLabel.text = "FINAL DAY";
                return;
            }

            m_sliderLabel.text = currentRound == 0 ? "START" : $"DAY {currentRound}";
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.GameEnd) { return; }
            ResetCounter();
        }

        private void UpdateRoundCountSlider() => m_currentRoundSlider.localScale = m_currentRoundSliderScale;

        private float GetSliderScale() => (float)GameManager.Instance.CurrentRoundCount / GameManager.Instance.MaxRounds;

        private void ResetCounter()
        {
            m_currentRoundSliderScale.x = (float)1 / GameManager.Instance.MaxRounds;
            m_sliderLabel.text = "DAY 1";
            UpdateRoundCountSlider();
        }
    }
}
