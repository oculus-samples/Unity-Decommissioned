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
    [CreateAssetMenu(menuName = "Decommissioned/Audio/Clip by Role")]
    public class ClipForRole : AudioClipSelector
    {
        /// <summary>
        /// Decide which clip to play, based on user's role.
        /// </summary>
        /// <param name="clipIndex">Index of the clip to play.</param>
        /// <returns>0 for crew, 1 for moles.</returns>
        public override bool TrySelectClip(out int clipIndex)
        {
            var localPlayerId = PlayerManager.LocalPlayerId;
            var localPlayerRole = PlayerRole.GetByPlayerId(localPlayerId).CurrentRole;
            clipIndex = localPlayerRole == Role.Mole ? 1 : 0;
            return true;
        }
    }
}
