using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Provides a custom editor window for the DuckGPT application, enabling users to interact with an AI assistant and
/// manage project scripts, console logs, and scene hierarchy within the Unity Editor.
/// </summary>
internal class PrimaryEditorWindow : EditorWindow
{
    [MenuItem("Window/DuckGPT/Launch")]
    public static void ShowWindow() => GetWindow<PrimaryEditorWindow>("DuckGPT");

    #region Duck Viariables
    private Image duckImage;
    private Label duckLabel;
    private CustomAnimator duckAnimator;
    #endregion

    #region Chat Variables
    private TextField questionInput;
    public TextElement chatText;
    private TextElement tabTitleText;

    private string userName = "You";
    private string duckName;
    #endregion

    #region Microphone Variables
    private Button micBtn;
    private Texture2D micIcon;
    private bool micEnabled = false;
    #endregion

    #region Console Log Variables
    private bool consoleLogSelected = false;
    public Button consoleButton;
    private string consoleErrors = "";
    #endregion

    #region Hierarchy Variables
    private bool hierarchySelected = false;
    public Button hierarchyButton;
    private string hierarchy = "";
    #endregion

    #region Script Variables
    private bool scriptsSelected = false;
    private DropdownField scriptsDropdown;
    private List<string> availableScripts = new();
    private Dictionary<string, string> scriptNameToPath = new(); 
    private string selectedScriptPath = "";
    private string scriptContext = "";
    #endregion

    #region Color Variables
    private Color selectedColor;
    private Color standardColor = new(153f / 255f, 153f / 255f, 153f / 255f);
    private string selectedColorHex;
    #endregion

    private void Awake()
    {
        titleContent = new GUIContent("DuckGPT");

        ConsoleLogHandler.GetRecentErrors();

        ConfigurationEditorWindow.ShowWindow();
    }

