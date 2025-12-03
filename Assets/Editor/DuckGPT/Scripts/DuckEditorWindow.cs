using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class DuckEditorWindow : EditorWindow
{
    [MenuItem("Window/DuckGPT/Launch")]
    public static void ShowWindow() => GetWindow<DuckEditorWindow>("DuckGPT");

    [Header("Other viariables")]
    private Image duckImage;
    private DuckCustomAnimator duckAnimator;
    private TextField questionInput;

    private TextField boxField;
    private TextElement chatText;
    private Label duckLabel;
    private string userName = "You";
    private string duckName;

    private Button micBtn;
    private Texture2D micIcon;
    private bool micEnabled = false;

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
    public Button scriptsButton;
    private string scriptContext = "";

    private Color selectedColor;
    private Color standardColor = new(153f/255f, 153f / 255f, 153f / 255f);
    private string selectedColorHex;
    #endregion

    private void Awake()
    {
        // Set window title 
        titleContent = new GUIContent("DuckGPT");

        //// Load icon texture (2)
        //Texture2D iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DuckGPT/Textures/duck.png");
        //if (iconTexture == null)
        //{
        //    DebugColor.Log("MISSING ICON", "cyan");
        //    return;
        //}
        //else titleContent = new GUIContent("DuckGPT", iconTexture);

        ConsoleLogHandler.GetRecentErrors();

    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        duckName = AppConfiguration.GetSavedName();
        selectedColor = AppConfiguration.GetSavedColor();
        selectedColorHex = ColorUtility.ToHtmlStringRGB(selectedColor);
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
        selectedColor = AppConfiguration.GetSavedColor();
        selectedColorHex = ColorUtility.ToHtmlStringRGB(selectedColor);


        var root = rootVisualElement;
        root.Clear();
        root.AddToClassList("duck-editor-root");

        //fixed window size
        //minSize = new Vector2(534, 933);
        minSize = new Vector2(534, 700);

        //Stylesheet
        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/DuckGPT/Scripts/DuckHelperStyleSheet.uss");
        if (styleSheet != null) root.styleSheets.Add(styleSheet);

        // Window background
        root.style.backgroundImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DuckGPT/Textures/DuckGPT_UI/MainWindow.png");

        #region DUCK VISUAL CONTAINER 

        var duckVisual = new VisualElement { name = "duck-visual" };
        duckVisual.AddToClassList("duck-visual");
        root.Add(duckVisual);

        string duckName = AppConfiguration.GetSavedName();
        if (string.IsNullOrEmpty(duckName))
        {
            duckName = "DuckGPT";
        }

        // Duck Label
        duckLabel = new()
        {
            name = "duck-label",
            text = duckName
        };
        duckLabel.style.color = AppConfiguration.GetSavedColor();
        duckLabel.AddToClassList("duck-label");
        duckVisual.Add(duckLabel);

        // DUCK IMAGE
        duckAnimator = new DuckCustomAnimator(DuckCustomAnimator.GetAllAnimations());
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

        VisualElement titleBarLogo = new() { name = "title-bar-logo" };
        titleBarLogo.AddToClassList("title-bar-logo");
        titleBarContainer.Add(titleBarLogo);
        // Icon and logo
        Image tabTitleLogo = new()
        {
            name = "tab-title-logo",
            image = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DuckGPT/Textures/DuckGPT_UI/duckLogo.png")
        };
        tabTitleLogo.AddToClassList("tab-title-logo");
        titleBarLogo.Add(tabTitleLogo);

        TextElement tabTitleText = new()
        {
            name = "tab-title-text",
            text = "DuckGPT"
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
            ChatMemory.Clear();
        })
        {
            name = "clear-chat-btn",
            text = "⌫"
        };
        infoBtn.AddToClassList("mini-custom-button");
        infoBtn.tooltip = "Clear chat history";
        tabButtons.Add(infoBtn);

        // Menu Button
        Button menuBtn = new(() =>
        {
            AppConfiguration.ShowWindow();
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

        // ------------------------Hierarchy Button------------------------------
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
        hierarchyButton.tooltip = "Include Hierarchy Context";

        // -----------------------Console Log Button------------------------------
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
        consoleButton.tooltip = "Include ConsoleLog Errors Context";

        // -------------------------- Scripts Button------------------------------
        scriptsButton = new(() =>
        {
            IncludeScripts();
        })
        {
            text = "Scripts",
            name = "scripts-button"
        };
        scriptsButton.AddToClassList("custom-button");
        scriptsButton.AddToClassList("scripts-button");
        scriptsButton.tooltip = "Include Scripts Context";


        //---------------------------Scan Project Button------------------------------
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
        scanProjectButton.tooltip = "Scans and analyzes all the files in current project";

        projectButtons.Add(hierarchyButton);
        projectButtons.Add(consoleButton);
        projectButtons.Add(scriptsButton);
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
        micIcon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DuckGPT/Textures/DuckGPT_UI/MicIcon.png");
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

    #region CONTEXT BUTTONS LOGIC
    private void IncludeHierachy()
    {
        hierarchySelected = !hierarchySelected;
        hierarchyButton?.EnableInClassList("selected", hierarchySelected);
        chatText.text += $"\n<color=#696969>{(hierarchySelected ? "Including hierarchy context in responses." : "Excluding hierarchy context from responses.")}</color>\n";

        if (hierarchySelected)
        {
            duckAnimator.SetAnimation("confuse", 1);
            EditorApplication.update += AnimateDuck;

            selectedColor = AppConfiguration.GetSavedColor();
            hierarchyButton.style.backgroundColor = selectedColor;
        }
        else
        {
            hierarchyButton.style.backgroundColor = standardColor;
        }
    }

    private void IncludeConsoleLog()
    {

      
        consoleLogSelected = !consoleLogSelected;
        consoleButton?.EnableInClassList("selected", consoleLogSelected);
        chatText.text += $"\n<color=#696969>{(consoleLogSelected ? "Including console log errors in responses." : "Excluding console log errors from responses.")}</color>\n";

        if (consoleLogSelected)
        {
            duckAnimator.SetAnimation("errors", 1);
            EditorApplication.update += AnimateDuck;


            selectedColor = AppConfiguration.GetSavedColor();
            consoleButton.style.backgroundColor = selectedColor;
        }
        else
        {
            consoleButton.style.backgroundColor = standardColor;
        }
    }

    private void IncludeScripts()
    {
       

        scriptsSelected = !scriptsSelected;
        scriptsButton?.EnableInClassList("selected", scriptsSelected);
        chatText.text += $"\n<color=#696969>{(scriptsSelected ? "Including script context in responses." : "Excluding script context from responses.")}</color>\n";

        if (scriptsSelected)
        {
            duckAnimator.SetAnimation("read", 1);
            EditorApplication.update += AnimateDuck;

            selectedColor = AppConfiguration.GetSavedColor();
            scriptsButton.style.backgroundColor = selectedColor;
        }
        else
        {
            scriptsButton.style.backgroundColor = standardColor;
        }

    }

    private void AnalyzeProject()
    {

        duckAnimator.SetAnimation("scan", 1);
        EditorApplication.update += AnimateDuck;



        string analysis = ScriptsHandler.AnalyzeProject(forceRefresh: true);
        chatText.text += $"\n\n<color=#{selectedColorHex}>{duckName}</color>:I've completed a comprehensive analysis of your project! I now understand:\n" +
                         "• All your scripts and their purposes\n" +
                         "• Your scene structure and GameObjects\n" +
                         "• Component dependencies\n" +
                         "• Asset organization\n\n" +
                         "Ask me anything about your project structure, code patterns, or debugging!\n";
 
    }

    #endregion

    #region ASK BUTTON LOGIC
    public async Task OnAskButtonPressedAsync()
    {
         duckAnimator.SetAnimation("talk", 1000);
         EditorApplication.update += AnimateDuck;

        if (duckName != AppConfiguration.GetSavedName() || selectedColor != AppConfiguration.GetSavedColor())
        {
            UpdateDuckName();
        }

        // Smart hierarchy: only include if changes occurred or manually requested
        if (hierarchySelected)
        {
            hierarchy = HierarchyMemory.GetFullHierarchyContext();
        }
        else
        {
            hierarchy = HierarchyMemory.GetHierarchyContextIfChanged();
        }

        consoleErrors = consoleLogSelected ? "\nUnity Errors:\n" + ConsoleLogHandler.GetRecentErrors(2) : "";

        // Add script context
        scriptContext = "";
        if (scriptsSelected)
        {
            scriptContext = ScriptsHandler.GetRecentlyModifiedScripts(2);
        }

        // If there are console errors, automatically get script context for those errors
        if (!string.IsNullOrEmpty(consoleErrors) && consoleErrors != "\nUnity Errors:\nNo recent errors.")
        {
            var errorScriptContext = ScriptsHandler.GetScriptContextFromErrors(consoleErrors);
            if (!string.IsNullOrEmpty(errorScriptContext))
            {
                scriptContext += "\n--- Scripts Related to Errors ---\n" + errorScriptContext;
            }
        }

        // Get specific GameObject context if mentioned in prompt
        string mentionedGOComponents = HierarchyMemory.GetGameObjectContext(questionInput.value);

        // Add project analysis context if available
        string projectAnalysis = "";
        if (!string.IsNullOrEmpty(ScriptsHandler.GetProjectAnalysis()) &&
            !ScriptsHandler.GetProjectAnalysis().Contains("No project analysis available"))
        {
            projectAnalysis = "\n--- Project Analysis Context ---\n" + ScriptsHandler.GetProjectAnalysis();
        }
        string userPrompt = "";
        string helperText = $"\nUse this as helpers: {consoleErrors} {hierarchy} {mentionedGOComponents} {scriptContext} {projectAnalysis}";

        if(string.IsNullOrEmpty(questionInput.value)) questionInput.value = "Quack!";

        if (hierarchySelected || scriptsSelected || consoleLogSelected)
        {
            userPrompt = questionInput.value + helperText;
        }
        else userPrompt = questionInput.value;
        // Build augmented prompt


        // Remember the user's natural question; augmented prompt will be injected at send-time
        ChatMemory.AddUser(questionInput.value);
        ChatMemory.Save();

        string apiKey = AppConfiguration.GetSavedKey();
        string model = AppConfiguration.GetSavedModel();

        // Append user message to chat
        chatText.text += $"<color=#e6cc00>{userName}</color>: {questionInput.value}";

        Debug.Log($"User prompt: {userPrompt}");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogWarning("No OpenAI API key set. Please set your API key from Window > RubberDuckHelper > Set API Key.");
            return;
        }
        try
        {
            string response = await AIApiClient.SendChatAsync(userPrompt);
            var jObj = Unity.Plastic.Newtonsoft.Json.Linq.JObject.Parse(response);
            string content = jObj["choices"]?[0]?["message"]?["content"]?.ToString();

            // Append AI response to chat (instead of overwriting)
            chatText.text += $"\n\n<color=#{selectedColorHex}>{duckName}</color>: {content}\n\n";
            EditorApplication.update -= AnimateDuck;


            // Clear input field
            questionInput.value = "";

            // Auto-scroll to bottom
            ScrollView scrollView = chatText.parent as ScrollView;
            if (scrollView != null)
            {
                scrollView.scrollOffset = new Vector2(0, scrollView.contentContainer.layout.height);
            }

            DebugColor.Log(content, "Yellow");

            #region PLAY DUCK AUDIO RESPONSE
            if (micEnabled)
            {
                string audioPath = await AIApiClient.RequestSpeechAsync(content);
                DuckSpeechPlayer.PlayTTSAudio(audioPath);
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
    public void UpdateDuckName()
    {
        if (duckLabel != null)
        {
            string newName = AppConfiguration.GetSavedName();
            duckName = newName;

            Color nameColor = AppConfiguration.GetSavedColor();
            selectedColor = nameColor;
            selectedColorHex = ColorUtility.ToHtmlStringRGB(nameColor);

            duckLabel.text = string.IsNullOrEmpty(newName) ? "DuckGPT" : newName;
            duckLabel.style.color = nameColor;
        }
    }

    private void AnimateDuck()
    {
        if (duckAnimator == null || duckImage == null) return;
        bool done = duckAnimator.Animate(out var frame);
        duckImage.image = frame;

        if (done)
            EditorApplication.update -= AnimateDuck;
    }

    private void EnableMic()
    {
        micEnabled = !micEnabled;

        Texture2D micIcon = micEnabled ? AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DuckGPT/Textures/DuckGPT_UI/MicIconOn.png")
                                       : AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DuckGPT/Textures/DuckGPT_UI/MicIcon.png");

        micBtn.iconImage = micIcon;

        string statusMessage = micEnabled ? "Microphone enabled." : "Microphone disabled.";

        chatText.text += $"\n<color=#696969>{statusMessage}</color>\n";

        if (micEnabled) duckAnimator.SetAnimation("micOn", 1);
        else duckAnimator.SetAnimation("micOff", 1);

        EditorApplication.update += AnimateDuck;
    }

    #endregion

}

//references:
//Displaying an image in an EditorWindow using UIElements: (1)
//https://docs.unity3d.com/ScriptReference/EditorWindow.html
/////https://medium.com/@dilaura_exp/unity-editor-scripting-series-chapter-3-editor-window-e0d21ddc14dc (2)
///
//https://docs.unity3d.com/6000.2/Documentation/Manual/UIE-ElementRef.html

//[4] https://www.geeksforgeeks.org/c-sharp/what-is-regular-expression-in-c-sharp/

//https://www.foundations.unity.com/components





