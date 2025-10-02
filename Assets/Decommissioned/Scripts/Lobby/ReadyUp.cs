// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.ScriptableObjects;
using Meta.Multiplayer.PlayerManagement;
using Meta.XR.Samples;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Lobby
{
    [MetaCodeSample("Decommissioned")]
    public class ReadyUp : MonoBehaviour
    {
        [SerializeField] private PlayerReadyUpEvent m_onPlayerReadyUp;

        [NonSerialized]
        public bool IsPlayerReady;

        public event Action<bool, PlayerId> OnPlayerReady;

        public void Start()
        {
            IsPlayerReady = false;
            GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
        }

        private void OnPhaseChanged(Phase phase) => SetReadyPlayerState(false);

        /**
         * Set whether the local player is ready or not.
         * <param name="isReady">A boolean assigning a state of readiness (true) or un-readiness to the local player.</param>
         */
        public void SetReadyPlayerState(bool isReady)
        {
            if (NetworkManager.Singleton == null) { return; }
            IsPlayerReady = isReady;
            var playerId = PlayerManager.LocalPlayerId;

            OnPlayerReady?.Invoke(IsPlayerReady, playerId);
            m_onPlayerReadyUp.Raise(new ReadyStatus
            {
                PlayerId = playerId,
                IsPlayerReady = IsPlayerReady
            });
        }

        /**
         * Struct storing the status of the local player,
         * including their ID and whether or not they
         * are ready.
         */
        [Serializable]
        public struct ReadyStatus
        {
            [SerializeField]
            public PlayerId PlayerId;
            [SerializeField]
            public bool IsPlayerReady;
        }
    }
}
