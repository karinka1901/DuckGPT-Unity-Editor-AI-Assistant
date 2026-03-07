using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Provides static methods for tracking, analyzing, and retrieving user script information within a Unity project.
/// </summary>

[InitializeOnLoad]
public static class ScriptsHandler
{
    private static readonly List<string> recentlyModifiedScripts = new();
    private static readonly Dictionary<string, DateTime> scriptModificationTimes = new();

    private static string cachedProjectAnalysis = "";
    private static bool analysisInProgress = false;

    static ScriptsHandler()
    {
        EditorApplication.playModeStateChanged += _ => OnPlayModeStateChanged();
        EditorApplication.projectChanged += RefreshScripts;
    }

    private static void OnPlayModeStateChanged()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            RefreshScripts();
    }

    public static void RefreshScripts()
    {
        try
        {
            var currentScriptPaths = new HashSet<string>(GetUserScriptPaths());

            recentlyModifiedScripts.RemoveAll(path => !currentScriptPaths.Contains(path));

            foreach (string removedPath in scriptModificationTimes.Keys.Except(currentScriptPaths).ToList())
                scriptModificationTimes.Remove(removedPath);

            foreach (string path in currentScriptPaths)
            {
                if (!File.Exists(path)) continue;

                DateTime lastWrite = File.GetLastWriteTime(path);

                if (scriptModificationTimes.TryGetValue(path, out DateTime previousWrite) && lastWrite > previousWrite)
                {
                    if (!recentlyModifiedScripts.Contains(path))
                    {
                        recentlyModifiedScripts.Add(path);
                        if (recentlyModifiedScripts.Count > 10)
                            recentlyModifiedScripts.RemoveAt(0);
                    }
                }

                scriptModificationTimes[path] = lastWrite;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DuckGPT] Error refreshing script times: {e.Message}");
        }
    }

    /// <summary>Returns paths of user-created scripts within the configured folders.</summary>
    public static List<string> GetUserScriptPaths()
    {
        var scriptPaths = new List<string>();
        var includedFolders = ConfigurationEditorWindow.GetIncludedFolders() ?? new List<string> { "Assets/" };

        foreach (string folder in includedFolders)
        {
            if (string.IsNullOrEmpty(folder)) continue;

            foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (path.StartsWith("Assets/") && path.EndsWith(".cs") && !path.Contains("DuckGPT") && !path.Contains("/Editor/"))
                {
                    string fullPath = Path.GetFullPath(path);
                    if (!scriptPaths.Contains(fullPath))
                        scriptPaths.Add(fullPath);
                }

            }
        }

        return scriptPaths;
    }

    /// <summary>Returns paths of scripts attached to GameObjects in the active scene.</summary>
    public static List<string> GetSceneScriptPaths()
    {
        var sceneScripts = new HashSet<string>();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        if (!scene.IsValid()) return new List<string>();

        foreach (var rootObj in scene.GetRootGameObjects())
            CollectScriptsFromGameObject(rootObj, sceneScripts);

        return sceneScripts.ToList();
    }

    private static void CollectScriptsFromGameObject(GameObject obj, HashSet<string> scriptPaths)
    {
        if (obj == null) return;

        foreach (var component in obj.GetComponents<MonoBehaviour>())
        {
            if (component == null) continue;

            var script = MonoScript.FromMonoBehaviour(component);
            if (script == null) continue;

            string path = AssetDatabase.GetAssetPath(script);
            if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/"))
                scriptPaths.Add(Path.GetFullPath(path));
        }

        for (int i = 0; i < obj.transform.childCount; i++)
            CollectScriptsFromGameObject(obj.transform.GetChild(i).gameObject, scriptPaths);
    }

    /// <summary>Performs a full project analysis. Returns cached result unless forceRefresh is true.</summary>
    public static string AnalyzeProject(bool forceRefresh = false)
    {
        if (!forceRefresh && !string.IsNullOrEmpty(cachedProjectAnalysis))
            return cachedProjectAnalysis;

        if (analysisInProgress)
            return "Project analysis already in progress...";

        try
        {
            analysisInProgress = true;

            var analysis = new StringBuilder();
            analysis.AppendLine("=== SMART PROJECT ANALYSIS ===");
            analysis.AppendLine($"Analysis Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            analysis.AppendLine($"Unity Version: {Application.unityVersion}");
            analysis.AppendLine();

            analysis.AppendLine(GetSmartProjectOverview());
            analysis.AppendLine(GetSmartScriptAnalysis());
            analysis.AppendLine(GetSceneSpecificAnalysis());
            analysis.AppendLine(GetAssetAnalysis());
            analysis.AppendLine(GetSmartDependencyAnalysis());

            cachedProjectAnalysis = analysis.ToString();
            return cachedProjectAnalysis;
        }
        catch (Exception e)
        {
            Debug.LogError($"[DuckGPT] Error during project analysis: {e.Message}");
            return $"Project analysis failed: {e.Message}";
        }
        finally
        {
            analysisInProgress = false;
        }
    }

    private static string GetSmartProjectOverview()
    {
        var overview = new StringBuilder();
        overview.AppendLine("--- YOUR PROJECT OVERVIEW ---");

        var includedFolders = ConfigurationEditorWindow.GetIncludedFolders() ?? new List<string> { "Assets/" };
        string[] folderArray = includedFolders.ToArray();

        var userScripts = GetUserScriptPaths();
        var sceneScripts = GetSceneScriptPaths();

        overview.AppendLine($"Analyzed Folders: {string.Join(", ", includedFolders)}");
        overview.AppendLine($"Project Name: {Application.productName}");
        overview.AppendLine($"Your Custom Scripts: {userScripts.Count}");
        overview.AppendLine($"Scripts Used in Current Scene: {sceneScripts.Count}");
        overview.AppendLine($"Total Scenes: {AssetDatabase.FindAssets("t:Scene", folderArray).Length}");
        overview.AppendLine($"Total Prefabs: {AssetDatabase.FindAssets("t:Prefab", folderArray).Length}");

        if (sceneScripts.Count > 0)
        {
            overview.AppendLine("\nScripts active in current scene:");
            foreach (var scriptPath in sceneScripts.Take(10))
                overview.AppendLine($"  - {Path.GetFileNameWithoutExtension(scriptPath)}");

            if (sceneScripts.Count > 10)
                overview.AppendLine($"  ... and {sceneScripts.Count - 10} more");
        }

        overview.AppendLine();
        return overview.ToString();
    }

    private static string GetSmartScriptAnalysis()
    {
        var analysis = new StringBuilder();
        analysis.AppendLine("--- YOUR SCRIPT ANALYSIS ---");

        var userScripts = GetUserScriptPaths();
        var sceneScripts = GetSceneScriptPaths();
        var scriptTypes = new Dictionary<string, List<(string name, bool inScene)>>();

        foreach (var path in userScripts)
        {
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                string content = File.ReadAllText(path);
                bool inScene = sceneScripts.Contains(path);
                string category = CategorizeScript(content);

                if (!scriptTypes.ContainsKey(category))
                    scriptTypes[category] = new List<(string, bool)>();

                scriptTypes[category].Add((fileName, inScene));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DuckGPT] Error analysing script {path}: {e.Message}");
            }
        }

        foreach (var category in scriptTypes.Keys)
        {
            var scriptsInCategory = scriptTypes[category];
            int sceneCount = scriptsInCategory.Count(s => s.inScene);

            analysis.AppendLine($"\n{category} ({scriptsInCategory.Count} total, {sceneCount} in scene):");

            foreach (var script in scriptsInCategory.Where(s => s.inScene).Take(5))
                analysis.AppendLine($"  ✓ {script.name} (in scene)");

            foreach (var script in scriptsInCategory.Where(s => !s.inScene).Take(5))
                analysis.AppendLine($"    {script.name}");

            if (scriptsInCategory.Count > 10)
                analysis.AppendLine($"    ... and {scriptsInCategory.Count - 10} more");
        }

        analysis.AppendLine();
        return analysis.ToString();
    }

    private static string GetSceneSpecificAnalysis()
    {
        var analysis = new StringBuilder();
        analysis.AppendLine("--- CURRENT SCENE DETAILED ANALYSIS ---");

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            analysis.AppendLine("No active scene loaded.");
            return analysis.ToString();
        }

        var componentTypes = new Dictionary<string, int>();
        var scriptInstances = new Dictionary<string, List<string>>();
        int totalGameObjects = 0;

        analysis.AppendLine($"Scene: {scene.name}");

        foreach (var rootObj in scene.GetRootGameObjects())
            AnalyzeGameObjectWithScripts(rootObj, componentTypes, scriptInstances, ref totalGameObjects);

        analysis.AppendLine($"Total GameObjects: {totalGameObjects}");
        analysis.AppendLine($"Root GameObjects: {scene.GetRootGameObjects().Length}");

        if (scriptInstances.Count > 0)
        {
            analysis.AppendLine("\nYour scripts in this scene:");
            foreach (var kvp in scriptInstances.OrderByDescending(x => x.Value.Count))
            {
                analysis.AppendLine($"  {kvp.Key} (used on {kvp.Value.Count} objects):");
                foreach (var objName in kvp.Value.Take(3))
                    analysis.AppendLine($"    - {objName}");

                if (kvp.Value.Count > 3)
                    analysis.AppendLine($"    ... and {kvp.Value.Count - 3} more");
            }
        }

        analysis.AppendLine("\nMost used components:");
        foreach (var kvp in componentTypes.OrderByDescending(x => x.Value).Take(8))
            analysis.AppendLine($"  {kvp.Key}: {kvp.Value}");

        analysis.AppendLine();
        return analysis.ToString();
    }

    private static void AnalyzeGameObjectWithScripts(GameObject obj, Dictionary<string, int> componentTypes,
        Dictionary<string, List<string>> scriptInstances, ref int totalCount)
    {
        if (obj == null) return;

        totalCount++;

        foreach (var component in obj.GetComponents<Component>())
        {
            if (component == null) continue;

            string typeName = component.GetType().Name;
            componentTypes[typeName] = componentTypes.GetValueOrDefault(typeName, 0) + 1;

            if (component is MonoBehaviour mb)
            {
                var script = MonoScript.FromMonoBehaviour(mb);
                if (script != null)
                {
                    string path = AssetDatabase.GetAssetPath(script);
                    if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/") && !path.Contains("DuckGPT"))
                    {
                        if (!scriptInstances.ContainsKey(typeName))
                            scriptInstances[typeName] = new List<string>();
                        scriptInstances[typeName].Add(obj.name);
                    }
                }
            }
        }

        for (int i = 0; i < obj.transform.childCount; i++)
            AnalyzeGameObjectWithScripts(obj.transform.GetChild(i).gameObject, componentTypes, scriptInstances, ref totalCount);
    }

    private static string GetSmartDependencyAnalysis()
    {
        var analysis = new StringBuilder();
        analysis.AppendLine("--- YOUR SCRIPT DEPENDENCIES ---");

        var userScripts = GetUserScriptPaths();
        var sceneScripts = GetSceneScriptPaths();
        var dependencies = new Dictionary<string, HashSet<string>>();

        var scriptsToAnalyze = sceneScripts
            .Concat(userScripts.Where(s => !sceneScripts.Contains(s)))
            .Take(25);

        foreach (var path in scriptsToAnalyze)
        {
            try
            {
                dependencies[Path.GetFileNameWithoutExtension(path)] = ExtractDependencies(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DuckGPT] Error analysing dependencies for {path}: {e.Message}");
            }
        }

        var sortedDependencies = dependencies
            .Where(d => d.Value.Count > 1)
            .OrderByDescending(d => sceneScripts.Any(s => Path.GetFileNameWithoutExtension(s) == d.Key)
                ? 1000 + d.Value.Count : d.Value.Count)
            .Take(8);

        analysis.AppendLine("Scripts with notable dependencies:");
        foreach (var dep in sortedDependencies)
        {
            bool isInScene = sceneScripts.Any(s => Path.GetFileNameWithoutExtension(s) == dep.Key);
            string marker = isInScene ? "✓ " : "  ";
            analysis.AppendLine($"{marker}{dep.Key} ({dep.Value.Count} dependencies){(isInScene ? " [IN SCENE]" : "")}:");

            foreach (var dependency in dep.Value.Take(4))
                analysis.AppendLine($"    - {dependency}");

            if (dep.Value.Count > 4)
                analysis.AppendLine($"    ... and {dep.Value.Count - 4} more");
        }

        analysis.AppendLine();
        return analysis.ToString();
    }

    private static string CategorizeScript(string content)
    {
        if (content.Contains("MonoBehaviour"))                              return "MonoBehaviour Scripts";
        if (content.Contains("ScriptableObject"))                           return "ScriptableObject Scripts";
        if (content.Contains("EditorWindow") || content.Contains("Editor")) return "Editor Scripts";
        if (content.Contains("interface "))                                 return "Interfaces";
        if (content.Contains("enum "))                                      return "Enums";
        if (content.Contains("static class"))                               return "Static Utility Classes";
        if (content.Contains("struct "))                                    return "Structs";
        return "Other Scripts";
    }

    private static string GetAssetAnalysis()
    {
        var analysis = new StringBuilder();
        analysis.AppendLine("--- ASSET ANALYSIS ---");

        var includedFolders = ConfigurationEditorWindow.GetIncludedFolders() ?? new List<string> { "Assets/" };
        string[] folderArray = includedFolders.ToArray();

        analysis.AppendLine($"Analyzed Folders: {string.Join(", ", includedFolders)}");
        analysis.AppendLine();

        var assetTypes = new Dictionary<string, int>
        {
            ["Textures"]    = AssetDatabase.FindAssets("t:Texture2D",      folderArray).Length,
            ["Audio Clips"] = AssetDatabase.FindAssets("t:AudioClip",      folderArray).Length,
            ["Materials"]   = AssetDatabase.FindAssets("t:Material",       folderArray).Length,
            ["Meshes"]      = AssetDatabase.FindAssets("t:Mesh",           folderArray).Length,
            ["Animations"]  = AssetDatabase.FindAssets("t:AnimationClip",  folderArray).Length,
            ["Prefabs"]     = AssetDatabase.FindAssets("t:Prefab",         folderArray).Length,
            ["Shaders"]     = AssetDatabase.FindAssets("t:Shader",         folderArray).Length
        };

        foreach (var assetType in assetTypes)
            analysis.AppendLine($"{assetType.Key}: {assetType.Value}");

        analysis.AppendLine();
        return analysis.ToString();
    }

    private static HashSet<string> ExtractDependencies(string scriptContent)
    {
        var dependencies = new HashSet<string>();

        foreach (Match match in Regex.Matches(scriptContent, @"using\s+([^;]+);"))
            dependencies.Add(match.Groups[1].Value.Trim());

        foreach (Match match in Regex.Matches(scriptContent, @"(GetComponent|RequireComponent|AddComponent)<([^>]+)>"))
            dependencies.Add(match.Groups[2].Value.Trim());

        return dependencies;
    }

    /// <summary>Returns cached project analysis, or a prompt to run one if none exists.</summary>
    public static string GetProjectAnalysis() =>
        string.IsNullOrEmpty(cachedProjectAnalysis)
            ? "No project analysis available. Use 'Analyze Project' to generate one."
            : cachedProjectAnalysis;

    /// <summary>Clears the cached project analysis.</summary>
    public static void ClearAnalysisCache()
    {
        cachedProjectAnalysis = "";
        Debug.Log("[DuckGPT] Project analysis cache cleared.");
    }

    /// <summary>Returns content of scripts referenced in a Unity error message.</summary>
    public static string GetScriptContextFromErrors(string errorMessage)
    {
        var scriptContext = new StringBuilder();

        foreach (var scriptPath in ExtractScriptPathsFromError(errorMessage).Take(3))
        {
            string content = GetScriptContent(scriptPath);
            if (!string.IsNullOrEmpty(content))
            {
                scriptContext.AppendLine($"\n--- Script: {Path.GetFileName(scriptPath)} ---");
                scriptContext.AppendLine(content);
                scriptContext.AppendLine("--- End Script ---\n");
            }
        }

        return scriptContext.ToString();
    }

    private static List<string> ExtractScriptPathsFromError(string errorMessage)
    {
        var paths = new List<string>();
        var regex = new Regex(@"\(at\s+([^:]+\.cs):(\d+).*?\)");

        foreach (Match match in regex.Matches(errorMessage))
        {
            string fullPath = Path.GetFullPath(match.Groups[1].Value);
            if (File.Exists(fullPath) && !paths.Contains(fullPath))
                paths.Add(fullPath);
        }

        return paths;
    }

    /// <summary>Returns script content with line numbers. Truncates large files.</summary>
    public static string GetScriptContent(string filePath, int contextLines = 50)
    {
        try
        {
            if (!File.Exists(filePath))
                return $"File not found: {filePath}";

            string[] lines = File.ReadAllLines(filePath);
            string fileName = Path.GetFileName(filePath);
            var content = new StringBuilder();
            content.AppendLine($"// File: {fileName}");

            if (lines.Length > contextLines * 2)
            {
                content.AppendLine($"// (showing first and last {contextLines} lines)");

                for (int i = 0; i < Math.Min(contextLines, lines.Length); i++)
                    content.AppendLine($"{i + 1:D3}: {lines[i]}");

                content.AppendLine("... (middle content truncated) ...");

                int startLine = Math.Max(contextLines, lines.Length - contextLines);
                for (int i = startLine; i < lines.Length; i++)
                    content.AppendLine($"{i + 1:D3}: {lines[i]}");
            }
            else
            {
                for (int i = 0; i < lines.Length; i++)
                    content.AppendLine($"{i + 1:D3}: {lines[i]}");
            }

            return content.ToString();
        }
        catch (Exception e)
        {
            return $"Error reading {filePath}: {e.Message}";
        }
    }

    /// <summary>Returns raw script content for use as AI context.</summary>
    public static string GetScriptContent(string scriptPath)
    {
        if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
            return "";

        try
        {
            return $"\n--- Script: {Path.GetFileName(scriptPath)} ---\n{File.ReadAllText(scriptPath)}\n";
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DuckGPT] Failed to read script {scriptPath}: {ex.Message}");
            return "";
        }
    }

    /// <summary>Returns content of recently modified scripts for AI context.</summary>
    public static string GetRecentlyModifiedScripts(int maxScripts = 3)
    {
        if (recentlyModifiedScripts.Count == 0) return "";

        var content = new StringBuilder();
        content.AppendLine("\n--- Recently Modified Scripts ---");

        foreach (var scriptPath in recentlyModifiedScripts.TakeLast(maxScripts))
        {
            if (scriptPath.Contains("Assets/") && !scriptPath.Contains("DuckGPT"))
                content.AppendLine(GetScriptContent(scriptPath, 30));
        }

        return content.ToString();
    }

    /// <summary>Searches for scripts by name and returns their contents.</summary>
    public static string FindScriptsByName(string scriptName)
    {
        var content = new StringBuilder();

        foreach (var scriptPath in GetUserScriptPaths()
            .Where(p => Path.GetFileNameWithoutExtension(p)
                .Contains(scriptName, StringComparison.OrdinalIgnoreCase))
            .Take(3))
        {
            content.AppendLine(GetScriptContent(scriptPath, 50));
        }

        return content.ToString();
    }

    /// <summary>Returns script lines around the error location from a stack trace.</summary>
    public static string GetScriptContextForError(string errorMessage, string stackTrace)
    {
        var context = new StringBuilder();
        var regex = new Regex(@"\(at\s+([^:]+\.cs):(\d+).*?\)");

        foreach (Match match in regex.Matches(stackTrace).Cast<Match>().Take(2))
        {
            string fullPath = Path.GetFullPath(match.Groups[1].Value);
            int lineNumber = int.Parse(match.Groups[2].Value);

            if (!File.Exists(fullPath)) continue;

            context.AppendLine($"\n--- Error in {Path.GetFileName(fullPath)} at line {lineNumber} ---");

            try
            {
                string[] lines = File.ReadAllLines(fullPath);
                int startLine = Math.Max(0, lineNumber - 10);
                int endLine = Math.Min(lines.Length - 1, lineNumber + 10);

                for (int i = startLine; i <= endLine; i++)
                {
                    string marker = i == lineNumber - 1 ? ">>> " : "    ";
                    context.AppendLine($"{marker}{i + 1:D3}: {lines[i]}");
                }
            }
            catch (Exception e)
            {
                context.AppendLine($"Error reading file: {e.Message}");
            }

            context.AppendLine("--- End Error Context ---\n");
        }

        return context.ToString();
    }
}

