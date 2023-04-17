// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using Meta.Utilities;
using ScriptableObjectArchitecture;
using TMPro;
using UnityEngine;

namespace Meta.Decommissioned.UI
{
    /**
     * Class for managing the appearance and behavior of a piece of floating text spawned in the scene by
     * a FloatingTextSpawner.
     * <seealso cref="FloatingHealthTextSpawner"/>
     */
    public class FloatingHealthChangeText : MonoBehaviour
    {
        [SerializeField, AutoSetFromChildren] private TMP_Text m_healthChangeText;
        [SerializeField] private ColorVariable m_damageTextColor;
        [SerializeField] private ColorVariable m_healTextColor;

        [SerializeField] private float m_textMovementSpeed = 1f;

        [SerializeField] private float m_numberTextLifetime = 1.5f;
        [SerializeField] private float m_infoTextLifetime = 3.5f;
        private float m_textLifetime = 1.5f;

        [SerializeField] private float m_numberFontSize = 1f;
        [SerializeField] private float m_wordFontSize = 0.5f;

        [SerializeField] private Vector3 m_textOffset = new(0, 0f, 0);
        [SerializeField] private Vector3 m_targetOffset = new(0, 0.3f, 0);

        private Vector3 m_initialPosition;
        private Vector3 m_targetPosition;
        private float m_timer;

        /**
         * Sets this floating text's displayed value. Changed its color based on whether it is an increase
         * or decrease.
         * <param name="healthChange">The value that will be displayed by this object.</param>
         */
        public void SetDamageText(int healthChange)
        {
            var healthDecreased = healthChange < 0;
            m_textLifetime = m_numberTextLifetime;
            m_healthChangeText.fontSize = m_numberFontSize;
            m_healthChangeText.color = healthDecreased ? m_damageTextColor : m_healTextColor;
            m_healthChangeText.text = healthDecreased
                ? $"-{Math.Abs(healthChange)}"
                : $"+{Math.Abs(healthChange)}";

        }

        /**
         * Sets the text displayed by the spawned floating text object.
         * <param name="text">The text that will be displayed by this object.</param>
         */
        public void SetInfoText(string text)
        {
            m_textLifetime = m_infoTextLifetime;
            m_healthChangeText.fontSize = m_wordFontSize;
            m_healthChangeText.text = text;
        }

        private void Start()
        {
            m_initialPosition = transform.position;
            m_targetPosition = m_initialPosition + m_targetOffset;
            transform.localPosition += m_textOffset;
            Destroy(gameObject, m_textLifetime);
        }

        private void Update()
        {
            m_timer += Time.deltaTime;

            var fadeTime = m_textLifetime / 2f;
            var fadeSpeedFactor = (m_timer - fadeTime) / (m_textLifetime - fadeTime);

            if (m_timer > fadeTime) { m_healthChangeText.color = Color.Lerp(m_healthChangeText.color, Color.clear, fadeSpeedFactor); }
            transform.position = Vector3.MoveTowards(transform.position, m_targetPosition, m_textMovementSpeed * Time.deltaTime);
        }
    }
}
