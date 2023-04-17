// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Packages/com.meta.utilities.watch-window/LICENSE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Meta.Utilities.WatchWindow
{
    public class WatchElement : VisualElement
    {
        private class Data : ScriptableObject
        {
            public string MinValue = "0";
            public string MaxValue = "1";
        }

        private Data m_data = ScriptableObject.CreateInstance<Data>();
        private SerializedObject m_dataObject;

        public void OnEnable(WatchWindow watchWindow, WatchElementSettings settings)
        {
            style.flexShrink = 0;

            m_watchWindow = watchWindow;
            m_settings = settings;

            var tree = Resources.Load<VisualTreeAsset>("WatchElement");
            Clear();
            tree.CloneTree(this);

            m_codeText = this.Q<TextField>("CodeText");
            m_codeText.RegisterCallback<FocusOutEvent>(_ => GenerateNewWatch());
            m_codeText.RegisterCallback<KeyDownEvent>(_ => GenerateNewWatch());
            m_codeText.RegisterCallback<InputEvent>(_ => GenerateNewWatch());
            _ = m_codeText.RegisterValueChangedCallback(_ => GenerateNewWatch());
            m_codeText.RegisterCallback<GeometryChangedEvent>(_ => OnGeometryChanged());

            m_valueContainer = this.Q<IMGUIContainer>("ValueContainer");
            m_valueContainer.onGUIHandler += OnValueGUI;
            m_valueContainer.RegisterCallback<WheelEvent>(OnWheelEvent);

            var watchElement = this.Q<VisualElement>("RootElement");
            var dragLine = this.Q<VisualElement>("DragLine");
            dragLine.AddManipulator(new VerticalResizer(this, 20));

            // do this with a stylesheet
            this.Q<VisualElement>("unity-text-input").style.overflow = Overflow.Visible;

            this.Q<Button>("DeleteButton").clicked += () => watchWindow.Remove(this);
            this.Q<Button>("GraphToggle").clicked += ToggleGraph;
            m_timeModeButton = this.Q<Button>("TimeModeButton");
            m_timeModeButton.clicked += ToggleTimeMode;

            m_selectButton = this.Q<Button>("SelectObjectButton");
            m_selectButton.clicked += OnSelectObjectClicked;

            m_codeLabel = this.Q<Label>("CodeLabel");
            m_playPauseButton = this.Q<Toggle>("PlayPauseButton");

            LoadSettings();

            m_dataObject = new SerializedObject(m_data);
            this.Bind(m_dataObject);
        }

        private void OnSelectObjectClicked()
        {
            Selection.activeObject = m_returnValue as UnityEngine.Object;
        }

        private void ToggleTimeMode()
        {
            m_settings.m_timeMode = m_settings.m_timeMode == WatchElementSettings.TimeMode.Editor ?
                WatchElementSettings.TimeMode.Game : WatchElementSettings.TimeMode.Editor;
            m_timeModeButton.text = m_settings.m_timeMode.ToString().ToUpperInvariant();
            m_values.Clear();
            DirtySettings();
        }

        private void ToggleGraph()
        {
            m_settings.m_showCurves = !m_settings.m_showCurves;
            UpdateGraphVisibility();
            DirtySettings();
        }

        private void UpdateGraphVisibility()
        {
            this.Q("GraphControls").style.display = m_settings.m_showCurves ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void DirtySettings()
        {
            EditorUtility.SetDirty(WatchWindowSettings.Instance);
        }

        private void LoadSettings()
        {
            SetWatch(m_settings.m_code);
            m_codeText.value = m_settings.m_code;
            if (m_settings.m_codeTextWidth != 0)
            {
                m_codeText.style.width = m_settings.m_codeTextWidth;
                this.Q<TwoPaneSplitView>().fixedPaneInitialDimension = m_settings.m_codeTextWidth;
            }
            if (m_settings.m_height != 0)
            {
                style.height = m_settings.m_height;
            }
            m_timeModeButton.text = m_settings.m_timeMode.ToString().ToUpperInvariant();
            UpdateGraphVisibility();
        }

        private WatchWindow m_watchWindow;
        internal WatchElementSettings m_settings;

        public void OnDisable()
        {
            m_valueContainer.onGUIHandler -= OnValueGUI;
        }

        public override void HandleEvent(EventBase evt)
        {
            switch (evt)
            {
                case GeometryChangedEvent:
                    OnGeometryChanged();
                    break;
                default:
                    // Debug.Log(evt);
                    base.HandleEvent(evt);
                    break;
            }
        }

        private void OnGeometryChanged()
        {
            EditorApplication.delayCall += () =>
            {
                m_settings.m_codeTextWidth = m_codeText.resolvedStyle.width;
                m_settings.m_height = resolvedStyle.height;
                DirtySettings();
                // AssetDatabase.SaveAssets();
            };
        }

        private void OnWheelEvent(WheelEvent evt)
        {
            m_settings.m_maxTime += evt.delta.y * WatchWindowSettings.Instance.m_scrollTimeSpeed;
            DirtySettings();
        }

        private enum WatchDataType
        {
            Float,
            Vector,
        }

        private struct WatchData
        {
            public WatchDataType type;
            public float t;
            public float f;
            public Vector3 v3;

            public float MinValue()
            {
                return type switch
                {
                    WatchDataType.Float => f,
                    WatchDataType.Vector => Mathf.Min(v3.x, Mathf.Min(v3.y, v3.z)),
                    _ => throw null,
                };
            }
            public float MaxValue()
            {
                return type switch
                {
                    WatchDataType.Float => f,
                    WatchDataType.Vector => Mathf.Max(v3.x, Mathf.Max(v3.y, v3.z)),
                    _ => throw null,
                };
            }
        }

        private Queue<WatchData> m_values = new();

        private void OnValueGUI()
        {
            var rect = GUILayoutUtility.GetRect(10, 10000, 10, 10000, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (Event.current.type == EventType.Repaint)
            {
                OnRepaint(rect);
            }
        }

        private void OnRepaint(Rect rect)
        {
            // if (m_graphDirty is false)
            //     return;
            m_graphDirty = false;

            GUI.BeginClip(rect);
            GL.PushMatrix();

            GL.Clear(true, false, Color.black);

            if (m_material == null)
                m_material = new Material(Shader.Find("Hidden/Internal-Colored"));

            if (m_material != null && m_settings.m_showCurves)
            {
                _ = m_material.SetPass(0);
                ClearRect(rect, Color.black);
                DrawGrid(rect);
                DrawCurve(rect, Color.yellow, d => d.type == WatchDataType.Float ? d.f : null);
                DrawCurve(rect, Color.red, d => d.type == WatchDataType.Vector ? d.v3.x : null);
                DrawCurve(rect, Color.green, d => d.type == WatchDataType.Vector ? d.v3.y : null);
                DrawCurve(rect, Color.blue, d => d.type == WatchDataType.Vector ? d.v3.z : null);
            }

            GL.PopMatrix();
            GUI.EndClip();
        }

        private void DrawCurve(Rect rect, Color color, Func<WatchData, float?> getData)
        {
            GL.Begin(GL.LINES);
            GL.Color(color);

            var lastTime = m_values.LastOrDefault().t;
            var bounds = m_values.Any() ? Rect.MinMaxRect(
                lastTime - m_settings.m_maxTime,
                m_values.Min(v => v.MinValue()),
                lastTime,
                m_values.Max(v => v.MaxValue())
            ) : Rect.zero;

            if (bounds.height == 0)
                bounds.height = 1;

            void AddPoint(float t, float f)
            {
                t = (t - bounds.min.x) / bounds.size.x * rect.size.x;
                f = (f - bounds.min.y) / bounds.size.y * rect.size.y;
                GL.Vertex3(t, rect.height - f, 0);
            }

            foreach (var (a, b) in m_values.Zip(m_values.Skip(1)))
            {
                if (a.t < lastTime - m_settings.m_maxTime)
                    continue;

                var d0 = getData(a);
                var d1 = getData(b);
                if (d0.HasValue && d1.HasValue)
                {
                    AddPoint(a.t, d0.Value);
                    AddPoint(b.t, d1.Value);
                }
            }

            GL.End();

            m_data.MinValue = $"{bounds.yMin:G4}";
            m_data.MaxValue = $"{bounds.yMax:G4}";
            if (m_dataObject?.targetObject != null)
                _ = m_dataObject.UpdateIfRequiredOrScript();
        }

        private static void DrawGrid(Rect rect)
        {
            GL.Begin(GL.LINES);

            for (var i = 0; i < 16; i++)
            {
                var lineColour = i % 4 == 0 ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.2f, 0.2f, 0.2f);
                GL.Color(lineColour);

                if (i * (rect.width / 16) >= 0 && i * (rect.width / 16) < rect.width)
                {
                    GL.Vertex3(i * (rect.width / 16), 0, 0);
                    GL.Vertex3(i * (rect.width / 16), rect.height, 0);
                }

                if (i * (rect.height / 4) >= 0 && i * (rect.height / 4) < rect.height)
                {
                    GL.Vertex3(0, i * (rect.height / 4), 0);
                    GL.Vertex3(rect.width, i * (rect.height / 4), 0);
                }
            }

            GL.End();
        }

        private static void ClearRect(Rect rect, Color color)
        {
            GL.Begin(GL.QUADS);
            GL.Color(color);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(rect.width, 0, 0);
            GL.Vertex3(rect.width, rect.height, 0);
            GL.Vertex3(0, rect.height, 0);
            GL.End();
        }

        private Func<object> m_watchMethod;
        private static int s_count = 0;
        private Material m_material;

        public void SetWatch(string code)
        {
            if (code is null)
                return;

            m_returnWatchTime = null;

            EditorApplication.delayCall += () =>
            {
                m_settings.m_code = code;
                DirtySettings();
            };

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().
                Where(a => a.IsDynamic == false && string.IsNullOrEmpty(a.Location) == false);

            var parseOptions = new CSharpParseOptions().WithKind(SourceCodeKind.Regular);
            var codeTree = CSharpSyntaxTree.ParseText(code, parseOptions.WithKind(SourceCodeKind.Script));

            var root = (CompilationUnitSyntax)codeTree.GetRoot();
            var finalNode = root.Members.LastOrDefault();
            var body = null as SyntaxNode;
            if (finalNode is not null)
            {
                var node = SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement($"return ({finalNode.ToFullString() ?? "null"});"));
                body = root.WithMembers(SyntaxFactory.List(root.Members.SkipLast(1).Concat(new[] { node })));
            }
            else
            {
                body = SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement($"return ({root.ToFullString() ?? "null"});"));
            }

            var finalCode = WatchWindowSettings.Instance.m_codePrecursor +
                $"public static class Program {{ public static object Run() {{ {body.ToFullString()} }} {m_watchWindow.m_variablesCode} }}";

            var ignoreAccessChecksCode = assemblies.
                Select(a => $"[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"{a.GetName().Name}\")]").
                ListToString("\n") +
                    @"
                        namespace System.Runtime.CompilerServices
                        {
                            [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                            public class IgnoresAccessChecksToAttribute : Attribute
                            {
                                public IgnoresAccessChecksToAttribute(string assemblyName)
                                {
                                    AssemblyName = assemblyName;
                                }

                                public string AssemblyName { get; }
                            }
                    }";
            var ignoreAccessChecksTree = CSharpSyntaxTree.ParseText(ignoreAccessChecksCode, parseOptions);
            var tree = CSharpSyntaxTree.ParseText(finalCode, parseOptions);
            FixUsings(parseOptions, ref tree);

            var references = assemblies.
                Select(a => MetadataReference.CreateFromFile(a.Location));
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).
                WithUsings("UnityEngine", "UnityEditor", "System.Linq").
                WithMetadataImportOptions(MetadataImportOptions.All). // required for ignoring access checks
                WithAllowUnsafe(true); // required for ignoring access checks
            var topLevelBinderFlagsProperty = typeof(CSharpCompilationOptions).
                GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
            topLevelBinderFlagsProperty.SetValue(options, (uint)1 << 22);

            using var dllStream = new MemoryStream();
            using var pdbStream = new MemoryStream();
            var comp = CSharpCompilation.Create("WatchAsm" + s_count++, new[] { ignoreAccessChecksTree, tree }, references, options);
            var result = comp.Emit(dllStream, pdbStream, options: new EmitOptions().WithDebugInformationFormat(DebugInformationFormat.PortablePdb));

            if (result.Success)
            {
                var asm = Assembly.Load(dllStream.ToArray(), pdbStream.ToArray());
                var script = asm.GetTypes().FirstOrDefault(a => a.Name == "Program");
                var entryPointMethod = script.GetMethod("Run", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                var del = (Func<object>)entryPointMethod.CreateDelegate(typeof(Func<object>));

                m_watchMethod = del;

                // EditorApplication.delayCall += AssetDatabase.SaveAssets;
            }
            else
            {
                m_watchMethod = () => result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ListToString();
            }

            lock (this)
            {
                m_currentWatchCode = code;
            }
        }

        private static void FixUsings(CSharpParseOptions parseOptions, ref SyntaxTree tree)
        {
            // if the user adds using statements, they will appear after our variable declarations, which causes a compiler error
            var root = (CompilationUnitSyntax)tree.GetRoot();
            var additionalUsings = CSharpSyntaxTree.ParseText(root.DescendantTrivia().ListToString("")).GetRoot() as CompilationUnitSyntax;
            if (additionalUsings?.Usings.Any() == true)
            {
                var skippedTokensTrivia = root.DescendantTrivia().Where(t => t.IsKind(SyntaxKind.SkippedTokensTrivia));
                root = root.
                    ReplaceTrivia(skippedTokensTrivia, (a, b) => SyntaxFactory.Whitespace(" ")).
                    AddUsings(additionalUsings.Usings.ToArray());
                tree = CSharpSyntaxTree.ParseText(root.ToString(), parseOptions);
            }
        }

        private void UpdateWatch()
        {
            if (m_returnWatchTime is { } prev && prev <= WatchTime && WatchTime - prev < 1.0f / 100)
                return;
            m_returnWatchTime = WatchTime;

            if (m_codeLabel == null)
                return;

            try
            {
                m_returnValue = m_watchMethod?.Invoke();
                if (m_returnValue is UnityEngine.Object retObj)
                {
                    m_codeLabel.text = EditorJsonUtility.ToJson(m_returnValue, true);
                    m_selectButton.visible = true;
                    m_selectButton.text = retObj.name ?? "Object";
                }
                else if (m_returnValue is System.Collections.IEnumerable enumerable and not string)
                {
                    m_codeLabel.text = enumerable.
                        Cast<object>().
                        Take(32). // to prevent slowdowns
                        Select(o => o?.ToString() ?? "NULL").
                        ListToString();
                    m_selectButton.visible = false;
                }
                else
                {
                    m_codeLabel.text = m_returnValue?.ToString() ?? "NULL";
                    m_selectButton.visible = false;
                }

                if (m_playPauseButton.value && ShouldUpdateGraph)
                {
                    var fVal = m_returnValue.AsDecimal();
                    if (fVal.HasValue)
                    {
                        AddValue(WatchTime, (float)fVal.Value);
                    }
                    else if (m_returnValue is Vector3 v3)
                    {
                        AddValue(WatchTime, v3);
                    }
                }
            }
            catch (Exception ex)
            {
                m_selectButton.visible = false;

                try
                {
                    m_codeLabel.text = ex.ToString();
                }
                catch (IndexOutOfRangeException)
                {
                    m_codeLabel.text = ex.Message;
                }
            }
        }

        private float WatchTime => m_settings.m_timeMode == WatchElementSettings.TimeMode.Editor ?
                Time.realtimeSinceStartup : Time.time;

        private bool ShouldUpdateGraph => m_settings.m_timeMode == WatchElementSettings.TimeMode.Editor ||
            EditorApplication.isPlaying;

        private void AddValue(float t, float f)
        {
            m_values.Enqueue(new WatchData { type = WatchDataType.Float, t = t, f = f });
            Reduce();
            m_graphDirty = true;
        }

        private void AddValue(float t, Vector3 v)
        {
            m_values.Enqueue(new WatchData { type = WatchDataType.Vector, t = t, v3 = v });
            Reduce();
            m_graphDirty = true;
        }

        private void Reduce()
        {
            // add some extra buffer so that you can scroll forward and back a bit
            var maxTime = m_values.LastOrDefault().t;
            while (m_values.Any() && m_values.Peek().t + m_settings.m_maxTime * 1.33f < maxTime)
                _ = m_values.Dequeue();
        }

        public void Update()
        {
            UpdateWatch();

            if (m_graphDirty)
                m_valueContainer.MarkDirtyRepaint();
        }

        private string m_currentWatchCode;
        private Task m_generateTask;
        private TextField m_codeText;
        private IMGUIContainer m_valueContainer;
        private Button m_timeModeButton;
        private Button m_selectButton;
        private object m_returnValue;
        private float? m_returnWatchTime;
        private Label m_codeLabel;
        private bool m_graphDirty;
        private Toggle m_playPauseButton;

        public void GenerateNewWatch(bool force = false)
        {
            lock (this)
            {
                var prevTask = m_generateTask ?? Task.CompletedTask;
                m_generateTask = Task.Factory.StartNew(Generate, default, default, TaskScheduler.Default).Unwrap();

                async Task Generate()
                {
                    await prevTask;

                    await Task.Delay(1);

                    var (code, cur) = ("", "");
                    lock (this)
                    {
                        code = m_codeText.value;
                        cur = m_currentWatchCode;
                    }
                    if (cur != code || force)
                    {
                        try
                        {
                            SetWatch(code);
                            GenerateNewWatch();
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            GenerateNewWatch(force);
                        }
                    }
                }
            }
        }
    }
}
