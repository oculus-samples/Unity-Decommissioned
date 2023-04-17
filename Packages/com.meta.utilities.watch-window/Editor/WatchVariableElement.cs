// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Packages/com.meta.utilities.watch-window/LICENSE

using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Meta.Utilities.WatchWindow
{
    public static class WatchVariables
    {
        public static object Get(string identifier) => WatchWindowSettings.Instance.m_variables.First(v => v.m_identifier == identifier).Target;
    }

    public class WatchVariableElement : VisualElement
    {
        private WatchWindow m_watchWindow;
        internal WatchVariableSettings m_settings;
        private TextField m_identifier;
        private ObjectField m_object;

        public void OnEnable(WatchWindow watchWindow, WatchVariableSettings settings)
        {
            style.flexShrink = 0;

            m_watchWindow = watchWindow;
            m_settings = settings;
            m_settings.m_cachedTarget = m_settings.Target; // ensure it's cached

            var tree = Resources.Load<VisualTreeAsset>("WatchVariable");
            Clear();
            tree.CloneTree(this);

            m_identifier = this.Q<TextField>("Identifier");
            m_identifier.value = m_settings.m_identifier;
            _ = m_identifier.RegisterValueChangedCallback(evt => UpdateIdentifier(evt.newValue));

            m_object = this.Q<ObjectField>("Object");
            m_object.value = m_settings.Target;
            _ = m_object.RegisterValueChangedCallback(evt => UpdateTarget(evt.newValue));

            this.Q<Button>("DeleteButton").clicked += () => watchWindow.Remove(this);
        }

        private void UpdateIdentifier(string identifier)
        {
            m_settings.m_identifier = identifier;
            EditorUtility.SetDirty(WatchWindowSettings.Instance);
            m_watchWindow.UpdateVariablesCode();
        }

        internal void UpdateTarget(Object target)
        {
            m_settings.Target = target;
            if (m_object.value != target)
            {
                m_object.value = target;
                MarkDirtyRepaint();
                EditorUtility.SetDirty(WatchWindowSettings.Instance);
                m_watchWindow.UpdateVariablesCode();
            }
        }
    }
}
