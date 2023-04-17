// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Game.MiniGames
{
    /**
     * Class for managing a subset of pipes; set range and check if every pipe's current pressure level
     * is within it.
     *
     * <seealso cref="HydroponicsMoistureObject"/>
     */
    public class HydroponicsMoistureManager : MonoBehaviour
    {
        [SerializeField] private HydroponicsMoistureObject[] m_moistures = new HydroponicsMoistureObject[3];
        [SerializeField] private float m_checkMoistureTimeInterval = 2f;
        [SerializeField] private UnityEvent<bool[]> m_onMoistureInRangeChecked;
        private Coroutine m_moistureCheckCoroutine;

        /**
         * Start decay coroutine for all pipes managed by this component; pipes decay after a
         * specified interval in the Pipe class.
         * <seealso cref="HydroponicsMoistureObject"/>
         */
        public void StartMoistureDecay()
        {
            foreach (var pipe in m_moistures) { pipe.StartMoistureDecayServerRpc(); }
            m_moistureCheckCoroutine = StartCoroutine(WaitToCheckPipes());
        }

        /**
         * Reset each pipe managed by this component to its default pressure level.
         */
        public void ResetMoistureDecay()
        {
            if (m_moistureCheckCoroutine != null) { StopCoroutine(m_moistureCheckCoroutine); }
            foreach (var plant in m_moistures) { plant.ResetMoistureDecayServerRpc(); }
        }

        /**
         * Check if every Pipe managed by this component is within the
         * specified range.
         * <returns>A boolean indicating whether or not all pipes are within range.</returns>
         * <seealso cref="HydroponicsMoistureObject"/>
         */
        public bool CheckMoisture()
        {
            var plantMoistureConditions = new[] { m_moistures[0].IsPlantMoisturized(), m_moistures[1].IsPlantMoisturized(), m_moistures[2].IsPlantMoisturized() };
            //Temporarily removing these ClientRPCs to save on network resources. They eventually execute a server-only method anyways. - B.S.
            OnMoistureChecked(plantMoistureConditions);
            return !plantMoistureConditions.Contains(false);
        }

        private void OnMoistureChecked(bool[] plantMoistureConditions) =>
            m_onMoistureInRangeChecked.Invoke(plantMoistureConditions);

        private IEnumerator WaitToCheckPipes()
        {
            while (true)
            {
                yield return new WaitForSeconds(m_checkMoistureTimeInterval);
                _ = CheckMoisture();
            }
        }
    }
}
