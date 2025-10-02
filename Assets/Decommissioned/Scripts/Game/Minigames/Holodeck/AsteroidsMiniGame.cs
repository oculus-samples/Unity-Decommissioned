// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Linq;
using Meta.Decommissioned.UI;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Game.MiniGames
{
    /// <summary>
    /// Defines the logic of the "Asteroids" MiniGame. V2 of the "Satellite Uplink" MiniGame.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    public class AsteroidsMiniGame : NetworkBehaviour
    {
        /// <summary>
        /// A reference to the <see cref="MiniGames.MiniGame"/> associated with this MiniGame logic.
        /// </summary>
        [Tooltip("A reference to the Minigame associated with this MiniGame logic.")]
        [field: SerializeField, AutoSet] public MiniGame MiniGame { get; private set; }

        [Tooltip("A reference to the Satellite object associated with this MiniGame.")]
        [SerializeField] internal Satellite m_satellite;

        [SerializeField, Tooltip("The transform that the floating health text spawner will display at for health changes not caused by asteroid hits.")]
        private Transform m_healthSpawnerDefaultLocation;

        /// <summary>
        /// The interval at which to spawn each asteroid.
        /// </summary>
        [Tooltip("The interval at which to spawn each asteroid.")]
        [SerializeField] private float m_incomingAsteroidInterval = 1.5f;

        /// <summary>
        /// The asteroid pool parent object that should contain every asteroid object to be used in this MiniGame.
        /// </summary>
        [Tooltip("The asteroid pool parent object that should contain every asteroid object to be used in this MiniGame.")]
        [SerializeField] private GameObject m_asteroidPool;

        /// <summary>
        /// The debris pool parent object that should contain every debris object to be used in this MiniGame.
        /// </summary>
        [Tooltip("The debris pool parent object that should contain every debris object to be used in this MiniGame.")]
        [SerializeField] private GameObject m_debrisPool;

        /// <summary>
        /// A reference to this MiniGame's floating health text spawner object.
        /// </summary>
        [Tooltip("A reference to this MiniGame's floating health text spawner object.")]
        [field: SerializeField] public FloatingHealthTextSpawner HealthTextSpawner { get; private set; }

        /// <summary>
        /// This event executes whenever a floating tutorial message has been submitted.
        /// </summary>
        [Tooltip("This event executes whenever a floating tutorial message has been submitted.")]
        [SerializeField] private UnityEvent<string> m_onFloatingTutorialMessageSubmit;

        /// <summary>
        /// The object that will display any floating tutorial messages.
        /// </summary>
        [Tooltip("The object that will display any floating tutorial messages.")]
        [SerializeField] private GameObject m_floatingTutorialMessage;

        [Tooltip("The max height that asteroids can spawn relative to the satellite position.")]
        [SerializeField] private float m_maxYSpawnHeight = 2f;

        private AsteroidObject[] m_asteroids;
        private AsteroidObject[] m_debris;

        internal bool m_runAsteroids;

        /// <summary>
        /// Has this client been shown the first asteroid tutorial message? This variable is updated on clients independently!
        /// </summary>
        [HideInInspector]
        public bool HasShownAsteroidTutorial;

        /// <summary>
        /// Has this client been shown the first debris tutorial message? This variable is updated on clients independently!
        /// </summary>
        [HideInInspector]
        public bool HasShownDebrisTutorial;

        /// <summary>
        /// Has this client been shown the punch warning tutorial message? This variable is updated on clients independently!
        /// </summary>
        [HideInInspector]
        public bool HasShownPunchWarning;

        private bool m_firstAsteroid = true;
        private bool m_firstDebris = true;
        private float m_floatingTutorialShowTime = 5f;

        public override void OnNetworkSpawn()
        {
            SetupAsteroidPool();
            base.OnNetworkSpawn();
        }
        private void OnEnable()
        {
            GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
            HealthTextSpawner.OnHealthVisualSpawned.AddListener(OnHealthVisualSpawned);
        }

        private void OnDisable()
        {
            GamePhaseManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            HealthTextSpawner.OnHealthVisualSpawned.RemoveListener(OnHealthVisualSpawned);
        }

        private void OnHealthVisualSpawned(int healthChange)
        {
            HealthTextSpawner.transform.position = m_healthSpawnerDefaultLocation.position;
        }

        private void OnPhaseChanged(Phase phase)
        {
            if (m_runAsteroids && phase != Phase.Night) { End(); }
        }
        private void Awake()
        {
            MiniGame.MiniGameInit = Begin;
        }

        private void SetupAsteroidPool()
        {
            var asteroidCount = m_asteroidPool.transform.childCount;
            if (asteroidCount == 0)
            {
                Debug.LogError("There were no objects contained in the Asteroid Pool! Please make sure the given Asteroid Pool has objects contained.", m_asteroidPool);
                m_asteroids = new AsteroidObject[0];
            }

            var debrisCount = m_debrisPool.transform.childCount;
            if (debrisCount == 0)
            {
                Debug.LogError("There were no objects contained in the Debris Pool! Please make sure the given Debris Pool has objects contained.", m_debrisPool);
                m_debris = new AsteroidObject[0];
            }

            m_asteroids = new AsteroidObject[asteroidCount];
            m_debris = new AsteroidObject[debrisCount];

            for (var i = 0; i < asteroidCount; i++)
            {
                var asteroid = m_asteroidPool.transform.GetChild(i).gameObject;
                if (asteroid.TryGetComponent(out AsteroidObject asteroidLogic))
                {
                    m_asteroids[i] = asteroidLogic;
                }
                else
                {
                    Debug.LogError("An asteroid in the asteroid pool did not have a AsteroidObject component assigned to it!", asteroid);
                }
            }
            for (var i = 0; i < debrisCount; i++)
            {
                var debris = m_debrisPool.transform.GetChild(i).gameObject;
                if (debris.TryGetComponent(out AsteroidObject asteroidLogic))
                {
                    m_debris[i] = asteroidLogic;
                }
                else
                {
                    Debug.LogError("A debris in the debris pool did not have a AsteroidObject component assigned to it!", debris);
                }
            }
        }
        /// <summary>
        /// Displays a floating tutorial message to the player.
        /// </summary>
        /// <param name="position">The position that the floating tutorial messgae should show up in.</param>
        /// <param name="message">The text to display on the floating tutorial message.</param>
        public void DisplayFloatingTutorialMessage(Vector3 position, string message)
        {
            if (m_floatingTutorialMessage)
            {
                m_floatingTutorialMessage.transform.position = position;
                m_onFloatingTutorialMessageSubmit?.Invoke(message);
                _ = StartCoroutine(ClearFloatingTutorialMessageAfterTime(m_floatingTutorialShowTime));
            }
        }
        private IEnumerator ClearFloatingTutorialMessageAfterTime(float time)
        {
            yield return new WaitForSeconds(time);

            ClearFloatingTutorialMessage();
        }
        private void ClearFloatingTutorialMessage()
        {
            m_onFloatingTutorialMessageSubmit?.Invoke(string.Empty);
            if (m_floatingTutorialMessage)
            {
                m_floatingTutorialMessage.transform.position = m_asteroidPool.transform.position;
            }
        }
        [ContextMenu("Start Asteroids")]
        public void Begin()
        {
            StartAsteroidsSessionServerRpc();
            m_satellite.BeginSatellite();
        }
        [ServerRpc(RequireOwnership = false)]
        private void StartAsteroidsSessionServerRpc()
        {
            if (!IsServer)
            {
                return;
            }

            m_runAsteroids = true;
            m_firstAsteroid = true;
            m_firstDebris = true;
            _ = StartCoroutine(RunAsteroidSession());
        }


        [ContextMenu("Stop Asteroids")]
        private void End()
        {
            m_runAsteroids = false;
            Cleanup();
        }
        private void SpawnAsteroid()
        {
            var inactiveAsteroids = m_asteroids.Where(item => !item.IsInPlay).ToArray();
            if (inactiveAsteroids.Any() is false)
                return;

            var randomAsteroid = Random.Range(0, inactiveAsteroids.Length);
            var selectedAsteroid = inactiveAsteroids[randomAsteroid];
            var randomSpawn = GetRandomAsteroidPosition();
            selectedAsteroid.IsInPlay = true;
            selectedAsteroid.AttackTargetClientRpc(randomSpawn);

            ShowAsteroidToAppropriateClients(selectedAsteroid);

            if (m_firstAsteroid && GetActivePlayerID() is { } id)
            {
                m_firstAsteroid = false;
                selectedAsteroid.RequestTutorialMessageClientRpc(TutorialType.FirstAsteroid, NetcodeHelpers.CreateSendRpcParams(id));
            }
        }
        private void SpawnDebris()
        {
            var inactiveDebris = m_debris.Where(item => !item.IsInPlay).ToArray();
            if (inactiveDebris.Any() is false)
                return;

            var randomDebris = Random.Range(0, inactiveDebris.Length);
            var selectedDebris = inactiveDebris[randomDebris];
            var randomSpawn = GetRandomAsteroidPosition();
            selectedDebris.IsInPlay = true;
            selectedDebris.AttackTargetClientRpc(randomSpawn);

            ShowAsteroidToAppropriateClients(selectedDebris);

            if (m_firstDebris && GetActivePlayerID() is { } id)
            {
                m_firstDebris = false;
                selectedDebris.RequestTutorialMessageClientRpc(TutorialType.FirstDebris, NetcodeHelpers.CreateSendRpcParams(id));
            }
        }
        private void ShowAsteroidToAppropriateClients(AsteroidObject asteroid)
        {
            var commander = LocationManager.Instance.
                GetPlayersInRoom(MiniGameRoom.Commander).
                FirstOrDefault();

            if (GetActivePlayerID() is { } id && id != default) { asteroid.ShowAsteroidForPlayer(id); }

            if (commander?.GetOwnerPlayerId() is { } commId && commId != default) { asteroid.ShowAsteroidForPlayer(commId); }
        }
        private Vector3 GetRandomAsteroidPosition()
        {
            Vector3 ret;
            var randomRotation = Random.insideUnitCircle;
            randomRotation = randomRotation.normalized * 5;
            var randomHeight = Random.Range(0, m_maxYSpawnHeight);
            ret = m_satellite.transform.position + new Vector3(randomRotation.x, randomHeight, randomRotation.y);
            return ret;
        }
        private void Cleanup()
        {
            if (IsServer)
            {
                foreach (var asteroid in m_asteroids) { asteroid.ResetAsteroidServerRpc(); }
                foreach (var debris in m_debris) { debris.ResetAsteroidServerRpc(); }
            }

            foreach (var asteroid in m_asteroids)
            {
                asteroid.StopAsteroid();
                asteroid.transform.position = m_asteroidPool.transform.position;
            }

            foreach (var debris in m_debris)
            {
                debris.StopAsteroid();
                debris.transform.position = m_debrisPool.transform.position;
            }
        }
        private IEnumerator RunAsteroidSession()
        {
            YieldInstruction wait = new WaitForSeconds(m_incomingAsteroidInterval);
            while (m_runAsteroids)
            {
                if (CoinToss()) { SpawnAsteroid(); }
                else { SpawnDebris(); }
                yield return wait;
            }
        }

        private PlayerId? GetActivePlayerID()
        {
            //This assumes only one client will be in this station (which should be true since this is a singleplayer MiniGame)
            return LocationManager.Instance.GetPlayersInRoom(MiniGame.Config.Room).
                WhereNonNull().
                Select(player => player.GetOwnerPlayerId()).
                FirstOrDefault();
        }

        private bool CoinToss() => Random.Range(0, 100) < 50;
    }
}
