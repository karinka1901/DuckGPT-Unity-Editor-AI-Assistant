#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager.UI;
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
    [HideInInspector] public Box interactableBox;

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
        VisualElement root = rootVisualElement;
 
        // Load duck image
        Texture2D duckTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Textures/duck.png");
        if (duckTexture == null)
        {
            Debug.LogError("Duck image not found at the specified path.");
            return;
        }
        else
        {

            duckImage = new Image { image = duckTexture };

            root.Add(duckImage);
        }

        // Create an interactable box



    }

    //private void OnGUI()
    //{

    //   interactableBox.RegisterCallback<MouseEnterEvent>(evt =>
    //    {
    //        EditorGUIUtility.AddCursorRect(new Rect(20, 20, 140, 40), MouseCursor.Pan);
    //      //  Debug.Log("Quack! Quack!");
    //    });
    //    interactableBox.RegisterCallback<MouseLeaveEvent>(evt =>
    //    {
    //     //  Debug.Log("The duck is quiet now.");
    //    });
    //}



}
//references:
//Dispplaying an image in an EditorWindow using UIElements: (1)
//https://docs.unity3d.com/ScriptReference/EditorWindow.html
////
///https://medium.com/@dilaura_exp/unity-editor-scripting-series-chapter-3-editor-window-e0d21ddc14dc (2)
///
//https://docs.unity3d.com/6000.2/Documentation/Manual/UIE-ElementRef.html
#endif