// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Linq;
using Meta.Decommissioned.ScriptableObjects;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static Meta.Decommissioned.ScriptableObjects.PlayerColorConfig;

namespace Meta.Decommissioned.Player
{
    /// <summary>
    /// Manages the color assigned to each player when they join a game. This color is used to distinguish players
    /// in various contexts during a match.
    /// </summary>
    public class PlayerColor : NetworkMultiton<PlayerColor>
    {
        private readonly NetworkVariable<GameColor> m_currentColor = new();

        [SerializeField] private PlayerColorConfig m_colorConfig;

        public GameColor GameColor => m_currentColor.Value;
        public Color Color => m_colorConfig.GetColorFromGameColor(GameColor);
        public UnityEvent<Color> OnColorChanged;

        public static PlayerColor GetByPlayerId(PlayerId id) => GetByPlayerObject(id.Object);

        public static PlayerColor GetByPlayerObject(NetworkObject player) =>
            Instances.FirstOrDefault(p => p.NetworkObject == player);

        protected new void Awake()
        {
            base.Awake();
            m_currentColor.OnValueChanged += OnCurrentColorChanged;
        }

        private void OnCurrentColorChanged(GameColor previousValue, GameColor newValue) => OnColorChanged?.Invoke(Color);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            OnCurrentColorChanged(default, m_currentColor.Value);
        }

        public Color MultiplyColorFromGameColor(float multiplier)
        {
            var gameColor = Color;
            gameColor.r *= multiplier;
            gameColor.g *= multiplier;
            gameColor.b *= multiplier;
            return gameColor;
        }

        public void SetPlayerColor(GameColor color)
        {
            if (IsServer)
            {
                m_currentColor.Value = color;
            }
            else
            {
                Debug.LogError("Tried to assign a player a color when we aren't the server!");
            }
        }
    }
}
