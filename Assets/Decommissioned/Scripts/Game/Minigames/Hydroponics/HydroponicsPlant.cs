// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using Random = System.Random;

namespace Meta.Decommissioned.Game.MiniGames
{
    /**
     * Class for managing and tracking the state of a plant in the hydroponics station.
     */
    [MetaCodeSample("Decommissioned")]
    public class HydroponicsPlant : NetworkBehaviour
    {
        /// <summary>
        /// The delay that the plant will wait before triggering the watered event again after it has been watered.
        /// </summary>
        [Tooltip("The delay (seconds) that the plant will wait before triggering the watered event again after it has been watered.")]
        [SerializeField] private float m_wateringDelay = 1f;
        private NetworkVariable<Nutrient> m_requiredNutrient = new(Nutrient.None);

        [Tooltip("The minimum delay (seconds) that the plant will wait before changing its required nutrient.")]
        [SerializeField] private float m_minNutrientChangeDelay = 2f;
        [Tooltip("The maximum delay (seconds) that the plant will wait before changing its required nutrient.")]
        [SerializeField] private float m_maxNutrientChangeDelay = 5f;
        [SerializeField] private AudioSource m_wateringAudioSource;

        /// <summary>
        /// Invoked when this plant is watered by a particle from the watering hose.
        /// </summary>
        [Tooltip("Invoked when this plant is watered by a particle from the watering hose.")]
        [SerializeField] private UnityEvent m_onPlantWatered;
        [Tooltip("Invoked when this plant receives a nutrient from the hose. True when the nutrient received matches the one it requires.")]
        [SerializeField] private UnityEvent<bool> m_onNutrientReceived;
        [SerializeField] private UnityEvent<Nutrient> m_onRequiredNutrientChanged;

        private Nutrient[] m_validNutrientTypes = { Nutrient.Red, Nutrient.Blue, Nutrient.Yellow };
        private bool m_canBeWatered = true;
        private Coroutine m_nutrientCoroutine;

        private void OnEnable() => m_requiredNutrient.OnValueChanged += OnRequiredNutrientChanged;

        private void OnRequiredNutrientChanged(Nutrient previousNutrient, Nutrient newNutrient) => m_onRequiredNutrientChanged.Invoke(newNutrient);

        /**
         * Activate or deactivate the behavior that updates the nutrient this plant requires
         * at randomized intervals.
         * <param name="willStartRoutine"> Indicate whether to stop or start the behavior. </param>
         */
        public void TogglePeriodicNutrientChange(bool willStartRoutine)
        {
            if (!IsServer) { return; }
            if (m_nutrientCoroutine != null || !willStartRoutine) { StopCoroutine(m_nutrientCoroutine); }
            if (!willStartRoutine) { return; }
            SetRandomNutrient();
            m_nutrientCoroutine = StartCoroutine(ChangeNutrientAfterDelay());
        }

        /// <summary>
        /// Informs this plant that it has been touched by water, invoking the necessary events.
        /// </summary>
        public void WaterPlant(Nutrient nutrient)
        {
            if (!m_canBeWatered) { return; }
            m_canBeWatered = false;
            m_onPlantWatered?.Invoke();
            m_onNutrientReceived.Invoke(nutrient == m_requiredNutrient.Value);

            if (!m_wateringAudioSource.isPlaying)
            {
                m_wateringAudioSource.loop = true;
                m_wateringAudioSource.Play();
            }

            _ = StartCoroutine(WaitForEndOfWater());
        }

        private IEnumerator WaitForEndOfWater()
        {
            yield return new WaitForSeconds(m_wateringDelay);
            m_wateringAudioSource.loop = false;
            m_canBeWatered = true;
        }
        private IEnumerator ChangeNutrientAfterDelay()
        {
            while (true)
            {
                var nutrientChangeDelay = UnityEngine.Random.Range(m_minNutrientChangeDelay, m_maxNutrientChangeDelay);
                yield return new WaitForSeconds(nutrientChangeDelay);
                SetRandomNutrient();
            }
        }

        private void SetRandomNutrient() => m_requiredNutrient.Value = (Nutrient)m_validNutrientTypes.GetValue(new Random().Next(m_validNutrientTypes.Length));
    }
}
