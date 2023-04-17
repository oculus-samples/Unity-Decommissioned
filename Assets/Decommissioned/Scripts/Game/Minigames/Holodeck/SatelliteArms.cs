// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Linq;
using Meta.Decommissioned.Interactables;
using Meta.Multiplayer.Avatar;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Oculus.Avatar2;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

namespace Meta.Decommissioned.Game.MiniGames
{
    public class SatelliteArms : NetworkBehaviour
    {
        [SerializeField] internal Renderer m_satelliteHand_L;
        [SerializeField] internal Renderer m_satelliteHand_R;

        [SerializeField] internal Collider m_satelliteHandCollider_L;
        [SerializeField] internal Collider m_satelliteHandCollider_R;

        [SerializeField] private Transform m_leftLaser;
        [SerializeField] private Transform m_rightLaser;

        [SerializeField] private VisualEffect m_leftLaserEffect;
        [SerializeField] private VisualEffect m_rightLaserEffect;

        [SerializeField] private ParticleSystem m_leftLaserChargeFX;
        [SerializeField] private ParticleSystem m_rightLaserChargeFX;

        [SerializeField] private float m_maxLaserSize = 10f;

        [SerializeField] private UnityEvent m_onLeftLaserCharging;
        [SerializeField] private UnityEvent m_onLeftLaserFiring;
        [SerializeField] private UnityEvent m_onLeftLaserStopped;

        [SerializeField] private UnityEvent m_onRightLaserCharging;
        [SerializeField] private UnityEvent m_onRightLaserFiring;
        [SerializeField] private UnityEvent m_onRightLaserStopped;

        [SerializeField, AutoSetFromParent] private PunchingInteraction m_punchInteraction;
        [SerializeField, AutoSetFromParent] private LaserInteraction m_laserInteraction;

        private MaterialPropertyBlock m_leftHandMaterialProperties;
        private MaterialPropertyBlock m_rightHandMaterialProperties;
        private readonly int m_colorProperty = Shader.PropertyToID("_BaseColor");

        [SerializeField, AutoSet]
        private Animator m_satelliteAnimator;

        private Transform m_localLeftWrist;
        private Transform m_localRightWrist;
        private Transform m_activeLeftWrist;
        private Transform m_activeRightWrist;
        internal Transform m_chest;

        private bool m_satelliteIsActive;
        private bool m_resetIK;

        private bool m_canShootLaser_left;
        private bool m_canShootLaser_right;
        private bool m_leftHandOnScreen = true;
        private bool m_leftHandShouldBeClosed = true;
        private bool m_rightHandOnScreen = true;
        private bool m_rightHandShouldBeClosed = true;

        private readonly NetworkVariable<bool> m_isLaserFiring_left = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> m_isLaserFiring_right = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private Vector3 m_laserSize_left = Vector3.one;
        private Vector3 m_laserSize_right = Vector3.one;

        private readonly int m_leftClawAnimatorParameter = Animator.StringToHash("leftClawOpen");
        private readonly int m_rightClawAnimatorParameter = Animator.StringToHash("rightClawOpen");
        private readonly string m_laserAliveParameter = "Alive";

        private void Awake()
        {
            if (m_satelliteHand_L)
            {
                m_leftHandMaterialProperties = new();
                m_satelliteHand_L.SetPropertyBlock(m_leftHandMaterialProperties);
            }
            else
            {
                Debug.LogWarning("The Satellite Arms did not have a left hand mesh renderer set in the component! This will result in degraded effects.");
            }
            if (m_satelliteHand_R)
            {
                m_rightHandMaterialProperties = new();
                m_satelliteHand_R.SetPropertyBlock(m_rightHandMaterialProperties);
            }
            else
            {
                Debug.LogWarning("The Satellite Arms did not have a right hand mesh renderer set in the component! This will result in degraded effects.");
            }
            m_laserSize_left = m_leftLaser.localScale;
            m_laserSize_right = m_rightLaser.localScale;

            m_isLaserFiring_left.OnValueChanged += OnLeftLaserFiringChanged;
            m_isLaserFiring_right.OnValueChanged += OnRightLaserFiringChanged;

            _ = StartCoroutine(SetupLocalJoints());
        }

