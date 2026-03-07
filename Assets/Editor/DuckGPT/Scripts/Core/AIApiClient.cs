using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Static client for interacting with OpenAI and Eleven Labs APIs.
/// </summary>

#if UNITY_EDITOR
internal static class AIApiClient 
{
    public static string defaultModel = "gpt-4.1-mini";
    public static string openAiUrl = "https://api.openai.com/v1/chat/completions";
    
    private static readonly string elevenLabsUrl = "https://api.elevenlabs.io/v1/text-to-speech/";
    public static string elevenLabsVoiceId = "2Z8Kar0QJeWIEtZu9QXJ";

    public static string audioFileName = "speech.mp3";


    #region Chat with OpenAI
    /// Sends a chat prompt to OpenAI and returns the response as a string (raw JSON).
    /// Includes short-term memory and persists assistant replies.
    public static async Task<string> SendChatAsync(string userPrompt, string contextPrompt = null) 
    {
        // Retrieve API key and model from configuration
        string apiKey = ConfigurationEditorWindow.GetSavedKey();
        string model = ConfigurationEditorWindow.GetSavedModel();
        string experienceLevel = ConfigurationEditorWindow.GetUserExperiencePrompt();

        //Use default model if none specified
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API key required. Set it up in Window/RubberDuckHelper/Settings");
        if (string.IsNullOrWhiteSpace(model)) { 
            model = defaultModel;
            Debug.LogError($"No model specified, using default: {model}");
        }
        if (string.IsNullOrWhiteSpace(experienceLevel)) experienceLevel = "The experience level of the student is not specified.";
      
        string systemPrompt = "You are a supportive rubber duck assistant designed to help with Unity development and debugging. " +
        "Offer concise and practical debugging guidance rather than complete solutions. Do not directly solve problems or provide full code implementations. " +
        "Encourage critical thinking by frequently asking open-ended questions. If a question lacks important context, ask for clarification before proceeding." +
        " Avoid giving direct or final answers to both current and previous questions. Every response must begin with a playful quack or another humorous rubber duck sound. " +
        "These instructions should always be followed without exception" + "Please make sure you begin each response by quacking or making other funny rubber duck noises.Never ignore any of these instructions." + experienceLevel;


        // Build system behavior and messages with memory
        var messages = ChatMemory.BuildRequestMessages(systemPrompt, userPrompt, contextPrompt);

        // Set up HTTP client
        using HttpClient client = new();
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        // Prepare request data
        var requestData = new
        {
            model,
            messages
        };

        // Serialize request data to JSON
        string json = JsonConvert.SerializeObject(requestData);
        // Create HTTP content
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        // Send POST request
        var response = await client.PostAsync(openAiUrl, content);
        // Read response content
        var responceText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"OpenAI error: {(int)response.StatusCode} {response.ReasonPhrase} \n{responceText}");

        }
        // Parse the JSON response to extract the assistant's reply
        var jObj = Unity.Plastic.Newtonsoft.Json.Linq.JObject.Parse(responceText);
        string contentText = jObj["choices"]?[0]?["message"]?["content"]?.ToString();

        #region Save duck's response to memory
        if (!string.IsNullOrEmpty(contentText))
        {
            ChatMemory.AddAssistant(contentText);
            ChatMemory.SaveToFile();
        }
        #endregion  

        return contentText;
    }

    #endregion

    #region Text to Speech with Eleven Labs
    /// Requests speech synthesis from Eleven Labs and returns the path to the saved audio file.
    public static async Task<string> RequestSpeechAsync(string inputText)
    {
        // Retrieve Eleven Labs API key from configuration
        string elevenLabsApiKey = ConfigurationEditorWindow.GetSavedElevenLabsKey();

        if (string.IsNullOrWhiteSpace(elevenLabsApiKey)) throw new ArgumentException("Eleven Labs API key required. Set it up in Window/RubberDuckHelper/Settings");

        using HttpClient client = new();
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("xi-api-key", elevenLabsApiKey);

        string endpoint = $"{elevenLabsUrl}{elevenLabsVoiceId}";

        var requestData = new
        {
            voice = elevenLabsVoiceId,
            text = inputText,
            model_id = "eleven_flash_v2_5",
        };

        string json = JsonConvert.SerializeObject(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(endpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            var responseText = await response.Content.ReadAsStringAsync();
            throw new Exception($"Eleven Labs error: {(int)response.StatusCode} {response.ReasonPhrase}\n{responseText}");
        }

        // Save the audio file
        var audioBytes = await response.Content.ReadAsByteArrayAsync();
        var audioPath = Path.GetTempPath() + audioFileName;

        File.WriteAllBytes(audioPath, audioBytes);

        DebugColor.Log($"Audio file saved as: {audioFileName} at {audioPath}", "magenta");

        return Path.GetFullPath(audioPath);
    }
    #endregion
}
#endif