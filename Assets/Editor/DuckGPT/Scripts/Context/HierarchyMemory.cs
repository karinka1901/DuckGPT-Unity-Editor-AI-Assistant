#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class GameObjectData
{
    public String name;
    public bool activeSelf;
    public bool activeInHierarchy;
    public String tag;
    public String layer;
    public List<String> components = new List<String>();
    public List<GameObjectData> children = new List<GameObjectData>();
    public long lastModified;
}

[Serializable]
public class SceneHierarchyCache
{
    public String sceneName;
    public String scenePath;
    public List<GameObjectData> rootObjects = new List<GameObjectData>();
    public long lastCacheTime;
    public String hierarchyHash;
}
/// <summary>
/// Provides static methods for managing and retrieving information about the Unity scene hierarchy, including tracking
/// changes and caching hierarchy data for efficient access.
/// </summary>
/// <remarks>HierarchyMemory monitors Unity Editor events to detect changes in the scene hierarchy and maintains a
/// cache of the current state. The cache is persisted as a JSON file in the ProjectSettings directory, enabling quick
/// retrieval of hierarchy context or specific GameObject information. This class is intended for use within the Unity
/// Editor and is not suitable for runtime environments.</remarks>
public static class HierarchyMemory
{
    private static readonly Regex QuotedNameRegex = new(@"[""']([^""']+)[""']", RegexOptions.Compiled); // Regex to match quoted names in the format "name" or 'name'

    private static SceneHierarchyCache sceneHierarchyCache;
    private static bool isSubscribed;

