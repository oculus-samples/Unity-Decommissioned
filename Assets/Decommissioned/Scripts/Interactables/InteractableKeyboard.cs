// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Interactables
{
    /// <summary>
    /// Small class storing configuration and values for a key on a virtual keyboard.
    /// </summary>
    [MetaCodeSample("Decommissioned")]
    [Serializable]
    public class KeyboardKey
    {
        [field: SerializeField] public Transform KeyTransform { get; private set; }
        [field: SerializeField] public string KeyText { get; private set; }
        internal bool m_isAnimating;
        internal float m_keyAnimTime;
        internal Vector3 m_restPosition;

        public KeyboardKey(Transform keyTransform, string keyText)
        {
            KeyTransform = keyTransform;
            KeyText = keyText;
            m_isAnimating = false;
            m_keyAnimTime = 0f;
            m_restPosition = keyTransform.localPosition;
        }
    }

    /// <summary>
    /// Encapsulates configuration and behavior for a virtual keyboard players can interact with during the game.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractableKeyboard : MonoBehaviour
    {
        [SerializeField] private AnimationCurve m_keyboardPressCurve;
        [SerializeField] private List<KeyboardKey> m_keyboardKeys = new();
        [SerializeField] private AudioTrigger m_typingAudio;

        public UnityEvent OnKeyPressed;

        private void Awake() => InitKeys();

        private void InitKeys()
        {
            foreach (var key in m_keyboardKeys)
            {
                key.m_restPosition = key.KeyTransform.localPosition;
                key.m_isAnimating = false;
                key.m_keyAnimTime = 0f;
            }
        }

        private void PressKeyboardKey(KeyboardKey key)
        {
            var keyIndex = m_keyboardKeys.IndexOf(key);

            if (keyIndex == -1)
            {
                Debug.LogError($"Tried to press the keyboard key {key.KeyText} on a keyboard, but that key doesn't exist in the list!");
                return;
            }

            var keyToPress = m_keyboardKeys[keyIndex];

            if (!keyToPress.m_isAnimating)
            {
                keyToPress.m_keyAnimTime = 0f;
                keyToPress.m_isAnimating = true;
            }

            m_typingAudio.PlayAudio();
        }

        public void PressKeyboardKey(string key)
        {
            var filteredKey = GetSpecialCharacters(key);
            var keyToPress = m_keyboardKeys.FirstOrDefault(keyboardKey => keyboardKey.KeyText == filteredKey.ToUpper());

            if (keyToPress == null)
            {
                Debug.LogError($"Tried to press the keyboard key {key} on a keyboard, but that key doesn't exist in the list!");
                return;
            }

            PressKeyboardKey(keyToPress);
        }

        private void Update()
        {
            foreach (var key in m_keyboardKeys)
            {
                if (!key.m_isAnimating) { continue; }

                key.m_keyAnimTime += Time.deltaTime;
                var keyCurveValue = m_keyboardPressCurve.Evaluate(key.m_keyAnimTime);
                key.KeyTransform.localPosition = key.m_restPosition - new Vector3(0, keyCurveValue, 0);

                if (key.m_keyAnimTime <= m_keyboardPressCurve.keys.Last().time) { continue; }
                key.m_isAnimating = false;
                key.m_keyAnimTime = 0;
                key.KeyTransform.localPosition = key.m_restPosition;
            }
        }

        private void OnTriggerEnter(Collider other) => OnKeyPressed?.Invoke();

        private string GetSpecialCharacters(string input)
        {
            return input switch
            {
                "." => ",",
                "\"" => ";",
                "/" => ",",
                ":" => ";",
                "-" => "_",
                "=" => "+",
                "*" => "8",
                "!" => "1",
                _ => input,
            };
        }
    }
}