        private void OnRightLaserFiringChanged(bool previousValue, bool newValue) => OnLaserFiringChanged(newValue, false);

        private void OnLeftLaserFiringChanged(bool previousValue, bool newValue) => OnLaserFiringChanged(newValue, true);

        private void OnLaserFiringChanged(bool newValue, bool isLeft)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            var playerPosition = LocationManager.Instance.GetGamePositionByPlayer(localPlayer);

            if (playerPosition.MiniGameRoom is MiniGameRoom.Holodeck or MiniGameRoom.Commander)
            {
                if (isLeft)
                {
                    m_canShootLaser_left = !newValue;
                }
                else
                {
                    m_canShootLaser_right = !newValue;
                }

                if (newValue) { _ = StartCoroutine(ChargeLaser(isLeft)); }
                else { StopLaser(isLeft); }

                var amnimatorParam = isLeft ? m_leftClawAnimatorParameter : m_rightClawAnimatorParameter;
                m_satelliteAnimator.SetBool(amnimatorParam, newValue);
            }
        }

        private IEnumerator SetupLocalJoints()
        {
            yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null);
            var localPlayerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            AvatarEntity localPlayer = null;
            yield return new WaitUntil(() => localPlayerObject.TryGetComponent(out localPlayer));

            if (localPlayer == null)
            {
                Debug.LogError("Unable to get the AvatarEntity on the player!");
                yield break;
            }

            yield return new WaitUntil(() => localPlayer.HasJoints);

