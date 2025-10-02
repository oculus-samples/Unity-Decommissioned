// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using Meta.Decommissioned.Player;
using Meta.Multiplayer.PlayerManagement;
using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.Audio.Voiceover
{
    [MetaCodeSample("Decommissioned")]
    [CreateAssetMenu(menuName = "Decommissioned/Audio/Planning Phase VO")]
    public class PlanningPhaseAudio : AudioClipSelector
    {
        public override bool TrySelectClip(out int clipIndex)
        {
            var localPlayerId = PlayerManager.LocalPlayerId;
            var isCommander = PlayerStatus.GetByPlayerId(localPlayerId).CurrentStatus == PlayerStatus.Status.Commander;
            if (isCommander)
            {
                clipIndex = 0;
                return true;
            }
            var localPlayerRole = PlayerRole.GetByPlayerId(localPlayerId).CurrentRole;
            clipIndex = localPlayerRole == Role.Mole ? 2 : 1;
            return true;
        }
    }
}
