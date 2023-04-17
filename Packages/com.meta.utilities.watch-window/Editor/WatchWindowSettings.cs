// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Packages/com.meta.utilities.watch-window/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Meta.Utilities.WatchWindow
{
    [Serializable]
    public class WatchVariableSettings
    {
        public string m_identifier;
        public string m_target;
        public Extensions.ScenePathNode[] m_scenePath;
        [NonSerialized] private int? m_cachedInstanceId;
        [NonSerialized] internal UnityEngine.Object m_cachedTarget;

        public UnityEngine.Object Target
        {
            get
            {
                if (m_cachedTarget != null)
                    return m_cachedTarget;

                m_cachedTarget = null;

                if (m_cachedInstanceId.HasValue)
                {
                    m_cachedTarget = EditorUtility.InstanceIDToObject(m_cachedInstanceId.Value);
                    if (m_cachedTarget != null)
                        return m_cachedTarget;
                }

                if (m_target?.ParseGlobalObjectId() is { } id)
                {
                    m_cachedTarget = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
                    if (m_cachedTarget != null)
                    {
                        m_cachedInstanceId = m_cachedTarget.GetInstanceID();
                        return m_cachedTarget;
                    }
                }

                if (m_scenePath.GetObject() is { } obj)
                {
                    m_cachedTarget = obj;
                    m_cachedInstanceId = obj.GetInstanceID();

                    if (WatchWindow.Instance != null)
                        WatchWindow.Instance.UpdateVariableTargets();
                }
                else
                {
                    m_cachedTarget = null;
                    m_cachedInstanceId = null;
                }

                return m_cachedTarget;
            }
            set
            {
                if (m_cachedTarget != value && (value != null || m_cachedTarget != null))
                {
                    m_cachedTarget = value;
                    m_target = GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
                    m_scenePath = m_cachedTarget.GetScenePath().ToArray();
                }
            }
        }
    }

    [Serializable]
    public class WatchElementSettings
    {
        public enum TimeMode
        {
            Editor,
            Game
        }
        public string m_code;
        public float m_codeTextWidth = 0;
        public float m_height = 0;
        public float m_maxTime = 10;
        public bool m_showCurves = false;
        public TimeMode m_timeMode = TimeMode.Editor;
    }

    public class WatchWindowSettings : ScriptableObject
    {
        internal static string AssetsFolder => "Assets";
        internal static string EditorFolder => "Editor";
        internal static string AssetPath => $"{AssetsFolder}/{EditorFolder}/WatchWindowSettings.asset";

        public const string PATH = "Preferences/Watch Window";
        private static WatchWindowSettings s_instance;
        internal static WatchWindowSettings Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = AssetDatabase.LoadAssetAtPath<WatchWindowSettings>(AssetPath);
                    if (s_instance == null)
                    {
                        s_instance = CreateInstance<WatchWindowSettings>();
                        _ = AssetDatabase.CreateFolder(AssetsFolder, EditorFolder);
                        AssetDatabase.CreateAsset(s_instance, AssetPath);
                        AssetDatabase.SaveAssets();
                    }
                }
                return s_instance;
            }
        }

        private static Editor s_editor;

        [HideInInspector]
        public List<WatchVariableSettings> m_variables = new();

        [HideInInspector]
        public List<WatchElementSettings> m_watches = new();

        [TextArea(8, 32)]
        public string m_codePrecursor = @"
using static UnityEngine.Mathf;
using static UnityEngine.Time;
using static UnityEditor.Selection;
using static UnityEngine.GameObject;
using static UnityEngine.Vector3;
";

        public float m_scrollTimeSpeed = 0.1f;

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            try
            {
                return new SettingsProvider(PATH, SettingsScope.User)
                {
                    guiHandler = searchContext =>
                    {
                        Editor.CreateCachedEditor(Instance, null, ref s_editor);
                        s_editor.OnInspectorGUI();
                        _ = s_editor.serializedObject.ApplyModifiedProperties();
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw ex;
            }
        }
    }
}
