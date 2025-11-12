using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ConsoleLogHandler 
{
    private static readonly List<string> errorLogs = new List<string>();
    private static readonly object _lock = new();

    static ConsoleLogHandler()
    {
        // Subscribe to ONE log event to avoid duplicates.
        Application.logMessageReceived += HandleLog;

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
        if (logEntries == null) { Debug.LogError("Could not find UnityEditor.LogEntries type."); return; }

        var getCountMethod = logEntries.GetMethod("GetCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        var getEntryMethod = logEntries.GetMethod("GetEntryInternal", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        if (getCountMethod == null || getEntryMethod == null) { Debug.LogError("Could not find LogEntries methods."); return; }

        var logEntryType = System.Type.GetType("UnityEditor.LogEntry, UnityEditor.dll");
        if (logEntryType == null) { Debug.LogError("Could not find UnityEditor.LogEntry type."); return; }

        var logEntryInstance = System.Activator.CreateInstance(logEntryType);
        if (logEntryInstance == null) { Debug.LogError("Could not create LogEntry instance."); return; }

        var conditionField = logEntryType.GetField("condition");
        var stackTraceField = logEntryType.GetField("stackTrace");
        var typeField = logEntryType.GetField("mode");
        if (conditionField == null || stackTraceField == null || typeField == null) return;

        int count = (int)getCountMethod.Invoke(null, null);

        // Rebuild from history to avoid duplicates
        var newErrors = new List<string>(capacity: Mathf.Min(count, 200));
        for (int i = 0; i < count; i++)
        {
            getEntryMethod.Invoke(null, new object[] { i, logEntryInstance });
            string condition = (string)conditionField.GetValue(logEntryInstance);
            string stackTrace = (string)stackTraceField.GetValue(logEntryInstance);
            int mode = (int)typeField.GetValue(logEntryInstance);

            // Try to interpret mode as LogType; only keep Error/Exception
            var type = (LogType)mode;
            if (type == LogType.Error || type == LogType.Exception)
            {
                string entry = $"{condition}\n{stackTrace}";
                if (newErrors.Count == 0 || newErrors[newErrors.Count - 1] != entry)
                    newErrors.Add(entry);
            }
        }

        lock (_lock)
        {
            errorLogs.Clear();
            // Keep only the last 20
            int start = Mathf.Max(0, newErrors.Count - 20);
            for (int i = start; i < newErrors.Count; i++)
                errorLogs.Add(newErrors[i]);
        }
    }

    private static void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception) return;

        string entry = $"{logString}\n{stackTrace}";
        lock (_lock)
        {
            // Deduplicate consecutive duplicates (e.g., when multiple events fire)
            if (errorLogs.Count > 0 && errorLogs[errorLogs.Count - 1] == entry)
                return;

            errorLogs.Add(entry);
            if (errorLogs.Count > 20)
                errorLogs.RemoveAt(0);
        }
    }

    public static string GetRecentErrors(int maxCount = 5)
    {
        lock (_lock)
        {
            if (errorLogs.Count == 0) return "No recent errors.";

            var result = new List<string>(maxCount);
            var seen = new HashSet<string>();
            for (int i = errorLogs.Count - 1; i >= 0 && result.Count < maxCount; i--)
            {
                var e = errorLogs[i];
                if (seen.Add(e))
                    result.Add(e);
            }
            result.Reverse();
            return string.Join("\n---\n", result);
        }
    }



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
}

//https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/ConsoleWindow.cs

//https://gist.github.com/Hertzole/d28243075a1074ce7b2ff713fcfe8573