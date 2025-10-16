using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AI_Duck : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Window/UI Toolkit/AI_Duck")]
    public static void ShowExample()
    {
        AI_Duck wnd = GetWindow<AI_Duck>();
        wnd.titleContent = new GUIContent("AI_Duck");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        VisualElement label = new Label("Hello World! From C#");
        root.Add(label);

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);
    }
}
