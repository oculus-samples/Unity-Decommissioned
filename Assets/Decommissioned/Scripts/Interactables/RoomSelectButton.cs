// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Decommissioned.Surveillance;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Interactables
{
    /// <summary>
    /// A button that, when pressed, causes the <see cref="SurveillanceSystem"/> to display the associated
    /// <see cref="MiniGameRoom"/>.
    /// </summary>
    public class RoomSelectButton : MonoBehaviour
    {
        [SerializeField] private MeshRenderer m_button;

        [field: SerializeField] public MiniGameRoom MiniGameRoom { get; set; }

        [SerializeField] private Material[] m_activeMaterials;
        [SerializeField] private Material[] m_inactiveMaterials;

        [SerializeField] private UnityEvent m_setActive = new();
        [SerializeField] private UnityEvent m_setInactive = new();

        public Action OnPress;

        public void Press()
        {
            OnPress?.Invoke();
            SurveillanceSystem.Instance.SwitchToRoom_ServerRpc(MiniGameRoom);
        }

        public void SetActive(bool active)
        {
            if (active)
            {
                m_setActive.Invoke();
            }
            else
            {
                m_setInactive.Invoke();
            }
        }

        public void SetActiveMaterials() => SetMaterials(m_activeMaterials);

        public void SetInactiveMaterials() => SetMaterials(m_inactiveMaterials);

        private void SetMaterials(Material[] newMaterials) => m_button.materials = newMaterials;
    }
}
