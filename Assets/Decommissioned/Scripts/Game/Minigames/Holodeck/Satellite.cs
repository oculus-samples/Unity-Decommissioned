// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Linq;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    /// <summary>
    /// A small sister script that handles the satellite for the "Asteroids" mini game.
    /// </summary>
    public class Satellite : NetworkBehaviour
    {
        /// <summary>
        /// The target position for asteroids to fly towards.
        /// </summary>
        [Tooltip("The target position for asteroids to fly towards.")]
        [field: SerializeField] public Transform AsteroidTargetPosition { get; private set; }

        [Tooltip("A reference to the main mini game logic of this mini game.")]
        [SerializeField] private AsteroidsMiniGame m_miniGameLogic;

        private CameraFollowing m_follower;

        [SerializeField] internal SatelliteArms m_satelliteArms;

        private bool m_isFollowingPlayer;
        private Vector3 m_satelliteRotation = Vector3.zero;

        private void OnEnable()
        {
            GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
        }

        private void OnPhaseChanged(Phase newPhase)
        {
            if (newPhase == Phase.Night)
            {
                BeginSatellite();
                return;
            }

            if (!IsServer)
            {
                return;
            }

            DisableSatelliteFollowerClientRpc();
            DisableSatelliteIKClientRpc();
        }

        private void OnDisable()
        {
            GamePhaseManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }

        public void BeginSatellite()
        {
            StopAllCoroutines();
            _ = StartCoroutine(WaitForPlayersInMiniGame());
        }

        [ClientRpc]
        private void DisableSatelliteIKClientRpc()
        {
            if (m_satelliteArms && m_satelliteArms.isActiveAndEnabled)
            {
                m_satelliteArms.EndSatelliteArms();
            }
        }

        [ClientRpc]
        private void DisableSatelliteFollowerClientRpc()
        {
            if (m_satelliteArms && !m_satelliteArms.isActiveAndEnabled)
            {
                m_follower.enabled = false;
            }
            m_isFollowingPlayer = false;
        }

        private void EnableSatelliteFollower()
        {
            if (m_satelliteArms && !m_satelliteArms.isActiveAndEnabled)
            {
                m_follower.enabled = true;
            }
            m_isFollowingPlayer = true;
        }

        private IEnumerator WaitForPlayersInMiniGame()
        {
            yield return new WaitUntil(() => LocationManager.Instance.GetPlayersInRoom(MiniGameRoom.Holodeck).Count() > 0);
            var occupyingPlayer = LocationManager.Instance.GetPlayersInRoom(MiniGameRoom.Holodeck).ToArray();

            if (occupyingPlayer.Length == 0)
            {
                yield break;
            }

            if (NetworkManager.Singleton.LocalClient.PlayerObject == occupyingPlayer[0])
            {
                EnableSatelliteFollower();
                m_satelliteArms.StartSatelliteArms(true);
            }
            else
            {
                var commander = LocationManager.Instance.GetPlayersInRoom(MiniGameRoom.Commander).FirstOrDefault();
                if (NetworkManager.Singleton.LocalClient.PlayerObject == commander)
                {
                    m_satelliteArms.StartSatelliteArms(false);
                }
            }

            if (IsServer) { NetworkObject.ChangeOwnership(occupyingPlayer[0].GetOwnerPlayerId() ?? PlayerId.New()); }
        }

        private void Update()
        {
            if (m_isFollowingPlayer && m_satelliteArms?.m_chest != null)
            {
                m_satelliteRotation.y = m_satelliteArms.m_chest.eulerAngles.y - 90;
                transform.eulerAngles = m_satelliteRotation;
                transform.position = m_satelliteArms.m_chest.position;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!m_miniGameLogic.m_runAsteroids)
            {
                return;
            }

            if (other.TryGetComponent(out AsteroidObject asteroid))
            {
                asteroid.OnAsteroidReachedTargetServerRpc();
            }
        }
    }
}
