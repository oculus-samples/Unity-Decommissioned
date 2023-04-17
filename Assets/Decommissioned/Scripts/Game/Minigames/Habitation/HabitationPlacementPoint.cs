// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Utilities;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    public class HabitationPlacementPoint : MonoBehaviour
    {
        [HideInInspector]
        public HabitationMiniGame MiniGameLogic;
        public bool IsOccupied => PlacedItem != null;
        [field: SerializeField] public HabitationObject PlacedItem;
        public int PlacementIndex;
        [SerializeField, AutoSet] internal Rigidbody m_placementPointBody;

        [SerializeField] private MeshRenderer m_pedestalLight;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out HabitationObject miniGameItem))
            {
                MiniGameLogic.OnItemInserted(miniGameItem, PlacementIndex);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out HabitationObject miniGameItem))
            {
                MiniGameLogic.OnItemRemoved(miniGameItem, PlacementIndex);
            }
        }

        public void ChangePedestalColor(Color newColor)
        {
            if (m_pedestalLight)
            {
                m_pedestalLight.material.color = newColor;
            }
        }
    }
}
