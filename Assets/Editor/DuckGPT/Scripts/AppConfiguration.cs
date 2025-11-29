using Codice.Client.Common.GameUI;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Provides a Unity Editor window for configuring DuckGPT integration settings, including API keys, model selection,
/// and user experience level.
/// </summary>
/// <remarks>This window allows users to enter and manage OpenAI and ElevenLabs API keys, select the preferred AI
/// model, and specify their Unity experience level to tailor DuckGPT's responses. Settings are persisted using Unity's
/// EditorPrefs and can be reset to defaults. Access this window via the 'Window > RubberDuckHelper > Settings' menu in
/// the Unity Editor.</remarks>

#if UNITY_EDITOR
internal class AppConfiguration : EditorWindow
{
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Settings", "Cache Management" };

    [Header("OpenAI API Key")]
    const string KeyPref = "DuckGPT_OpenAIKey";
    private string apiKey;

    [Header("OpenAI Model")]
    const string ModelPref = "DuckGPT_OpenAIModels";
    private string model;  
    
    private static readonly string[] availableModels = new[]
    {
        "gpt-4.1-mini",
        "gpt-5",
        "gpt-5-mini",
        "o4-mini",
        "gpt-4.1"
    };
    private int selectedModelIndex;

    [Header("ElevenLabs API Key")]
    const string ElevenLabsKeyPref = "DuckGPT_ElevenLabsKey";
    private string elevenLabsApiKey;

    [Header("Unity Experience Level")]
    const string ExperienceLevelPref = "DuckGPT_ExperienceLevel";
    private string experienceLevel;

    private static readonly string[] experienceLevels = new[]
    {
        "Beginner",
        "Intermediate",
        "Advanced",
        "Expert"
    };
    private int selectedExperienceLevelIndex;
    public static string systemPrompt;

    [Header("Duck Name")]
    private static string DuckNamePref = "DuckGPT";
    private static string DuckNameColorPref = "DuckGPT_NameColor";
    private string duckName;
    private Color duckNameColor = Color.yellow; 

    [MenuItem("Window/DuckGPT/Settings")]
    public static void ShowWindow() => GetWindow<AppConfiguration>("DuckGPT Configurations");

