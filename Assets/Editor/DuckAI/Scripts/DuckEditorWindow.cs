using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
public class DuckEditorWindow : EditorWindow
{
    [MenuItem("Window/RubberDuckHelper")]
    public static void DisplayDuckWindow()
    {
        DuckEditorWindow window = GetWindow<DuckEditorWindow>();
        window.Show();
    }

    [HideInInspector] public Image duckImage;
    private DuckAnimations duckBehaviour;

    private void Awake()
    {
        // Set window title 
        titleContent = new GUIContent("Duck Helper");

        // Load icon texture (2)
        Texture2D iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DuckAI/Textures/duck.png");
        if (iconTexture == null) return;
        else titleContent = new GUIContent("Duck Helper", iconTexture);
    }


    public void CreateGUI()
    {
        var root = rootVisualElement;

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/DuckAI/Scripts/DuckHelperStyleSheet.uss");
        if (styleSheet != null)
        {
            root.styleSheets.Add(styleSheet);
        }

        #region DUCK IMAGE ANIMATION

        // DUCK IMAGE
        var animDefs = new Dictionary<string, (string, int)>
        {
            { "jump", ("Assets/Editor/DuckAI/Animations/duck_jump", 4) },
            { "wave", ("Assets/Editor/Textures/duck_wave", 6) },
            { "talk", ("Assets/Editor/Textures/duck_talk", 5) }
        };
        duckBehaviour = new DuckAnimations(DuckAnimations.GetAllAnimations());

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
            duckBehaviour.SetAnimation("jump",2);
            EditorApplication.update += AnimateDuck;
        });

        animateButton.name = "animate-duck-button";
        animateButton.AddToClassList("animate-duck-button");
        root.Add(animateButton);


        #endregion

        #region AI QUESTION INPUT AND BUTTON

        TextField questionInput = new TextField("")
        {
            multiline = true,
            value = "Type your question here..."
        };
        questionInput.name = "question-input";
        questionInput.AddToClassList("question-input");
        root.Add(questionInput);

        Button askButton = new Button(async () =>
        {
            duckBehaviour.SetAnimation("jump", 2);
            EditorApplication.update += AnimateDuck;

            string userPrompt = questionInput.value;
            string apiKey = OpenAISettings.GetSavedKey();
            string model = OpenAISettings.GetSavedModel();
            //DebugColor.Log($"Using model: {model}", "red");
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

                DebugColor.Log( content, "Yellow");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("DuckAI error: " + ex.Message);
            }
        })
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

    private void OnDisable()
    {
        EditorApplication.update -= AnimateDuck;
    }

    public void OnAskButtonPressed()
    {

    }
}

//references:
//Dispplaying an image in an EditorWindow using UIElements: (1)
//https://docs.unity3d.com/ScriptReference/EditorWindow.html
////
///https://medium.com/@dilaura_exp/unity-editor-scripting-series-chapter-3-editor-window-e0d21ddc14dc (2)
///
//https://docs.unity3d.com/6000.2/Documentation/Manual/UIE-ElementRef.html
