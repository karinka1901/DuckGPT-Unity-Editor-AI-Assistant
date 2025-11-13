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



    #region NOT USED CURRENTLY
    public static string TryBuildComponentsContextFromErrors(string errors) // Extracts GameObject components info if a MissingComponentException is found [4]
    {
        if (string.IsNullOrEmpty(errors)) return "";

        // Example error:
        // MissingComponentException: There is no 'Rigidbody' attached to the "duck" game object, but a script is trying to access it.
        Match match = Regex.Match(errors, "MissingComponentException: There is no '([^']+)' attached to the \"([^\"]+)\" game object", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return BuildCodeSnippetsFromErrors(errors, contextLines: 2, maxSnippets: 2);
        }

        string missingType = match.Groups[1].Value; // Rigidbody
        string gameObjectName = match.Groups[2].Value; // duck

        GameObject gameObject = HierarchyHandler.FindInActiveScene(gameObjectName);
        string componentsList = HierarchyHandler.GetComponentsString(gameObject);

        // Append code snippets around the error sites
        string snippetContext = BuildCodeSnippetsFromErrors(errors, contextLines: 2, maxSnippets: 2);

        return $"\n{gameObjectName} Components: \n {componentsList}  {snippetContext}";
    }

    // Regex to extract frames like: (at Assets/Scripts/Script.cs:123)
    private static readonly Regex StackFrameRegex = new Regex(@"\(at (Assets[\\/][^:\n]+\.cs):(\d+)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Builds short code snippets around the file:line locations found in the given error text.
    /// </summary>
    private static string BuildCodeSnippetsFromErrors(string errors, int contextLines = 2, int maxSnippets = 2)
    {
        if (string.IsNullOrEmpty(errors)) return "";

        var matches = StackFrameRegex.Matches(errors);
        if (matches.Count == 0) return "";

        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        StringBuilder stringBuilder = new();

        int added = 0;
        foreach (Match match in matches)
        {
            string relPath = match.Groups[1].Value.Replace('\\', '/'); // unify
            if (!int.TryParse(match.Groups[2].Value, out int lineNumber)) continue;

            string key = $"{relPath}:{lineNumber}";
            if (!seen.Add(key)) continue;

            string fileText = ReadAssetText(relPath);
            if (fileText == null) continue;

            string[] lines = fileText.Split(new[] { "\r\n", "\n", "\r" }, System.StringSplitOptions.None);
            int idx = Mathf.Clamp(lineNumber - 1, 0, lines.Length - 1);
            int start = Mathf.Max(0, idx - contextLines);
            int end = Mathf.Min(lines.Length - 1, idx + contextLines);

            stringBuilder.AppendLine($"\nCode snippet from {relPath}:{lineNumber}");
            for (int i = start; i <= end; i++)
            {
                string marker = (i == idx) ? ">>" : "  ";
                stringBuilder.AppendLine($"{marker} {i + 1,4}: {lines[i]}");
            }

            added++;
            if (added >= maxSnippets) break;
        }

        return stringBuilder.ToString();
    }

    private static string ReadAssetText(string assetPath)
    {
        // Try AssetDatabase first (works for assets under Assets/)
        var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
        if (textAsset != null) return textAsset.text;

        return null;
    }

    #endregion
}



//https://gist.github.com/Hertzole/d28243075a1074ce7b2ff713fcfe8573