    void OnEnable() 
    {
        apiKey = EditorPrefs.GetString(KeyPref, "");
        model = EditorPrefs.GetString(ModelPref, availableModels[0]);
        elevenLabsApiKey = EditorPrefs.GetString(ElevenLabsKeyPref, "");
        experienceLevel = EditorPrefs.GetString(ExperienceLevelPref, experienceLevels[0]);
        duckName = EditorPrefs.GetString(DuckNamePref, duckName);
        duckNameColor = GetSavedColor();
        GetSystemPrompt();  
    }
    private void OnGUI()
    {
        // Tab toolbar
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));

        GUILayout.Space(10);

        // Display content based on selected tab
        switch (selectedTab)
        {
            case 0:
                DrawSettingsTab();
                break;
            case 1:
                DrawCacheManagementTab();
                break;
        }
    }
    #region App Settings Tab
    private void DrawSettingsTab()
    {
        GUILayout.Label("AI CONFIGUARTIONS", EditorStyles.largeLabel);
        GUILayout.Space(5);

        #region API KEY SETTER

        GUILayout.Label("OpenAI API Key", EditorStyles.boldLabel);
        apiKey = EditorGUILayout.TextField("API Key", apiKey);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save"))
        {
            EditorPrefs.SetString(KeyPref, apiKey.Trim());
            Debug.Log("[DuckGPT] API Key saved");
        }
        if (GUILayout.Button("Clear"))
        {
            EditorPrefs.DeleteKey(KeyPref);
            apiKey = "";
            Debug.Log("[DuckGPT] API Key cleared");
        }
        GUILayout.EndHorizontal();
        #endregion

        #region MODEL SETTER

        GUILayout.Space(10);

        GUILayout.Label("OpenAI Model", EditorStyles.boldLabel);

        selectedModelIndex = EditorGUILayout.Popup("Model", selectedModelIndex, availableModels);
        model = availableModels[selectedModelIndex];

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save"))
        {
            EditorPrefs.SetString(ModelPref, model);
            Debug.Log($"[DuckGPT] Model set to {model}");

        }
        if (GUILayout.Button("Reset"))
        {
            EditorPrefs.DeleteKey(ModelPref);
            model = availableModels[0];
            selectedModelIndex = 0;
            Debug.Log("[DuckGPT] Model reset to default: " + model);
        }
        GUILayout.EndHorizontal();

        #endregion

        #region ELEVEN LABS API KEY SETTER
        GUILayout.Space(10);

        GUILayout.Label("Eleven Labs API Key", EditorStyles.boldLabel);

        elevenLabsApiKey = EditorGUILayout.TextField("Eleven Labs API Key", elevenLabsApiKey);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save"))
        {
            EditorPrefs.SetString(ElevenLabsKeyPref, elevenLabsApiKey.Trim());
            Debug.Log("[DuckGPT] Eleven Labs API Key saved");
        }
        if (GUILayout.Button("Clear"))
        {
            EditorPrefs.DeleteKey(ElevenLabsKeyPref);
            Debug.Log("[DuckGPT] Eleven Labs API Key cleared");
            elevenLabsApiKey = "";
        }
        GUILayout.EndHorizontal();
        #endregion


        GUILayout.Space(20);
        GUILayout.Label("RUBBER DUCK CONFIGUARTIONS", EditorStyles.largeLabel);
        GUILayout.Space(5);

        #region EXPERIENCE LEVEL SETTER
        GUILayout.Label("Unity Experience Level", EditorStyles.boldLabel);
        GUILayout.Label("Customize DuckGPT's responses for your experience level.");
        selectedExperienceLevelIndex = EditorGUILayout.Popup("Experience Level", selectedExperienceLevelIndex, experienceLevels);
        experienceLevel = experienceLevels[selectedExperienceLevelIndex];
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save"))
        {
            EditorPrefs.SetString(ExperienceLevelPref, experienceLevel);
            Debug.Log($"[DuckGPT] Experience level set to {experienceLevel}.");

        }
        if (GUILayout.Button("Reset"))
        {
            EditorPrefs.DeleteKey(ExperienceLevelPref);
            experienceLevel = experienceLevels[0];
            selectedExperienceLevelIndex = 0;
            Debug.Log("[DuckGPT] Experience level reset to default: " + experienceLevel);
        }
        GUILayout.EndHorizontal();
        #endregion

        #region Rubber Duck Name
        GUILayout.Space(10);

        GUILayout.Label("Rubber Duck Name", EditorStyles.boldLabel);
        GUILayout.Label("Name your rubber duck");

        duckName = EditorGUILayout.TextField("Name", duckName);
        duckNameColor = EditorGUILayout.ColorField("Name Color", duckNameColor);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save"))
        {
            EditorPrefs.SetString(DuckNamePref, duckName.Trim());

            // Save color
            EditorPrefs.SetFloat(DuckNameColorPref + "_R", duckNameColor.r);
            EditorPrefs.SetFloat(DuckNameColorPref + "_G", duckNameColor.g);
            EditorPrefs.SetFloat(DuckNameColorPref + "_B", duckNameColor.b);
            EditorPrefs.SetFloat(DuckNameColorPref + "_A", duckNameColor.a);

            GetWindow<DuckEditorWindow>().UpdateDuckName();
            Debug.Log($"[DuckGPT] Name '{duckName}' and color saved");
        }
        if (GUILayout.Button("Clear"))
        {
            EditorPrefs.DeleteKey(DuckNamePref);
            EditorPrefs.DeleteKey(DuckNameColorPref + "_R");
            EditorPrefs.DeleteKey(DuckNameColorPref + "_G");
            EditorPrefs.DeleteKey(DuckNameColorPref + "_B");
            EditorPrefs.DeleteKey(DuckNameColorPref + "_A");

            duckName = "";
            duckNameColor = Color.yellow;

            Debug.Log("[DuckGPT] Name and color cleared");
        }
        GUILayout.EndHorizontal();

        #endregion
    }

    #endregion


    #region Cache Management Tab
    private void DrawCacheManagementTab()
    {
        GUILayout.Label("DuckGPT Cache Management", EditorStyles.largeLabel);
        GUILayout.Space(10);

        GUILayout.Label("Cache Management", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (GUILayout.Button("Clear Chat History", GUILayout.Height(30)))
        {
            ChatMemory.Clear();
            DebugColor.Log("DuckGPT: Chat history cleared.", "Yellow");
        }

        if (GUILayout.Button("Clear Hierarchy Cache", GUILayout.Height(30)))
        {
            HierarchyMemory.MenuClearCache();
            DebugColor.Log("DuckGPT: Hierarchy cache cleared.", "Yellow");
        }

        if (GUILayout.Button("Clear Analysis Cache", GUILayout.Height(30)))
        {
            ScriptsHandler.ClearAnalysisCache();
            DebugColor.Log("DuckGPT: Analysis cache cleared.", "Yellow");
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Clear All Caches", GUILayout.Height(35)))
        {
            if (EditorUtility.DisplayDialog("Clear All Caches",
                "Are you sure you want to clear all DuckGPT caches?\n\n" +
                "This will clear:\n" +
                "Chat History\n" +
                "Hierarchy Cache\n" +
                "Analysis Cache",
                "Yes", "Cancel"))
            {
                ChatMemory.Clear();
                HierarchyMemory.MenuClearCache();
                ScriptsHandler.ClearAnalysisCache();
                DebugColor.Log("DuckGPT: All caches cleared.", "Yellow");
            }
        }
    }
    #endregion



#region Get Saved Configurations
public static string GetSavedName() => EditorPrefs.GetString(DuckNamePref, "");
    public static Color GetSavedColor()
    {
        float r = EditorPrefs.GetFloat(DuckNameColorPref + "_R", 1f); // Default yellow
        float g = EditorPrefs.GetFloat(DuckNameColorPref + "_G", 1f);
        float b = EditorPrefs.GetFloat(DuckNameColorPref + "_B", 0f);
        float a = EditorPrefs.GetFloat(DuckNameColorPref + "_A", 1f);
        return new Color(r, g, b, a);
    }

    public static string GetSavedKey() => EditorPrefs.GetString(KeyPref, "");
    public static string GetSavedModel() => EditorPrefs.GetString(ModelPref, availableModels[0]);
    public static string GetSavedElevenLabsKey() => EditorPrefs.GetString(ElevenLabsKeyPref, "");
    public static string GetSavedExperienceLevel() => EditorPrefs.GetString(ExperienceLevelPref, experienceLevels[0]);
    public static string GetSystemPrompt() // Sets system prompt based on saved experience level
    {
        string[] systemPrompts = new[]
  {
        "You are a friendly rubber duck AI. You respond conversationally and encourage the user to explain their thought process. Always add some duck noises. Also try to give short answers. Do not give full solutions, give guidance. Explain things in simple terms.",
        "You are a helpful rubber duck AI tailored to an intermediate developer. Provide clear, practical explanations, suggest implementations and debugging steps, and avoid overly simplistic ELI5 language. Keep answers concise but actionable.",
        "You are a knowledgeable rubber duck AI for advanced developers. Provide concise technical advice, point out likely causes, and suggest best-practice fixes. Include relevant code snippets when helpful and prioritize efficiency and correctness.",
        "You are a highly knowledgeable rubber duck AI. You provide concise, technical explanations and expect the user to have a strong understanding of programming concepts. Focus on efficiency and best practices, avoid basic explanations, and give short actionable guidance."
    };

        var savedExpLevel = GetSavedExperienceLevel();
        int experienceLvlIndex = System.Array.IndexOf(experienceLevels, savedExpLevel);
        if (experienceLvlIndex < 0) experienceLvlIndex = 0;

        systemPrompt = systemPrompts[experienceLvlIndex];

        return systemPrompt;
    }
}


#endregion

#endif
