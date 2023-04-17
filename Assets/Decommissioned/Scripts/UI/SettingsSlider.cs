// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.Decommissioned.Player;
using Meta.Utilities;
using Meta.Utilities.Input;
using NaughtyAttributes;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.UI
{
    public enum SliderAxis
    {
        X,
        Y,
        Z
    }

    /// <summary>
    /// Defines a grabbable slider that behaves to interface with a specified player setting.
    /// </summary>
    public class SettingsSlider : OneGrabFreeTransformer, ITransformer
    {
        [SerializeField] private string m_settingName = "";
        [SerializeField] private SliderAxis m_axis = SliderAxis.X;
        [SerializeField] private float m_maxSliderDistance = -0.25f;
        [SerializeField] private UnityEvent<float> m_sliderTargets;
        private Transform m_sliderTransform;
        private float m_zeroPosition;
        private Vector3 m_currentSliderPosition;
        private Quaternion m_originalSliderRotation;
        private bool m_isBeingChanged;

        [SerializeField] private Renderer m_renderer;
        [ShowNonSerializedField] private bool m_isHovering;
        [ShowNonSerializedField] private bool m_isChangingHovering;
        [ShowNonSerializedField] private float m_isHoveringT;

        private void Awake()
        {
            m_sliderTransform = transform.GetChild(0);

            if (m_sliderTransform == null)
            {
                Debug.LogError("Unable to initialize a slider: missing child object. Please make sure the slider's first child object is the grabbable object.");
                gameObject.SetActive(false);
            }

            m_currentSliderPosition = m_sliderTransform.localPosition;
            m_originalSliderRotation = m_sliderTransform.localRotation;

            m_zeroPosition = m_axis switch
            {
                SliderAxis.X => m_sliderTransform.localPosition.x,
                SliderAxis.Y => m_sliderTransform.localPosition.y,
                SliderAxis.Z => m_sliderTransform.localPosition.z,
                _ => m_sliderTransform.localPosition.x,
            };

            if (m_zeroPosition + m_maxSliderDistance == 0)
            {
                Debug.LogWarning("A Slider had a max value total of 0! This will cause division by zero errors. The max distance will be slightly offset to avoid this. Please ensure your max slider distance is set to something other than the rest position of the slider!");
                m_maxSliderDistance += 0.001f;
            }

            if (UserSettings.Instance != null && UserSettings.Instance.GetPlayerSetting(m_settingName, out float currentSetting))
            {
                var sliderPos = currentSetting * (m_zeroPosition + m_maxSliderDistance);
                SetSliderPosition(sliderPos);
            }
            else
            {
                SetSliderPosition(1 * (m_zeroPosition + m_maxSliderDistance));
            }
        }

        public void SetIsHovering(bool isHovering)
        {
            m_isHovering = isHovering;
            if (!m_isChangingHovering)
            {
                _ = StartCoroutine(HoveringRoutine());
            }

            if (HandRefHelper.Instance.LeftHandAnchor.Hand is SyntheticHand hand)
            {
                if (isHovering)
                {
                    if (hand.GetRootPose(out var pose))
                    {
                        hand.LockWristPose(pose, 0.75f, worldPose: true);
                    }
                }
                else
                {
                    hand.FreeWrist();
                }
            }
        }

        private IEnumerator HoveringRoutine()
        {
            m_renderer.sharedMaterial.EnableKeyword("_EMISSION");

            m_isChangingHovering = true;

            var block = new MaterialPropertyBlock();
            do
            {
                var dir = m_isHovering ? 1 : -1;
                m_isHoveringT += Time.deltaTime * dir * 4;

                UpdateColor();
                yield return null;
            }
            while (m_isHoveringT is > 0 and < 1);

            m_isHoveringT = m_isHovering ? 1 : 0;
            UpdateColor();

            m_isChangingHovering = false;

            void UpdateColor()
            {
                m_renderer.GetPropertyBlock(block);

                var color = Color.white * m_isHoveringT * 0.5f;
                color.a = 1;
                block.SetColor("_EmissionColor", color);

                m_renderer.SetPropertyBlock(block);
            }
        }

        private void UpdateSliderPosition()
        {
            m_currentSliderPosition.x = m_axis == SliderAxis.X ? m_sliderTransform.localPosition.x : 0;
            m_currentSliderPosition.y = m_axis == SliderAxis.Y ? m_sliderTransform.localPosition.y : 0;
            m_currentSliderPosition.z = m_axis == SliderAxis.Z ? m_sliderTransform.localPosition.z : 0;

            if (m_maxSliderDistance < 0)
            {
                var sliderPos = GetSliderPosition();
                if (sliderPos < m_maxSliderDistance)
                {
                    ClampSlider(true);
                }
                else if (sliderPos > 0)
                {
                    ClampSlider(false);
                }
            }
            else if (m_maxSliderDistance > 0)
            {
                var sliderPos = GetSliderPosition();
                if (sliderPos > m_maxSliderDistance)
                {
                    ClampSlider(true);
                }
                else if (sliderPos < 0)
                {
                    ClampSlider(false);
                }
            }

            if (m_isBeingChanged)
            {
                UpdateSliderSetting();
            }

            m_sliderTransform.localPosition = m_currentSliderPosition;
            m_sliderTransform.localRotation = m_originalSliderRotation;
        }

        private float GetSliderPosition() =>
            m_axis switch
            {
                SliderAxis.X => m_currentSliderPosition.x,
                SliderAxis.Y => m_currentSliderPosition.y,
                SliderAxis.Z => m_currentSliderPosition.z,
                _ => m_currentSliderPosition.x,
            };
        private void SetSliderPosition(float position)
        {
            switch (m_axis)
            {
                case SliderAxis.X:
                    m_currentSliderPosition.x = position;
                    break;
                case SliderAxis.Y:
                    m_currentSliderPosition.y = position;
                    break;
                case SliderAxis.Z:
                    m_currentSliderPosition.z = position;
                    break;
                default:
                    m_currentSliderPosition.x = position;
                    break;
            }
            m_sliderTransform.localPosition = m_currentSliderPosition;
        }

        private float GetSliderPercentage() => GetSliderPosition() / (m_zeroPosition + m_maxSliderDistance);

        private void ClampSlider(bool isAtMax)
        {
            switch (m_axis)
            {
                case SliderAxis.X:
                    m_currentSliderPosition.x = isAtMax ? m_zeroPosition + m_maxSliderDistance : m_zeroPosition;
                    break;
                case SliderAxis.Y:
                    m_currentSliderPosition.y = isAtMax ? m_zeroPosition + m_maxSliderDistance : m_zeroPosition;
                    break;
                case SliderAxis.Z:
                    m_currentSliderPosition.z = isAtMax ? m_zeroPosition + m_maxSliderDistance : m_zeroPosition;
                    break;
                default:
                    m_currentSliderPosition.x = isAtMax ? m_zeroPosition + m_maxSliderDistance : m_zeroPosition;
                    break;
            }
        }

        private void SaveSliderSetting()
        {
            if (!m_settingName.IsNullOrEmpty())
            {
                UserSettings.Instance.SetValue(m_settingName, GetSliderPercentage());
            }
        }

        private void UpdateSliderSetting()
        {
            m_sliderTargets.Invoke(GetSliderPercentage());
        }

        void ITransformer.Initialize(IGrabbable grabbable)
        {
            Initialize(grabbable);
        }

        void ITransformer.BeginTransform()
        {
            BeginTransform();
            m_isBeingChanged = true;
        }

        void ITransformer.UpdateTransform()
        {
            UpdateTransform();
            UpdateSliderPosition();
        }

        void ITransformer.EndTransform()
        {
            EndTransform();
            m_isBeingChanged = false;
            SaveSliderSetting();
        }
    }
}
