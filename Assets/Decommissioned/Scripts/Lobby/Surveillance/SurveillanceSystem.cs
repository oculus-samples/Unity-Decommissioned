// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Decommissioned.Occlusion;
using Meta.Multiplayer.Networking;
using Meta.XR.Samples;
using NaughtyAttributes;
using Oculus.Avatar2;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Surveillance
{
    [MetaCodeSample("Decommissioned")]
    public class SurveillanceSystem : NetworkSingleton<SurveillanceSystem>
    {
        private NetworkVariable<MiniGameRoom> m_currentTargetRoom = new();

        [ShowNativeProperty]
        private MiniGameRoom CurrentTargetRoom => m_currentTargetRoom.Value;

        [ShowNativeProperty]
        public SurveillanceCameraPosition CurrentActiveCamera => SurveillanceCameraPosition.Instances.
            FirstOrDefault(camera => camera.ResidingRoom == CurrentTargetRoom);

        [SerializeField]
        private Camera m_surveillanceCamera;
        private bool m_isActive;
        [SerializeField]
        private MeshRenderer m_surveillanceDisplay;
        [SerializeField]
        private Material m_surveillanceScreenOnMat;
        [SerializeField]
        private Material m_surveillanceScreenOffMat;
        [SerializeField]
        [Range(1, 120)]
        private int m_cameraFrameRate = 30;

        private new void Awake()
        {
            base.Awake();

            DisableSurveillance();
            SwitchToRoom_ServerRpc(MiniGameRoom.Habitation);
        }

        private new void OnEnable()
        {
            base.OnEnable();
            LocationManager.Instance.OnPlayerJoinedRoom += OnPlayerJoinedRoom;
            m_currentTargetRoom.OnValueChanged += OnTargetRoomChanged;
        }

        private void OnDisable()
        {
            LocationManager.Instance.OnPlayerJoinedRoom -= OnPlayerJoinedRoom;
        }

        private void OnPlayerJoinedRoom(NetworkObject player, MiniGameRoom room)
        {
            if (player.IsLocalPlayer)
            {
                if (room is MiniGameRoom.Commander)
                {
                    EnableSurveillance();
                }
                else
                {
                    DisableSurveillance();
                }
            }
        }

        [ContextMenu("Enable Surveillance")]
        public void EnableSurveillance()
        {
            m_isActive = true;
            m_surveillanceDisplay.material = m_surveillanceScreenOnMat;
            AvatarLODManager.Instance.AddExtraCamera(m_surveillanceCamera);
            _ = StartCoroutine(RunCameraRender());
        }
        [ContextMenu("Disable Surveillance")]
        public void DisableSurveillance()
        {
            m_surveillanceDisplay.material = m_surveillanceScreenOffMat;
            AvatarLODManager.Instance.RemoveExtraCamera(m_surveillanceCamera);
            m_isActive = false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SwitchToRoom_ServerRpc(MiniGameRoom room)
        {
            m_currentTargetRoom.Value = room;
            UpdateCameraPosition();
        }

        private void OnTargetRoomChanged(MiniGameRoom previousValue, MiniGameRoom newValue)
        {
            UpdateCameraPosition();
            var commander = LocationManager.Instance.GetPlayersInRoom(MiniGameRoom.Commander)?.FirstOrDefault();
            if (commander != null && NetworkManager.Singleton.LocalClient.PlayerObject == commander)
            {
                var viewedRoom = new[] { newValue };
                RoomOcclusionZoneManager.Instance.ApplyOcclusion(MiniGameRoom.Commander, viewedRoom);
            }
        }

        private void UpdateCameraPosition()
        {
            if (CurrentActiveCamera is { } target)
            {
                m_surveillanceCamera.transform.position = target.CameraPosition.position;
                m_surveillanceCamera.transform.rotation = target.CameraPosition.rotation;
                m_surveillanceCamera.farClipPlane = target.FarClipDistance;
                m_surveillanceCamera.fieldOfView = target.FOV;
            }

            UpdateActiveCameraLights();
        }

        private void UpdateActiveCameraLights()
        {
            foreach (var camera in SurveillanceCameraPosition.Instances)
            {
                camera.UpdateCameraLight(camera == CurrentActiveCamera);
            }
        }

        private IEnumerator RunCameraRender()
        {
            while (m_isActive)
            {
                yield return new WaitForSeconds((float)m_cameraFrameRate / (m_cameraFrameRate * m_cameraFrameRate));

                m_surveillanceCamera.Render();
            }
        }
    }
}