    private static String CacheFilePath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "../ProjectSettings/DuckGPT_HierarchyCache.json"));

    static HierarchyMemory()
    {
        SubscribeToHierarchyChanges();
    }

    private static void SubscribeToHierarchyChanges() // Subscribes to Unity Editor events to track changes in the scene hierarchy
    {
        if (isSubscribed)
            return;

        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;

        isSubscribed = true;
    }
    #region Event Handlers
    private static void OnHierarchyChanged()
    {
        RefreshCache();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshCache();
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        sceneHierarchyCache = null;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode ||
            state == PlayModeStateChange.EnteredPlayMode)
        {
            RefreshCache();
        }
    }

    public static String GetHierarchyContextIfChanged()
    {
        EnsureCacheLoaded();

        if (sceneHierarchyCache == null || HasHierarchyChanged())
        {
            RefreshCache();
            return FormatHierarchyContext();
        }

        return "";
    }
    #endregion

    public static String GetFullHierarchyContext()
    {
        EnsureCacheLoaded();
        RefreshCache();
        return FormatHierarchyContext();
    }

    public static String GetGameObjectContext(String prompt) // Retrieves context information about GameObjects mentioned in the provided prompt, based on the cached scene hierarchy data.
    {
        if (String.IsNullOrWhiteSpace(prompt))
            return "";

        EnsureCacheLoaded();

        if (sceneHierarchyCache?.rootObjects == null || sceneHierarchyCache.rootObjects.Count == 0) // If the cache is not available or empty, return an empty string as context.
            return "";

        List<String> mentionedObjects = ExtractGameObjectNames(prompt)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (mentionedObjects.Count == 0)
            return "";

        StringBuilder context = new();
        bool foundAny = false;

        foreach (String objectName in mentionedObjects)
        {
            GameObjectData objectData = FindGameObjectInCache(objectName);
            if (objectData == null)
                continue;

            if (!foundAny)
            {
                context.AppendLine("\nMentioned GameObjects:");
                foundAny = true;
            }

            context.AppendLine($"\n{objectName}:");
            context.AppendLine($"  State: active={objectData.activeSelf}, tag={objectData.tag}, layer={objectData.layer}");
            context.AppendLine($"  Components: {String.Join(", ", objectData.components)}");
        }

        return foundAny ? context.ToString() : ""; 
    }

    private static void RefreshCache() // Rebuilds the scene hierarchy cache by capturing the current state of the active scene and saving it to disk. 
    {
        try
        {
            SceneHierarchyCache newCache = BuildCacheForActiveScene();
            if (newCache == null)
                return;

            sceneHierarchyCache = newCache;
            SaveCache();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[DuckGPT]: Failed to update hierarchy cache: {exception.Message}");
        }
    }

    private static SceneHierarchyCache BuildCacheForActiveScene() // Constructs a new SceneHierarchyCache object by capturing the current state of the active scene
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return null;

        SceneHierarchyCache newCache = new()
        {
            sceneName = scene.name,
            scenePath = scene.path,
            lastCacheTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject rootObject in rootObjects)
            newCache.rootObjects.Add(CaptureGameObjectData(rootObject));

        newCache.hierarchyHash = CreateHierarchyHash(newCache);
        return newCache;
    }

    private static GameObjectData CaptureGameObjectData(GameObject gameObject)
    {
        GameObjectData data = new()
        {
            name = gameObject.name,
            activeSelf = gameObject.activeSelf,
            activeInHierarchy = gameObject.activeInHierarchy,
            tag = gameObject.tag,
            layer = LayerMask.LayerToName(gameObject.layer),
            lastModified = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        Component[] components = gameObject.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null)
                continue;

            data.components.Add(GetComponentDescription(component));
        }

        foreach (Transform child in gameObject.transform)
            data.children.Add(CaptureGameObjectData(child.gameObject));

        return data;
    }

    private static String GetComponentDescription(Component component)
    {
        String componentInfo = component.GetType().Name;

        if (component is Behaviour behaviour)
            return $"{componentInfo} (enabled={behaviour.enabled})";

        if (component is Renderer renderer)
            return $"{componentInfo} (enabled={renderer.enabled})";

        return componentInfo;
    }

    private static String CreateHierarchyHash(SceneHierarchyCache cache)
    {
        StringBuilder hashBuilder = new StringBuilder();

        foreach (GameObjectData rootObject in cache.rootObjects)
            AppendObjectHash(hashBuilder, rootObject);

        return hashBuilder.ToString().GetHashCode().ToString();
    }

    private static void AppendObjectHash(StringBuilder builder, GameObjectData gameObjectData)
    {
        builder.Append($"{gameObjectData.name}|{gameObjectData.activeSelf}|{gameObjectData.tag}|{gameObjectData.layer}|");
        builder.Append(String.Join(",", gameObjectData.components));

        foreach (GameObjectData child in gameObjectData.children)
            AppendObjectHash(builder, child);
    }

    private static bool HasHierarchyChanged()
    {
        if (sceneHierarchyCache == null)
            return true;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return false;

        if (sceneHierarchyCache.sceneName != scene.name)
            return true;

        if (sceneHierarchyCache.scenePath != scene.path)
            return true;

        GameObject[] currentRootObjects = scene.GetRootGameObjects();
        return currentRootObjects.Length != sceneHierarchyCache.rootObjects.Count;
    }

    private static String FormatHierarchyContext()
    {
        if (sceneHierarchyCache?.rootObjects == null || sceneHierarchyCache.rootObjects.Count == 0)
            return "";

        StringBuilder context = new StringBuilder();
        context.AppendLine($"\nScene Hierarchy ({sceneHierarchyCache.sceneName}):");

        foreach (GameObjectData rootObject in sceneHierarchyCache.rootObjects)
            FormatGameObjectContext(context, rootObject, 0);

        return context.ToString();
    }

    private static void FormatGameObjectContext(StringBuilder builder, GameObjectData gameObjectData, int indent)
    {
        String indentText = new String(' ', indent * 2);
        builder.AppendLine($"{indentText}{gameObjectData.name} [active={gameObjectData.activeSelf}, tag={gameObjectData.tag}]");

        if (gameObjectData.components.Count > 0)
            builder.AppendLine($"{indentText}  Components: {String.Join(", ", gameObjectData.components.Take(5))}");

        foreach (GameObjectData child in gameObjectData.children)
            FormatGameObjectContext(builder, child, indent + 1);
    }

    private static List<String> ExtractGameObjectNames(String prompt)
    {
        List<String> names = new List<String>();
        MatchCollection matches = QuotedNameRegex.Matches(prompt);

        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
                names.Add(match.Groups[1].Value);
        }

        return names;
    }

        private static GameObjectData FindGameObjectInCache(String name)
    {
        if (sceneHierarchyCache?.rootObjects == null)
            return null;

        foreach (GameObjectData rootObject in sceneHierarchyCache.rootObjects)
        {
            GameObjectData foundObject = FindInGameObjectData(rootObject, name);
            if (foundObject != null)
                return foundObject;
        }

        return null;
    }

    private static GameObjectData FindInGameObjectData(GameObjectData gameObjectData, String name)
    {
        if (gameObjectData.name.Equals(name, StringComparison.OrdinalIgnoreCase))
            return gameObjectData;

        foreach (GameObjectData child in gameObjectData.children)
        {
            GameObjectData foundObject = FindInGameObjectData(child, name);
            if (foundObject != null)
                return foundObject;
        }

        return null;
    }

    private static void EnsureCacheLoaded()
    {
        if (sceneHierarchyCache != null)
            return;

        try
        {
            if (!File.Exists(CacheFilePath))
                return;

            String json = File.ReadAllText(CacheFilePath);
            sceneHierarchyCache = JsonConvert.DeserializeObject<SceneHierarchyCache>(json);

            Scene currentScene = SceneManager.GetActiveScene();
            if (sceneHierarchyCache?.sceneName != currentScene.name || sceneHierarchyCache?.scenePath != currentScene.path)
                sceneHierarchyCache = null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"DuckGPT: Failed to load hierarchy cache: {exception.Message}");
            sceneHierarchyCache = null;
        }
    }

    private static void SaveCache()
    {
        try
        {
            String directory = Path.GetDirectoryName(CacheFilePath);
            if (!String.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            String json = JsonConvert.SerializeObject(sceneHierarchyCache, Formatting.Indented);
            File.WriteAllText(CacheFilePath, json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"DuckGPT: Failed to save hierarchy cache: {exception.Message}");
        }
    }
}
#endif