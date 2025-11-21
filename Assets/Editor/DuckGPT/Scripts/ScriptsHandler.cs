using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ScriptsHandler
{
    private static readonly List<string> recentlyModifiedScripts = new List<string>();
    private static readonly Dictionary<string, DateTime> scriptModificationTimes = new Dictionary<string, DateTime>();
    
    // Project analysis cache
    private static string cachedProjectAnalysis = "";
    private static DateTime lastAnalysisTime = DateTime.MinValue;
    private static bool analysisInProgress = false;
    
    static ScriptsHandler()
    {
        EditorApplication.playmodeStateChanged += OnPlayModeStateChanged;
        // Watch for asset changes to track script modifications
        EditorApplication.projectChanged += OnProjectChanged;
    }

    public static void OnPlayModeStateChanged()
    {
        // Refresh script information when play mode changes
        if (EditorApplication.isPlayingOrWillChangePlaymode == false)
        {
            RefreshScriptModificationTimes();
        }
    }

    private static void OnProjectChanged()
    {
        RefreshScriptModificationTimes();
    }

    private static void RefreshScriptModificationTimes()
    {
        try
        {
            var scriptPaths = GetUserScriptPaths();
            foreach (var path in scriptPaths)
            {
                if (File.Exists(path))
                {
                    var lastWrite = File.GetLastWriteTime(path);
                    if (scriptModificationTimes.ContainsKey(path))
                    {
                        if (lastWrite > scriptModificationTimes[path])
                        {
                            // Script was modified
                            if (!recentlyModifiedScripts.Contains(path))
                            {
                                recentlyModifiedScripts.Add(path);
                                if (recentlyModifiedScripts.Count > 10) // Keep only 10 recent modifications
                                {
                                    recentlyModifiedScripts.RemoveAt(0);
                                }
                            }
                        }
                    }
                    scriptModificationTimes[path] = lastWrite;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DuckGPT ScriptsHandler: Error refreshing script times: {e.Message}");
        }
    }

    /// <summary>
        /// Get only user-created scripts in Assets folder (excluding Packages, Library, etc.)
        /// </summary>
        public static List<string> GetUserScriptPaths()
        {
            var scriptPaths = new List<string>();
            var assetGuids = AssetDatabase.FindAssets("t:MonoScript");
        
            foreach (string guid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
            
                if (path.StartsWith("Assets/") && path.EndsWith(".cs"))
                {
                    // Skip DuckGPT scripts from analysis to avoid recursion
                    if (!path.Contains("DuckGPT"))
                    {
                        var fullPath = Path.GetFullPath(path);
                        scriptPaths.Add(fullPath);
                    }
                }
            }
        
            return scriptPaths;
        }

        /// <summary>
        /// Get scripts that are actually used in the current scene
        /// </summary>
        public static List<string> GetSceneScriptPaths()
        {
            var sceneScripts = new HashSet<string>();
        
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().IsValid())
                return new List<string>();

            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        
            foreach (var rootObj in rootObjects)
            {
                CollectScriptsFromGameObject(rootObj, sceneScripts);
            }
        
            return sceneScripts.ToList();
        }

        private static void CollectScriptsFromGameObject(GameObject obj, HashSet<string> scriptPaths)
        {
            if (obj == null) return;
        
            var components = obj.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component != null)
                {
                    var script = MonoScript.FromMonoBehaviour(component);
                    if (script != null)
                    {
                        var path = AssetDatabase.GetAssetPath(script);
                        if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/"))
                        {
                            var fullPath = Path.GetFullPath(path);
                            scriptPaths.Add(fullPath);
                        }
                    }
                }
            }

            // Recursively check children
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                CollectScriptsFromGameObject(obj.transform.GetChild(i).gameObject, scriptPaths);
            }
        }

        /// <summary>
        /// Perform focused project analysis - only user scripts and scene scripts
        /// </summary>
        public static string AnalyzeProject(bool forceRefresh = false)
    {
        if (!forceRefresh && !string.IsNullOrEmpty(cachedProjectAnalysis))
        {
            return cachedProjectAnalysis;
        }

        if (analysisInProgress)
        {
            return "Project analysis already in progress...";
        }

        try
        {
            analysisInProgress = true;
            EditorUtility.DisplayProgressBar("Duck Analysis", "Analyzing your custom scripts...", 0.1f);

            var analysis = new StringBuilder();
            analysis.AppendLine("=== SMART PROJECT ANALYSIS ===");
            analysis.AppendLine($"Analysis Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            analysis.AppendLine($"Unity Version: {Application.unityVersion}");
            analysis.AppendLine("Focus: User-created scripts and scene-specific analysis");
            analysis.AppendLine();

            // 1. Project Overview (user scripts only)
            EditorUtility.DisplayProgressBar("Duck Analysis", "Analyzing your scripts...", 0.2f);
            analysis.AppendLine(GetSmartProjectOverview());

            // 2. User Script Analysis
            EditorUtility.DisplayProgressBar("Duck Analysis", "Categorizing your scripts...", 0.4f);
            analysis.AppendLine(GetSmartScriptAnalysis());

            // 3. Scene-Specific Analysis
            EditorUtility.DisplayProgressBar("Duck Analysis", "Analyzing current scene scripts...", 0.6f);
            analysis.AppendLine(GetSceneSpecificAnalysis());

            // 4. Asset Analysis (same as before)
            EditorUtility.DisplayProgressBar("Duck Analysis", "Analyzing assets...", 0.8f);
            analysis.AppendLine(GetAssetAnalysis());

            // 5. Script Dependencies (user scripts only)
            EditorUtility.DisplayProgressBar("Duck Analysis", "Analyzing your script dependencies...", 0.9f);
            analysis.AppendLine(GetSmartDependencyAnalysis());

            cachedProjectAnalysis = analysis.ToString();
            lastAnalysisTime = DateTime.Now;

            EditorUtility.ClearProgressBar();
            Debug.Log("DuckGPT: Smart project analysis completed successfully!");
            
            return cachedProjectAnalysis;
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"DuckGPT: Error during project analysis: {e.Message}");
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
        
        var userScripts = GetUserScriptPaths();
        var sceneScripts = GetSceneScriptPaths();
        var sceneGuids = AssetDatabase.FindAssets("t:Scene");
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        
        overview.AppendLine($"Your Custom Scripts: {userScripts.Count}");
        overview.AppendLine($"Scripts Used in Current Scene: {sceneScripts.Count}");
        overview.AppendLine($"Total Scenes: {sceneGuids.Length}");
        overview.AppendLine($"Total Prefabs: {prefabGuids.Length}");
        overview.AppendLine($"Project Name: {Application.productName}");
        
        if (sceneScripts.Count > 0)
        {
            overview.AppendLine("\nScripts active in current scene:");
            foreach (var scriptPath in sceneScripts.Take(10))
            {
                var fileName = Path.GetFileNameWithoutExtension(scriptPath);
                overview.AppendLine($"  - {fileName}");
            }
            if (sceneScripts.Count > 10)
            {
                overview.AppendLine($"  ... and {sceneScripts.Count - 10} more");
            }
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
                var fileName = Path.GetFileNameWithoutExtension(path);
                var content = File.ReadAllText(path);
                var inScene = sceneScripts.Contains(path);

                // Categorize scripts
                var category = CategorizeScript(content, fileName);
                if (!scriptTypes.ContainsKey(category))
                    scriptTypes[category] = new List<(string, bool)>();
                
                scriptTypes[category].Add((fileName, inScene));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error analyzing script {path}: {e.Message}");
            }
        }

        foreach (var category in scriptTypes.Keys)
        {
            var scriptsInCategory = scriptTypes[category];
            var sceneCount = scriptsInCategory.Count(s => s.inScene);
            
            analysis.AppendLine($"\n{category} ({scriptsInCategory.Count} total, {sceneCount} in scene):");
            
            // Show scene scripts first, marked with ✓
            foreach (var script in scriptsInCategory.Where(s => s.inScene).Take(5))
            {
                analysis.AppendLine($"  ✓ {script.name} (in scene)");
            }
            
            // Then show other scripts
            foreach (var script in scriptsInCategory.Where(s => !s.inScene).Take(5))
            {
                analysis.AppendLine($"    {script.name}");
            }
            
            if (scriptsInCategory.Count > 10)
            {
                analysis.AppendLine($"    ... and {scriptsInCategory.Count - 10} more");
            }
        }

        analysis.AppendLine();
        return analysis.ToString();
    }

    private static string GetSceneSpecificAnalysis()
    {
        var analysis = new StringBuilder();
        analysis.AppendLine("--- CURRENT SCENE DETAILED ANALYSIS ---");

        if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().IsValid())
        {
            analysis.AppendLine("No active scene loaded.");
            return analysis.ToString();
        }

        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        analysis.AppendLine($"Scene: {activeScene.name}");
        
        var rootObjects = activeScene.GetRootGameObjects();
        var componentTypes = new Dictionary<string, int>();
        var scriptInstances = new Dictionary<string, List<string>>(); // script -> gameobjects using it
        int totalGameObjects = 0;

        foreach (var rootObj in rootObjects)
        {
            AnalyzeGameObjectWithScripts(rootObj, componentTypes, scriptInstances, ref totalGameObjects);
        }

        analysis.AppendLine($"Total GameObjects: {totalGameObjects}");
        analysis.AppendLine($"Root GameObjects: {rootObjects.Length}");
        
        // Show your custom scripts and where they're used
        if (scriptInstances.Count > 0)
        {
            analysis.AppendLine("\nYour scripts in this scene:");
            foreach (var kvp in scriptInstances.OrderByDescending(x => x.Value.Count))
            {
                analysis.AppendLine($"  {kvp.Key} (used on {kvp.Value.Count} objects):");
                foreach (var objName in kvp.Value.Take(3))
                {
                    analysis.AppendLine($"    - {objName}");
                }
                if (kvp.Value.Count > 3)
                {
                    analysis.AppendLine($"    ... and {kvp.Value.Count - 3} more");
                }
            }
        }

        // Show most used components
        analysis.AppendLine("\nMost used components:");
        foreach (var kvp in componentTypes.OrderByDescending(x => x.Value).Take(8))
        {
            analysis.AppendLine($"  {kvp.Key}: {kvp.Value}");
        }

        analysis.AppendLine();
        return analysis.ToString();
    }

    private static void AnalyzeGameObjectWithScripts(GameObject obj, Dictionary<string, int> componentTypes, 
        Dictionary<string, List<string>> scriptInstances, ref int totalCount)
    {
        if (obj == null) return;
        
        totalCount++;
        
        var components = obj.GetComponents<Component>();
        foreach (var component in components)
        {
            if (component != null)
            {
                var typeName = component.GetType().Name;
                componentTypes[typeName] = componentTypes.GetValueOrDefault(typeName, 0) + 1;
                
                // Track custom MonoBehaviour scripts
                if (component is MonoBehaviour mb)
                {
                    var script = MonoScript.FromMonoBehaviour(mb);
                    if (script != null)
                    {
                        var path = AssetDatabase.GetAssetPath(script);
                        if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/") && !path.Contains("DuckGPT"))
                        {
                            if (!scriptInstances.ContainsKey(typeName))
                                scriptInstances[typeName] = new List<string>();
                            scriptInstances[typeName].Add(obj.name);
                        }
                    }
                }
            }
        }

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            AnalyzeGameObjectWithScripts(obj.transform.GetChild(i).gameObject, componentTypes, scriptInstances, ref totalCount);
        }
    }

    private static string GetSmartDependencyAnalysis()
    {
        var analysis = new StringBuilder();
        analysis.AppendLine("--- YOUR SCRIPT DEPENDENCIES ---");

        var userScripts = GetUserScriptPaths();
        var sceneScripts = GetSceneScriptPaths();
        var dependencies = new Dictionary<string, HashSet<string>>();

        // Analyze only user scripts, prioritize scene scripts
        var scriptsToAnalyze = sceneScripts.Concat(userScripts.Where(s => !sceneScripts.Contains(s))).Take(25);

        foreach (var path in scriptsToAnalyze)
        {
            try
            {
                var content = File.ReadAllText(path);
                var fileName = Path.GetFileNameWithoutExtension(path);
                
                dependencies[fileName] = ExtractDependencies(content);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error analyzing dependencies for {path}: {e.Message}");
            }
        }

        // Show scripts with most dependencies, prioritizing scene scripts
        var sortedDependencies = dependencies
            .Where(d => d.Value.Count > 1)
            .OrderByDescending(d => sceneScripts.Any(s => Path.GetFileNameWithoutExtension(s) == d.Key) ? 1000 + d.Value.Count : d.Value.Count)
            .Take(8);

        analysis.AppendLine("Scripts with notable dependencies:");
        foreach (var dep in sortedDependencies)
        {
            var isInScene = sceneScripts.Any(s => Path.GetFileNameWithoutExtension(s) == dep.Key);
            var marker = isInScene ? "✓ " : "  ";
            analysis.AppendLine($"{marker}{dep.Key} ({dep.Value.Count} dependencies){(isInScene ? " [IN SCENE]" : "")}:");
            foreach (var dependency in dep.Value.Take(4))
            {
                analysis.AppendLine($"    - {dependency}");
            }
            if (dep.Value.Count > 4)
            {
                analysis.AppendLine($"    ... and {dep.Value.Count - 4} more");
            }
        }

        analysis.AppendLine();
        return analysis.ToString();
    }

    // Keep all the existing methods below (CategorizeScript, GetAssetAnalysis, etc.)
    private static string CategorizeScript(string content, string fileName)
    {
        if (content.Contains("MonoBehaviour"))
            return "MonoBehaviour Scripts";
        else if (content.Contains("ScriptableObject"))
            return "ScriptableObject Scripts";
        else if (content.Contains("EditorWindow") || content.Contains("Editor"))
            return "Editor Scripts";
        else if (content.Contains("interface "))
            return "Interfaces";
        else if (content.Contains("enum "))
            return "Enums";
        else if (content.Contains("static class"))
            return "Static Utility Classes";
        else if (content.Contains("struct "))
            return "Structs";
        else
            return "Other Scripts";
    }

    private static string GetAssetAnalysis()
    {
        var analysis = new StringBuilder();
        analysis.AppendLine("--- ASSET ANALYSIS ---");

        var assetTypes = new Dictionary<string, int>
        {
            ["Textures"] = AssetDatabase.FindAssets("t:Texture2D").Length,
            ["Audio Clips"] = AssetDatabase.FindAssets("t:AudioClip").Length,
            ["Materials"] = AssetDatabase.FindAssets("t:Material").Length,
            ["Meshes"] = AssetDatabase.FindAssets("t:Mesh").Length,
            ["Animations"] = AssetDatabase.FindAssets("t:AnimationClip").Length,
            ["Prefabs"] = AssetDatabase.FindAssets("t:Prefab").Length,
            ["Shaders"] = AssetDatabase.FindAssets("t:Shader").Length
        };

        foreach (var assetType in assetTypes)
        {
            analysis.AppendLine($"{assetType.Key}: {assetType.Value}");
        }

        analysis.AppendLine();
        return analysis.ToString();
    }

    private static HashSet<string> ExtractDependencies(string scriptContent)
    {
        var dependencies = new HashSet<string>();
        
        // Extract using statements
        var usingMatches = Regex.Matches(scriptContent, @"using\s+([^;]+);");
        foreach (Match match in usingMatches)
        {
            dependencies.Add(match.Groups[1].Value.Trim());
        }

        // Extract component references
        var componentMatches = Regex.Matches(scriptContent, @"(GetComponent|RequireComponent|AddComponent)<([^>]+)>");
        foreach (Match match in componentMatches)
        {
            dependencies.Add(match.Groups[2].Value.Trim());
        }

        return dependencies;
    }

    /// <summary>
    /// Get cached project analysis or prompt for new analysis
    /// </summary>
    public static string GetProjectAnalysis()
    {
        if (string.IsNullOrEmpty(cachedProjectAnalysis))
        {
            return "No project analysis available. Use 'Analyze Project' button to generate comprehensive project analysis.";
        }

        return cachedProjectAnalysis;
    }

    /// <summary>
    /// Clear cached analysis to force refresh
    /// </summary>
    public static void ClearAnalysisCache()
    {
        cachedProjectAnalysis = "";
        lastAnalysisTime = DateTime.MinValue;
        Debug.Log("DuckGPT: Project analysis cache cleared.");
    }

    /// <summary>
    /// Get all C# script file paths in the project (deprecated - use GetUserScriptPaths instead)
    /// </summary>
    public static List<string> GetAllScriptPaths()
    {
        return GetUserScriptPaths(); // Redirect to user scripts only
    }

    /// <summary>
    /// Get script content for files mentioned in error messages
    /// </summary>
    public static string GetScriptContextFromErrors(string errorMessage)
    {
        var scriptContext = new StringBuilder();
        var scriptPaths = ExtractScriptPathsFromError(errorMessage);
        
        foreach (var scriptPath in scriptPaths.Take(3)) // Limit to 3 scripts to avoid huge prompts
        {
            var content = GetScriptContent(scriptPath);
            if (!string.IsNullOrEmpty(content))
            {
                var fileName = Path.GetFileName(scriptPath);
                scriptContext.AppendLine($"\n--- Script: {fileName} ---");
                scriptContext.AppendLine(content);
                scriptContext.AppendLine("--- End Script ---\n");
            }
        }
        
        return scriptContext.ToString();
    }

    /// <summary>
    /// Extract script file paths from Unity error messages
    /// </summary>
    private static List<string> ExtractScriptPathsFromError(string errorMessage)
    {
        var paths = new List<string>();
        
        // Match Unity error format: (at Assets/path/to/file.cs:line)
        var regex = new Regex(@"\(at\s+([^:]+\.cs):(\d+).*?\)");
        var matches = regex.Matches(errorMessage);
        
        foreach (Match match in matches)
        {
            var relativePath = match.Groups[1].Value;
            var fullPath = Path.GetFullPath(relativePath);
            if (File.Exists(fullPath) && !paths.Contains(fullPath))
            {
                paths.Add(fullPath);
            }
        }
        
        return paths;
    }

    /// <summary>
    /// Get the content of a script file with line numbers for debugging
    /// </summary>
    public static string GetScriptContent(string filePath, int contextLines = 50)
    {
        try
        {
            if (!File.Exists(filePath))
                return $"File not found: {filePath}";

            var lines = File.ReadAllLines(filePath);
            var fileName = Path.GetFileName(filePath);
            
            // If file is too long, get relevant sections around error lines
            if (lines.Length > contextLines * 2)
            {
                return GetTruncatedScriptContent(lines, fileName, contextLines);
            }
            
            var content = new StringBuilder();
            content.AppendLine($"// File: {fileName}");
            
            for (int i = 0; i < lines.Length; i++)
            {
                content.AppendLine($"{i + 1:D3}: {lines[i]}");
            }
            
            return content.ToString();
        }
        catch (Exception e)
        {
            return $"Error reading {filePath}: {e.Message}";
        }
    }

    private static string GetTruncatedScriptContent(string[] lines, string fileName, int contextLines)
    {
        var content = new StringBuilder();
        content.AppendLine($"// File: {fileName} (showing first {contextLines} lines and last {contextLines} lines)");
        
        // Show beginning of file
        for (int i = 0; i < Math.Min(contextLines, lines.Length); i++)
        {
            content.AppendLine($"{i + 1:D3}: {lines[i]}");
        }
        
        if (lines.Length > contextLines * 2)
        {
            content.AppendLine("... (middle content truncated) ...");
            
            // Show end of file
            int startLine = Math.Max(contextLines, lines.Length - contextLines);
            for (int i = startLine; i < lines.Length; i++)
            {
                content.AppendLine($"{i + 1:D3}: {lines[i]}");
            }
        }
        
        return content.ToString();
    }

    /// <summary>
    /// Get content of recently modified scripts (user scripts only)
    /// </summary>
    public static string GetRecentlyModifiedScripts(int maxScripts = 3)
    {
        if (recentlyModifiedScripts.Count == 0)
            return "";

        var content = new StringBuilder();
        content.AppendLine("\n--- Recently Modified Scripts ---");
        
        var scriptsToShow = recentlyModifiedScripts.TakeLast(maxScripts);
        foreach (var scriptPath in scriptsToShow)
        {
            // Only show user scripts
            if (scriptPath.Contains("Assets/") && !scriptPath.Contains("DuckGPT"))
            {
                var scriptContent = GetScriptContent(scriptPath, 30); // Show 30 lines context
                content.AppendLine(scriptContent);
            }
        }
        
        return content.ToString();
    }

    /// <summary>
    /// Search for scripts by name or content (user scripts only)
    /// </summary>
    public static string FindScriptsByName(string scriptName)
    {
        var scriptPaths = GetUserScriptPaths();
        var matchingScripts = scriptPaths.Where(path => 
            Path.GetFileNameWithoutExtension(path).Contains(scriptName, StringComparison.OrdinalIgnoreCase))
            .Take(3);

        var content = new StringBuilder();
        foreach (var scriptPath in matchingScripts)
        {
            var scriptContent = GetScriptContent(scriptPath, 50);
            content.AppendLine(scriptContent);
        }

        return content.ToString();
    }

    /// <summary>
    /// Get script context for a specific error with line numbers
    /// </summary>
    public static string GetScriptContextForError(string errorMessage, string stackTrace)
    {
        var context = new StringBuilder();
        
        // Extract file and line information from stack trace
        var regex = new Regex(@"\(at\s+([^:]+\.cs):(\d+).*?\)");
        var matches = regex.Matches(stackTrace);
        
        foreach (Match match in matches.Cast<Match>().Take(2)) // Limit to 2 files
        {
            var relativePath = match.Groups[1].Value;
            var lineNumber = int.Parse(match.Groups[2].Value);
            var fullPath = Path.GetFullPath(relativePath);
            
            if (File.Exists(fullPath))
            {
                var fileName = Path.GetFileName(fullPath);
                context.AppendLine($"\n--- Error in {fileName} at line {lineNumber} ---");
                
                try
                {
                    var lines = File.ReadAllLines(fullPath);
                    var startLine = Math.Max(0, lineNumber - 10); // 10 lines before error
                    var endLine = Math.Min(lines.Length - 1, lineNumber + 10); // 10 lines after error
                    
                    for (int i = startLine; i <= endLine; i++)
                    {
                        var marker = i == lineNumber - 1 ? ">>> " : "    ";
                        context.AppendLine($"{marker}{i + 1:D3}: {lines[i]}");
                    }
                }
                catch (Exception e)
                {
                    context.AppendLine($"Error reading file: {e.Message}");
                }
                
                context.AppendLine("--- End Error Context ---\n");
            }
        }
        
        return context.ToString();
    }

#if UNITY_EDITOR
  
    [MenuItem("Window/RubberDuckHelper/Clear Analysis Cache")]
    private static void MenuClearAnalysisCache()
    {
        ClearAnalysisCache();
        EditorUtility.DisplayDialog("Duck Helper", "Analysis cache cleared.", "OK");
    }
   
#endif
}

