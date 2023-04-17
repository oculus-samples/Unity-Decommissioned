// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using TMPro;
using UnityEngine;

namespace Meta.Decommissioned.UI
{
    public class StationHealthChangeDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_healthChangeReadOutText;
        [SerializeField] private SpriteRenderer m_healthChangeIcon;
        [SerializeField] private Color m_healthChangePositiveColor;
        [SerializeField] private Color m_healthChangeNeutralColor;
        [SerializeField] private Color m_healthChangeNegativeColor;

        [SerializeField] private Sprite m_healthChangePositiveIcon;
        [SerializeField] private Sprite m_healthChangeNegativeIcon;
        private Color m_defaultColor;

        private void Awake() => m_defaultColor = m_healthChangeReadOutText.color;

        public void ResetDisplay()
        {
            UpdateDisplay(0, false);
            m_healthChangeReadOutText.text = "";
        }

        /**
         * Update the percent change shown on the display.
         * <param name="healthChange">The change in health since the beginning of the round.</param>
         * <param name="healthIncreased">Indicates whether or not this change was an increase or decrease in value.</param>
         */
        public void UpdateDisplay(float healthChange, bool healthIncreased)
        {
            m_healthChangeReadOutText.text = $"{healthChange}%";
            if (m_healthChangeIcon) { UpdateIcon(healthIncreased, healthChange > 0f); }
        }

        private void UpdateIcon(bool isIncrease, bool healthChanged = true)
        {
            m_healthChangeIcon.gameObject.SetActive(healthChanged);
            if (!healthChanged)
            {
                m_healthChangeReadOutText.color = m_defaultColor;
                m_healthChangeIcon.color = m_healthChangeNeutralColor;
                return;
            }
            m_healthChangeIcon.sprite = isIncrease ? m_healthChangePositiveIcon : m_healthChangeNegativeIcon;
            m_healthChangeIcon.color = m_healthChangeReadOutText.color = isIncrease ? m_healthChangePositiveColor : m_healthChangeNegativeColor;
        }
    }
}
