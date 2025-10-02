// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Collections.Generic;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Meta.XR.Samples;
using ScriptableObjectArchitecture;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Game.MiniGames
{
    /** The MiniGame key for the Chemistry MiniGame; this game object determines the current
     * combination of chemicals inputted by the player and whether or not it is correct.
     * <seealso cref="ChemistryMiniGame"/>
     * <seealso cref="BeakerFillVisual"/>
     */
    [MetaCodeSample("Decommissioned")]
    public class ChemistryBeaker : MonoBehaviour
    {
        [Tooltip("Reference to the recipe used to check our solution.")]
        [SerializeField] private ChemistryRecipe m_recipe;

        [SerializeField, AutoSet] private NetworkObject m_networkObject;

        [Tooltip("Object for managing the appearance of the liquid within the beaker.")]
        [SerializeField, AutoSetFromChildren] private BeakerFillVisual m_fillVisual;
        [SerializeField] private ColorVariable m_noMixtureColor;
        [SerializeField] private ColorVariable m_chemicalAColor;
        [SerializeField] private ColorVariable m_chemicalBColor;
        [SerializeField] private ColorVariable m_chemicalCColor;

        [SerializeField] private IntVariable m_maxTotalChemicalsPossible;
        [SerializeField] private IntVariable m_maxMixCountPossible;

        [SerializeField] private IntVariable m_maxTemperature;
        [SerializeField] private IntVariable m_minTemperature;
        [SerializeField] private AudioSource m_boilingAudioSource;
        [SerializeField] private float m_chemicalDispenseRate = 1f;
        [SerializeField] private float m_chemicalDispenseTimeInterval = 0.5f;

        [SerializeField] private UnityEvent<ChemistryBeaker> m_onChemicalAdded;
        [SerializeField] private UnityEvent<int> m_onTemperatureChanged;
        [SerializeField] private UnityEvent<int> m_onBeakerMixed;

        private int m_currentTemperature = 10;
        private float m_chemicalACount;
        private float m_chemicalBCount;
        private float m_chemicalCCount;
        private int m_mixCount;

        private Vector3 m_initialPosition;
        private Quaternion m_initialRotation;

        private Coroutine m_dispenseChemicalRoutine;

        public bool CanReceiveInput { get; set; }
        public bool IsOnBurner { get; set; }
        public bool IsEmpty { get; private set; }
        public float AverageAccuracy { get; private set; }
        public float CurrentTemperature => m_currentTemperature;
        public Color MixtureColor { get; private set; }

        private void Awake()
        {
            IsEmpty = true;
            m_initialPosition = transform.position;
            m_initialRotation = transform.rotation;
        }

        /** Clear the beaker of all colors and values. */
        public void ClearBeakerValues()
        {
            m_chemicalACount = m_chemicalBCount = m_chemicalCCount = 0;
            m_fillVisual.ResetFillNodes(m_noMixtureColor);
            m_currentTemperature = m_minTemperature;
            IsEmpty = true;
            m_mixCount = 0;
            AverageAccuracy = 0f;
            m_onTemperatureChanged.Invoke(m_currentTemperature);
        }

        /**
         * Start dispensing the selected chemical into the beaker.
         * <param name="chemicalIndex">The index of the chemical to be dispensed into the beaker.</param>
         */
        public void StartDispensingChemical(int chemicalIndex)
        {
            if (!CanReceiveInput) { return; }
            StopDispensingChemical();
            IsEmpty = false;
            m_dispenseChemicalRoutine = StartCoroutine(DispenseChemical(chemicalIndex));
        }

        /**
         * Stop dispensing chemicals into the beaker.
         */
        public void StopDispensingChemical() { if (m_dispenseChemicalRoutine != null) { StopCoroutine(m_dispenseChemicalRoutine); } }
        private IEnumerator DispenseChemical(int chemicalIndex)
        {
            while (CanReceiveInput)
            {
                yield return new WaitForSeconds(m_chemicalDispenseTimeInterval);
                AddChemical(chemicalIndex);
            }
        }

        private void Respawn()
        {
            transform.position = m_initialPosition;
            transform.rotation = m_initialRotation;
        }

        /** Add a chemical of a specific color to the beaker.
         * <param name="chemicalIndex"> The numerical value corresponding to the chemical being added.</param>
         */
        public void AddChemical(int chemicalIndex)
        {
            if (m_chemicalACount + m_chemicalCCount + m_chemicalBCount >= m_maxTotalChemicalsPossible) { return; }
            m_mixCount = 0;

            switch (chemicalIndex)
            {
                case 1:
                    m_chemicalACount += m_chemicalDispenseRate;
                    MixtureColor = m_chemicalAColor;
                    break;
                case 2:
                    m_chemicalBCount += m_chemicalDispenseRate;
                    MixtureColor = m_chemicalBColor;
                    break;
                case 3:
                    m_chemicalCCount += m_chemicalDispenseRate;
                    MixtureColor = m_chemicalCColor;
                    break;
                default:
                    break;
            }

            m_fillVisual.UpdateFillVisual(MixtureColor, m_chemicalACount + m_chemicalBCount + m_chemicalCCount);
            m_onChemicalAdded.Invoke(this);
            AverageAccuracy = CheckSolutionAccuracy();
        }

        /** Increase the current temperature of the beaker.
         * <param name="temperatureChange">The amount the temperature is being changed by.</param>
         */
        public void ChangeTemperature(int temperatureChange)
        {
            if (!IsOnBurner || IsEmpty) { return; }
            m_currentTemperature = Mathf.Clamp(m_currentTemperature + temperatureChange, m_minTemperature, m_maxTemperature);
            m_onTemperatureChanged.Invoke(m_currentTemperature);
            AverageAccuracy = CheckSolutionAccuracy();
        }

        /**
         * Set the beaker's "mixed" state.
         */
        public void MixBeakerChemicals()
        {
            if (IsEmpty || m_mixCount >= m_maxMixCountPossible)
            {
                if (!IsEmpty) { m_onBeakerMixed.Invoke(m_mixCount); }
                AverageAccuracy = CheckSolutionAccuracy();
                return;
            }

            m_mixCount = Mathf.Clamp(m_mixCount + 1, 0, m_recipe.MinimumMixCountNeeded + 1);
            if (m_mixCount == 1)
            {
                var colorValues = new Dictionary<Color, float>()
            {
                {m_chemicalAColor, m_chemicalACount},
                {m_chemicalBColor, m_chemicalBCount},
                {m_chemicalCColor, m_chemicalCCount}
            };
                m_fillVisual.SetMixedFillVisualColor(colorValues);
            }
            MixtureColor = m_fillVisual.CurrentFillColor;
            AverageAccuracy = CheckSolutionAccuracy();
            m_onBeakerMixed.Invoke(m_mixCount);
        }

        /**
         * Toggle the "boiling water" audio on the beaker on or off.
         */
        public void ToggleBoilingSound(bool boilingAudioOn)
        {
            if (IsEmpty || !IsOnBurner || !boilingAudioOn)
            {
                m_boilingAudioSource.Stop();
                return;
            }

            if (!m_boilingAudioSource.isPlaying) { m_boilingAudioSource.Play(); }
        }

        /** Reset the beaker's values and generate a new solution.*/
        public void OnMiniGameReset()
        {
            m_currentTemperature = m_minTemperature;
            m_onTemperatureChanged.Invoke(m_currentTemperature);
            ClearBeakerValues();
            Respawn();
        }

        public void ChangeBeakerOwner(PlayerId newPlayerId)
        {
            m_networkObject.ChangeOwnership(newPlayerId);
        }

        private float CheckSolutionAccuracy()
        {
            var chemicalAAccuracy = GetAccuracy(m_recipe.ChemicalANeeded, m_chemicalACount);
            var chemicalBAccuracy = GetAccuracy(m_recipe.ChemicalBNeeded, m_chemicalBCount);
            var chemicalCAccuracy = GetAccuracy(m_recipe.ChemicalCNeeded, m_chemicalCCount);
            _ = Mathf.Abs((m_chemicalACount + m_chemicalBCount + m_chemicalCCount) / (m_recipe.ChemicalANeeded + m_recipe.ChemicalBNeeded + m_recipe.ChemicalCNeeded) - 1);

            var averageChemicalAccuracy = (chemicalAAccuracy + chemicalBAccuracy + chemicalCAccuracy) / 3;
            var chemAccuracy = GetAccuracy(1, averageChemicalAccuracy);
            var tempAccuracy = GetAccuracy(m_recipe.TemperatureRequired, m_currentTemperature);
            var mixAccuracy = GetAccuracy(m_recipe.MinimumMixCountNeeded, m_mixCount);
            _ = Mathf.Abs(m_currentTemperature / m_recipe.TemperatureRequired - 1);

            var averageAccuracy = (chemAccuracy + tempAccuracy + mixAccuracy) / 3;

            return GetAccuracy(1, averageAccuracy);
        }

        private float GetAccuracy(float acceptedValue, float measuredValue)
        {
            if (acceptedValue == 0)
            {
                return measuredValue == 0 ? 1f : 0;
            }

            var accuracy = (acceptedValue - (acceptedValue - measuredValue)) / acceptedValue;

            // Treating percentages over 100% as inaccuracy: in this case, the difference is the true accuracy.  
            if (accuracy > 1)
            {
                var overshotAmount = accuracy - 1;
                accuracy = 1 - overshotAmount;
            }

            return accuracy;
        }
    }
}
