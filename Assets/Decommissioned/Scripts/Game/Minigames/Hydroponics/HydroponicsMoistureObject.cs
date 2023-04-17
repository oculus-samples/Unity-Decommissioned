// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections;
using Meta.Decommissioned.Lobby;
using Meta.Multiplayer.PlayerManagement;
using NaughtyAttributes;
using ScriptableObjectArchitecture;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Meta.Decommissioned.Game.MiniGames
{
    /**
     * An object representing a pipe for the hydroponics station.
     * Pressure value decays overtime at a specified rate.
     * <seealso cref="HydroponicsMiniGame"/>
     */
    public class HydroponicsMoistureObject : NetworkBehaviour
    {
        [SerializeField] private GamePosition m_pumpPlayerPosition;
        [SerializeField] private float m_defaultMoistureValue = 0.7f;

        [SerializeField] private float m_highestPossibleMaxMoisture = 0.9f;
        [SerializeField] private float m_lowestPossibleMaxMoisture = 0.3f;
        [SerializeField] private float m_moistureRange = 0.2f;

        [SerializeField] private float m_minStartingMoistureValue = 0.3f;
        [SerializeField] private float m_maxStartingMoistureValue = 1f;

        [SerializeField] private float m_moistureRestoreRate = 0.01f;
        [SerializeField] private float m_moistureDecayRate = 0.01f;
        [SerializeField] private float m_moistureDecayInterval = 3f;

        [SerializeField] private StringVariable m_moistureThresholdReadoutText;

        [SerializeField]
        private NetworkVariable<float> m_currentCondition = new(0.7f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        [SerializeField] private NetworkVariable<float> m_maxMoisture = new(0.9f);
        [SerializeField] private NetworkVariable<float> m_minMoisture = new(0.7f);

        [SerializeField] private UnityEvent<float> m_onCurrentMoistureChange;
        [SerializeField] private UnityEvent<string> m_onMoistureThresholdChange;
        [SerializeField] private UnityEvent<bool> m_onMoistureStatusChecked;
        [SerializeField] private UnityEvent<bool, bool> m_onMoistureStatusChangedChecked;

        private Coroutine m_moistureCoroutine;

        private void Awake()
        {
            m_currentCondition.OnValueChanged += OnMoistureChange;
            m_maxMoisture.OnValueChanged += OnMoistureThresholdUpdated;
            m_minMoisture.OnValueChanged += OnMoistureThresholdUpdated;
        }

        private void OnDisable()
        {
            m_currentCondition.OnValueChanged -= OnMoistureChange;
            m_maxMoisture.OnValueChanged -= OnMoistureThresholdUpdated;
            m_minMoisture.OnValueChanged -= OnMoistureThresholdUpdated;
        }

        /**
         * Start coroutine for decreasing the current pressure value of this pipe overtime.
         */
        [ServerRpc(RequireOwnership = false)]
        public void StartMoistureDecayServerRpc()
        {
            if (!IsServer) { return; }
            m_currentCondition.Value = (float)Math.Round(Random.Range(m_minStartingMoistureValue, m_maxStartingMoistureValue), 2);
            m_maxMoisture.Value = (float)Math.Round(Random.Range(m_lowestPossibleMaxMoisture, m_highestPossibleMaxMoisture), 2);
            m_minMoisture.Value = (float)Math.Round(Random.Range(m_maxMoisture.Value - .01f, m_maxMoisture.Value - m_moistureRange), 2);
            if (m_moistureCoroutine != null) { StopCoroutine(m_moistureCoroutine); }
            m_moistureCoroutine = StartCoroutine(DecreaseMoisture());
            NotifyStartDecayClientRpc();
        }

        /**
         * Increase the current pressure value of this pipe by a specified amount.
         */
        [Button("Increase Plant Moisture")]
        public void IncreaseMoisture()
        {
            if (!IsServer) { return; }
            m_currentCondition.Value = Mathf.Clamp(m_currentCondition.Value + m_moistureRestoreRate, 0.1f, 1f);
        }

        /**
         * Reset the current pressure value of this pipe to the default and
         * stop pressure decay.
         */
        [ContextMenu("Reset Plant Moisture")]
        [ServerRpc(RequireOwnership = false)]
        public void ResetMoistureDecayServerRpc()
        {
            if (m_moistureCoroutine != null) { StopCoroutine(m_moistureCoroutine); }
            m_currentCondition.Value = m_defaultMoistureValue;
        }

        /**
         * Reset the moisture visuals for clients.
         */
        [ClientRpc]
        private void NotifyStartDecayClientRpc()
        {
            if (!m_pumpPlayerPosition.IsOccupied) { return; }
            if (m_pumpPlayerPosition.OccupyingPlayerId != PlayerManager.LocalPlayerId) { return; }
            CheckMoistureThresholdStatus();
        }

        /**
         * Determines whether or not this plant is at the correct moisture level.
         * <returns>True if the plant's moisture is within its threshold.</returns>
         */
        public bool IsPlantMoisturized() => m_currentCondition.Value >= m_minMoisture.Value &&
                                            m_currentCondition.Value <= m_maxMoisture.Value;

        private void OnMoistureChange(float oldMoisture, float newMoisture)
        {
            m_onCurrentMoistureChange.Invoke(newMoisture);
            var oldMoistureCorrect = oldMoisture <= m_maxMoisture.Value && oldMoisture >= m_minMoisture.Value;
            var newThresholdStateReached = IsPlantMoisturized() != oldMoistureCorrect;
            CheckMoistureThresholdStatus(newThresholdStateReached);
        }

        private void OnMoistureThresholdUpdated(float prevMoistureValue, float newMoistureValue) =>
            m_onMoistureThresholdChange.Invoke(string.Format(m_moistureThresholdReadoutText, m_maxMoisture.Value * 100, m_minMoisture.Value * 100));

        private void CheckMoistureThresholdStatus(bool thresholdStateChanged = false)
        {
            var playerValid = m_pumpPlayerPosition.IsOccupied && m_pumpPlayerPosition.OccupyingPlayerId == PlayerManager.LocalPlayerId;
            if (!playerValid) { return; }
            m_onMoistureStatusChecked.Invoke(IsPlantMoisturized());
            m_onMoistureStatusChangedChecked.Invoke(thresholdStateChanged, IsPlantMoisturized());
        }

        private IEnumerator DecreaseMoisture()
        {
            while (true)
            {
                m_currentCondition.Value = Mathf.Clamp(m_currentCondition.Value - m_moistureDecayRate, 0, m_maxStartingMoistureValue);
                yield return new WaitForSeconds(m_moistureDecayInterval);
            }
        }
    }
}
