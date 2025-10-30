using UnityEditor;
using UnityEngine;

/// <summary>
/// Represents a Unity Editor window for configuring OpenAI API settings, including the API key and model selection.
/// </summary>
/// <remarks>This window allows users to set and save their OpenAI API key and preferred model for use in the
/// editor. The settings are stored using Unity's <see cref="EditorPrefs"/> and are specific to the Unity Editor
/// environment.</remarks>

#if UNITY_EDITOR
public class OpenAIConfiguration : EditorWindow
{
    const string KeyPref = "DuckAI_OpenAIKey";
    private string apiKey;

    const string ModelPref = "DuckAI_OpenAIModels";
    private string model;

    private static readonly string[] availableModels = new[]
    {
        "gpt-4.1-mini",
        "gpt-5",
        "o4-mini",
        "gpt-4.1"
    };

    private int selectedModelIndex;

    [MenuItem("Window/RubberDuckHelper/Settings")]
    public static void ShowWindow() => GetWindow<OpenAIConfiguration>("Open AI Configurations");

    void OnEnable()
    {
        apiKey = EditorPrefs.GetString(KeyPref, "");
        model = EditorPrefs.GetString(ModelPref, availableModels[0]);
    }

    void OnGUI()
    {
        #region API KEY SETTER
        GUILayout.Label("OpenAI API Key (Editor only)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Enter your API Key", MessageType.Info);
        apiKey = EditorGUILayout.TextField("API Key", apiKey);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save"))
        {
            EditorPrefs.SetString(KeyPref, apiKey.Trim());
            Close();
        }
        if (GUILayout.Button("Clear"))
        {
            EditorPrefs.DeleteKey(KeyPref);
            apiKey = "";
        }
        GUILayout.EndHorizontal();
        #endregion

        #region MODEL SETTER
        GUILayout.Space(10);
        GUILayout.Label("OpenAI Model", EditorStyles.boldLabel);

        selectedModelIndex = EditorGUILayout.Popup("Model", selectedModelIndex, availableModels);
        model = availableModels[selectedModelIndex];

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Model"))
        {
            EditorPrefs.SetString(ModelPref, model);
            //Debug.Log($"Saved model: {model}");
            Close();
        }
        if (GUILayout.Button("Clear Model"))
        {
            EditorPrefs.DeleteKey(ModelPref);
            model = availableModels[0];
            selectedModelIndex = 0;
        }
        GUILayout.EndHorizontal();
        #endregion
    }

    public static string GetSavedKey() => EditorPrefs.GetString(KeyPref, "");
    public static string GetSavedModel() => EditorPrefs.GetString(ModelPref, availableModels[0]);
}
#endif