            m_localLeftWrist = localPlayer.GetJointTransform(CAPI.ovrAvatar2JointType.LeftHandWrist);
            m_localRightWrist = localPlayer.GetJointTransform(CAPI.ovrAvatar2JointType.RightHandWrist);
            m_chest = localPlayer.GetJointTransform(CAPI.ovrAvatar2JointType.Chest);
        }

        private IEnumerator SetupCommanderJoints()
        {
            var activePlayer = LocationManager.Instance.GetPlayersInRoom(MiniGameRoom.Holodeck).FirstOrDefault();

            if (activePlayer == null)
            {
                Debug.LogError("Unable to get the player on the asteroids MiniGame as the commander!");
                yield break;
            }

            AvatarEntity player = null;
            yield return new WaitUntil(() => activePlayer.TryGetComponent(out player));

            if (player == null)
            {
                Debug.LogError("Unable to get the AvatarEntity on the active player as the commander!");
                yield break;
            }

            m_activeLeftWrist = player.GetJointTransform(CAPI.ovrAvatar2JointType.LeftHandWrist);
            m_activeRightWrist = player.GetJointTransform(CAPI.ovrAvatar2JointType.RightHandWrist);
        }

        [ContextMenu("Init Arms")]
        private void InitArms() => StartSatelliteArms(true);

        public void StartSatelliteArms(bool isLocal)
        {
            if (isLocal)
            {
                var localId = PlayerManager.LocalPlayerId;
                RequestOwnershipServerRpc(localId);

                m_activeLeftWrist = m_localLeftWrist;
                m_activeRightWrist = m_localRightWrist;
                m_canShootLaser_left = true;
                m_canShootLaser_right = true;
            }
            else { _ = StartCoroutine(SetupCommanderJoints()); }

            m_satelliteIsActive = true;

            m_punchInteraction.StartWatchingPose(m_activeLeftWrist, m_activeRightWrist);
            m_laserInteraction.StartWatchingPose();
        }

        public void EndSatelliteArms()
        {
            if (IsServer)
            {
                NetworkObject.ChangeOwnership(PlayerId.ServerPlayerId());
                m_isLaserFiring_left.Value = false;
                m_isLaserFiring_right.Value = false;
            }

            m_satelliteIsActive = false;
            m_resetIK = true;
            m_punchInteraction.StopWatchingPose();
            m_laserInteraction.StopWatchingPose();

            if (m_satelliteHand_L)
            {
                m_leftHandMaterialProperties.SetColor(m_colorProperty, Color.cyan);
                m_satelliteHand_L.SetPropertyBlock(m_leftHandMaterialProperties);
            }
            if (m_satelliteHand_R)
            {
                m_rightHandMaterialProperties.SetColor(m_colorProperty, Color.cyan);
                m_satelliteHand_R.SetPropertyBlock(m_rightHandMaterialProperties);
            }
            m_canShootLaser_left = false;
            m_canShootLaser_right = false;
        }

        [ServerRpc]
        private void RequestOwnershipServerRpc(PlayerId newOwner) => NetworkObject.ChangeOwnership(newOwner);

        public void OnLaserHandPoseEntered(bool isLeft)
        {
            if (!IsOwner)
            {
                return;
            }

            if ((isLeft && (!m_canShootLaser_left || m_punchInteraction.LeftHandIsInPunchPose)) || (!isLeft && (!m_canShootLaser_right || m_punchInteraction.RightHandIsInPunchPose))) { return; }

            if (isLeft)
            {
                m_leftHandShouldBeClosed = false;
                m_isLaserFiring_left.Value = true;
            }
            else
            {
                m_rightHandShouldBeClosed = false;
                m_isLaserFiring_right.Value = true;
            }
        }

        public void OnLaserHandPoseExited(bool isLeft)
        {
            if (!IsOwner)
            {
                return;
            }

            if (isLeft)
            {
                m_leftHandShouldBeClosed = true;
                if (m_leftHandOnScreen)
                {
                    m_isLaserFiring_left.Value = false;
                }
            }
            else
            {
                m_rightHandShouldBeClosed = true;
                if (m_rightHandOnScreen)
                {
                    m_isLaserFiring_right.Value = false;
                }
            }
        }
        public void OnPunchPoseExited(bool isLeft)
        {
            if (!m_punchInteraction.LeftHandIsInPunchPose && !m_punchInteraction.RightHandIsInPunchPose)
            {
                ResetHandColor();
            }
        }

        private IEnumerator ChargeLaser(bool isLeft)
        {
            if (isLeft)
            {
                m_leftLaserChargeFX.Play();
                m_onLeftLaserCharging.Invoke();
            }
            else
            {
                m_rightLaserChargeFX.Play();
                m_onRightLaserCharging.Invoke();
            }

            yield return new WaitForSeconds(.5f);

            var ownerStoppedCharging = IsOwner && ((isLeft && !m_laserInteraction.LeftHandInLaserPose) || (!isLeft && !m_laserInteraction.RightHandInLaserPose));
            var notFiringLaser = isLeft ? !m_isLaserFiring_left.Value : !m_isLaserFiring_right.Value;
            if (ownerStoppedCharging || notFiringLaser)
            {
                StopLaser(isLeft);
                yield break;
            }

            FireLaser(isLeft);
        }

        private void FireLaser(bool isLeft)
        {
            if (isLeft)
            {
                m_canShootLaser_left = true;
                m_leftLaser.gameObject.SetActive(true);
                m_leftLaser.localScale = new Vector3(1, .05f, 1);
                m_laserSize_left.y = .05f;
                m_laserSize_left.y = m_maxLaserSize;
                m_leftLaser.localScale = m_laserSize_left;
                m_leftLaserEffect.SetBool(m_laserAliveParameter, true);
                m_leftLaserEffect.Play();
                m_onLeftLaserFiring.Invoke();
            }
            else
            {
                m_canShootLaser_right = true;
                m_rightLaser.gameObject.SetActive(true);
                m_rightLaser.localScale = new Vector3(1, .05f, 1);
                m_laserSize_right.y = .05f;
                m_laserSize_right.y = m_maxLaserSize;
                m_rightLaser.localScale = m_laserSize_right;
                m_rightLaserEffect.SetBool(m_laserAliveParameter, true);
                m_rightLaserEffect.Play();
                m_onRightLaserFiring.Invoke();
            }
        }
        private void StopLaser(bool isLeft)
        {
            if (isLeft)
            {
                if (IsOwner)
                {
                    m_isLaserFiring_left.Value = false;
                }
                m_leftLaserChargeFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                m_canShootLaser_left = true;
                m_leftLaser.gameObject.SetActive(false);
                m_leftLaserEffect.Stop();
                m_leftLaserEffect.SetBool(m_laserAliveParameter, false);
                m_onLeftLaserStopped.Invoke();

            }
            else
            {
                if (IsOwner)
                {
                    m_isLaserFiring_right.Value = false;
                }
                m_rightLaserChargeFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                m_canShootLaser_right = true;
                m_rightLaser.gameObject.SetActive(false);
                m_rightLaserEffect.Stop();
                m_rightLaserEffect.SetBool(m_laserAliveParameter, false);
                m_onRightLaserStopped.Invoke();

            }
        }

        private void CalculateHandColor()
        {
            if (m_punchInteraction.LeftHandIsInPunchPose && !m_laserInteraction.LeftHandInLaserPose)
            {
                var color = Color.Lerp(Color.cyan, Color.red, m_punchInteraction.LeftHandVelocity / m_punchInteraction.PunchForceRequired);
                m_leftHandMaterialProperties.SetColor(m_colorProperty, color);
            }
            else
            {
                m_leftHandMaterialProperties.SetColor(m_colorProperty, Color.cyan);
            }

            if (m_punchInteraction.RightHandIsInPunchPose && !m_laserInteraction.RightHandInLaserPose)
            {
                var color = Color.Lerp(Color.cyan, Color.red, m_punchInteraction.RightHandVelocity / m_punchInteraction.PunchForceRequired);
                m_rightHandMaterialProperties.SetColor(m_colorProperty, color);
            }
            else
            {
                m_rightHandMaterialProperties.SetColor(m_colorProperty, Color.cyan);
            }

            if (m_punchInteraction.LeftHandIsInPunchPose || m_punchInteraction.RightHandIsInPunchPose)
            {
                if (m_satelliteHand_R)
                {
                    m_satelliteHand_R.SetPropertyBlock(m_rightHandMaterialProperties);
                }
                if (m_satelliteHand_L)
                {
                    m_satelliteHand_L.SetPropertyBlock(m_leftHandMaterialProperties);
                }
            }
        }
        private void ResetHandColor()
        {
            m_leftHandMaterialProperties.SetColor(m_colorProperty, Color.cyan);
            m_rightHandMaterialProperties.SetColor(m_colorProperty, Color.cyan);

            if (m_satelliteHand_R)
            {
                m_satelliteHand_R.SetPropertyBlock(m_rightHandMaterialProperties);
            }
            if (m_satelliteHand_L)
            {
                m_satelliteHand_L.SetPropertyBlock(m_leftHandMaterialProperties);
            }
        }

        private void Update()
        {
            if (!m_satelliteIsActive)
            {
                return;
            }

            if (m_satelliteIsActive && m_activeLeftWrist != null && m_activeRightWrist != null)
            {
                var camera = Camera.main;
                var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                m_leftHandOnScreen = GeometryUtility.TestPlanesAABB(planes, new Bounds(m_activeLeftWrist.position, new Vector3(0.1f, 0.1f, 0.1f)));
                m_rightHandOnScreen = GeometryUtility.TestPlanesAABB(planes, new Bounds(m_activeRightWrist.position, new Vector3(0.1f, 0.1f, 0.1f)));
                if (m_rightHandShouldBeClosed && m_rightHandOnScreen)
                {
                    OnLaserHandPoseExited(false);
                }

                if (m_leftHandShouldBeClosed && m_leftHandOnScreen)
                {
                    OnLaserHandPoseExited(true);
                }
            }

            CalculateHandColor();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (m_satelliteIsActive && m_activeLeftWrist != null & m_activeRightWrist != null)
            {
                m_satelliteAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                m_satelliteAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                m_satelliteAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                m_satelliteAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                m_satelliteAnimator.SetIKPosition(AvatarIKGoal.LeftHand, m_activeLeftWrist.position);
                m_satelliteAnimator.SetIKPosition(AvatarIKGoal.RightHand, m_activeRightWrist.position);
                m_satelliteAnimator.SetIKRotation(AvatarIKGoal.LeftHand, m_activeLeftWrist.rotation);
                m_satelliteAnimator.SetIKRotation(AvatarIKGoal.RightHand, m_activeRightWrist.rotation);
            }
            else if (m_resetIK)
            {
                m_satelliteAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                m_satelliteAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
                m_satelliteAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
                m_satelliteAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
                m_resetIK = false;
            }
        }
    }
}
