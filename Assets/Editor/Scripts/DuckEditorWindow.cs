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
    //[HideInInspector] public Box interactableBox;
    private VisualElement interactableBox;

    private void Awake()
    {
        // Set window title 
        titleContent = new GUIContent("Duck Helper");

        // Load icon texture (2)
        Texture2D iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Textures/duck.png");
        if (iconTexture == null) return;
        else titleContent = new GUIContent("Duck Helper", iconTexture);
    }


    public void CreateGUI()
    {
        var root = rootVisualElement;

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/Scripts/DuckHelperStyleSheet.uss");
        if (styleSheet != null)
        {
            root.styleSheets.Add(styleSheet);
        }


        // DUCK IMAGE
        Texture2D duckTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Textures/duck.png");
        duckImage = new Image
        {
            image = duckTexture,
            scaleMode = ScaleMode.ScaleToFit
        };

        duckImage.name = "duck-image";
        duckImage.AddToClassList("duck-image");
        root.Add(duckImage);

        // INVISIBLE INTERACTABLE BOX 
        interactableBox = new VisualElement();
        interactableBox.name = "interactable-box";
        interactableBox.AddToClassList("interactable-box");
        root.Add(interactableBox);
    }


    private void OnGUI()
    {
        interactableBox.RegisterCallback<MouseEnterEvent>(evt =>
        {
            Debug.Log("Mouse entered duck!");
        });

        interactableBox.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            Debug.Log("Mouse left duck!");
        });

        interactableBox.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("Quack!");
        });
    }
}



//references:
//Dispplaying an image in an EditorWindow using UIElements: (1)
//https://docs.unity3d.com/ScriptReference/EditorWindow.html
////
///https://medium.com/@dilaura_exp/unity-editor-scripting-series-chapter-3-editor-window-e0d21ddc14dc (2)
///
//https://docs.unity3d.com/6000.2/Documentation/Manual/UIE-ElementRef.html
