// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.XR.Samples;
using TMPro;
using UnityEngine;

namespace Meta.Decommissioned.UI
{
    [MetaCodeSample("Decommissioned")]
    public class ConnectionDots : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_dotsText;
        private string m_text = "";

        private void AnimateDots()
        {
            _ = StartCoroutine(Dots());

            IEnumerator Dots()
            {
                yield return new WaitForSecondsRealtime(1f);

                if (m_text.Length > 2)
                {
                    m_text = "";
                }

                m_text += ".";
                m_dotsText.text = m_text;
                AnimateDots();
            }
        }
        private void Start()
        {
            AnimateDots();
        }
    }
}
