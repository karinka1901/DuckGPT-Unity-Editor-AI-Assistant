using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ConsoleLogHandler 
{
    private static readonly List<string> errorLogs = new List<string>();
    static ConsoleLogHandler()
    {
        Application.logMessageReceived += HandleLog;
        Application.logMessageReceivedThreaded += HandleLog;

        // subscribe to playmode changes so we can capture history after play stops
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            RefreshFromConsoleHistory();
        }
    }

    public static void RefreshFromConsoleHistory()
    {
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
        if (logEntries == null)
        {
            Debug.LogError("Could not find UnityEditor.LogEntries type.");
            return;
        }

        var getCountMethod = logEntries.GetMethod("GetCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        if (getCountMethod == null)
        {
            Debug.LogError("Could not find GetCount method.");
            return;
        }

        var getEntryMethod = logEntries.GetMethod("GetEntryInternal", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        if (getEntryMethod == null)
        {
            Debug.LogError("Could not find GetEntryInternal method.");
            return;
        }

        var logEntryType = System.Type.GetType("UnityEditor.LogEntry, UnityEditor.dll");
        if (logEntryType == null)
        {
            Debug.LogError("Could not find UnityEditor.LogEntry type.");
            return;
        }

        var logEntryInstance = System.Activator.CreateInstance(logEntryType);
        if (logEntryInstance == null)
        {
            Debug.LogError("Could not create LogEntry instance.");
            return;
        }

        int count = (int)getCountMethod.Invoke(null, null);
        for (int i = 0; i < count; i++)
        {
            getEntryMethod.Invoke(null, new object[] { i, logEntryInstance });
            var conditionField = logEntryType.GetField("condition");
            var stackTraceField = logEntryType.GetField("stackTrace");
            var typeField = logEntryType.GetField("mode");
            if (conditionField == null || stackTraceField == null || typeField == null)
            {
               // Debug.LogError("Could not find one or more fields in LogEntry.");
                continue;
            }
            string condition = (string)conditionField.GetValue(logEntryInstance);
            string stackTrace = (string)stackTraceField.GetValue(logEntryInstance);
            int mode = (int)typeField.GetValue(logEntryInstance);
            LogType logType = (LogType)mode;
            HandleLog(condition, stackTrace, logType);
        }
    }

    private static void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            errorLogs.Add($"{logString}\n{stackTrace}");
            if (errorLogs.Count > 20)
                errorLogs.RemoveAt(0);
        }
    }

    public static string GetRecentErrors(int maxCount = 5)
    {
        int count = Mathf.Min(maxCount, errorLogs.Count);
        if (count == 0) return "No recent errors.";
        return string.Join("\n---\n", errorLogs.GetRange(errorLogs.Count - count, count));
    }
}

//https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/ConsoleWindow.cs

//https://gist.github.com/Hertzole/d28243075a1074ce7b2ff713fcfe8573