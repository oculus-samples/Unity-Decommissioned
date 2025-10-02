// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using Meta.Utilities;
using Meta.XR.Samples;
using NaughtyAttributes;
using ScriptableObjectArchitecture;
using UnityEngine;
using UnityEngine.Events;
using Random = System.Random;

namespace Meta.Decommissioned.Game.MiniGames
{
    /**
     * This object represents the recipe that the player must match in order to complete the Chemistry MiniGame.
     * Generates and stores the values changed by each interaction to be accessed and evaluated.
     */
    [MetaCodeSample("Decommissioned")]
    public class ChemistryRecipe : MonoBehaviour
    {
        [Tooltip("How many, in total, of all chemicals can a recipe require?")]
        [SerializeField] private IntVariable m_maxTotalChemicals;
        [Tooltip("How many, at most, of each color can a recipe require?")]
        [SerializeField] private IntVariable m_maxChemicalRequirement;
        [Tooltip("How few, at lease, of each color can a recipe require?")]
        [SerializeField] private IntVariable m_minChemicalRequirement;
        [Tooltip("How high of a temperature can a recipe require?")]
        [SerializeField] private IntVariable m_maxTemperatureRequirement;
        [Tooltip("How low of a temperature can a recipe require?")]
        [SerializeField] private IntVariable m_minTemperatureRequirement;
        [Tooltip("How many shakes, at most, can a recipe require?")]
        [SerializeField] private IntVariable m_maxMixingRequirement;

        [Tooltip("The required chemical amount will only ever be a factor of this number.")]
        [SerializeField]
        private int m_chemicalAmountFactor = 3;
        [Tooltip("The required temperature will only ever be a factor of this number.")]
        [SerializeField] private int m_temperatureFactor = 10;


        [SerializeField] private EnumDictionary<LabelName, StringVariable> m_recipeStrings;

        [SerializeField] private UnityEvent<string> m_onRecipeReady;

        public float ChemicalANeeded { get; private set; }
        public float ChemicalBNeeded { get; private set; }
        public float ChemicalCNeeded { get; private set; }
        public float TemperatureRequired { get; private set; }
        public int MinimumMixCountNeeded { get; private set; }

        private void Start() => SetNewSolution();

        /** Generate a new, random solution for the chemistry MiniGame.*/
        [Button]
        public void SetNewSolution()
        {
            var rand = new Random();
            GenerateChemicalAmounts();
            TemperatureRequired = (int)Math.Round(rand.Next(m_minTemperatureRequirement, m_maxTemperatureRequirement)
                                                  / (double)m_temperatureFactor) * m_temperatureFactor;

            MinimumMixCountNeeded = rand.Next(m_maxMixingRequirement);
            SetNewRecipeText();
        }

        /** On MiniGame reset, generate a new solution.*/
        public void OnMiniGameReset() => SetNewSolution();

        /** Send a string with the required color, temperature, and mixing values to any component that cares.*/
        private void SetNewRecipeText()
        {
            var ingredientAText = string.Format(m_recipeStrings[LabelName.AmountNeededLabel], m_recipeStrings[LabelName.ChemicalAName], ChemicalANeeded);
            var ingredientBText = string.Format(m_recipeStrings[LabelName.AmountNeededLabel], m_recipeStrings[LabelName.ChemicalBName], ChemicalBNeeded);
            var ingredientCText = string.Format(m_recipeStrings[LabelName.AmountNeededLabel], m_recipeStrings[LabelName.ChemicalCName], ChemicalCNeeded);
            var temperatureText = $"{m_recipeStrings[LabelName.TemperatureLabel]}: {TemperatureRequired}{m_recipeStrings[LabelName.TemperatureScaleLabel]}";
            var mixingText = MinimumMixCountNeeded > 0 ? string.Format(m_recipeStrings[LabelName.MixInstruction], MinimumMixCountNeeded)
                : m_recipeStrings[LabelName.NoMixInstruction];

            var recipeText = $"{ingredientAText}\n{ingredientBText}\n{ingredientCText}\n{temperatureText}\n{mixingText}";
            m_onRecipeReady.Invoke(recipeText.ToUpper());
        }

        private void GenerateChemicalAmounts()
        {
            var remainingChemAmountPossible = (int)m_maxTotalChemicals;
            var rand = new Random();

            ChemicalANeeded = (int)Math.Round(rand.Next(m_minChemicalRequirement, m_maxChemicalRequirement)
                                              / (double)m_chemicalAmountFactor) * m_chemicalAmountFactor;

            remainingChemAmountPossible -= (int)ChemicalANeeded;

            if (remainingChemAmountPossible <= 0 || remainingChemAmountPossible < m_minChemicalRequirement)
            {
                ChemicalBNeeded = 0;
                ChemicalCNeeded = 0;
                return;
            }

            ChemicalBNeeded = (int)Math.Round(rand.Next(m_minChemicalRequirement, remainingChemAmountPossible)
                                              / (double)m_chemicalAmountFactor) * m_chemicalAmountFactor;

            remainingChemAmountPossible -= (int)ChemicalBNeeded;

            if (remainingChemAmountPossible < m_minChemicalRequirement || remainingChemAmountPossible < m_minChemicalRequirement)
            {
                ChemicalCNeeded = 0;
                return;
            }

            ChemicalCNeeded = (int)Math.Round(rand.Next(m_minChemicalRequirement, remainingChemAmountPossible)
                                              / (double)m_chemicalAmountFactor) * m_chemicalAmountFactor;

            // If we haven't assigned a value greater than 0 to any chemicals, select random chemical to assign a value of m_temperatureFactor or more to
            if (remainingChemAmountPossible <= m_maxTotalChemicals) { return; }

            var randomChemicalIndex = rand.Next(3);

            switch (randomChemicalIndex)
            {
                case 0:
                    ChemicalANeeded = (int)Math.Round(rand.Next(m_temperatureFactor, remainingChemAmountPossible)
                                                      / (double)m_chemicalAmountFactor) * m_chemicalAmountFactor;
                    break;
                case 1:
                    ChemicalBNeeded = (int)Math.Round(rand.Next(m_temperatureFactor, remainingChemAmountPossible)
                                                      / (double)m_chemicalAmountFactor) * m_chemicalAmountFactor;
                    break;
                case 2:
                    ChemicalCNeeded = (int)Math.Round(rand.Next(m_temperatureFactor, remainingChemAmountPossible)
                                                      / (double)m_chemicalAmountFactor) * m_chemicalAmountFactor;
                    break;
                default:
                    break;
            }
        }

        private enum LabelName
        {
            Unknown,
            TemperatureLabel,
            TemperatureScaleLabel,
            MixInstruction,
            NoMixInstruction,
            ChemicalAName,
            ChemicalBName,
            ChemicalCName,
            AmountNeededLabel
        }
    }
}
