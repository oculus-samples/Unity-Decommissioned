// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.Utilities;
using ScriptableObjectArchitecture;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Meta.Decommissioned.Game.MiniGames
{
    public enum Nutrient
    {
        Red,
        Yellow,
        Blue,
        None
    }

    /**
     * Class for changing and tracking the state of the hose on the hydroponics station.
     */
    public class HydroponicsHoseManager : NetworkBehaviour
    {
        [SerializeField] private float m_delayBeforeNewPumpNeeded = 1f;
        [SerializeField] private EnumDictionary<Nutrient, ColorVariable> m_nutrientColors;
        [SerializeField] private NetworkVariable<bool> m_hoseIsSpraying;
        [SerializeField] private UnityEvent m_onHoseWaterEnabled;
        [SerializeField] private UnityEvent m_onHoseWaterDisabled;
        [SerializeField] private UnityEvent<Nutrient> m_onProvidedNutrientChanged;

        [Header("Spherecast / Watering Values")]
        //used to time out the spherecast and water it randomly
        [SerializeField] private float m_wateringTimeOutMin = 0.25f;
        [SerializeField] private float m_wateringTimeOutMax = 1f;
        [SerializeField] private float m_spherecastRadius = 0.25f;

        [SerializeField] private Rigidbody[] m_hoseRigidbodies;
        public Nutrient CurrentNutrient
        {
            get => m_currentNutrient.Value;
            set
            {
                if (IsServer) { m_currentNutrient.Value = value; }
                else { Debug.LogError("Tried to set CurrentNutrient for the Hydroponics_HoseManager as a client! This is not allowed."); }
            }
        }
        private NetworkVariable<Nutrient> m_currentNutrient = new(Nutrient.None);
        [SerializeField, AutoSet]
        private VisualEffect m_waterParticles;
        [SerializeField] private VisualEffect m_waterSplashParticles;
        private ExposedProperty m_waterColorPropertyName = "InputColor";
        private ExposedProperty m_waterSplashPositionPropertyName = "StartPos";
        private bool m_waterSplashIsPlaying;
        private bool m_hosePumped;
        private bool m_hoseRoutineRunning;

        private void OnEnable()
        {
            m_currentNutrient.OnValueChanged += OnNutrientChanged;
            m_hoseIsSpraying.OnValueChanged += OnSprayStateChanged;

            //These variables need to be initialized to keep physics from going haywire
            foreach (var temp in m_hoseRigidbodies)
            {
                temp.centerOfMass = Vector3.zero;
                temp.inertiaTensor = Vector3.zero;
            }
        }
        /**
         * Turn on the hose for a short time.
         */
        [ServerRpc(RequireOwnership = false)]
        public void ActivateHoseServerRpc(int nutrientValue)
        {
            if (!IsServer) { return; }
            m_hosePumped = true;
            ChangeNutrient(nutrientValue);
            EnableHoseWater();
            //If the hose is already running, this will indicate that it should continue running unless no pumps occur within the time limit.
            if (!m_hoseRoutineRunning) { _ = StartCoroutine(CheckIfHosePumpedAfterDelay()); }
        }

        public void ActivateHose(int nutrientValue)
        {
            if (IsServer)
            {
                ActivateHoseServerRpc(nutrientValue);
                return;
            }

            if (m_hosePumped)
            {
                return;
            }

            _ = StartCoroutine(HosePumpTimeout());
            m_hosePumped = true;
            ActivateHoseServerRpc(nutrientValue);
        }

        private float m_raycastTimer;
        private float m_raycastTimeOut;
        private void Update()
        {
            if (m_hoseIsSpraying.Value != true || (!IsServer && LocationManager.Instance.GetGamePositionByPlayer(NetworkManager.Singleton.LocalClient?.PlayerObject)?.MiniGameRoom != MiniGameRoom.Hydroponics))
            {
                return;
            }

            m_raycastTimer += Time.deltaTime;

            if (m_raycastTimer > m_raycastTimeOut)
            {
                if (Physics.SphereCast(m_waterParticles.transform.position, m_spherecastRadius, m_waterParticles.transform.up, out var hit, 10f))
                {
                    if (hit.collider.gameObject.TryGetComponent(out HydroponicsPlant plantLogic))
                    {
                        StartWaterSplashEffect(hit.point);
                        if (IsServer)
                        {
                            plantLogic.WaterPlant(CurrentNutrient);
                        }
                    }
                }
                else
                {
                    StopWaterSplashEffect();
                }
                m_raycastTimeOut = Random.Range(m_wateringTimeOutMin, m_wateringTimeOutMax);
                m_raycastTimer = 0f;
            }
        }

        private void StartWaterSplashEffect(Vector3 position)
        {
            if (m_waterSplashParticles.HasVector3(m_waterSplashPositionPropertyName))
            {
                m_waterSplashParticles.SetVector3(m_waterSplashPositionPropertyName, position);
            }
            if (!m_waterSplashIsPlaying)
            {
                m_waterSplashIsPlaying = true;
                m_waterSplashParticles.Play();
            }
        }
        private void StopWaterSplashEffect()
        {
            if (m_waterSplashIsPlaying)
            {
                m_waterSplashIsPlaying = false;
                if (m_waterSplashParticles.HasVector3(m_waterSplashPositionPropertyName))
                {
                    m_waterSplashParticles.SetVector3(m_waterSplashPositionPropertyName, Vector3.zero);
                }
                m_waterSplashParticles.Stop();
            }
        }

        /**
         * Activate the watering particle effect on the hose.
         */
        [ContextMenu("Turn On Hose")]
        public void EnableHoseWater()
        {
            if (IsServer) { m_hoseIsSpraying.Value = true; }
            m_onHoseWaterEnabled.Invoke();
        }
        /**
         * Deactivate the watering particle effect on the hose.
         */
        [ContextMenu("Turn Off Hose")]
        public void DisableHoseWater()
        {
            if (IsServer) { m_hoseIsSpraying.Value = false; }
            m_onHoseWaterDisabled.Invoke();
        }
        private void OnSprayStateChanged(bool previousState, bool isSpraying)
        {
            if (isSpraying)
            {
                m_waterParticles.Play();
                EnableHoseWater();
            }
            else
            {
                m_waterParticles.Stop();
                StopWaterSplashEffect();
                DisableHoseWater();
            }


        }

        /**
         * Change the current nutrient being distributed by the water.
         */
        public void ChangeNutrient(int nutrientValue)
        {
            if (!IsServer) { return; }
            switch (nutrientValue)
            {
                case 1:
                    CurrentNutrient = Nutrient.Red;
                    break;
                case 2:
                    CurrentNutrient = Nutrient.Yellow;
                    break;
                case 3:
                    CurrentNutrient = Nutrient.Blue;
                    break;
                default:
                    break;
            }
        }

        private void ChangeWaterColor(Color color)
        {
            if (m_waterParticles.HasVector4(m_waterColorPropertyName))
            {
                m_waterParticles.SetVector4(m_waterColorPropertyName, color);
            }
            if (m_waterSplashParticles.HasVector4(m_waterColorPropertyName))
            {
                m_waterSplashParticles.SetVector4(m_waterColorPropertyName, color);
            }
        }

        private void OnNutrientChanged(Nutrient oldNutrient, Nutrient newNutrient)
        {
            ChangeWaterColor(m_nutrientColors[newNutrient]);
            m_onProvidedNutrientChanged.Invoke(newNutrient);
        }

        private IEnumerator HosePumpTimeout()
        {
            yield return new WaitForSeconds(m_delayBeforeNewPumpNeeded / 2);

            m_hosePumped = false;
        }

        private IEnumerator CheckIfHosePumpedAfterDelay()
        {
            m_hoseRoutineRunning = true;
            while (true)
            {
                m_hosePumped = false;

                yield return new WaitForSeconds(m_delayBeforeNewPumpNeeded);

                // if the hose is not pumped within the time limit, stop the hose.
                if (!m_hosePumped)
                {
                    DisableHoseWater();
                    m_hoseRoutineRunning = false;
                    yield break;
                }
            }
        }
    }
}