    private void OnEnable()
    {
        // Subscribe to play mode state changes
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        // Load saved duck name and color
        duckName = ConfigurationEditorWindow.GetSavedName();
        selectedColor = ConfigurationEditorWindow.GetSavedColor();
        selectedColorHex = ColorUtility.ToHtmlStringRGB(selectedColor);
        
        // Initialize speech player with animation callbacks
        TTSPlayer.Initialize(StartDuckAnimation, StopDuckAnimation);
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= AnimateDuck;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            ConsoleLogHandler.RefreshFromConsoleHistory();
            consoleErrors = ConsoleLogHandler.GetRecentErrors(10);

            Repaint();
        }
    }

    #region GUI SETUP
    public void CreateGUI()
    {
        // saved duck name and color from app configuration
        selectedColor = ConfigurationEditorWindow.GetSavedColor();
        selectedColorHex = ColorUtility.ToHtmlStringRGB(selectedColor);

        var root = rootVisualElement;
        root.Clear();
        root.AddToClassList("duck-editor-root");

        minSize = new Vector2(534, 933);

        //Stylesheet
        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/DuckGPT/Scripts/UI/Stylesheets/PrimaryWindowStyleSheet.uss");
        if (styleSheet != null) root.styleSheets.Add(styleSheet);

        // Window background
        root.style.backgroundImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DuckGPT/Textures/MainWindow.png");

        #region DUCK VISUAL CONTAINER 

        //DUCK VISUAL CONTAINER
        VisualElement duckVisual = new() { name = "duck-visual" };
        duckVisual.AddToClassList("duck-visual");
        root.Add(duckVisual);

        //DUCK LABEL
        string duckName = ConfigurationEditorWindow.GetSavedName();
        if (string.IsNullOrEmpty(duckName)) duckName = "DuckGPT";

        duckLabel = new()
        {
            name = "duck-label",
            text = duckName
        };
        duckLabel.style.color = ConfigurationEditorWindow.GetSavedColor();
        duckLabel.AddToClassList("duck-label");
        duckVisual.Add(duckLabel);

        // DUCK IMAGE
        duckAnimator = new CustomAnimator(CustomAnimator.GetAllAnimations());

        duckImage = new Image
        {
            name = "duck-image",
            image = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DuckGPT/Textures/duck.png")

        };
        duckImage.AddToClassList("duck-image");
        duckVisual.Add(duckImage);

        #endregion

        #region TITLE BAR CONTAINER
        VisualElement titleBarContainer = new() { name = "title-bar-container" };
        titleBarContainer.AddToClassList("title-bar-container");
        root.Add(titleBarContainer);

        // DuckGPT Logo and Title
        VisualElement titleBarLogo = new() { name = "title-bar-logo" };
        titleBarLogo.AddToClassList("title-bar-logo");
        titleBarContainer.Add(titleBarLogo);

        // Titlebar icon
        Image tabTitleLogo = new()
        {
            name = "tab-title-logo",
            image = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DuckGPT/Textures/duckLogo.png")
        };
        tabTitleLogo.AddToClassList("tab-title-logo");
        titleBarLogo.Add(tabTitleLogo);

        // Titlebar label
        tabTitleText = new()
        {
            name = "tab-title-text",
            text = "DuckGPT" +$" - {ConfigurationEditorWindow.GetExperienceLabel()}"
        };
        tabTitleText.AddToClassList("tab-title-text");
        titleBarLogo.Add(tabTitleText);

        #region TAB BUTTONS 
        var tabButtons = new VisualElement { name = "tab-buttons" };
        tabButtons.AddToClassList("tab-buttons");
        titleBarContainer.Add(tabButtons);

        // Info Button
        Button infoBtn = new(() =>
        {
            chatText.text = "";
            ChatMemory.ClearChatHistory();
        })
        {
            name = "clear-chat-btn",
            text = "↻"
        };
        infoBtn.AddToClassList("mini-custom-button");
        infoBtn.tooltip = "Clear chat history";
        tabButtons.Add(infoBtn);

        // Menu Button
        Button menuBtn = new(() =>
        {
            ConfigurationEditorWindow.ShowWindow();
        })
        {
            name = "menu-btn",
            text = "⁝"
        };
        menuBtn.AddToClassList("menu-btn");
        menuBtn.AddToClassList("mini-custom-button");
        menuBtn.tooltip = "App configuartion window";

        tabButtons.Add(menuBtn);

        // Close Button
        Button closeBtn = new(() =>
        {
            this.Close();
        })
        {
            name = "close-btn",
            text = "X"
        };
        closeBtn.AddToClassList("close-btn");
        closeBtn.AddToClassList("mini-custom-button");
        closeBtn.tooltip = "Close";

        tabButtons.Add(closeBtn);

        #endregion
        #endregion

        #region CONTENT AREA

        VisualElement contentArea = new() //contaner for buttons/text box/input
        {
            name = "content-area"
        };
        contentArea.AddToClassList("content-area");
        root.Add(contentArea);

        #region PROJECT BUTTONS CONTAINER (RIGHT SIDE)
        VisualElement projectButtons = new()
        {
            name = "project-buttons"
        };

        // Hierarchy Button
        hierarchyButton = new(() =>
        {
            IncludeHierachy();
        })
        {
            text = "Hierarchy",
            name = "hierarchy-button"
        };
        hierarchyButton.AddToClassList("custom-button");
        hierarchyButton.AddToClassList("hierarchy-button");
        hierarchyButton.tooltip = "Include current scene hierarchy";

        // Console Log Button
        consoleButton = new(() =>
        {
            IncludeConsoleLog();
        })
        {
            text = "Console",
            name = "console-button"
        };
        consoleButton.AddToClassList("custom-button");
        consoleButton.AddToClassList("console-button");
        consoleButton.tooltip = "Include recent console errors";

        //Scripts Button

        availableScripts = GetAllProjectScripts();

        List<string> displayNames = new() { "None", "All" };

        foreach (string scriptPath in availableScripts)
        {
            string fileName = System.IO.Path.GetFileName(scriptPath);
            displayNames.Add(fileName);
            scriptNameToPath[fileName] = scriptPath;
        }

        scriptsDropdown = new DropdownField("Scripts", displayNames, 0)
        {
            name = "scripts-dropdown",
            value = "Scripts"
        };
        scriptsDropdown.AddToClassList("scripts-dropdown");
        scriptsDropdown.AddToClassList("custom-button");
        scriptsDropdown.tooltip = "Select a script to include in context";

        scriptsDropdown.RegisterValueChangedCallback(evt =>
        {
            VisualElement dropdownInput = scriptsDropdown.Q<VisualElement>(className: "unity-base-field__input");

            if (evt.newValue == "None")
            {
                scriptsSelected = false;
                selectedScriptPath = "";

                scriptsDropdown.value = "Scripts";

                chatText.text += $"\n\n<color=#696969>Script selection cleared.</color>";

                if (dropdownInput != null) dropdownInput.style.backgroundColor = standardColor;
            }

            else if (evt.newValue == "All")
            {
                scriptsSelected = true;
                selectedScriptPath = "ALL_SCRIPTS";

                if (dropdownInput != null) dropdownInput.style.backgroundColor = selectedColor;

                duckAnimator.SetAnimation("read", 1);
                EditorApplication.update += AnimateDuck;

                chatText.text += $"\n\n<color=#696969>All scripts will be included in context.</color>";
            }
            else // Specific script selected
            {
                if (scriptNameToPath.TryGetValue(evt.newValue, out string fullPath))
                {
                    selectedScriptPath = fullPath;
                    scriptsSelected = true;

                    if (dropdownInput != null) dropdownInput.style.backgroundColor = selectedColor;

                    duckAnimator.SetAnimation("read", 1);
                    EditorApplication.update += AnimateDuck;

                    chatText.text += $"\n\n<color=#696969>Selected script: {evt.newValue}</color>";
                }
            }
        });

        //Scan Project Button
        Button scanProjectButton = new(() =>
        {
            AnalyzeProject();
        })
        {
            text = "Scan Project",
            name = "scan-project-button"
        };
        scanProjectButton.AddToClassList("project-button");
        scanProjectButton.AddToClassList("custom-button");
        scanProjectButton.tooltip = "Full project scan (comprehensive one-time analysis)";

        projectButtons.Add(hierarchyButton);
        projectButtons.Add(consoleButton);
        projectButtons.Add(scriptsDropdown);
        projectButtons.Add(scanProjectButton);
        contentArea.Add(projectButtons);
        #endregion

        #region TEXT BOX AREA (LEFT SIDE)
        var textBoxArea = new VisualElement { name = "text-box-area" };
        root.Add(textBoxArea);

        Box textBox = new()
        {
            name = "text-box",
        };
        textBox.AddToClassList("text-box");
        textBoxArea.Add(textBox);

        var scrollView = new ScrollView { name = "chat-scroll-view" };
        textBoxArea.Add(scrollView);

        chatText = new TextElement
        {
            name = "chat-text",
            text = ""
        };
        chatText.AddToClassList("chat-text");
        scrollView.Add(chatText);

        root.Add(contentArea);
        contentArea.Add(textBoxArea);

        #endregion
        #endregion

        #region INPUT AREA (BELOW TEXT BOX) 
        VisualElement inputArea = new()
        {
            name = "input-area"
        };
        micIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DuckGPT/Textures/MicIcon.png");
        micBtn = new(() =>
        {
            EnableMic();

        })
        {
            name = "mic-btn",
            iconImage = micIcon,
        };
        micBtn.AddToClassList("mini-custom-button");
        micBtn.AddToClassList("mic-btn");

        questionInput = new TextField("")
        {
            name = "question-input",
            multiline = false,
            value = "Quack!"
        };
        questionInput.AddToClassList("question-input");

        Button askButton = new(async () => await OnAskButtonPressedAsync())
        {
            text = "Quack!",
            name = "ask-button"

        };
        askButton.AddToClassList("custom-button");
        askButton.AddToClassList("ask-button");

        inputArea.Add(micBtn);
        inputArea.Add(questionInput);
        inputArea.Add(askButton);
        contentArea.Add(inputArea);
    }
    #endregion

    #endregion

    #region CONTEXT TOGGLES LOGIC
    private void IncludeHierachy()
    {
        hierarchySelected = !hierarchySelected;

        if (hierarchySelected)
        {
            duckAnimator.SetAnimation("confuse", 1);
            EditorApplication.update += AnimateDuck;

            selectedColor = ConfigurationEditorWindow.GetSavedColor();
            hierarchyButton.style.backgroundColor = selectedColor;

            hierarchyButton.EnableInClassList("selected", true);

            chatText.text += $"\n\n<color=#696969>Including hierarchy context in responses.</color>";
        }
        else
        {
            hierarchyButton.style.backgroundColor = standardColor;

            hierarchyButton.EnableInClassList("selected", false);

            chatText.text += $"\n\n<color=#696969>Excluding hierarchy context from responses.</color>";
        }
    }

    private void IncludeConsoleLog()
    {
        consoleLogSelected = !consoleLogSelected;

        if (consoleLogSelected)
        {
            duckAnimator.SetAnimation("errors", 1);
            EditorApplication.update += AnimateDuck;

            selectedColor = ConfigurationEditorWindow.GetSavedColor();
            consoleButton.style.backgroundColor = selectedColor;

            consoleButton.EnableInClassList("selected", true);

            chatText.text += $"\n\n<color=#696969>Including console log errors in responses.</color>";
        }
        else
        {
            consoleButton.style.backgroundColor = standardColor;

            consoleButton.EnableInClassList("selected", false);

            chatText.text += $"\n\n<color=#696969>Excluding console log errors from responses.</color>";
        }
    }

    private void AnalyzeProject()
    {
        duckAnimator.SetAnimation("scan", 1);
        EditorApplication.update += AnimateDuck;

        string analysis = ScriptsHandler.AnalyzeProject(forceRefresh: true);
        ChatMemory.SetProjectAnalysis(analysis); // store once in memory
        //chatText.text += $"\n\n<color=#{selectedColorHex}>{duckName}</color>:I've completed a comprehensive analysis of your project! I now understand:\n\n" +
        //                 "🦆 All your scripts and their purposes\n" +
        //                 "\U0001f986 Your scene structure and GameObjects\n" +
        //                 "\U0001f986 Component dependencies\n" +
        //                 "\U0001f986 Asset organization";
        chatText.text += $"\n\n<color=#{selectedColorHex}>{duckName}</color>: Quack! I've completed a comprehensive analysis of your project! Ask me anything!";
    }

    #endregion

    #region ASK BUTTON FUNCTIONALITY
    public async Task OnAskButtonPressedAsync()
    {
        // Start thinking animation
        duckAnimator.SetAnimation("thinking", 1);        
        EditorApplication.update += AnimateDuck;

        // Update duck name and color if changed in app configuration cs
        if (duckName != ConfigurationEditorWindow.GetSavedName() || selectedColor != ConfigurationEditorWindow.GetSavedColor())
        {
            UpdateDuckName();
        }

        #region INCLUDE HIERARCHY
        string mentionedGameObjectComponents = HierarchyHandler.LookForComponentsInPrompt(questionInput.value);
        if (hierarchySelected)
        {
            hierarchy = HierarchyHandler.GetHierarchyString();
        }
 
        #endregion

        #region INCLUDE CONSOLE LOG ERRORS
        if (consoleLogSelected)
        {
            consoleErrors = "\n\nUnity Errors:\n\n" + ConsoleLogHandler.GetRecentErrors(2);

            // Automatically include scripts referenced in those errors
            string errorScriptContext = ScriptsHandler.GetScriptContextFromErrors(consoleErrors);
            if (!string.IsNullOrEmpty(errorScriptContext))
                scriptContext += errorScriptContext;
        }
        else consoleErrors = "";
        #endregion

        #region INCLUDE SCRIPTS
        if (scriptsSelected && !string.IsNullOrEmpty(selectedScriptPath))
        {
            if (selectedScriptPath == "ALL_SCRIPTS")
            {
                // Get content of all scripts
                scriptContext = "";
                foreach (string scriptPath in availableScripts)
                {
                    string fileName = System.IO.Path.GetFileName(scriptPath);
                    string content = ScriptsHandler.GetScriptContent(scriptPath);
                    scriptContext += $"\n\n--- {fileName} ---\n{content}\n";
                }
            }
            else
            {
                // Get the content of the selected script
                scriptContext = ScriptsHandler.GetScriptContent(selectedScriptPath);
            }
        }
        else
        {
            scriptContext = "";
        }
        #endregion

        #region USER PROMPT
        string userPrompt = questionInput.value;
        if (string.IsNullOrEmpty(questionInput.value))
        {
            userPrompt = "Quack!";
        }
        string contextPrompt = null;

        if (hierarchySelected || scriptsSelected || consoleLogSelected)
            contextPrompt = $"Use this context: {consoleErrors} {hierarchy} {scriptContext}";

        else if (!string.IsNullOrEmpty(mentionedGameObjectComponents))
            contextPrompt = $"Use this context: {mentionedGameObjectComponents}";

        //DebugColor.Log($"{contextPrompt}", "Cyan");
        #endregion

        #region SAVE USER MESSAGE TO CHAT MEMORY
        ChatMemory.AddUser(questionInput.value);
        ChatMemory.SaveToFile();
        #endregion

        string apiKey = ConfigurationEditorWindow.GetSavedKey();
        string model = ConfigurationEditorWindow.GetSavedModel();

        // Append user message to chat
        chatText.text += $"\n\n<color=#e6cc00>{userName}</color>: {userPrompt}";
        // Clear input field
        questionInput.value = "";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogWarning("No OpenAI API key set. Please set your API key!");
            ConfigurationEditorWindow.ShowWindow();
            chatText.text += $"\n\n<color=#{selectedColorHex}>{duckName}</color>: Quack! Please set your OpenAI API key in the App Configuration window.";
            return;
        }

        Debug.Log($"User prompt: {userPrompt}");
        
        try
        {
            string response = await AIApiClient.SendChatAsync(userPrompt, contextPrompt);
 
            // Append duck response to chat 
            chatText.text += $"\n\n<color=#{selectedColorHex}>{duckName}</color>: {response}";
           
            DebugColor.Log(response, "Yellow");

            #region PLAY DUCK AUDIO RESPONSE
            if (micEnabled)
            {
                string audioPath = await AIApiClient.RequestSpeechAsync(response);
                TTSPlayer.PlayTTSAudio(audioPath); 
            }
            #endregion
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[DuckGPT] Error: " + ex.Message);
            chatText.text += $"\n\n{duckName}: Quack! Something went wrong: {ex.Message}";
            EditorApplication.update -= AnimateDuck;
        }

    }
    #endregion

    #region OTHER METHODS
    private List<string> GetAllProjectScripts()
    {
        List<string> scripts = ScriptsHandler.GetUserScriptPaths();

        scripts.Sort((a, b) =>
            System.IO.Path.GetFileName(a).CompareTo(System.IO.Path.GetFileName(b)));

        return scripts;
    }

    public void UpdateDuckName()
    {
        if (duckLabel != null)
        {
            string newName = ConfigurationEditorWindow.GetSavedName();
            duckName = newName;

            Color nameColor = ConfigurationEditorWindow.GetSavedColor();
            selectedColor = nameColor;
            selectedColorHex = ColorUtility.ToHtmlStringRGB(nameColor);

            duckLabel.text = string.IsNullOrEmpty(newName) ? "DuckGPT" : newName;
            duckLabel.style.color = nameColor;
        }
    }

    public void UpdateTabTitle()
    {
        if (tabTitleText != null)
        {
            string experienceLabel = ConfigurationEditorWindow.GetExperienceLabel();
            tabTitleText.text = $"DuckGPT - {experienceLabel}";
        }
    }

    private void EnableMic()
    {
        micEnabled = !micEnabled;

        if (micEnabled) 
        {
            TTSPlayer.canPlayAudio = true;
            micIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DuckGPT/Textures/MicIconOn.png");
            chatText.text += $"\n\n<color=#696969>Microphone enabled</color>";
            duckAnimator.SetAnimation("micOn", 1);
        }
        else 
        {
            TTSPlayer.canPlayAudio = false;
            micIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DuckGPT/Textures/MicIcon.png");
            chatText.text += $"\n\n<color=#696969>Microphone disabled</color>";
            duckAnimator.SetAnimation("micDrop", 1);
        }
        micBtn.iconImage = micIcon;
        EditorApplication.update += AnimateDuck;
    }

    #endregion

    #region Animation Control Callbacks
    private void AnimateDuck()
    {
        if (duckAnimator == null || duckImage == null) return;
        bool animationComplete = duckAnimator.Animate(out var frame);
        duckImage.image = frame;

        if (animationComplete)
            EditorApplication.update -= AnimateDuck;
    }

    private void StartDuckAnimation(string animationName, int repeatCount)
    {
        if (duckAnimator != null)
        {
            duckAnimator.SetAnimation(animationName, repeatCount);
            EditorApplication.update += AnimateDuck;
        }
    }
    private void StopDuckAnimation()
    {
        EditorApplication.update -= AnimateDuck;
    }

    #endregion

    
}
