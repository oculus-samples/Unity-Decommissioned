// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.Decommissioned.Utils;
using Meta.Multiplayer.Avatar;
using Meta.Multiplayer.Core;
using Meta.Utilities;
using Oculus.Avatar2;
using Oculus.Interaction;
using Unity.Burst;
using Unity.Netcode;
using UnityEngine;
using static Oculus.Avatar2.CAPI;

namespace Meta.Decommissioned.Player
{
    [BurstCompile]
    public class PlayerArmband : NetworkBehaviour
    {
        [SerializeField] private MeshRenderer[] m_armbandMeshes;
        [SerializeField, AutoSetFromParent] private OvrAvatarEntity m_avatarEntity;
        [SerializeField, AutoSetFromParent] private AvatarMeshQuery m_avatarMeshQuery;
        [SerializeField, AutoSetFromParent] private PlayerColor m_playerColor;
        [SerializeField, AutoSetFromChildren] private Animator m_animator;
        [SerializeField] private PokeInteractable m_openButton;
        [SerializeField] private PokeInteractable[] m_settingsButtons;
        [SerializeField] private MonoBehaviour[] m_settingsSliders;
        [SerializeField] private Transform[] m_strapScalarPoints;
        [SerializeField] private ExternallyScaledObject[] m_strapScalars;
        [SerializeField] private GameObject m_backStrap;
        [SerializeField] private GameObject m_frontStrap;

        private Transform m_armTransform;
        private Transform m_wristTransform;
        private Vector3 m_currentArmDirection = Vector3.zero;
        private Quaternion m_armbandTargetRotation = Quaternion.identity;

        private readonly int m_colorProperty = Shader.PropertyToID("_EmissionColor");
        private readonly int m_settingsOpenAnimatorParam = Animator.StringToHash("settingsOpen");

        private readonly NetworkVariable<float> m_frontStrapScalePoint = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<float> m_backStrapScalePoint = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<float> m_armOffset = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<float> m_armRotation = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        [SerializeField] private float m_upperPercent = 0.6f;
        [SerializeField] private float m_lowerPercent = 0.9f;
        [SerializeField] private float m_measurementOffsetScalar;
        [SerializeField] private float m_measurementOffsetOrigin;

        [SerializeField] private float m_defaultFrontStrapScalePoint = 1f;
        [SerializeField] private float m_defaultBackStrapScalePoint = 1f;
        [SerializeField] private float m_defaultArmOffset = 1f;
        [SerializeField] private float m_defaultArmRotation = 1f;
        private readonly NetworkVariable<bool> m_settingsOpen = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                EnableAllInteractions();
            }
            else
            {
                DisableAllInteractions();
            }

