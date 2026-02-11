using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.ComponentModel;

internal class AppConfiguration : EditorWindow
{
    #region Variables
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

    // Add these new variables for folder selection
    [Header("Project Analysis Folders")]
    private const string AnalysisFoldersPref = "DuckGPT_AnalysisFolders";
    private List<string> includedFolders = new List<string>();
    private Vector2 folderScrollPosition;

    #endregion

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

        LoadIncludedFolders();
        GetSystemPrompt();
    }

    private void LoadIncludedFolders()
    {
        string savedFolders = EditorPrefs.GetString(AnalysisFoldersPref, "Assets/Scripts");
        if (!string.IsNullOrEmpty(savedFolders))
        {
            includedFolders = new List<string>(savedFolders.Split('|'));
        }
        else
        {
            // Default folders
            includedFolders = new List<string> { "Assets/Scripts" };
        }
    }

    private void SaveIncludedFolders()
    {
        string foldersString = string.Join("|", includedFolders);
        EditorPrefs.SetString(AnalysisFoldersPref, foldersString);
    }

    private VisualElement folderListContainer;

    public void CreateGUI()
    {
        var root = rootVisualElement;
        root.AddToClassList("config-root");

        root.Clear();

        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Assets/Editor/DuckGPT/Scripts/ConfigurationsWindowStyleSheet.uss");
        if (styleSheet != null) root.styleSheets.Add(styleSheet);


        #region Tab Buttons and Containers
        VisualElement tabContainer = new()
        {
            name = "TabContainer"
        };
        tabContainer.AddToClassList("tab-container");

        VisualElement settingsTab = new()
        {
            name = "SettingsTab"
        };
        VisualElement cacheManagementTab = new()
        {
            name = "CacheManagementTab"
        };
        cacheManagementTab.style.display = DisplayStyle.None;

        Button settingsTabButton = new(() =>
        {
            settingsTab.style.display = DisplayStyle.Flex;
            cacheManagementTab.style.display = DisplayStyle.None;
       
        })
        {
            text = "App Settings",
            name = "SettingsTabButton"
        };
        settingsTabButton.AddToClassList("tab-button");
        settingsTabButton.AddToClassList("custom-button");  

        Button cacheManagementTabButton = new(() =>
        {
            settingsTab.style.display = DisplayStyle.None;
            cacheManagementTab.style.display = DisplayStyle.Flex;
      
        })
        {
            text = "Cache Management",
            name = "CacheTabButton"
        };
        cacheManagementTabButton.AddToClassList("tab-button");
        cacheManagementTabButton.AddToClassList("custom-button"); 

        tabContainer.Add(settingsTabButton);
        tabContainer.Add(cacheManagementTabButton);

        root.Add(tabContainer);
        root.Add(settingsTab);
        root.Add(cacheManagementTab);
        #endregion

        #region AI Configurations

        #region OpenAI

        VisualElement aiConfig = new()
        {
            name = "AIConfig"
        };
        Label aiConfigLabel = new("AI CONFIGURATIONS")
        {
            name = "SectionLabel"
        };

        VisualElement openAIConfigContainer = new()
        {
            name = "OpenAIConfigContainer"
        };
        openAIConfigContainer.AddToClassList("openai-config-container");

        Label apiKeyLabel = new("OpenAI API Key:");
        apiKeyLabel.AddToClassList("api-key-label");

        TextField apiKeyField = new()
        {
            value = apiKey
        };
        apiKeyField.AddToClassList("api-key-field");

        openAIConfigContainer.Add(apiKeyLabel);
        openAIConfigContainer.Add(apiKeyField);

        VisualElement buttonsContainer = new()
        {
            name = "ButtonsContainer",
        };
        buttonsContainer.AddToClassList("buttons-container");
        Button saveApiKeyButton = new(() =>
        {
            EditorPrefs.SetString(KeyPref, apiKeyField.value.Trim());
            DebugColor.Log("[DuckGPT] API Key saved", "yellow");
        })
        {
            text = "Save"
        };
        saveApiKeyButton.AddToClassList("settings-button");
        saveApiKeyButton.AddToClassList("custom-button");

        Button clearApiKeyButton = new (() =>
        {
            EditorPrefs.DeleteKey(KeyPref);
            apiKeyField.value = "";
            DebugColor.Log("[DuckGPT] API Key cleared", "blue");
        })
        {
            text = "Clear"
        };
        clearApiKeyButton.AddToClassList("settings-button");
        clearApiKeyButton.AddToClassList("custom-button");
        buttonsContainer.Add(saveApiKeyButton);
        buttonsContainer.Add(clearApiKeyButton);

        VisualElement modelTypeContainer = new(){
            name = "ModelButtonsContainer",
        };
        modelTypeContainer.AddToClassList("openai-config-container");

        Label modelLabel = new("OpenAI Model:");
        modelLabel.AddToClassList("model-label");

        PopupField<string> modelPopup = new(
            new List<string>(availableModels),
            model
        )
        {
            value = model
        };
        modelPopup.AddToClassList("field");

        VisualElement modelButtonsContainer = new()
        {
            name = "ModelButtonsContainer",
        };
        modelButtonsContainer.AddToClassList("buttons-container");
        modelTypeContainer.Add(modelLabel);
        modelTypeContainer.Add(modelPopup);

        Button saveModelButton = new(() =>
        {
            EditorPrefs.SetString(ModelPref, modelPopup.value);
            DebugColor.Log($"[DuckGPT] Model set to {modelPopup.value}", "yellow");
        })
        {
            text = "Save"
        };
        saveModelButton.AddToClassList("settings-button");
        saveModelButton.AddToClassList("custom-button");

        Button resetModelButton = new(() =>
        {
            EditorPrefs.DeleteKey(ModelPref);
            modelPopup.value = availableModels[0];
            DebugColor.Log("[DuckGPT] Model reset to default: " + modelPopup.value, "blue");
        })
        {
            text = "Reset"
        };
        resetModelButton.AddToClassList("settings-button");
        resetModelButton.AddToClassList("custom-button");
        modelButtonsContainer.Add(saveModelButton);
        modelButtonsContainer.Add(resetModelButton);

        #endregion

        #region Elevent Labs
        Label elevenLabsKeyLabel = new("Eleven Labs API Key:");

        TextField elevenLabsKeyField = new()
        {
            value = elevenLabsApiKey
        };

        VisualElement elevenLabsButtonsContainer = new()
        {
            name = "ElevenLabsButtonsContainer",
        };
        elevenLabsButtonsContainer.AddToClassList("buttons-container");

        Button saveElevenLabsKeyButton = new(() =>
        {
            EditorPrefs.SetString(ElevenLabsKeyPref, elevenLabsKeyField.value.Trim());
            DebugColor.Log("[DuckGPT] Eleven Labs API Key saved", "yellow");
        })
        {
            text = "Save"
        };
        saveElevenLabsKeyButton.AddToClassList("settings-button");
        saveElevenLabsKeyButton.AddToClassList("custom-button");

        Button clearElevenLabsKeyButton = new (() =>
        {
            EditorPrefs.DeleteKey(ElevenLabsKeyPref);
            elevenLabsKeyField.value = "";
            DebugColor.Log("[DuckGPT] Eleven Labs API Key cleared", "blue");
        })
        {
            text = "Clear"
        };

        elevenLabsButtonsContainer.Add(saveElevenLabsKeyButton);
        elevenLabsButtonsContainer.Add(clearElevenLabsKeyButton);

        clearElevenLabsKeyButton.AddToClassList("settings-button");
        clearElevenLabsKeyButton.AddToClassList("custom-button");

        #endregion

        aiConfig.Add(aiConfigLabel);
        aiConfig.Add(openAIConfigContainer);
        aiConfig.Add(buttonsContainer);

        aiConfig.Add(modelTypeContainer);
        aiConfig.Add(modelButtonsContainer);

        aiConfig.Add(elevenLabsKeyLabel);
        aiConfig.Add(elevenLabsKeyField);
        aiConfig.Add(elevenLabsButtonsContainer);



        #endregion

        #region Rubber Duck Configurations

        VisualElement duckConfig = new()
        {
            name = "DuckConfig"
        };
        Label duckConfigLabel = new("RUBBER DUCK CONFIGURATIONS")
        {
            name = "SectionLabel"
        };
        Label experienceLevelLabel = new("Unity Experience Level:");
        PopupField<string> experienceLevelPopup = new(
            new List<string>(experienceLevels),
            experienceLevel
        )
        {
            value = experienceLevel
        };

        VisualElement experienceLevelButtonsContainer = new()
        {
            name = "ExperienceLevelButtonsContainer",
        };
        experienceLevelButtonsContainer.AddToClassList("buttons-container");

        Button saveExperienceLevelButton = new(() =>
        {
            EditorPrefs.SetString(ExperienceLevelPref, experienceLevelPopup.value);
            GetSystemPrompt();
            DebugColor.Log($"[DuckGPT] Experience level set to {experienceLevelPopup.value}.", "yellow");
        })
        {
            text = "Save"
        };
        saveExperienceLevelButton.AddToClassList("settings-button");
        saveExperienceLevelButton.AddToClassList("custom-button");

        Button resetExperienceLevelButton = new(() =>
        {
            EditorPrefs.DeleteKey(ExperienceLevelPref);
            experienceLevelPopup.value = experienceLevels[0];
            GetSystemPrompt();
            DebugColor.Log("[DuckGPT] Experience level reset to default: " + experienceLevelPopup.value, "blue");
        })
        {
            text = "Reset"
        };
        resetExperienceLevelButton.AddToClassList("settings-button");
        resetExperienceLevelButton.AddToClassList("custom-button");

        experienceLevelButtonsContainer.Add(saveExperienceLevelButton);
        experienceLevelButtonsContainer.Add(resetExperienceLevelButton);

        Label duckAppearance = new("Duck Name and Accent Color:");

        VisualElement duckNameContainer = new()
        {
            name = "DuckAppearanceContainer"
        };
        duckNameContainer.AddToClassList("duck-name-container");

        Label duckNameLabel = new("Duck Name:");
        duckNameLabel.AddToClassList("duck-name-label");

        TextField duckNameField = new()
        {
            value = duckName
        };
        duckNameField.AddToClassList("duck-name-field");

        duckNameContainer.Add(duckNameLabel);
        duckNameContainer.Add(duckNameField);

        ColorField duckNameColorField = new()
        {
            value = duckNameColor
        };

        VisualElement duckNameButtonsContainer = new()
        {
            name = "DuckNameButtonsContainer",
        };
        duckNameButtonsContainer.AddToClassList("buttons-container");

        Button saveDuckNameButton = new(() =>
        {
            EditorPrefs.SetString(DuckNamePref, duckNameField.value.Trim());
            // Save color
            EditorPrefs.SetFloat(DuckNameColorPref + "_R", duckNameColorField.value.r);
            EditorPrefs.SetFloat(DuckNameColorPref + "_G", duckNameColorField.value.g);
            EditorPrefs.SetFloat(DuckNameColorPref + "_B", duckNameColorField.value.b);
            EditorPrefs.SetFloat(DuckNameColorPref + "_A", duckNameColorField.value.a);
            GetWindow<PrimaryEditorWindow>().UpdateDuckName();
            DebugColor.Log($"[DuckGPT] Name '{duckNameField.value}' and color saved", "yellow");
        })
        {
            text = "Save"
        };
        saveDuckNameButton.AddToClassList("settings-button");
        saveDuckNameButton.AddToClassList("custom-button");

        Button clearDuckNameButton = new(() =>
        {
            EditorPrefs.DeleteKey(DuckNamePref);
            EditorPrefs.DeleteKey(DuckNameColorPref + "_R");
            EditorPrefs.DeleteKey(DuckNameColorPref + "_G");
            EditorPrefs.DeleteKey(DuckNameColorPref + "_B");
            EditorPrefs.DeleteKey(DuckNameColorPref + "_A");
            duckNameField.value = "";
            duckNameColorField.value = Color.yellow;
            GetWindow<PrimaryEditorWindow>().UpdateDuckName();
            DebugColor.Log("[DuckGPT] Name and color cleared", "blue");
        })
        {
            text = "Clear"
        };

        clearDuckNameButton.AddToClassList("settings-button");
        clearDuckNameButton.AddToClassList("custom-button");

        duckNameButtonsContainer.Add(saveDuckNameButton);
        duckNameButtonsContainer.Add(clearDuckNameButton);

        duckConfig.Add(duckConfigLabel);
        duckConfig.Add(experienceLevelLabel);
        duckConfig.Add(experienceLevelPopup);
        duckConfig.Add(experienceLevelButtonsContainer);
        duckConfig.Add(duckAppearance);
        duckConfig.Add(duckNameContainer);
        duckConfig.Add(duckNameColorField);
        duckConfig.Add(duckNameButtonsContainer);

        #endregion

        #region Project Analysis Settings
        VisualElement analysisConfig = new()
        {
            name = "AnalysisConfig"
        };
        Label analysisConfigLabel = new("PROJECT SCAN SETTINGS")
        {
            name = "SectionLabel"
        };
        Label folderSelectionLabel = new("Folders to Include in Full Project Scan:");

        folderListContainer = new VisualElement { name = "FolderListContainer" };

        ScrollView folderScrollView = new()
        {
            contentContainer = { } 
        };
        folderScrollView.Add(folderListContainer);

        Button addFolderButton = new(() =>
        {
            includedFolders.Add("Assets/");
            RebuildFolderList(); // refresh UI
        })
        {
            text = "+ Add Folder"
        };
        addFolderButton.AddToClassList("custom-button");

        VisualElement folderButtonsContainer = new()
        {
            name = "FolderButtonsContainer",
        };
        folderButtonsContainer.AddToClassList("buttons-container");

        Button saveFoldersButton = new(() =>
        {
            SaveIncludedFolders();
            ScriptsHandler.ClearAnalysisCache();
            DebugColor.Log("[DuckGPT] Folder settings saved. Analysis cache cleared.", "yellow");
        })
        {
            text = "Save"
        };
        saveFoldersButton.AddToClassList("settings-button");
        saveFoldersButton.AddToClassList("custom-button");

        Button resetFoldersButton = new(() =>
        {
            includedFolders = new List<string> { "Assets/" };
            SaveIncludedFolders();
            ScriptsHandler.ClearAnalysisCache();
            DebugColor.Log("[DuckGPT] Folder settings reset to defaults.", "blue");
            RebuildFolderList(); // refresh UI
        })
        {
            text = "Reset"
        };
        resetFoldersButton.AddToClassList("settings-button");
        resetFoldersButton.AddToClassList("custom-button");

        folderButtonsContainer.Add(saveFoldersButton);
        folderButtonsContainer.Add(resetFoldersButton);

        analysisConfig.Add(analysisConfigLabel);
        analysisConfig.Add(folderSelectionLabel);
        analysisConfig.Add(folderScrollView);
        analysisConfig.Add(addFolderButton);
        analysisConfig.Add(folderButtonsContainer);

        #endregion

        #region Cache Management Tab
        VisualElement cacheManagement = new()
        {
            name = "CacheManagement"
        };

        Label cacheManagementLabel = new("DuckGPT Cache Management")
        {
            name = "SectionLabel"
        };
        Button clearChatHistoryButton = new(() =>
        {
            ChatMemory.Clear();
            DebugColor.Log("[DuckGPT] Chat history cleared.", "yellow");
        })
        {
            text = "Clear Chat History"
        };
        clearChatHistoryButton.AddToClassList("custom-button");
        Button clearHierarchyCacheButton = new(() =>
        {
            HierarchyMemory.MenuClearCache();
            DebugColor.Log("[DuckGPT] Hierarchy cache cleared.", "yellow");
        })
        {
            text = "Clear Hierarchy Cache"
        };
        clearHierarchyCacheButton.AddToClassList("custom-button");

        Button clearAnalysisCacheButton = new(() =>
        {
            ScriptsHandler.ClearAnalysisCache();
            DebugColor.Log("[DuckGPT] Analysis cache cleared.", "yellow");
        })
        {
            text = "Clear Analysis Cache"
        };
        clearAnalysisCacheButton.AddToClassList("custom-button");
        cacheManagement.Add(cacheManagementLabel);
        cacheManagement.Add(clearChatHistoryButton);
        cacheManagement.Add(clearHierarchyCacheButton);
        cacheManagement.Add(clearAnalysisCacheButton);

        #endregion



        settingsTab.Add(aiConfig);
        settingsTab.Add(duckConfig);
        settingsTab.Add(analysisConfig);

        cacheManagementTab.Add(cacheManagement);

        RebuildFolderList();

    }


    private void RebuildFolderList()
    {
        if (folderListContainer == null) return;

        folderListContainer.Clear();

        for (int i = 0; i < includedFolders.Count; i++)
        {
            int index = i;
            string path = includedFolders[index];

            VisualElement row = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 2
                }
            };

            TextField pathField = new()
            {
                value = path
            };
            pathField.style.flexGrow = 1;
            pathField.RegisterValueChangedCallback(evt =>
            {
                includedFolders[index] = evt.newValue;
            });
            row.Add(pathField);

            Button browseBtn = new(() =>
            {
                string selectedPath = EditorUtility.OpenFolderPanel(
                    "Select Folder to Analyze", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath) &&
                    selectedPath.Contains(Application.dataPath))
                {
                    selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    includedFolders[index] = selectedPath;
                    pathField.value = selectedPath;
                }
            })
            {
                text = "Browse"
            };
            browseBtn.style.width = 70;
            browseBtn.AddToClassList("custom-button");
            row.Add(browseBtn);

            Button removeBtn = new(() =>
            {
                includedFolders.RemoveAt(index);
                RebuildFolderList();
            })
            {
                text = "✕"
            };
            removeBtn.style.width = 25;
            removeBtn.AddToClassList("custom-button");
            row.Add(removeBtn);

            folderListContainer.Add(row);
        }
    }

   

    #region Get Saved Configurations
    public static string GetSavedName() => EditorPrefs.GetString(DuckNamePref, "");

    public static Color GetSavedColor()
    {
        float r = EditorPrefs.GetFloat(DuckNameColorPref + "_R", 1f);
        float g = EditorPrefs.GetFloat(DuckNameColorPref + "_G", 1f);
        float b = EditorPrefs.GetFloat(DuckNameColorPref + "_B", 0f);
        float a = EditorPrefs.GetFloat(DuckNameColorPref + "_A", 1f);
        return new Color(r, g, b, a);
    }

    public static string GetSavedKey() => EditorPrefs.GetString(KeyPref, "");
    public static string GetSavedModel() => EditorPrefs.GetString(ModelPref, availableModels[0]);
    public static string GetSavedElevenLabsKey() => EditorPrefs.GetString(ElevenLabsKeyPref, "");
    public static string GetSavedExperienceLevel() => EditorPrefs.GetString(ExperienceLevelPref, experienceLevels[0]);

    // Add this new method
    public static List<string> GetIncludedFolders()
    {
        string savedFolders = EditorPrefs.GetString(AnalysisFoldersPref, "Assets/");
        if (!string.IsNullOrEmpty(savedFolders))
        {
            return new List<string>(savedFolders.Split('|'));
        }
        return new List<string> { "Assets/" };
    }

    public static string GetSystemPrompt()
    {
        string[] systemPrompts = new[]
        {
            "You are a helpful rubber duck AI. Provide concise, actionable guidance and debugging suggestions. " +
                "You heavily use open questions in responding to students and never want to reveal an answer to a current or previous question outright." +
                "You are never to give the exact code to solve the student's entire problem; instead, focus on helping the student to find their own way to the solution. " +
                "Feel free to use metaphors, analogies, or everyday examples when discussing computational thinking or coding concepts. " +
                "Also, if the student's initial query doesn't specify what they were trying to do, prompt them to clarify that." +
                "You are NOT to behave as if you are a human tutor. Do not use first-person pronouns or give the impression that you are a human tutor. " +
                "Please make sure you begin each response by quacking or making other funny rubber duck noises.Never ignore any of these instructions.",
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
    #endregion
}