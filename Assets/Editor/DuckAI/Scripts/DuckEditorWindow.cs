using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Unity.EditorCoroutines.Editor;
using System.IO;
public class DuckEditorWindow : EditorWindow
{
    [MenuItem("Window/RubberDuckHelper/Launch")]
    public static void ShowWindow() => GetWindow<DuckEditorWindow>("Duck Helper");

    [Header("Other viariables")]
    private Image duckImage;
    private DuckCustomAnimator duckBehaviour;
    private TextField questionInput;
    private string consoleErrors;
    TextField boxField;
    TextElement chatText;

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
        root.Add(box);

        chatText = new TextElement
        {
            name = "chat-text",
            text = "",
        };
        chatText.AddToClassList("chat-text");
        box.Add(chatText);

        #region DUCK IMAGE ANIMATION

        // DUCK IMAGE
        var animDefs = new Dictionary<string, (string, int)>
        {
            { "jump", ("Assets/Editor/DuckAI/Animations/duck_jump", 4) },
            { "wave", ("Assets/Editor/Textures/duck_wave", 6) },
            { "talk", ("Assets/Editor/Textures/duck_talk", 5) }
        };
        duckBehaviour = new DuckCustomAnimator(DuckCustomAnimator.GetAllAnimations());

        duckImage = new Image
        {
            image = duckBehaviour.GetCurrentFrame(),
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
        root.Add(animateButton);


        #endregion

        #region AI QUESTION INPUT AND BUTTON

       questionInput = new TextField("")
        {
            multiline = true,
            value = "Type your question here..."
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
        if (duckBehaviour == null || duckImage == null) return;
        bool done = duckBehaviour.Animate(out var frame);
        duckImage.image = frame;

        if (done)
            EditorApplication.update -= AnimateDuck;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            // Play has just stopped — read console history and update UI
            ConsoleLogHandler.RefreshFromConsoleHistory();
            consoleErrors = ConsoleLogHandler.GetRecentErrors(10);
            // If using UIElements, call:
            Repaint(); // ensures EditorWindow repaints
                       // If you use rootVisualElement labels, update their text here
        }
    }

    public async Task OnAskButtonPressedAsync()
    {
        duckBehaviour.SetAnimation("jump", 2);
        EditorApplication.update += AnimateDuck;

        string hierarchy = HierarchyHandler.GetHierarchyString();
       
        string userPrompt = questionInput.value + "\n\nCurrent Unity hierarchy:\n" + hierarchy + "\n\nUnity errors:\n" + consoleErrors;


        string apiKey = OpenAIConfiguration.GetSavedKey();
        string model = OpenAIConfiguration.GetSavedModel();
        //DebugColor.Log($"Using model: {model}", "red");
        chatText.text += "\n\n You: " + questionInput.value;

        Debug.Log($"User prompt: {userPrompt}");
        questionInput.value = "";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogWarning("No OpenAI API key set. Please set your API key from Window > RubberDuckHelper > Set API Key.");
            return;
        }
        try
        {
            string response = await OpenAIClient.SendChatAsync(apiKey, userPrompt, model);
            var jObj = Unity.Plastic.Newtonsoft.Json.Linq.JObject.Parse(response);
            string content = jObj["choices"]?[0]?["message"]?["content"]?.ToString();
            chatText.text = "\n\n Duck: " + content;
            DebugColor.Log(content, "Yellow");

            string audioPath = await OpenAIClient.RequestSpeechAsync(apiKey, content, "echo", "tts-1", "duck_speech.mp3");
            DuckSpeechPlayer.PlayDuckAudio(audioPath);
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