            UpdateArmband();
        }

        private void OnEnable()
        {
            m_frontStrapScalePoint.OnValueChanged += (_, _) => UpdateArmband();
            m_backStrapScalePoint.OnValueChanged += (_, _) => UpdateArmband();
            m_armOffset.OnValueChanged += (_, _) => UpdateArmband();
            m_armRotation.OnValueChanged += (_, _) => UpdateArmband();

            if (m_playerColor != null)
            {
                m_playerColor.OnColorChanged.AddListener(OnPlayerColorChanged);
            }
            m_avatarMeshQuery.OnMeshDataAcquired += MeasureArm;
            m_settingsOpen.OnValueChanged += OnSettingsMenuStateChanged;
        }

        private void OnDisable()
        {
            if (m_playerColor != null)
            {
                m_playerColor.OnColorChanged.RemoveListener(OnPlayerColorChanged);
            }
            m_settingsOpen.OnValueChanged -= OnSettingsMenuStateChanged;
            m_avatarMeshQuery.OnMeshDataAcquired -= MeasureArm;
        }

        private void OnSettingsMenuStateChanged(bool previousValue, bool newValue)
        {
            m_animator.SetBool(m_settingsOpenAnimatorParam, newValue);
        }

        private void OnPlayerColorChanged(Color newColor)
        {
            UpdateArmbandColor();
        }

        /// <summary>
        /// Measures the user's currently assigned wrist to fit the wristband to.
        /// </summary>
        [ContextMenu("Measure Arm")]
        public void MeasureArm()
        {
            if (NetworkManager.Singleton == null || NetworkObject.IsOwner)
            {
                if (m_avatarEntity is AvatarEntity { HasUserAvatar: false })
                {
                    Debug.LogWarning("User has no avatar, assuming defaults");
                    SetDefaultValues();
                    return;
                }

                if (!m_avatarMeshQuery.HasMeshData || m_avatarMeshQuery.VertexCount <= 0)
                {
                    Debug.LogError("Player mesh was not captured correctly! Unable to measure the player's arm, using default values...");
                }
                else
                {
                    _ = StartCoroutine(CalculateArmWidth());
                }
            }
            else
            {
                //Just update the transform and color for other players on spawn
                UpdateArmband();
            }
        }

        private void SetDefaultValues()
        {
            UpdateArmbandTransform();
            m_armOffset.Value = m_defaultArmOffset;
            m_armRotation.Value = m_defaultArmRotation;
            UpdateArmbandTransform();
            m_frontStrapScalePoint.Value = m_defaultFrontStrapScalePoint;
            m_backStrapScalePoint.Value = m_defaultBackStrapScalePoint;
            UpdateArmbandSize();
        }

        private IEnumerator CalculateArmWidth()
        {
            if (m_avatarMeshQuery.VertexCount is null or 0)
            {
                Debug.LogError("Tried to measure a player's arm size while unable to get the mesh vertices!");
                yield break;
            }

            UpdateArmbandTransform();

            if (m_armTransform == null)
            {
                Debug.LogError("Tried to measure the user's arm size without the arm joint being set up correctly!");
                yield break;
            }

            if (!PlayerCamera.Instance.TryGetComponent(out Oculus.Interaction.AvatarIntegration.HandTrackingInputManager userTracking))
            {
                Debug.LogError("Tried to measure the user's arm size without the tracking being set up correctly!");
                yield break;
            }

            m_avatarEntity.SetInputManager(null);

            yield return null;//Wait a frame to allow the avatar to return to bind pose for measurements

            var armDetails = m_avatarMeshQuery.GetArmDetails(true, m_upperPercent, m_lowerPercent);

            m_armOffset.Value = armDetails.UpperRadius * m_measurementOffsetScalar + m_measurementOffsetOrigin;

            var lowerTopOnUpperLine = Vector3.Project(armDetails.LowerTop - armDetails.UpperBottom, armDetails.UpperTop - armDetails.UpperBottom) + armDetails.UpperBottom;
            m_armRotation.Value = Vector3.Angle(armDetails.UpperTop - armDetails.LowerTop, lowerTopOnUpperLine - armDetails.LowerTop);
            UpdateArmbandTransform();//Update transforms again to ensure newly measured values get updated

            m_backStrapScalePoint.Value = (armDetails.UpperRadius + 0.01f) * 2; // Use arm upper radius to scale back strap, diameter = radius * 2
            m_frontStrapScalePoint.Value = armDetails.LowerRadius * 2; // same for front strap
            UpdateArmbandSize();

            m_avatarEntity.SetInputManager(userTracking);
        }

        public void UpdateArmband()
        {
            UpdateArmbandTransform();
            UpdateArmbandSize();
            UpdateArmbandColor();
        }

        private void UpdateArmbandColor()
        {
            var playerColor = m_playerColor.Color;
            foreach (var mesh in m_armbandMeshes)
            {
                mesh.material.SetColor(m_colorProperty, playerColor);
            }
        }

        private void UpdateArmbandSize()
        {
            m_strapScalarPoints[0].localPosition = new Vector3(m_strapScalars[0].transform.localPosition.x, m_frontStrapScalePoint.Value + m_strapScalars[0].transform.localPosition.y, 0); // end point = start point + arm diameter * vec3(0, 1, 0)
            m_strapScalarPoints[1].localPosition = new Vector3(m_strapScalars[1].transform.localPosition.x, m_backStrapScalePoint.Value + m_strapScalars[1].transform.localPosition.y, 0);
            m_strapScalars[0].UpdateObjectScale();
            m_strapScalars[1].UpdateObjectScale();
        }

        private void UpdateArmbandTransform()
        {
            if (m_armTransform == null)
            {
                m_armTransform = m_avatarMeshQuery.GetJointTransform(ovrAvatar2JointType.LeftArmLower);
            }
            if (m_wristTransform == null)
            {
                m_wristTransform = m_avatarMeshQuery.GetJointTransform(ovrAvatar2JointType.LeftHandWrist);
            }

            if (m_armTransform != null && m_wristTransform != null)
            {
                m_currentArmDirection = m_wristTransform.position - m_armTransform.position;
                if (m_currentArmDirection.sqrMagnitude > 0)
                {
                    m_armbandTargetRotation = Quaternion.LookRotation(m_currentArmDirection, m_wristTransform.up) * Quaternion.Euler(-m_armRotation.Value, 0, 0);
                    m_backStrap.transform.localRotation = Quaternion.Euler(0, 0, -m_armRotation.Value);
                    m_frontStrap.transform.localRotation = Quaternion.Euler(0, 0, -m_armRotation.Value);
                    transform.SetPositionAndRotation(m_armTransform.position + m_armOffset.Value * m_wristTransform.up, m_armbandTargetRotation);
                }
            }
        }

        [ContextMenu("Toggle Settings")]
        public void ToggleSettings()
        {
            m_settingsOpen.Value = !m_settingsOpen.Value;

            foreach (var button in m_settingsButtons)
            {
                button.enabled = m_settingsOpen.Value;
            }
            foreach (var slider in m_settingsSliders)
            {
                slider.enabled = m_settingsOpen.Value;
            }
        }

        private void DisableAllInteractions()
        {
            foreach (var button in m_settingsButtons)
            {
                button.enabled = false;
            }
            foreach (var slider in m_settingsSliders)
            {
                slider.enabled = false;
            }
            m_openButton.enabled = false;
        }

        private void EnableAllInteractions()
        {
            foreach (var button in m_settingsButtons)
            {
                button.enabled = m_settingsOpen.Value;
            }
            foreach (var slider in m_settingsSliders)
            {
                slider.enabled = m_settingsOpen.Value;
            }
            m_openButton.enabled = true;
        }

        private void Update()
        {
            UpdateArmbandTransform();
        }
    }
}
