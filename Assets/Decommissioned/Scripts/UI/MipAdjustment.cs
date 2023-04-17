// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meta.Decommissioned.UI
{
    public class MipAdjustment : MonoBehaviour
    {
        [SerializeField] private List<Adjustment> m_textureAdjustments;

        private void Start()
        {
            foreach (var adjustment in m_textureAdjustments)
            {
                adjustment.Apply();
            }
        }

        [Serializable]
        public class Adjustment
        {
            [SerializeField] private Texture2D m_texture;
            [SerializeField] private float m_bias;

            public void Apply()
            {
                m_texture.mipMapBias = m_bias;
            }
        }
    }
}
