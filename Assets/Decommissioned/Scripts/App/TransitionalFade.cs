// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections;
using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.Decommissioned
{
    public class TransitionalFade : Singleton<TransitionalFade>
    {
        [SerializeField] private float m_fadeSpeed = .01f;
        [SerializeField, Tooltip("How long to wait during a transition before the player is considered stuck. After this time frame of being on the transition screen, the player will be disconnected and reset back to the main menu.")]
        private float m_transitionTimeout = 30f;
        private YieldInstruction m_transitionTimeoutWait;
        private Coroutine m_timeoutCoroutine;

        [SerializeField, AutoSetFromChildren] private MeshRenderer m_fadeBackground;
        [SerializeField, AutoSetFromChildren] private Camera m_overlayCam;
        [SerializeField, AutoSetFromChildren] private TextMeshPro m_reasonText;
        [SerializeField] private Phase[] m_trackedPhases;

        private Transform m_overlayCamTransform;
        private Transform m_playerCamera;
        private Transform m_playerCameraRig;

        private bool m_isFadingIn;
        private bool m_includeLogo = true;
        private float m_currentFade;
        private Color m_currentScreenFade = new(1, 1, 1, 0);

        private Material m_fadeBackroundMat;
        private string m_currentReasonText = "";
        public const string LOADING_TEXT = "Loading...";
        public const string ERROR_TEXT = "An error has occurred.\nReturning to main menu...";

        /// return true if handled
        public event Func<Scene, bool> OnSceneLoadTrigger;

        private new void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);

            m_transitionTimeoutWait = new WaitForSeconds(m_transitionTimeout);
            if (m_overlayCam == null)
            {
                Debug.LogError($"{gameObject.name}: Cannot show transitions! Please ensure that the Transitioner component is set up correctly to use transitions.");
                enabled = false;
            }
            m_overlayCamTransform = m_overlayCam.transform;

            m_playerCamera = Camera.main.transform;
            m_fadeBackroundMat = m_fadeBackground.material;
            m_fadeBackroundMat.color = m_currentScreenFade;
        }

        private void Start()
        {
            if (m_playerCamera.root == m_playerCamera)
            {
                Debug.LogWarning("The player camera does not have a parent! The TransitionalFade will keep its default orientation this session.");
                return;
            }

            m_playerCameraRig = m_playerCamera.root;
        }

        private new void OnEnable()
        {
            base.OnEnable();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //Clients will rely on OnPlayerTeleportFinished() for the game scene
            if (OnSceneLoadTrigger?.Invoke(scene) != true)
            {
                FadeFromBlack(0.5f);
            }
        }

        public void OnPhaseTimerTransition(bool showLogo)
        {
            if (m_trackedPhases.Contains(GamePhaseManager.CurrentPhase)) { FadeToBlack(showLogo); }
        }

        private void Update()
        {
            if (m_playerCameraRig != null)
            {
                transform.rotation = m_playerCameraRig.rotation;
            }
            m_overlayCamTransform.rotation = m_playerCamera.rotation;
            AnimateFade();
        }

        private void AnimateFade()
        {
            if (m_isFadingIn && m_currentFade < 1)
            {
                m_currentFade = Mathf.MoveTowards(m_currentFade, 1, m_fadeSpeed * Time.deltaTime);
                m_currentScreenFade.a = m_currentFade;
                var screenColor = m_includeLogo ? 1 : 0;
                m_currentScreenFade.r = screenColor;
                m_currentScreenFade.g = screenColor;
                m_currentScreenFade.b = screenColor;
                m_reasonText.alpha = m_currentFade;
            }
            else if (!m_isFadingIn && m_currentFade > 0)
            {
                m_currentFade = Mathf.MoveTowards(m_currentFade, 0, m_fadeSpeed * Time.deltaTime);
                m_currentScreenFade.a = m_currentFade;
                if (m_currentScreenFade.a != 0)
                {
                    var screenColor = m_includeLogo ? 1 : 0;
                    m_currentScreenFade.r = screenColor;
                    m_currentScreenFade.g = screenColor;
                    m_currentScreenFade.b = screenColor;
                }
                if (m_reasonText.alpha != 0) { m_reasonText.alpha = m_currentFade; }
            }

            if (m_currentFade == 0 && m_overlayCam.enabled)
            {
                m_overlayCam.enabled = false;
                m_fadeBackground.enabled = false;
            }
            else if (m_currentFade > 0 && !m_overlayCam.enabled)
            {
                m_overlayCam.enabled = true;
                m_fadeBackground.enabled = true;
            }

            m_fadeBackroundMat.color = m_currentScreenFade;
        }

        public void FadeToBlack_MainMenu()
        {
            FadeToBlack(true, LOADING_TEXT, .5f);
        }

        /// <summary>
        /// Fades the screen into black.
        /// </summary>
        /// <param name="fadeLogo">Should the fade include the game logo?</param>
        /// <param name="reasonText">Text to display on the transition screen.</param>
        /// <param name="waitTime">How long to wait before starting the fade.</param>
        /// <param name="fadeInWaitTime">How long to wait to automatically fade back in.</param>
        public void FadeToBlack(bool fadeLogo = true, string reasonText = "", float waitTime = 0, float fadeInWaitTime = 0)
        {
            m_includeLogo = fadeLogo;
            m_currentReasonText = reasonText;
            m_reasonText.text = m_currentReasonText;
            if (waitTime > 0 || fadeInWaitTime > 0)
            {
                _ = StartCoroutine(WaitToFade(waitTime, fadeLogo, reasonText, true, fadeInWaitTime));
                return;
            }
            m_isFadingIn = true;
            if (m_timeoutCoroutine != null)
            {
                StopCoroutine(m_timeoutCoroutine);
            }
            m_timeoutCoroutine = StartCoroutine(MonitorTimeout());
        }

        /// <summary>
        /// Fades the screen out from black to the normal camera view.
        /// </summary>
        /// <param name="waitTime">How long to wait before starting the fade.</param>
        public void FadeFromBlack(float waitTime = 0)
        {
            m_currentReasonText = "";
            if (waitTime > 0)
            {
                _ = StartCoroutine(WaitToFade(waitTime, true, "", false));
                return;
            }
            m_isFadingIn = false;
            if (m_timeoutCoroutine != null)
            {
                StopCoroutine(m_timeoutCoroutine);
                m_timeoutCoroutine = null;
            }
        }

        private IEnumerator WaitToFade(float waitTime, bool fadeLogo, string reasonText, bool fadeToBlack, float fadeInWaitTime = 0)
        {
            yield return new WaitForSeconds(waitTime);

            if (fadeToBlack)
            {
                FadeToBlack(fadeLogo, reasonText);
                if (fadeInWaitTime > 0)
                {
                    yield return new WaitForSeconds(fadeInWaitTime);
                    FadeFromBlack();
                }
            }
            else
            {
                FadeFromBlack();
            }
        }

        private IEnumerator MonitorTimeout()
        {
            yield return m_transitionTimeoutWait;

            _ = StartCoroutine(Application.Instance.GoToMainMenuAfterTime());
            FadeToBlack(true, ERROR_TEXT);
        }
    }
}
