// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Multiplayer.Core;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Oculus.Interaction;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    public class HabitationObject : NetworkBehaviour
    {
        internal readonly NetworkVariable<bool> m_isInPosition = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        internal int m_position = -1;
        internal bool m_isAssignedToStation = false;
        internal bool m_isPickedForAnswer = false;

        internal readonly NetworkVariable<Vector3> m_respawnPoint = new(Vector3.zero);
        [SerializeField, AutoSet] internal SpringJoint m_spring;
        [SerializeField, AutoSet] internal ClientNetworkTransform m_netTransform;
        [SerializeField, AutoSet] internal Rigidbody m_rb;
        [SerializeField, AutoSet] private Grabbable m_grabbable;

        [field: SerializeField] public string ItemName { get; private set; } = "";
        public Vector4 TextureCoords = Vector4.one;
        private PointerEvent? m_currentPointerEvent;

        [SerializeField] private MeshRenderer m_itemMesh;
        private readonly int m_itemColorProperty = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock m_itemMaterialProperties;
        private readonly NetworkVariable<Color> m_itemColor = new(Color.white);

        [SerializeField] private AudioSource m_collisionAudio;

        public Color CurrentColor => m_itemColor.Value;

        private void Awake()
        {
            m_itemMaterialProperties = new();

            if (m_grabbable) { m_grabbable.WhenPointerEventRaised += OnPointerEventRaised; }

            GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
            m_isInPosition.OnValueChanged += OnItemPositionStatusChanged;
        }

        private void OnPhaseChanged(Phase newPhase)
        {
            if (newPhase != Phase.Discussion || !IsServer) { return; }
            NetworkObject.ChangeOwnership(PlayerId.ServerPlayerId());
            m_isInPosition.Value = false;
        }

        private void OnEnable() =>
            m_itemColor.OnValueChanged += OnItemColorChanged;

        private void OnDisable() =>
            m_itemColor.OnValueChanged -= OnItemColorChanged;

        private void OnItemPositionStatusChanged(bool previousValue, bool newValue)
        {
            if (m_collisionAudio) { m_collisionAudio.mute = newValue; }
        }

        private void OnPointerEventRaised(PointerEvent pointerEvent)
        {
            switch (pointerEvent.Type)
            {
                case PointerEventType.Hover:
                    break;
                case PointerEventType.Unhover:
                    break;
                case PointerEventType.Select:
                    m_currentPointerEvent = pointerEvent;
                    break;
                case PointerEventType.Unselect:
                    m_currentPointerEvent = null;
                    break;
                case PointerEventType.Move:
                    break;
                case PointerEventType.Cancel:
                    m_currentPointerEvent = null;
                    break;
                default:
                    break;
            }
        }

        private void OnItemColorChanged(Color oldColor, Color newColor) => SetItemColor(newColor);

        public void RespawnItem()
        {
            if (!IsOwner) { return; }
            m_netTransform.Teleport(m_respawnPoint.Value, Quaternion.identity, transform.localScale);
            m_rb.linearVelocity = Vector3.zero;
        }

        public void SetItemColor(Color color, bool setValue = false)
        {
            if (IsServer && setValue)
            {
                m_itemColor.Value = color;
                return;
            }

            if (!IsServer && setValue)
            {
                Debug.LogError("Tried to set a Simon Says Mini Game Item color as a client!");
                return;
            }

            m_itemMaterialProperties.SetColor(m_itemColorProperty, color);
            m_itemMesh.SetPropertyBlock(m_itemMaterialProperties);
        }

        public void UngrabItem()
        {
            if (IsBeingGrabbed())
            {
                var ungrabEvent = new PointerEvent(m_currentPointerEvent.Value.Identifier, PointerEventType.Cancel, m_currentPointerEvent.Value.Pose);
                m_grabbable.ProcessPointerEvent(ungrabEvent);
            }
        }

        public bool IsBeingGrabbed() => m_currentPointerEvent.HasValue;
    }
}
