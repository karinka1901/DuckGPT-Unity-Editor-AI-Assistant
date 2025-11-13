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

    private void Awake()
    {
        // Set window title 
        titleContent = new GUIContent("Duck Helper");

        // Load icon texture (2)
        Texture2D iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DuckAI/Textures/duck.png");
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

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/DuckAI/Scripts/DuckHelperStyleSheet.uss");
        if (styleSheet != null)
        {
            root.styleSheets.Add(styleSheet);
        }
        boxField = new TextField("")
        {
            multiline = true,
            value = ""
        };
        boxField.name = "box-field";
        boxField.AddToClassList("box-field");
       // root.Add(boxField);

        Box box = new Box();
        box.name = "box-field";
        box.AddToClassList("box-field");
      //  root.Add(box);

       
        box.Add(chatText);

        #region DUCK IMAGE ANIMATION

        var titleLabel = new Label("DuckGPT");
        titleLabel.name = "title-label";
        titleLabel.AddToClassList("title-label");
        root.Add(titleLabel);

        // DUCK IMAGE
        var animDefs = new Dictionary<string, (string, int)>
        {
            { "jump", ("Assets/Editor/DuckAI/Animations/duck_jump", 4) },
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

        #region AI QUESTION INPUT AND BUTTON

       consoleLogButton = new Button(() =>
        {
            IncludeConsoleLog();
        })
        {
            text = "Console Log"
        };
        consoleLogButton.name = "console-log-button";
        consoleLogButton.AddToClassList("console-log-button");
        root.Add(consoleLogButton);

        hierarchyButton = new Button(() =>
        {
           IncludeHierachy();
        })
        {
            text = "Hierarchy"
        };
        hierarchyButton.name = "hierarchy-button";
        hierarchyButton.AddToClassList("hierarchy-button");
        root.Add(hierarchyButton);

        Button scriptsButton = new Button(() =>
        {
           
        })
        {
            text = "Scripts"
        };
        scriptsButton.name = "scripts-button";
        scriptsButton.AddToClassList("scripts-button");
        root.Add(scriptsButton);

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
        if (!hierarchySelected)
        {
            hierarchySelected = true;
            hierarchyButton.style.backgroundColor = Color.green;

         //   hierarchy ="\nCurrent hierarchy:\n" + HierarchyHandler.GetHierarchyString();
        }
        else
        {
            hierarchySelected = false;
            hierarchyButton.style.backgroundColor = Color.grey;

            hierarchy = "";
        }
    }

    private void IncludeConsoleLog()
    {
        if (!consoleLogSelected)
        {
            consoleLogSelected = true;
            consoleLogButton.style.backgroundColor = Color.green;

            //  consoleErrors = "\nUnity Errors:\n" + ConsoleLogHandler.GetRecentErrors(10);
        }
        else
        {
            consoleLogSelected = false;
            consoleLogButton.style.backgroundColor = Color.grey;

            consoleErrors = "";
        }
    }


    public async Task OnAskButtonPressedAsync()
    {
        duckAnimator.SetAnimation("talk", 8);
        EditorApplication.update += AnimateDuck;

        hierarchy = hierarchySelected ? "\nCurrent hierarchy:\n" + HierarchyHandler.GetHierarchyString() : "";
        consoleErrors = consoleLogSelected ? "\nUnity Errors:\n" + ConsoleLogHandler.GetRecentErrors() : "";

   

        string mentionedGOComponents = HierarchyHandler.LookForComponentsInPrompt(questionInput.value + consoleErrors);
        string userPrompt = questionInput.value + "\n " + consoleErrors + hierarchy  + mentionedGOComponents ;
        

        string apiKey = ApiConfiguration.GetSavedKey();
        string model = ApiConfiguration.GetSavedModel();
        //DebugColor.Log($"Using model: {model}", "red");
        chatText.text += "\n\n You: " + questionInput.value;

        Debug.Log($"User prompt: {userPrompt}");
     //   questionInput.value = "";

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
            chatText.text = "\n\n You: " + questionInput.value + "\n\n Duck: " + content;
            questionInput.value = "";
            DebugColor.Log(content, "Yellow");

          //  #region PLAY DUCK AUDIO RESPONSE
          //  string audioPath = await AIApiClient.RequestSpeechAsync(content);
          //  DuckSpeechPlayer.PlayTTSAudio(audioPath);
           // #endregion
        }
        catch (System.Exception ex)
        {
            Debug.LogError("DuckAI error: " + ex.Message);
        }
    
    }

   

}

//references:
//Dispplaying an image in an EditorWindow using UIElements: (1)
//https://docs.unity3d.com/ScriptReference/EditorWindow.html
////
///https://medium.com/@dilaura_exp/unity-editor-scripting-series-chapter-3-editor-window-e0d21ddc14dc (2)
///
//https://docs.unity3d.com/6000.2/Documentation/Manual/UIE-ElementRef.html

//[4] https://www.geeksforgeeks.org/c-sharp/what-is-regular-expression-in-c-sharp/

//https://www.foundations.unity.com/components



// TO DO:
// - create error buttons when erros are present 
// - clicking error button adds error context to prompt

//add memory to the chat
