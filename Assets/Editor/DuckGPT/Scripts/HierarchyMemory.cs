#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class GameObjectData
{
    public string name;
    public bool activeSelf;
    public bool activeInHierarchy;
    public string tag;
    public string layer;
    public List<string> components = new();
    public List<GameObjectData> children = new();
    public long lastModified;
}

[Serializable]
public class SceneHierarchyCache
{
    public string sceneName;
    public string scenePath; 
    public List<GameObjectData> rootObjects = new();
    public long lastCacheTime;
    public string hierarchyHash; // To detect changes
}

public static class HierarchyMemory
{
    private static SceneHierarchyCache currentCache;
    private static bool isSubscribed = false;

    private static string CacheFilePath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "../ProjectSettings/DuckGPT_HierarchyCache.json"));

    static HierarchyMemory()
    {
        SubscribeToHierarchyChanges();
    }

    private static void SubscribeToHierarchyChanges()
    {
        if (isSubscribed) return;

        // Subscribe to various Unity events that indicate hierarchy changes
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;

        isSubscribed = true;
    }

    private static void OnHierarchyChanged()
    {
        // Hierarchy changed - invalidate cache
        Debug.Log("DuckGPT: Hierarchy changed, updating cache...");
        _ = System.Threading.Tasks.Task.Run(() => UpdateCacheAsync());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"DuckGPT: Scene loaded: {scene.name}");
        _ = System.Threading.Tasks.Task.Run(() => UpdateCacheAsync());
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        Debug.Log($"DuckGPT: Scene unloaded: {scene.name}");
        currentCache = null;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.EnteredPlayMode)
        {
            _ = System.Threading.Tasks.Task.Run(() => UpdateCacheAsync());
        }
    }

    // Get hierarchy context only if changes occurred
    public static string GetHierarchyContextIfChanged()
    {
        EnsureCacheLoaded();

        if (currentCache == null || HasHierarchyChanged())
        {
            UpdateCacheAsync().Wait(); // Force synchronous update for immediate use
            return FormatHierarchyContext();
        }

        return ""; // No changes, return empty (don't send hierarchy)
    }

    // Get full hierarchy context (for manual requests)
    public static string GetFullHierarchyContext()
    {
        EnsureCacheLoaded();
        UpdateCacheAsync().Wait();
        return FormatHierarchyContext();
    }

    // Get only GameObjects mentioned in the prompt
    public static string GetGameObjectContext(string prompt)
    {
        if (string.IsNullOrEmpty(prompt)) return "";

        EnsureCacheLoaded();
        if (currentCache == null) return "";

        var mentionedObjects = ExtractGameObjectNames(prompt);
        if (mentionedObjects.Count == 0) return "";

        var context = new StringBuilder();
        context.AppendLine("\nMentioned GameObjects:");

        foreach (var objName in mentionedObjects)
        {
            var objData = FindGameObjectInCache(objName);
            if (objData != null)
            {
                context.AppendLine($"\n{objName}:");
                context.AppendLine($"  State: active={objData.activeSelf}, tag={objData.tag}, layer={objData.layer}");
                context.AppendLine($"  Components: {string.Join(", ", objData.components)}");
            }
        }

        return context.ToString();
    }

    private static async System.Threading.Tasks.Task UpdateCacheAsync() 
    {
        try
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            var newCache = new SceneHierarchyCache
            {
                sceneName = scene.name,
                scenePath = scene.path, // Use scene.path instead of scene.guid
                lastCacheTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            // Build hierarchy data
            var rootObjects = scene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                newCache.rootObjects.Add(CaptureGameObjectData(root));
            }

            // Create hash to detect future changes
            newCache.hierarchyHash = CreateHierarchyHash(newCache);

            currentCache = newCache;
            SaveCache();

            Debug.Log($"DuckGPT: Cached hierarchy for scene '{scene.name}' with {newCache.rootObjects.Count} root objects");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DuckGPT: Failed to update hierarchy cache: {e.Message}");
        }
    }

    private static GameObjectData CaptureGameObjectData(GameObject obj)
    {
        var data = new GameObjectData
        {
            name = obj.name,
            activeSelf = obj.activeSelf,
            activeInHierarchy = obj.activeInHierarchy,
            tag = obj.tag,
            layer = LayerMask.LayerToName(obj.layer),
            lastModified = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // Capture components
        var components = obj.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp != null)
            {
                var compInfo = comp.GetType().Name;
                if (comp is Behaviour behaviour)
                    compInfo += $" (enabled={behaviour.enabled})";
                else if (comp is Renderer renderer)
                    compInfo += $" (enabled={renderer.enabled})";

                data.components.Add(compInfo);
            }
        }

        // Recursively capture children
        foreach (Transform child in obj.transform)
        {
            data.children.Add(CaptureGameObjectData(child.gameObject));
        }

        return data;
    }

    private static string CreateHierarchyHash(SceneHierarchyCache cache)
    {
        var hashBuilder = new StringBuilder();
        foreach (var root in cache.rootObjects)
        {
            AppendObjectHash(hashBuilder, root);
        }
        return hashBuilder.ToString().GetHashCode().ToString();
    }

    private static void AppendObjectHash(StringBuilder sb, GameObjectData obj)
    {
        sb.Append($"{obj.name}|{obj.activeSelf}|{obj.tag}|{obj.layer}|");
        sb.Append(string.Join(",", obj.components));
        foreach (var child in obj.children)
        {
            AppendObjectHash(sb, child);
        }
    }

    private static bool HasHierarchyChanged()
    {
        if (currentCache == null) return true;

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) return false;

        if (currentCache.sceneName != scene.name) return true;

        // Quick check: compare root object count
        var currentRoots = scene.GetRootGameObjects();
        if (currentRoots.Length != currentCache.rootObjects.Count) return true;

        // Could add more sophisticated change detection here
        return false;
    }

    private static string FormatHierarchyContext()
    {
        if (currentCache?.rootObjects == null || currentCache .rootObjects.Count == 0)
            return "";

        var context = new StringBuilder();
        context.AppendLine($"\nScene Hierarchy ({currentCache.sceneName}):");

        foreach (var root in currentCache.rootObjects)
        {
            FormatGameObjectContext(context, root, 0);
        }

        return context.ToString();
    }

    private static void FormatGameObjectContext(StringBuilder sb, GameObjectData obj, int indent)
    {
        var indentStr = new string(' ', indent * 2);
        sb.AppendLine($"{indentStr}{obj.name} [active={obj.activeSelf}, tag={obj.tag}]");
        
        if (obj.components.Count > 0)
        {
            sb.AppendLine($"{indentStr}  Components: {string.Join(", ", obj.components.Take(5))}"); // Limit to 5 most important
        }

        foreach (var child in obj.children)
        {
            FormatGameObjectContext(sb, child, indent + 1);
        }
    }

    private static List<string> ExtractGameObjectNames(string prompt)
    {
        var names = new List<string>();
        var matches = System.Text.RegularExpressions.Regex.Matches(prompt, @"[""']([^""']+)[""']");
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                names.Add(match.Groups[1].Value);
            }
        }

        return names;
    }

    private static GameObjectData FindGameObjectInCache(string name)
    {
        if (currentCache?.rootObjects == null) return null;

        foreach (var root in currentCache.rootObjects)
        {
            var found = FindInGameObjectData(root, name);
            if (found != null) return found;
        }
        return null;
    }

    private static GameObjectData FindInGameObjectData(GameObjectData obj, string name)
    {
        if (obj.name.Equals(name, StringComparison.OrdinalIgnoreCase))
            return obj;

        foreach (var child in obj.children)
        {
            var found = FindInGameObjectData(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static void EnsureCacheLoaded()
    {
        if (currentCache != null) return;

        try
        {
            if (File.Exists(CacheFilePath))
            {
                var json = File.ReadAllText(CacheFilePath);
                currentCache = JsonConvert.DeserializeObject<SceneHierarchyCache>(json);
                
                // Validate cache is for current scene
                var scene = SceneManager.GetActiveScene();
                if (currentCache?.sceneName != scene.name)
                {
                    currentCache = null;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DuckGPT: Failed to load hierarchy cache: {e.Message}");
            currentCache = null;
        }
    }

    private static void SaveCache()
    {
        try
        {
            var dir = Path.GetDirectoryName(CacheFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonConvert.SerializeObject(currentCache, Formatting.Indented);
            File.WriteAllText(CacheFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DuckGPT: Failed to save hierarchy cache: {e.Message}");
        }
    }

#if UNITY_EDITOR

    [MenuItem("Window/RubberDuckHelper/Clear Hierarchy Cache")]
    private static void MenuClearCache()
    {
        currentCache = null;
        if (File.Exists(CacheFilePath))
            File.Delete(CacheFilePath);
       DebugColor.Log("DuckGPT: Hierarchy cache cleared.", "Yellow");
    }
#endif
}
#endif