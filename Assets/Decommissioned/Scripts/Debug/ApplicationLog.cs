// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections.Generic;
using Meta.Utilities;
using Meta.XR.Samples;
using TMPro;
using UnityEngine;

namespace Meta.Decommissioned.Logging
{
    [MetaCodeSample("Decommissioned")]
    public class ApplicationLog : Singleton<ApplicationLog>
    {
        private const int MAX_CONSOLE_LINES = 15;
        private const float MAX_CONSOLE_WIDTH = 400.0f;
        private const float MAX_WORD_WIDTH = MAX_CONSOLE_WIDTH - 115.0f;

        private static uint s_logID;
        private static readonly Queue<string> s_consoleLines = new(MAX_CONSOLE_LINES);

        private static readonly Dictionary<LogType, string> s_logType = new()
    {
        { LogType.Error, "ERR| " },
        { LogType.Assert, "AST| " },
        { LogType.Warning, "WAR| " },
        { LogType.Log, "DBG| " },
        { LogType.Exception, "EXC| " }
    };

        private DebuggingPanel m_debuggingPanel;
        [SerializeField, AutoSet] private TMP_Text m_logLine;

        private new void Awake()
        {
            base.Awake();

            DontDestroyOnLoad(this);

            // if true, add empty strings for each line, to mimic behavior of console
            // filling from the bottom up.
            if (s_consoleLines.Count == 0)
            {
                for (var i = 0; i < MAX_CONSOLE_LINES; ++i)
                {
                    s_consoleLines.Enqueue("");
                }
            }

            UnityEngine.Application.logMessageReceived += LogCallback;
        }

        private void OnDestroy() => UnityEngine.Application.logMessageReceived -= LogCallback;

        public void SetDebuggingPanel(DebuggingPanel debuggingPanel_)
        {
            m_debuggingPanel = debuggingPanel_;
            PrintConsoleLog();
        }

        private void AddLogToConsole(string message, LogType type)
        {
            message = message.Trim();
            message = message.Trim('\n');

            var id = s_logID.ToString("0000");

            message = id + "> " + s_logType[type] + message;

            // each line should be added to the console
            foreach (var line in message.Split('\n'))
            {
                var temp = "";

                // check if a word overflows the line
                foreach (var word in line.Split(' '))
                {
                    temp += word + " ";
                    m_logLine.text = word;

                    // if true, need to wrap on character level because a single word in 
                    // the current log is wider than the widest allowed word
                    if (m_logLine.preferredWidth > MAX_WORD_WIDTH)
                    {
                        m_logLine.text = "";

                        for (var i = 0; i < temp.Length; ++i)
                        {
                            m_logLine.text += temp[i];
                            if (m_logLine.preferredWidth > MAX_CONSOLE_WIDTH)
                            {
                                AddConsoleLine(m_logLine.text[0..^1]);
                                m_logLine.text = temp[i].ToString();
                            }
                        }
                    }
                    // check if we need to wrap on word level
                    else
                    {
                        m_logLine.text = temp;

                        // if true, need to wrap on word level because the current log
                        // is wider than the widest allowed log
                        if (m_logLine.preferredWidth > MAX_CONSOLE_WIDTH)
                        {
                            AddConsoleLine(m_logLine.text[..(m_logLine.text.Length - word.Length - 1)]);
                            temp = word + " ";
                        }
                    }
                }

                if (m_logLine.preferredWidth <= MAX_CONSOLE_WIDTH)
                {
                    AddConsoleLine(m_logLine.text);
                }
            }
        }

        private void AddConsoleLine(string consoleLine)
        {
            if (s_consoleLines.Count == MAX_CONSOLE_LINES)
            {
                _ = s_consoleLines.Dequeue();
            }

            s_consoleLines.Enqueue(consoleLine);
        }

        private void PrintConsoleLog()
        {
            if (m_debuggingPanel == null)
            {
                return;
            }

            m_debuggingPanel.ConsoleLog.text = "";

            foreach (var line in s_consoleLines)
            {
                m_debuggingPanel.ConsoleLog.text += line + "\n";
            }
        }

        private void LogCallback(string condition, string stackTrace, LogType type)
        {
            AddLogToConsole(condition, type);
            PrintConsoleLog();
            ++s_logID;
        }
    }
}
