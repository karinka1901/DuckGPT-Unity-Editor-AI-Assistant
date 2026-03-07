using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Static class that captures and stores recent error logs from Unity's Console for AI debugging assistance.
/// Uses real-time event subscription and reflection-based access to Unity's internal LogEntry structure.
/// Internal UnityEditor.dll structure was reverse-engineered using ILSpy to access LogEntries and LogEntry classes.
/// Provides thread-safe deduplication and maintains a rolling buffer of the 20 most recent errors.
/// </summary>
/// 
[InitializeOnLoad]
public static class ConsoleLogHandler
{
    private static readonly List<string> errorLogs = new List<string>();
    private static readonly HashSet<string> processedErrors = new HashSet<string>(); 
    private static readonly object _lockObj = new();

    static ConsoleLogHandler()
    {
        Application.logMessageReceived += HandleLog;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state) // Clears errors on play mode change and refreshes from history on edit mode
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            RefreshFromConsoleHistory();
        }
        else if (state == PlayModeStateChange.EnteredPlayMode)
        {
            lock (_lockObj)
            {
                processedErrors.Clear();
            }
        }
    }

    public static void RefreshFromConsoleHistory() // Reads the Unity Console log history via reflection and processes error entries
    {
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");

        var getCountMethod = logEntries.GetMethod("GetCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        var getEntryMethod = logEntries.GetMethod("GetEntryInternal", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

        var logEntryType = System.Type.GetType("UnityEditor.LogEntry, UnityEditor.dll");
        var logEntryInstance = System.Activator.CreateInstance(logEntryType);

        // LogEntry structure from UnityEditor.dll
        var messageField = logEntryType.GetField("message"); //log message
        var fileField = logEntryType.GetField("file"); //file path
        var lineField = logEntryType.GetField("line"); //line number
        var columnField = logEntryType.GetField("column"); //column number
        var modeField = logEntryType.GetField("mode");  //log type 
        var instanceIDField = logEntryType.GetField("instanceID"); //unity object ID

        int count = (int)getCountMethod.Invoke(null, null);
        for (int i = 0; i < count; i++) //iterate through all log entries and extract errors
        {
            getEntryMethod.Invoke(null, new object[] { i, logEntryInstance });

            string message = (string)messageField.GetValue(logEntryInstance) ?? "";
            string file = (string)fileField.GetValue(logEntryInstance) ?? "";
            int line = (int)lineField.GetValue(logEntryInstance);
            int column = (int)columnField.GetValue(logEntryInstance);
            int mode = (int)modeField.GetValue(logEntryInstance);
            //   int instanceID = (int)instanceIDField.GetValue(logEntryInstance); //ill use it latwer if needed
            LogType logType = (LogType)mode;

            // stack trace from file and line info
            string stackTrace = "";
            if (!string.IsNullOrEmpty(file) && line > 0)
            {
                if (column != 0)
                    stackTrace = $"(at {file}:{line},{column})";
                else
                    stackTrace = $"(at {file}:{line})";
            }
            HandleLog(message, stackTrace, logType);
        }
    }

    private static void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            string error = $"{logString}\n{stackTrace}";
            string errorKey = $"{logString}|{stackTrace}|{type}"; // Unique key for the error to avoid duplicates

            if (processedErrors.Add(errorKey))
            {
                errorLogs.Add(error);
                if (errorLogs.Count > 20)
                {
                    errorLogs.RemoveAt(0);
                }
            }

        }
    }

    public static string GetRecentErrors(int maxCount = 5)
    {
        int count = Mathf.Min(maxCount, errorLogs.Count);
        if (count == 0) return "No recent errors.";
        return string.Join("\n---\n", errorLogs.GetRange(errorLogs.Count - count, count));
    }

}



//https://www.foundations.unity.com/patterns/errors-and-messaging
