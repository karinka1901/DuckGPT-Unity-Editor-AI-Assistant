using UnityEditor;
using UnityEngine;

/// <summary>
/// Configuration window for setting OpenAI and Eleven Labs API keys and selecting models in Unity Editor.
/// </summary>

#if UNITY_EDITOR
public class ApiConfiguration : EditorWindow
{
    const string KeyPref = "DuckAI_OpenAIKey";
    private string apiKey;

    const string ModelPref = "DuckAI_OpenAIModels";
    private string model;

    const string ElevenLabsKeyPref = "DuckAI_ElevenLabsKey";
    private string elevenLabsApiKey;

    private static readonly string[] availableModels = new[]
    {
        "gpt-4.1-mini",
        "gpt-5",
        "o4-mini",
        "gpt-4.1"
    };

    private int selectedModelIndex;

    [MenuItem("Window/RubberDuckHelper/Settings")]
    public static void ShowWindow() => GetWindow<ApiConfiguration>("Open AI Configurations");

    void OnEnable()
    {
        apiKey = EditorPrefs.GetString(KeyPref, "");
        model = EditorPrefs.GetString(ModelPref, availableModels[0]);
        elevenLabsApiKey = EditorPrefs.GetString(ElevenLabsKeyPref, "");
    }

    void OnGUI()
    {
        #region API KEY SETTER
        GUILayout.Label("OpenAI API Key", EditorStyles.boldLabel);
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

        #region ELEVEN LABS API KEY SETTER
        GUILayout.Space(10);
        GUILayout.Label("Eleven Labs API Key", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Enter your Eleven Labs API Key for text-to-speech functionality", MessageType.Info);
        elevenLabsApiKey = EditorGUILayout.TextField("Eleven Labs API Key", elevenLabsApiKey);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Eleven Labs Key"))
        {
            EditorPrefs.SetString(ElevenLabsKeyPref, elevenLabsApiKey.Trim());
            Close();
        }
        if (GUILayout.Button("Clear Eleven Labs Key"))
        {
            EditorPrefs.DeleteKey(ElevenLabsKeyPref);
            elevenLabsApiKey = "";
        }
        GUILayout.EndHorizontal();
        #endregion
    }

    public static string GetSavedKey() => EditorPrefs.GetString(KeyPref, "");
    public static string GetSavedModel() => EditorPrefs.GetString(ModelPref, availableModels[0]);

    public static string GetSavedElevenLabsKey() => EditorPrefs.GetString(ElevenLabsKeyPref, "");
}
#endif
