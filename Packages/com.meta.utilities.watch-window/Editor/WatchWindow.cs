// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Packages/com.meta.utilities.watch-window/LICENSE

using System.Linq;
using Microsoft.CodeAnalysis;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Meta.Utilities.WatchWindow
{
    public class WatchWindow : EditorWindow
    {
        private VisualElement m_elements;

        public static WatchWindow Instance;

        [MenuItem("Window/Analysis/Watch Window _%#W")]
        public static void ShowWatchWindow()
        {
            var consoleWindow = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
            var window = GetWindow<WatchWindow>("Watch", true, consoleWindow != null ? new[] { consoleWindow } : null);
            window.minSize = new Vector2(700, 250);
            window.Show();
        }

        public void OnEnable()
        {
            OnDisable();
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            var toolbarXml = Resources.Load<VisualTreeAsset>("WatchToolbar");
            var toolbar = toolbarXml.CloneTree();
            toolbar.Q<ToolbarButton>("NewWatchButton").clicked += CreateNewWatch;
            toolbar.Q<ToolbarButton>("NewVariableButton").clicked += CreateNewVariable;
            toolbar.Q<ToolbarButton>("SettingsButton").clicked += OpenSettings;
            rootVisualElement.Add(toolbar);

            var list = new ScrollView();
            list.style.flexGrow = 1;
            rootVisualElement.Add(list);

            m_elements = new VisualElement();
            m_elements.style.flexGrow = 1;
            list.Add(m_elements);

            LoadSettings();
            UpdateVariablesCode();
        }

        private void OpenSettings()
        {
            _ = SettingsService.OpenUserPreferences(WatchWindowSettings.PATH);
        }

        private void LoadSettings()
        {
            foreach (var setting in WatchWindowSettings.Instance.m_variables)
            {
                var varEl = new WatchVariableElement();
                m_elements.hierarchy.Add(varEl);
                varEl.OnEnable(this, setting);
            }

            foreach (var setting in WatchWindowSettings.Instance.m_watches)
            {
                var el = new WatchElement();
                m_elements.hierarchy.Add(el);
                el.OnEnable(this, setting);
            }
        }

        private void CreateNewVariable()
        {
            var variable = new WatchVariableElement();
            m_elements.hierarchy.Insert(m_elements.hierarchy.Children().OfType<WatchVariableElement>().Count(), variable);

            var settings = new WatchVariableSettings();
            WatchWindowSettings.Instance.m_variables.Add(settings);
            variable.OnEnable(this, settings);
        }

        private void CreateNewWatch()
        {
            var watch = new WatchElement();
            m_elements.hierarchy.Add(watch);

            var settings = new WatchElementSettings();
            WatchWindowSettings.Instance.m_watches.Add(settings);
            watch.OnEnable(this, settings);
        }

        public void OnDisable()
        {
            if (m_elements == null)
                return;

            // AssetDatabase.SaveAssets();

            foreach (var el in m_elements.hierarchy.Children().OfType<WatchElement>())
                el?.OnDisable();
        }

        public void Update()
        {
            Instance = this;

            if (m_elements == null)
                return;

            foreach (var el in m_elements.hierarchy.Children().OfType<WatchElement>())
                el?.Update();
        }

        public void Remove(WatchVariableElement variable)
        {
            _ = WatchWindowSettings.Instance.m_variables.Remove(variable.m_settings);
            EditorUtility.SetDirty(WatchWindowSettings.Instance);

            m_elements.hierarchy.Remove(variable);
        }

        public void Remove(WatchElement watchElement)
        {
            _ = WatchWindowSettings.Instance.m_watches.Remove(watchElement.m_settings);
            EditorUtility.SetDirty(WatchWindowSettings.Instance);

            watchElement.OnDisable();
            m_elements.hierarchy.Remove(watchElement);
        }

        public string m_variablesCode;
        public void UpdateVariablesCode()
        {
            m_variablesCode = WatchWindowSettings.Instance.m_variables.
                Where(v => v.Target != null).
                Select(v => (v.Target.GetType().FullName, v.m_identifier)).
                Select(t => $"static {t.FullName} {t.m_identifier} => ({t.FullName})Meta.Utilities.WatchWindow.WatchVariables.Get(\"{t.m_identifier}\");\n").
                ListToString(@"
            ");

            foreach (var watch in m_elements.hierarchy.Children().OfType<WatchElement>())
                watch.GenerateNewWatch(true);
        }

        public void UpdateVariableTargets()
        {
            foreach (var el in m_elements.hierarchy.Children().OfType<WatchVariableElement>())
                if (el.m_settings.Target != null)
                    el.UpdateTarget(el.m_settings.Target);
        }
    }
}
