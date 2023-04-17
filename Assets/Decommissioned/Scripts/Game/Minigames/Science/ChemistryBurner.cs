// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Game.MiniGames
{
    /**
     * This class represents an object that, when activated, increases and displays the current
     * temperature of the beaker.
     */
    public class ChemistryBurner : MonoBehaviour
    {
        [Tooltip("Text component showing the beaker's current temperature.")]
        [SerializeField] private TMP_Text m_beakerTemperatureReadout;
        [Tooltip("Particle Systems representing the visual flame effect shown when the beaker is on.")]
        [SerializeField] private ParticleSystem[] m_flameEffects;
        [Tooltip("The higher this value is, the faster the heat will change with each tick.")]
        [SerializeField] private int m_heatChangeRate = 1;
        [Tooltip("The amount of time (in seconds) before the heat changes.")]
        [SerializeField] private float m_heatChangeTimeInterval = 0.2f;
        private Coroutine m_heatCoroutine;
        public bool IsActivated { get; private set; }
        [SerializeField] private UnityEvent m_onBurnerActivated;
        [SerializeField] private UnityEvent m_onBurnerDeactivated;
        [SerializeField] private UnityEvent<int> m_onBurnerHeatTick;

        /** Activate or deactivate the burner.
         * <param name="willTurnBurnerOn">Boolean indicating whether we're turning the burner on or off.</param>
         */
        public void ToggleBurner(bool willTurnBurnerOn)
        {
            IsActivated = willTurnBurnerOn;
            foreach (var effect in m_flameEffects)
            {
                if (willTurnBurnerOn)
                {
                    effect.Play(true);
                }
                else
                {
                    effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
            if (willTurnBurnerOn)
            {
                m_heatCoroutine = StartCoroutine(HeatBeaker());
                m_onBurnerActivated.Invoke();
                return;
            }
            m_onBurnerDeactivated.Invoke();
            if (m_heatCoroutine != null) { StopCoroutine(m_heatCoroutine); }
        }

        /** Update the readout text for the current temperature of the beaker.
         * <param name="temperature">The value that will be shown on the label.</param>
         */
        public void UpdateTemperatureReadout(int temperature) => m_beakerTemperatureReadout.text = $"{temperature}°C";

        private IEnumerator HeatBeaker()
        {
            while (IsActivated)
            {
                yield return new WaitForSeconds(m_heatChangeTimeInterval);
                m_onBurnerHeatTick.Invoke(m_heatChangeRate);
            }
        }

        /** Activate or deactivate the burner.*/
        [ContextMenu("Toggle Burner")]
        public void ToggleBurnerDebug()
        {
            IsActivated = !IsActivated;
            foreach (var effect in m_flameEffects)
            {
                if (IsActivated)
                {
                    effect.Play(true);
                }
                else
                {
                    effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
            if (IsActivated)
            {
                m_heatCoroutine = StartCoroutine(HeatBeaker());
                m_onBurnerActivated.Invoke();
                return;
            }
            m_onBurnerDeactivated.Invoke();
            if (m_heatCoroutine != null) { StopCoroutine(m_heatCoroutine); }
        }
    }
}
