using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class DuckEditorWindow : EditorWindow
{
    [MenuItem("Window/RubberDuckHelper/Launch")]
    public static void ShowWindow() => GetWindow<DuckEditorWindow>("Duck Helper");

    [Header("Other viariables")]
    private Image duckImage;
    private DuckCustomAnimator duckAnimator;
    private TextField questionInput;
    
    TextField boxField;
    TextElement chatText;

    #region Console Log Variables
    private bool consoleLogSelected = false;
    public Button consoleLogButton;
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
    #endregion

    private void Awake()
    {
        // Set window title 
        titleContent = new GUIContent("Duck Helper");

        // Load icon texture (2)
        Texture2D iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DuckGPT/Textures/duck.png");
        if (iconTexture == null) return;
        else titleContent = new GUIContent("Duck Helper", iconTexture);

        ConsoleLogHandler.GetRecentErrors();

    }


    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= AnimateDuck;
    }

public void CreateGUI()
    {
        var root = rootVisualElement;
        root.AddToClassList("duck-editor-root");

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/DuckGPT/Scripts/DuckHelperStyleSheet.uss");
        if (styleSheet != null)
        {
            root.styleSheets.Add(styleSheet);
        }

        #region DUCK IMAGE ANIMATION

        var titleLabel = new Label("DuckGPT");
        titleLabel.name = "title-label";
        titleLabel.AddToClassList("title-label");
        root.Add(titleLabel);

        // DUCK IMAGE
        var animDefs = new Dictionary<string, (string, int)>
        {
            { "jump", ("Assets/Editor/DuckGPT/Animations/duck_jump", 4) },
            { "wave", ("Assets/Editor/Textures/duck_wave", 6) },
            { "talk", ("Assets/Editor/Textures/duck_talk", 5) }
        };
        duckAnimator = new DuckCustomAnimator(DuckCustomAnimator.GetAllAnimations());

        duckImage = new Image
        {
            image = duckAnimator.GetCurrentFrame(),
            scaleMode = ScaleMode.ScaleToFit
        };
        duckImage.name = "duck-image";
        duckImage.AddToClassList("duck-image");
        root.Add(duckImage);

        //  EditorApplication.update += AnimateDuck;

        Button animateButton = new Button(() =>
        {
            chatText.text = "";
        });

        animateButton.name = "animate-duck-button";
        animateButton.AddToClassList("animate-duck-button");
        //  root.Add(animateButton);


        #endregion

        VisualElement toggleContainer = new()
        {
            name = "toggle-container"
        };
        toggleContainer.AddToClassList("toggle-container");
        root.Add(toggleContainer);

        #region AI QUESTION INPUT AND BUTTON

        consoleLogButton = new(() =>
         {
             IncludeConsoleLog();
         })
        {
            text = "Console Log",
            name = "console-log-button"
        };
        consoleLogButton.AddToClassList("toggle-button");
        toggleContainer.Add(consoleLogButton);

        hierarchyButton = new(() =>
        {
            IncludeHierachy();
        })
        {
            text = "Hierarchy",
            name = "hierarchy-button"
        };
        hierarchyButton.AddToClassList("toggle-button");
        toggleContainer.Add(hierarchyButton);

        scriptsButton = new(() =>
        {
            IncludeScripts();
        })
        {
            text = "Scripts",
            name = "scripts-button"
        };
        scriptsButton.AddToClassList("toggle-button");
        toggleContainer.Add(scriptsButton);

        Button analyzeProjectButton = new(() =>
        {
            AnalyzeProject();
        })
        {
            text = "Analyze Project",
            name = "analyze-project-button"
        };
        analyzeProjectButton.AddToClassList("analyze-project-button");
        root.Add(analyzeProjectButton);

        var scrollView = new ScrollView();
        scrollView.name = "chat-scroll-view";
        scrollView.AddToClassList("chat-scroll-view");
        root.Add(scrollView);

        chatText = new TextElement
        {
            name = "chat-text",
            text = "",
        };
        chatText.AddToClassList("chat-text");
        scrollView.Add(chatText);

        questionInput = new TextField("")
        {
            multiline = true,
            value = "Quack..."
        };
        questionInput.name = "question-input";
        questionInput.AddToClassList("question-input");
        root.Add(questionInput);

        Button askButton = new Button(async () => await OnAskButtonPressedAsync())
   
        {
            text = "Ask the Duck"
        };
        askButton.name = "ask-duck-button";
        askButton.AddToClassList("ask-duck-button");
        root.Add(askButton);

        Button clearChat = new Button(() =>
        {
            chatText.text = "";

        })
        {
            text = "Clear Chat"
        };



        #endregion
    }

    private void AnimateDuck()
    {
        if (duckAnimator == null || duckImage == null) return;
        bool done = duckAnimator.Animate(out var frame);
        duckImage.image = frame;

        if (done)
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

    private void IncludeHierachy()
    {
        hierarchySelected = !hierarchySelected;
        hierarchyButton?.EnableInClassList("selected", hierarchySelected);
   
    }

    private void IncludeConsoleLog()
    {
        consoleLogSelected = !consoleLogSelected;
        consoleLogButton?.EnableInClassList("selected", consoleLogSelected);
    }

    private void IncludeScripts()
    {
       scriptsSelected = !scriptsSelected;
        scriptsButton?.EnableInClassList("selected", scriptsSelected);
    }

    private void AnalyzeProject()
    {
        string analysis = ScriptsHandler.AnalyzeProject(forceRefresh: true);
        chatText.text += "\n\n🦆: I've completed a comprehensive analysis of your project! I now understand:\n" +
                         "• All your scripts and their purposes\n" +
                         "• Your scene structure and GameObjects\n" +
                         "• Component dependencies\n" +
                         "• Asset organization\n\n" +
                         "Ask me anything about your project structure, code patterns, or debugging!";
    }

    public async Task OnAskButtonPressedAsync()
    {
        duckAnimator.SetAnimation("talk", 8);
        EditorApplication.update += AnimateDuck;

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

        // Build augmented prompt
        string userPrompt = "Answer this: " + questionInput.value + 
                           "\nUse this as helpers: " + consoleErrors + hierarchy + mentionedGOComponents + scriptContext + projectAnalysis;
        
        // Remember the user's natural question; augmented prompt will be injected at send-time
        ChatMemory.AddUser(questionInput.value);
        ChatMemory.Save();

        string apiKey = ApiConfiguration.GetSavedKey();
        string model = ApiConfiguration.GetSavedModel();
        chatText.text += "\n\n You: " + questionInput.value;

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
            chatText.text = "\n\n You: " + questionInput.value + "\n\n \U0001f986: " + content;
            questionInput.value = "";
            DebugColor.Log(content, "Yellow");

          //  #region PLAY DUCK AUDIO RESPONSE
          //  string audioPath = await AIApiClient.RequestSpeechAsync(content);
          //  DuckSpeechPlayer.PlayTTSAudio(audioPath);
           // #endregion
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[DuckGPT] Error: " + ex.Message);
        }
    
    }

   
}

//references:
//Displaying an image in an EditorWindow using UIElements: (1)
//https://docs.unity3d.com/ScriptReference/EditorWindow.html
/////https://medium.com/@dilaura_exp/unity-editor-scripting-series-chapter-3-editor-window-e0d21ddc14dc (2)
///
//https://docs.unity3d.com/6000.2/Documentation/Manual/UIE-ElementRef.html

//[4] https://www.geeksforgeeks.org/c-sharp/what-is-regular-expression-in-c-sharp/

//https://www.foundations.unity.com/components



// TO DO:
// - create error buttons when errors are present 
// - clicking error button adds error context to prompt
