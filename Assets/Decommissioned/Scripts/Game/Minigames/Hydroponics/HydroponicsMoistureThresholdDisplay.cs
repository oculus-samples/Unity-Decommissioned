// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.XR.Samples;
using ScriptableObjectArchitecture;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Game.MiniGames
{
    /// <summary>
    /// Class representing a display monitor; changes appearance and audio based on input from a
    /// moisture component (<see cref="HydroponicsMoistureObject"/>).
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class HydroponicsMoistureThresholdDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_thresholdDisplayText;
        [SerializeField] private ColorVariable m_plantMoisturizedColor;
        [SerializeField] private ColorVariable m_plantNotMoisturizedColor;
        [SerializeField] private UnityEvent m_onMoistureCorrect;
        [SerializeField] private UnityEvent m_onMoistureIncorrect;

        /// <summary>
        /// Updates the appearance of the displayed text based on moisture status.
        /// </summary>
        /// <param name="plantIsMoisturized">Indicates whether or not the <see cref="HydroponicsMoistureObject"/>
        /// component's current moisture levels is within its threshold.</param>
        public void UpdateTextAppearance(bool plantIsMoisturized) =>
            m_thresholdDisplayText.color = plantIsMoisturized ? m_plantMoisturizedColor : m_plantNotMoisturizedColor;

        /// <summary>
        /// Executes feedback behavior based on whether the plant's current moisture threshold state, as well as whether
        /// or not it has changed (correct vs. incorrect).
        /// </summary>
        /// <param name="thresholdStateChanged">Indicates whether or not the moisture status (within vs. outside the
        /// <see cref="HydroponicsMoistureObject"/> component's current threshold) has changed.</param>
        /// <param name="isPlantMoisturized">Indicates whether or not the <see cref="HydroponicsMoistureObject"/>
        /// component's current moisture levels is within its threshold.</param>
        public void CheckMoistureStatusChanged(bool thresholdStateChanged, bool isPlantMoisturized)
        {
            if (!thresholdStateChanged) { return; }
            if (isPlantMoisturized) { m_onMoistureCorrect.Invoke(); }
            else { m_onMoistureIncorrect.Invoke(); }
            UpdateTextAppearance(isPlantMoisturized);
        }
    }
}
