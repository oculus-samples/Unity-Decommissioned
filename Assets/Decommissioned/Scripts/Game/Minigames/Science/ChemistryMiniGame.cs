// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections;
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
     * <seealso cref="ChemistryBeaker"/>
     * <seealso cref="BeakerFillVisual"/>
     * <seealso cref="ChemistryRecipe"/>
     * <seealso cref="ChemistryBurner"/>
     */
    [MetaCodeSample("Decommissioned")]
    public class ChemistryMiniGame : NetworkBehaviour
    {
        [SerializeField, AutoSet] private MiniGame m_miniGame;

        [Tooltip("The renderer for the outcome of the submit dish. Changes color based on solution correctness.")]
        [SerializeField] private Renderer m_submittionOutcomeRenderer;
        [Tooltip("The object containing the solution the station will be checking against the recipe.")]
        [SerializeField] private ChemistryBeaker m_miniGameBeaker;

        [Tooltip("The object indicating the correct solution.")]
        [SerializeField] private ChemistryRecipe m_recipe;

        [Tooltip("The object used to increase the beaker's temperature.")]
        [SerializeField] private ChemistryBurner m_burner;

        [Tooltip("The default color of the submit dish's content.")]
        [SerializeField] private ColorVariable m_defaultColor;

        [Tooltip("How much, at most, can the station be healed or damaged by the player?")]
        [SerializeField] private int m_maxPossibleHealthChange = 10;

        [Tooltip("How much, at most, can the station be damaged by higher temperatures?")]
        [SerializeField] private int m_maxPossibleTemperatureDamageBonus = 5;

        [SerializeField] private UnityEvent<int> m_onCorrectSolution;
        [SerializeField] private UnityEvent<int> m_onIncorrectSolution;

        private NetworkVariable<Color> m_currentDishContentColor =
            new(Color.clear, writePerm: NetworkVariableWritePermission.Server);

        private void Awake()
        {
            m_currentDishContentColor.OnValueChanged += ChangeDishRenderColor;
            GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
        }

        private void OnPhaseChanged(Phase newPhase) => ResetMiniGame();

        private void ChangeDishRenderColor(Color oldColor, Color newColor) =>
            m_submittionOutcomeRenderer.material.color = newColor;

        /**
         * Check the contents of the beaker and execute behavior based on its accuracy.
         */
        public void CheckSolutionAccuracy()
        {
            if (m_miniGameBeaker.IsEmpty) { return; }
            m_submittionOutcomeRenderer.material.color = m_miniGameBeaker.MixtureColor;
            /* We want to interpolate between -maxPossibleHealthChange and +maxPossibleHealthChange, so we double the heal/damage
             value to Lerp with, then subtract the original value from the result. */
            var change = Mathf.Round(Mathf.Lerp(0, m_maxPossibleHealthChange * 2, m_miniGameBeaker.AverageAccuracy));
            change -= m_maxPossibleHealthChange;

            if (change < 0)
            {
                // Get percentage (out of 100) for temperature and use it to calculate bonus damage
                change -= Mathf.Round(Mathf.Lerp(0, m_maxPossibleTemperatureDamageBonus, m_miniGameBeaker.CurrentTemperature / 100));
                DamageMiniGameServerRpc((int)Math.Abs(change));
            }
            else { HealMiniGameServerRpc((int)change); }

            m_recipe.SetNewSolution();
            m_miniGameBeaker.ClearBeakerValues();
        }

        [ServerRpc(RequireOwnership = false)]
        private void DamageMiniGameServerRpc(int damageAmount = 0)
        {
            m_miniGame.DecreaseHealth(damageAmount);
            m_onIncorrectSolution.Invoke(-damageAmount);
        }

        [ServerRpc(RequireOwnership = false)]
        private void HealMiniGameServerRpc(int healAmount = 0)
        {
            m_miniGame.IncreaseHealth(healAmount);
            m_onCorrectSolution.Invoke(healAmount);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestChangeColorServerRpc(Color newColor) =>
            m_currentDishContentColor.Value = newColor;

        /** Behavior for when the MiniGame resets; should clear all colors and beaker values. */
        [ContextMenu("Start Chemistry MiniGame")]
        public void ResetMiniGame()
        {
            RequestChangeColorServerRpc(m_defaultColor);
            m_miniGameBeaker.OnMiniGameReset();
            m_recipe.OnMiniGameReset();
            m_burner.ToggleBurner(false);
            StopAllCoroutines();
            _ = StartCoroutine(WaitForPlayerInMiniGame());
        }

        private IEnumerator WaitForPlayerInMiniGame()
        {
            var timeoutCount = 0;
            while (!m_miniGame.SpawnPoint.IsOccupied && timeoutCount < 200)
            {
                timeoutCount++;
                yield return null;
            }

            if (m_miniGame.SpawnPoint.IsOccupied)
            {
                var occupyingPlayer = m_miniGame.SpawnPoint.OccupyingPlayer;
                var occupyingPlayerId = occupyingPlayer.GetOwnerPlayerId();
                if (occupyingPlayerId.HasValue)
                {
                    RequestChangeBeakerOwnershipServerRpc(occupyingPlayerId.Value);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestChangeBeakerOwnershipServerRpc(PlayerId newPlayerId)
        {
            m_miniGameBeaker.ChangeBeakerOwner(newPlayerId);
        }
    }
}
