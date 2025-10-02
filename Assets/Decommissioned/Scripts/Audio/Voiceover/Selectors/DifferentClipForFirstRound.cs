// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.Audio.Voiceover
{
    [MetaCodeSample("Decommissioned")]
    [CreateAssetMenu(menuName = "Decommissioned/Audio/First Round Different")]
    public class DifferentClipForFirstRound : AudioClipSelector
    {
        public override bool TrySelectClip(out int clipIndex)
        {
            clipIndex = GameManager.Instance.CurrentRoundCount == 1 ? 0 : 1;
            return true;
        }
    }
}
