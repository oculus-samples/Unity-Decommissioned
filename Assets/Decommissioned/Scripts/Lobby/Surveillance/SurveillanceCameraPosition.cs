// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Linq;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Utilities;
using UnityEngine;

namespace Meta.Decommissioned.Surveillance
{
    public class SurveillanceCameraPosition : Multiton<SurveillanceCameraPosition>
    {
        [SerializeField]
        private MiniGameRoom m_residingRoom = MiniGameRoom.None;
        public MiniGameRoom ResidingRoom => m_residingRoom;
        [SerializeField]
        private float m_farClipDistance = 15f;
        public float FarClipDistance => m_farClipDistance;
        [SerializeField]
        private float m_fov = 70f;
        public float FOV => m_fov;
        [SerializeField]
        private MeshRenderer m_cameraLight;
        [SerializeField]
        private Transform m_cameraPosition;
        public Transform CameraPosition => m_cameraPosition;
        private MaterialPropertyBlock m_cameraLightMaterialBlock;
        private int m_cameraLightColorProperty = Shader.PropertyToID("_BaseColor");

        public static SurveillanceCameraPosition GetByMiniGameRoom(MiniGameRoom room) => Instances.FirstOrDefault(cam => cam.ResidingRoom == room);

        private void Start()
        {
            m_cameraLightMaterialBlock = new();
            m_cameraLightMaterialBlock.SetColor(m_cameraLightColorProperty, Color.black);
            m_cameraLight.SetPropertyBlock(m_cameraLightMaterialBlock);
        }

        public void UpdateCameraLight(bool on)
        {
            m_cameraLightMaterialBlock.SetColor(m_cameraLightColorProperty, on ? Color.red : Color.black);
            m_cameraLight.SetPropertyBlock(m_cameraLightMaterialBlock);
        }
    }
}
