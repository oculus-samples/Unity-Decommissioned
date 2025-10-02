// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Collections.Generic;
using Meta.Decommissioned.Audio;
using Meta.Decommissioned.Interactables;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Game.MiniGames
{
    public enum AsteroidType
    {
        Asteroid = 0,
        Debris = 1
    }
    public enum TutorialType
    {
        FirstAsteroid = 0,
        FirstDebris = 1,
        PunchWarning = 2,
    }

    /// <summary>
    /// A small sister script that handles the asteroid objects for the "Asteroids" MiniGame.
    /// Most of this logic is client-sided to keep it responsive for the player.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class AsteroidObject : NetworkBehaviour
    {
        /// <summary>
        /// A reference to the base MiniGame logic that controls this object.
        /// </summary>
        [Tooltip("A reference to the base MiniGame logic that controls this object.")]
        [SerializeField]
        private AsteroidsMiniGame m_miniGameLogic;

        /// <summary>
        /// The type of object this is considered.
        /// </summary>
        [Tooltip("The type of object this is considered.")]
        [SerializeField] private AsteroidType m_asteroidType = AsteroidType.Asteroid;

        /// <summary>
        /// The speed that this asteroid will fly towards the target.
        /// </summary>
        [Tooltip("The speed that this asteroid will fly towards the target.")]
        [SerializeField] private float m_asteroidSpeed = 1f;

        /// <summary>
        /// Is this asteroid currently active in play? This variable is only managed by the server.
        /// </summary>
        [HideInInspector]
        public bool IsInPlay;

        /// <summary>
        /// Is this asteroid currently running in the gameplay?
        /// </summary>
        [HideInInspector]
        [SerializeField] private bool m_running;
        private Transform m_targetLocation;
        [SerializeField, AutoSet]
        private MeshRenderer m_renderer;
        [SerializeField, AutoSetFromParent] private PunchingInteraction m_punchInteraction;

        /// <summary>
        /// The current rotational axis this asteroid will spin on while floating towards the target. This is randomized every time this asteroid is spawned.
        /// </summary>
        [HideInInspector]
        [SerializeField] private Vector3 m_rotationAxis = Vector3.zero;

        /// <summary>
        /// Radius Object for generating astroid circle
        /// </summary>
        [SerializeField] private AsteroidRadius m_astroidRadius;

        /// <summary>
        /// A reference to the detroy FX this asteroid will play when it has been destroyed.
        /// </summary>
        [Tooltip("A reference to the detroy FX this asteroid will play when it has been destroyed.")]
        [SerializeField] private ParticleSystem m_destroyFx;

        /// <summary>
        /// This event executes whenever a tutorial message has been submitted to this asteroid.
        /// </summary>
        [Tooltip("This event executes whenever a tutorial message has been submitted to this asteroid.")]
        [SerializeField] private UnityEvent<string> m_onTutorialMessageSubmit;

        /// <summary>
        /// A list of which client IDs can currently see this asteroid. This list is only managed by the server.
        /// </summary>
        [HideInInspector]
        [SerializeField] private List<PlayerId> m_visibleTo = new();
        [SerializeField] private AudioSource m_ambientAudio;
        [SerializeField] private AudioClip m_incorrectAudio;
        [SerializeField] private UnityEvent<AsteroidObject> m_onAsteroidPunched;
        [SerializeField] private UnityEvent<AsteroidObject> m_onAsteroidLasered;
        [SerializeField] private UnityEvent<AsteroidObject> m_onAsteroidWrongAction;

        private const string TUTORIAL_TEXT_FIRST_ASTEROID = "This is an Asteroid. You must punch asteroids to defend the satellite.";
        private const string TUTORIAL_TEXT_FIRST_DEBRIS = "This is space debris. You must shoot debris with your laser to defend the satellite.";
        private const string TUTORIAL_TEXT_PUNCH_WARNING = "You have to punch the asteroids with some force to destroy them";
        private float m_tutorialShowTime = 6f;

        public void Awake()
        {
            if (m_miniGameLogic.m_satellite == null)
            {
                Debug.LogError("The Asteroids MiniGame did not have a satellite object assigned! This will cause issues with the asteroid objects!");
                m_targetLocation = m_miniGameLogic.gameObject.transform;
                return;
            }
            m_targetLocation = m_miniGameLogic.m_satellite.AsteroidTargetPosition;
        }

        /// <summary>
        /// Starts this asteroid to begin moving towards the target.
        /// </summary>
        /// <param name="startingPoint">The location that this asteroid will spawn and start moving towards the target.</param>
        [ClientRpc]
        public void AttackTargetClientRpc(Vector3 startingPoint)
        {
            transform.position = startingPoint;
            m_rotationAxis = Random.rotation.eulerAngles;
            m_running = true;
            m_astroidRadius.ShowRadius();
            if (m_ambientAudio && !m_ambientAudio.isPlaying) { m_ambientAudio.Play(); }
        }

        /// <summary>
        /// Stops this asteroid from moving towards the target. This is the same action as destroying an asteroid.
        /// </summary>
        [ClientRpc]
        private void StopAsteroidClientRpc(bool incorrectHit = false)
        {
            if (m_running) { StopAsteroid(incorrectHit); }
        }

        public void StopAsteroid(bool incorrectHit = false)
        {
            if (m_destroyFx && !m_destroyFx.isPlaying) { m_destroyFx.Play(); }
            if (m_ambientAudio && m_ambientAudio.isPlaying) { m_ambientAudio.Stop(); }
            if (incorrectHit)
            {
                m_onAsteroidWrongAction?.Invoke(this);
                if (AudioManager.Instance != null)
                {
                    _ = AudioManager.Instance.PlaySoundInSpace(transform.position, m_incorrectAudio);
                }
            }
            m_running = false;
            m_astroidRadius.HideRadius();
            m_miniGameLogic.HealthTextSpawner.transform.position = transform.position;
        }

        /// <summary>
        /// Resets this current asteroid to a dormant state.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ResetAsteroidServerRpc(bool incorrectHit = false)
        {
            StopAsteroidClientRpc(incorrectHit);
            m_running = false;
            IsInPlay = false;

            foreach (var playerID in PlayerManager.Instance.AllPlayerIds)
            {
                if (m_visibleTo.Contains(playerID)) { HideAsteroidForPlayer(playerID); }
            }
        }

        /// <summary>
        /// Hides this asteroid from the given client ID.
        /// </summary>
        /// <param name="playerId">The client ID of the client to hide this asteroid from.</param>
        private void HideAsteroidForPlayer(PlayerId playerId)
        {
            if (m_visibleTo.Contains(playerId))
            {
                _ = m_visibleTo.Remove(playerId);
                HideAsteroidClientRpc(NetcodeHelpers.CreateSendRpcParams(playerId));
            }
            else { Debug.LogWarning("Tried to hide an asteroid from a player that already had it hidden!"); }
        }

        /// <summary>
        /// Shows this asteroid to the given client ID.
        /// </summary>
        /// <param name="playerId">The client ID of the client to show this asteroid to.</param>
        public void ShowAsteroidForPlayer(PlayerId playerId)
        {
            if (!m_visibleTo.Contains(playerId))
            {
                m_visibleTo.Add(playerId);
                ShowAsteroidClientRpc(NetcodeHelpers.CreateSendRpcParams(playerId));
            }
            else { Debug.LogWarning("Tried to show an asteroid for a player that already had it shown!"); }
        }

        [ClientRpc]
        private void HideAsteroidClientRpc(ClientRpcParams clientRpcParams = default) => m_renderer.enabled = false;

        [ClientRpc]
        private void ShowAsteroidClientRpc(ClientRpcParams clientRpcParams = default) => m_renderer.enabled = true;

        private void OnAsteroidPunched()
        {
            if (m_running)
            {
                StopAsteroid();
                m_onAsteroidPunched.Invoke(this);
                ResetAsteroidServerRpc();
                //Disabling renderer here as well to keep clientside snappy
                m_renderer.enabled = false;
            }
        }

        private void OnAsteroidLasered()
        {
            if (m_running)
            {
                StopAsteroid();
                m_onAsteroidLasered.Invoke(this);
                ResetAsteroidServerRpc();
                //Disabling renderer here as well to keep clientside snappy
                m_renderer.enabled = false;
            }
        }

        private void OnAsteroidWrongAction()
        {
            if (m_running)
            {
                OnAsteroidReachedTargetServerRpc();
                StopAsteroid();
                m_onAsteroidWrongAction.Invoke(this);
                if (AudioManager.Instance != null)
                {
                    _ = AudioManager.Instance.PlaySoundInSpace(transform.position, m_incorrectAudio);
                }
                //Disabling renderer here as well to keep clientside snappy
                m_renderer.enabled = false;
            }
        }

        /// <summary>
        /// Informs this asteroid that it has collided with the target. This will destroy the asteroid and decrease this MiniGame's health.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void OnAsteroidReachedTargetServerRpc()
        {
            if (m_running)
            {
                ResetAsteroidServerRpc(true);
                m_miniGameLogic.MiniGame.DecreaseHealth();
            }
        }

        /// <summary>
        /// Requests a specific client or all clients to display a tutorial message on this asteroid.
        /// </summary>
        /// <param name="type">The type of tutorial to display.</param>
        /// <param name="clientRpcParams">RPC Params used to specify a specific client to display a tutorial to.</param>
        [ClientRpc]
        public void RequestTutorialMessageClientRpc(TutorialType type, ClientRpcParams clientRpcParams)
        {
            switch (type)
            {
                case TutorialType.FirstAsteroid:
                    if (!m_miniGameLogic.HasShownAsteroidTutorial)
                    {
                        DisplayTutorialMessage(TUTORIAL_TEXT_FIRST_ASTEROID);
                        m_miniGameLogic.HasShownAsteroidTutorial = true;
                    }
                    break;
                case TutorialType.FirstDebris:
                    if (!m_miniGameLogic.HasShownDebrisTutorial)
                    {
                        DisplayTutorialMessage(TUTORIAL_TEXT_FIRST_DEBRIS);
                        m_miniGameLogic.HasShownDebrisTutorial = true;
                    }
                    break;
                case TutorialType.PunchWarning:
                    if (!m_miniGameLogic.HasShownPunchWarning)
                    {
                        m_miniGameLogic.DisplayFloatingTutorialMessage(transform.position, TUTORIAL_TEXT_PUNCH_WARNING);
                        m_miniGameLogic.HasShownPunchWarning = true;
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Displays a tutorial message on this asteroid.
        /// </summary>
        /// <param name="message">The text to display on this asteroid.</param>
        private void DisplayTutorialMessage(string message)
        {
            m_onTutorialMessageSubmit?.Invoke(message);
            _ = StartCoroutine(ClearTutorialMessageAfterTime(m_tutorialShowTime));
        }

        private void ClearTutorialMessage() => m_onTutorialMessageSubmit?.Invoke(string.Empty);

        private IEnumerator ClearTutorialMessageAfterTime(float time)
        {
            yield return new WaitForSeconds(time);
            ClearTutorialMessage();
        }

        public void Update()
        {
            if (m_running)
            {
                var step = m_asteroidSpeed * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, m_targetLocation.position, step);
                transform.Rotate(m_rotationAxis, m_asteroidSpeed);
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!m_running) { return; }

            if (other.CompareTag("SatelliteHand"))
            {
                if ((other == m_miniGameLogic.m_satellite.m_satelliteArms.m_satelliteHandCollider_L && m_punchInteraction.LeftHandIsInPunchPose) ||
                    (other == m_miniGameLogic.m_satellite.m_satelliteArms.m_satelliteHandCollider_R && m_punchInteraction.RightHandIsInPunchPose))
                {
                    if ((other == m_miniGameLogic.m_satellite.m_satelliteArms.m_satelliteHandCollider_L && m_punchInteraction.LeftHandAtTargetVelocity) ||
                        (other == m_miniGameLogic.m_satellite.m_satelliteArms.m_satelliteHandCollider_R && m_punchInteraction.RightHandAtTargetVelocity))
                    {
                        if (m_asteroidType == AsteroidType.Asteroid) { OnAsteroidPunched(); }
                        else { OnAsteroidWrongAction(); }
                    }
                    else if (!m_miniGameLogic.HasShownPunchWarning)
                    {
                        m_miniGameLogic.HasShownPunchWarning = true;
                        m_miniGameLogic.DisplayFloatingTutorialMessage(transform.position, TUTORIAL_TEXT_PUNCH_WARNING);
                    }
                }
            }
            else if (other.CompareTag("SatelliteLaser"))
            {
                if (m_asteroidType == AsteroidType.Debris) { OnAsteroidLasered(); }
                else { OnAsteroidWrongAction(); }
            }
        }
    }
}
