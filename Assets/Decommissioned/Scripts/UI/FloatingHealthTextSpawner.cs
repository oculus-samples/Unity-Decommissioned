// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Lobby;
using Meta.Decommissioned.Player;
using Meta.XR.Samples;
using NaughtyAttributes;
using ScriptableObjectArchitecture;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.UI
{
    /**
     * Component that spawns a floating text object in the scene.
     * <seealso cref="FloatingHealthChangeText"/>
     */
    [MetaCodeSample("Decommissioned")]
    public class FloatingHealthTextSpawner : NetworkBehaviour
    {
        [SerializeField, Required] private GameObject m_floatingTextPrefab;
        [SerializeField, Required] private StringVariable m_healCapReachedText;
        [SerializeField, Required] private StringVariable m_damageCapReachedText;
        [SerializeField, Required] private GamePosition m_trackedGamePosition;
        [SerializeField]
        public UnityEvent<int> OnHealthVisualSpawned;

        private void OnEnable()
        {
            if (!m_floatingTextPrefab.TryGetComponent(out FloatingHealthChangeText _))
            {
                Debug.LogError("Floating text prefab does not have a FloatingHealthChangeText component!");
            }
        }

        /**
         * Spawn a floating health text object displaying a given value.
         * <param name="healthChange"> The value (change in health) that will be displayed by this object.</param>
         */
        public void SpawnFloatingHealthText(int healthChange)
        {
            if (!CheckSpawnConditionsValid()) { return; }
            SpawnFloatingHealthTextClientRpc(healthChange);
        }

        /**
         * Spawn a floating health text object displaying a text indicating that the health/damage cap has been reached.
         * <param name="isHealing">If true, the text indicating that the maximum health was reached will be displayed; if false, the
         * text for the maximum amount of damage will instead.</param>
         */
        public void SpawnHealthCapReachedText(bool isHealing = true)
        {
            if (!CheckSpawnConditionsValid()) { return; }
            SpawnHealthCapReachedTextClientRpc(isHealing);
        }

        [ClientRpc]
        private void SpawnFloatingHealthTextClientRpc(int healthChange)
        {
            var player = PlayerStatus.GetByPlayerObject(m_trackedGamePosition.OccupyingPlayer);
            if (!player.IsLocalPlayer) { return; }

            var floatingHealthChangeText = Instantiate(m_floatingTextPrefab, transform.position, Quaternion.identity).GetComponent<FloatingHealthChangeText>();
            floatingHealthChangeText.SetDamageText(healthChange);
            OnHealthVisualSpawned.Invoke(healthChange);
        }

        [ClientRpc]
        private void SpawnHealthCapReachedTextClientRpc(bool isHealing = true)
        {
            var player = PlayerStatus.GetByPlayerObject(m_trackedGamePosition.OccupyingPlayer);
            if (!player.IsLocalPlayer) { return; }

            var floatingHealthChangeText = Instantiate(m_floatingTextPrefab, transform.position, Quaternion.identity).GetComponent<FloatingHealthChangeText>();
            floatingHealthChangeText.SetInfoText(isHealing ? m_healCapReachedText.Value.ToUpper() : m_damageCapReachedText.Value.ToUpper());
            OnHealthVisualSpawned.Invoke(0);
        }

        private bool CheckSpawnConditionsValid()
        {
            var prefabIsValid = m_floatingTextPrefab.TryGetComponent(out FloatingHealthChangeText _);
            if (!prefabIsValid || m_trackedGamePosition == null || !m_trackedGamePosition.IsOccupied) { return false; }
            var player = PlayerStatus.GetByPlayerObject(m_trackedGamePosition.OccupyingPlayer);
            return player.CurrentGamePosition == m_trackedGamePosition;
        }

        [ContextMenu("Spawn Floating Health Text (Debug)")]
        public void SpawnFloatingHealthTextDebug()
        {
            var floatingHealthChangeText = Instantiate(m_floatingTextPrefab, transform.position, Quaternion.identity).GetComponent<FloatingHealthChangeText>();
            floatingHealthChangeText.SetDamageText(1);
            OnHealthVisualSpawned.Invoke(1);
        }
    }
}
