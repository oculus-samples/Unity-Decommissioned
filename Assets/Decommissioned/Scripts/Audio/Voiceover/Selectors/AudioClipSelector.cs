// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using Meta.XR.Samples;
using UnityEngine;

namespace Meta.Decommissioned.Audio.Voiceover
{
    /// <summary>
    /// Base class for audio-clip selection logic.
    /// </summary>
    /// <remarks></remarks>
    [MetaCodeSample("Decommissioned")]
    public abstract class AudioClipSelector : ScriptableObject
    {
        public abstract bool TrySelectClip(out int clipIndex);

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Index of selected clip.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if no clip was selected.</exception>
        public virtual int SelectClip() =>
            TrySelectClip(out var index) ? index : throw new IndexOutOfRangeException();
    }
}
