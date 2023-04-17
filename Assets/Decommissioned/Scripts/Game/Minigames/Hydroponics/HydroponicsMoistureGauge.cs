// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    public class HydroponicsMoistureGauge : MonoBehaviour
    {
        [Tooltip("A reference to the display needle of this gauge.")]
        [SerializeField] private Transform m_displayNeedle;
        [Tooltip("A reference to the moisture text display of this gauge.")]
        [SerializeField] private TextMeshPro m_moistureDisplay;

        private float m_currentMoisture = -1;
        [Tooltip("The minimum position of this needle when at 0%")]
        [SerializeField] private float m_minNeedlePosition = 10.095f;
        [Tooltip("The maximum position of this needle when at 100%")]
        [SerializeField] private float m_maxNeedlePosition = 10.274f;
        private Vector3 m_currentNeedlePosition;
        private Vector3 m_targetNeedlePosition;

        private void Start()
        {
            m_currentNeedlePosition = m_displayNeedle.localPosition;
            m_targetNeedlePosition = m_currentNeedlePosition;
        }

        /// <summary>
        /// Notifies the gauge that the moisture levels have changed.
        /// </summary>
        /// <param name="newMoisture">The new moisture value.</param>
        public void OnMoistureChanged(float newMoisture)
        {
            m_currentMoisture = newMoisture;
            var percentage = (m_currentMoisture * 100).ToString("0");
            m_moistureDisplay.text = $"{percentage}%";
            m_targetNeedlePosition.x = GetPositionForMoisture();

        }

        private void Update()
        {
            m_currentNeedlePosition.x = Mathf.MoveTowards(m_currentNeedlePosition.x, m_targetNeedlePosition.x, 0.0001f);
            m_displayNeedle.localPosition = m_currentNeedlePosition;
        }

        private float GetPositionForMoisture()
        {
            var position = math.remap(0f, 1f, m_minNeedlePosition, m_maxNeedlePosition, m_currentMoisture);
            return position;
        }
    }
}
