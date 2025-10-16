using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.UIElements;
public class MyEditorWindow : EditorWindow
{
    [MenuItem("Window/My Editor Window")]
    public static void DisplayDuckImg()
    {
        MyEditorWindow window = GetWindow<MyEditorWindow>();
        window.titleContent = new GUIContent("Duck");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        Texture2D duckTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/duck.png");

        if (duckTexture == null)
        {
            Debug.LogError("Duck image not found at the specified path.");
        }
        else
        {
            root.Add(new Image { image = duckTexture } );
        }
       
    }
}
//references:
//Dispplaying an image in an EditorWindow using UIElements:
//https://docs.unity3d.com/ScriptReference/EditorWindow.html
////